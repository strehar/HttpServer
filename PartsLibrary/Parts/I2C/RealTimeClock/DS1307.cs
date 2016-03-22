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

using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Feri.MS.Parts.Exceptions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Feri.MS.Parts.I2C.RealTimeClock
{
    internal class DS1307Helper
    {
        internal I2cDevice I2cController { get; set; }
        internal DS1307 Part { get; set; }
        internal int Address { get; set; }
        internal int ReferenceCount { get; set; } = 0;
    }
    /// <summary>
    /// 
    /// </summary>
    public class DS1307 : IDisposable
    {
        private static Dictionary<int, DS1307Helper> _initialized { get; set; } = new Dictionary<int, DS1307Helper>();
        public I2cDevice I2cController { get; private set; }
        private bool _isDisposed = false;
        public bool _debug = false;

        private bool IsInitialized { get; set; }
        public int Address { get; private set; } = 0;

        private DS1307(int address)
        {
            Address = address;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="i2cControllerDeviceId"></param>
        /// <returns></returns>
        public static DS1307 Create(int address = 0x68, string i2cControllerDeviceId = null)
        {
            // Adresa je 1101000
            DS1307 _part;
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

                _part = new DS1307(address);
                DS1307Helper helper = new DS1307Helper();
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

        private int BcdToDec(byte value)
        {
            return ((value / 16 * 10) + (value % 16));
        }

        private byte DecToBcd(int value)
        {
            return (byte)((value / 10 * 16) + (value % 10));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Ready()
        {
            try
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException("DS1307");
                }

                byte[] readBuffer;
                byte[] writeBuffer;

                readBuffer = new byte[1];
                writeBuffer = new byte[1];
                writeBuffer[0] = 0x00;

                _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

                BitArray _seconds = new BitArray(readBuffer);

                return !_seconds[7];
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enable"></param>
        public void Enable(bool enable)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0x00;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            if (enable)
            {
                BitArray _seconds = new BitArray(readBuffer);
                _seconds[7] = false;
                ((ICollection)_seconds).CopyTo(writeBuffer, 0);
            }
            else {
                BitArray _seconds = new BitArray(readBuffer);
                _seconds[7] = true;
                ((ICollection)_seconds).CopyTo(writeBuffer, 0);
            }

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DateTime GetTime()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[7];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0x00;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return new DateTime(BcdToDec(readBuffer[6]) + 2000, BcdToDec(readBuffer[5]), BcdToDec(readBuffer[4]), BcdToDec((byte)(readBuffer[2] & 0x3f)), BcdToDec(readBuffer[1]), BcdToDec((byte)(readBuffer[0] & 0x7f)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public void SetTime(DateTime time)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { 0x00, DecToBcd(time.Second), DecToBcd(time.Minute), DecToBcd(time.Hour), DecToBcd((int)time.DayOfWeek), DecToBcd(time.Day), DecToBcd(time.Month), DecToBcd(time.Year - 2000) };

            _initialized[Address].I2cController.Write(writeBuffer);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte GetConfig()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0x00;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void SetConfig(byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[2];
            writeBuffer[0] = 0x00;
            writeBuffer[1] = data;

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] GetRam()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[56];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0x08;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void SetRam(byte[] data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[57];
            writeBuffer[0] = 0x08;

            for (int i = 0; i < data.Length; i++)
            {
                writeBuffer[i + 1] = data[i];
            }

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public byte ReadAddress(byte address)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }
            if ((address < 0) || (address > 64))
            {
                throw new ArgumentException("Address is not valid.");
            }

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = address;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public void WriteAddress(byte address, byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("DS1307");
            }
            if ((address < 0) || (address > 64))
            {
                throw new ArgumentException("Address is not valid.");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[2];
            writeBuffer[0] = address;
            writeBuffer[1] = data;

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        #region IDisposable Support
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
            //GC.SuppressFinalize(this);
        }
        #endregion
    }
}
