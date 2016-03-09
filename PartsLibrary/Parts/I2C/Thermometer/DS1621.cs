#region Licence
/*
   Copyright 2016 Miha Strehar

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
#endregion

using Feri.MS.Parts.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Feri.MS.Parts.I2C.Thermometer
{
    internal class DS1621Helper
    {
        internal I2cDevice I2cController { get; set; }
        internal DS1621 Part { get; set; }
        internal int Address { get; set; }
        internal int ReferenceCount { get; set; } = 0;
    }
    // DS1621 digitalni termometer na i2c vodilu
    /// <summary>
    /// 
    /// </summary>
    public class DS1621 : IDisposable
    {
        // Deklaracije:
        /// <summary>
        /// 
        /// </summary>
        public const byte CONVERSION_DONE = 0x80;
        /// <summary>
        /// 
        /// </summary>
        public const byte TEMPERATURE_HIGH_FLAG = 0x40;
        /// <summary>
        /// 
        /// </summary>
        public const byte TEMPERATURE_LOW_FLAG = 0x20;
        /// <summary>
        /// 
        /// </summary>
        public const byte NONVOLATILE_MEMORA_BUSY = 0x10;
        /// <summary>
        /// 
        /// </summary>
        public const byte OUTPUT_POLARITY = 0x02;
        /// <summary>
        /// 
        /// </summary>
        public const byte ONESHOT_MODE = 0x01;

        // COMMANDS
        private const byte READ_TEMPERATURE = 0xAA;          // Vrne 2 Byte-a
        private const byte READ_COUNTER = 0xA8;              // Vrne 1 Byte
        private const byte READ_SLOPE = 0xA9;                // Vrne 1 Byte
        private const byte START_CONVERT_TEMPERATURE = 0xEE; // Ne vrne nič
        private const byte STOP_CONVERT_TEMPERATURE = 0x22;  // Ne vrne nič
        private const byte ACCESS_TEMPERATURE_HIGH = 0xA1;   // VRNE ali SPREJME 2 Byte-a
        private const byte ACCESS_TEMPERATURE_LOW = 0xA2;    // VRNE ali SPREJME 2 Byte-a
        private const byte ACCESS_CONFIG = 0xAC;             // VRNE ali SPREJME 1 Byte

        private static Dictionary<int, DS1621Helper> _initialized { get; set; } = new Dictionary<int, DS1621Helper>();
        /// <summary>
        /// 
        /// </summary>
        public I2cDevice I2cController { get; private set; }
        private bool _isDisposed = false;
        private bool _conversionStarted = false;
        /// <summary>
        /// 
        /// </summary>
        public bool _debug = false;

        private bool IsInitialized { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool OneShotMode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool HighPrecision { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public int Address { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public int Config { get; set; }

        private DS1621(int address)
        {
            Address = address;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="i2cControllerDeviceId"></param>
        /// <returns></returns>
        public static DS1621 Create(int address = 0x48, string i2cControllerDeviceId = null)
        {
            // Adresa je 1001+A2+A1+A0
            DS1621 _part;
            //check if address exists
            if (!_initialized.ContainsKey(address))
            {
                // there is none. DoMagic();
                if (string.IsNullOrEmpty(i2cControllerDeviceId))
                {
                    i2cControllerDeviceId = FindI2cControllers()[0].Id;
                }

                I2cConnectionSettings i2cSettings = new I2cConnectionSettings(address);
                i2cSettings.BusSpeed = I2cBusSpeed.StandardMode;

                Task<I2cDevice> controlerInitTask = Task.Run(async () => await I2cDevice.FromIdAsync(i2cControllerDeviceId, i2cSettings));
                I2cDevice _i2cController = controlerInitTask.Result;

                _part = new DS1621(address);
                DS1621Helper helper = new DS1621Helper();
                helper.Address = address;
                helper.Part = _part;
                helper.I2cController = _i2cController;

                _initialized.Add(address, helper);
            }
            else
            {
                _part = _initialized[address].Part;
            }
            _initialized[address].ReferenceCount++;
            return _part;
        }

        private static DeviceInformationCollection FindI2cControllers()
        {
            string advancedQuerySyntaxString = I2cDevice.GetDeviceSelector();
            Task<DeviceInformationCollection> initTask = Task.Run(async () => await DeviceInformation.FindAllAsync(advancedQuerySyntaxString));
            DeviceInformationCollection controllerDeviceIds = initTask.Result;
            if (controllerDeviceIds == null || controllerDeviceIds.Count == 0)
            {
                throw new I2CControllerException();
            }
            return controllerDeviceIds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ConfigRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            writeBuffer = new byte[] { ACCESS_CONFIG };
            readBuffer = new byte[1];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public void ConfigWrite(byte config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { ACCESS_CONFIG, config };
            _initialized[Address].I2cController.Write(writeBuffer);


        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] TemperatureRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            if (HighPrecision)
            {
                return TemperatureReadHighPrecision();
            }


            byte[] writeBuffer;
            byte[] readBuffer;

            if ((!_conversionStarted) || (OneShotMode))
            {
                writeBuffer = new byte[] { START_CONVERT_TEMPERATURE };
                _initialized[Address].I2cController.Write(writeBuffer);

                Task.Delay(10);
                _conversionStarted = true;
            }

            writeBuffer = new byte[] { READ_TEMPERATURE };
            readBuffer = new byte[2];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private byte[] TemperatureReadHighPrecision()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;
            byte[] readBuffer;
            double _temperature;
            double _temperatureRead;
            double _count_remain;
            double _count_per_c;

            if ((!_conversionStarted) || (OneShotMode))
            {
                writeBuffer = new byte[] { START_CONVERT_TEMPERATURE };
                _initialized[Address].I2cController.Write(writeBuffer);

                Task.Delay(10);
                _conversionStarted = true;
            }

            writeBuffer = new byte[] { READ_TEMPERATURE };
            readBuffer = new byte[2];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);
            _temperatureRead = (double)readBuffer[0];

            writeBuffer = new byte[] { READ_COUNTER };
            readBuffer = new byte[1];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);
            _count_remain = (double)readBuffer[0];

            writeBuffer = new byte[] { READ_SLOPE };
            readBuffer = new byte[1];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);
            _count_per_c = (double)readBuffer[0];

            _temperature = (_temperatureRead - 0.25) + ((_count_per_c - _count_remain) / _count_per_c);

            int whole = (int)_temperature;
            // Natančnost (v spodnji formuli *100 je na dve decimalki, *10 je na eno)
            int precision = (int)Math.Abs((_temperature - whole) * 10);

            byte[] rezultat = new byte[2] { (byte)whole, (byte)precision };

            return rezultat;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] TemperatureLowRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_LOW };
            readBuffer = new byte[2];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public void TemperatureLowWrite(byte[] config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_LOW, config[0], config[1] };
            _initialized[Address].I2cController.Write(writeBuffer);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] TemperatureHighRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_HIGH };
            readBuffer = new byte[2];
            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public void TemperatureHighWrite(byte[] config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_HIGH, config[0], config[1] };
            _initialized[Address].I2cController.Write(writeBuffer);

        }

        #region IDisposable Support
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // Clean up. If there is reference to the key, and there are more then one, reduce reference. if it's the last one, remove from the static directory of parts.
            if (_initialized.ContainsKey(Address))
            {
                if (_initialized[Address].ReferenceCount > 1)
                {
                    _initialized[Address].ReferenceCount--;
                }
                else
                {
                    _initialized[Address].I2cController.Dispose();
                    _initialized.Remove(Address);
                }
            }
            _isDisposed = true;
            Debug.WriteLineIf(_debug, "Disposing of part with address: " + Address);
        }
        #endregion
    }
}
