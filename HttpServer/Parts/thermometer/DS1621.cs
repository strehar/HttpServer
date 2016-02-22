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
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Feri.MS.Parts.Thermometer
{
    // DS1621 digitalni termometer na i2c vodil
    public class DS1621 : IDisposable
    {
        // Deklaracije:
        public const byte CONVERSION_DONE = 0x80;
        public const byte TEMPERATURE_HIGH_FLAG = 0x40;
        public const byte TEMPERATURE_LOW_FLAG = 0x20;
        public const byte NONVOLATILE_MEMORA_BUSY = 0x10;
        public const byte OUTPUT_POLARITY = 0x02;
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

        private I2cDevice _i2cController;
        private bool _isDisposed = false;
        private bool _conversionStarted = false;

        public bool OneShotMode { get; set; }
        private bool IsInitialized { get; set; }
        public bool HighPrecision { get; set; } = false;
        public int Address { get; set; } = 0;
        public int Config { get; set; }

        private DeviceInformationCollection FindI2cControllers()
        {
            string advancedQuerySyntaxString = I2cDevice.GetDeviceSelector();
            Task<DeviceInformationCollection> initTask = Task<DeviceInformationCollection>.Run(async () => await DeviceInformation.FindAllAsync(advancedQuerySyntaxString));
            DeviceInformationCollection controllerDeviceIds = initTask.Result;
            if (controllerDeviceIds == null || controllerDeviceIds.Count == 0)
            {
                throw new I2CControllerException();
            }
            return controllerDeviceIds;
        }

        public void Initialize()
        {
            Initialize(FindI2cControllers()[0].Id);
        }

        public void Initialize(string i2cControllerDeviceId)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The I2C controller is already initialized.");
            }

            // Adresa je 1001+A2+A1+A0
            if (Address == 0) Address = 0x48;
            I2cConnectionSettings i2cSettings = new I2cConnectionSettings(Address);
            i2cSettings.BusSpeed = I2cBusSpeed.StandardMode;

            Task<I2cDevice> controlerInitTask = Task<I2cDevice>.Run(async () => await I2cDevice.FromIdAsync(i2cControllerDeviceId, i2cSettings));
            _i2cController = controlerInitTask.Result;

            IsInitialized = true;
        }

        public byte[] ConfigRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            writeBuffer = new byte[] { ACCESS_CONFIG };
            readBuffer = new byte[1];
            _i2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        public void ConfigWrite(byte config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { ACCESS_CONFIG, config };
            _i2cController.Write(writeBuffer);


        }

        public byte[] TemperatureRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
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
                _i2cController.Write(writeBuffer);

                Task.Delay(10);
                _conversionStarted = true;
            }

            writeBuffer = new byte[] { READ_TEMPERATURE };
            readBuffer = new byte[2];
            _i2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        private byte[] TemperatureReadHighPrecision()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
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
                _i2cController.Write(writeBuffer);

                Task.Delay(10);
                _conversionStarted = true;
            }

            writeBuffer = new byte[] { READ_TEMPERATURE };
            readBuffer = new byte[2];
            _i2cController.WriteRead(writeBuffer, readBuffer);
            _temperatureRead = (double)readBuffer[0];

            writeBuffer = new byte[] { READ_COUNTER };
            readBuffer = new byte[1];
            _i2cController.WriteRead(writeBuffer, readBuffer);
            _count_remain = (double)readBuffer[0];

            writeBuffer = new byte[] { READ_SLOPE };
            readBuffer = new byte[1];
            _i2cController.WriteRead(writeBuffer, readBuffer);
            _count_per_c = (double)readBuffer[0];

            _temperature = (_temperatureRead - 0.25) + ((_count_per_c - _count_remain) / _count_per_c);

            //byte[] rezultat = BitConverter.GetBytes(_temperature);

            int whole = (int)_temperature;
            // Natančnost (v spodnji formuli *100 je na dve decimalki, *10 je na eno)
            int precision = (int)Math.Abs((_temperature - whole) * 10);

            byte[] rezultat = new byte[2] { (byte)whole, (byte)precision };

            return rezultat;
        }

        public byte[] TemperatureLowRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_LOW };
            readBuffer = new byte[2];
            _i2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        public void TemperatureLowWrite(byte[] config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_LOW, config[0], config[1] };
            _i2cController.Write(writeBuffer);

        }

        public byte[] TemperatureHighRead()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_HIGH };
            readBuffer = new byte[2];
            _i2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        public void TemperatureHighWrite(byte[] config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1621");
            }
            if (!IsInitialized)
            {
                Initialize();
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { ACCESS_TEMPERATURE_HIGH, config[0], config[1] };
            _i2cController.Write(writeBuffer);

        }

        public void Dispose()
        {
            if (_i2cController != null)
            {
                _i2cController.Dispose();
                _i2cController = null;
            }
            _isDisposed = true;
            //GC.SuppressFinalize(this);
        }
    }
}
