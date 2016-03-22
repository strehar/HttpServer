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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Feri.MS.Parts.I2C.EEPROM
{
    internal class AT24C32Helper
    {
        internal I2cDevice I2cController { get; set; }
        internal AT24C32 Part { get; set; }
        internal int Address { get; set; }
        internal int ReferenceCount { get; set; } = 0;
    }
    class AT24C32 : IDisposable
    {
        private static Dictionary<int, AT24C32Helper> _initialized { get; set; } = new Dictionary<int, AT24C32Helper>();
        public I2cDevice I2cController { get; private set; }
        private bool _isDisposed = false;
        public bool _debug = false;

        private bool IsInitialized { get; set; }
        public int Address { get; private set; } = 0;

        private AT24C32(int address)
        {
            Address = address;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="i2cControllerDeviceId"></param>
        /// <returns></returns>
        public static AT24C32 Create(int address = 0x50, string i2cControllerDeviceId = null)
        {
            // Adresa je 1010 000
            AT24C32 _part;
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

                _part = new AT24C32(address);
                AT24C32Helper helper = new AT24C32Helper();
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
        /// <param name="address"></param>
        /// <returns></returns>
        public byte ReadAddress(int address)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("AT24C32");
            }

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[2];
            byte[] _tmp = BitConverter.GetBytes(address);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_tmp);
            }
            writeBuffer[0] = _tmp[0];
            writeBuffer[1] = _tmp[1];

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public void WriteAddress(int address, byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("AT24C32");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[3];
            byte[] _tmp = BitConverter.GetBytes(address);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_tmp);
            }
            writeBuffer[0] = _tmp[0];
            writeBuffer[1] = _tmp[1];
            writeBuffer[3] = data;

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public byte[] ReadPage(int address, int pageSize)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("AT24C32");
            }
            if ((address > 4096) || (pageSize < 1) || (pageSize > 4096) || (pageSize + address > 4096))
                throw new ArgumentException("Invalid argument");

            byte[] readBuffer;
            byte[] writeBuffer;

            readBuffer = new byte[pageSize];
            writeBuffer = new byte[2];
            byte[] _tmp = BitConverter.GetBytes(address);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_tmp);
            }
            writeBuffer[0] = _tmp[0];
            writeBuffer[1] = _tmp[1];

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public void WritePage(int address, byte[] data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("AT24C32");
            }
            if ((address > 4096) || (address + data.Length > 4096))
                throw new ArgumentException("Invalid argument");

            byte[] writeBuffer;

            writeBuffer = new byte[data.Length+2];
            byte[] _tmp = BitConverter.GetBytes(address);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_tmp);
            }
            writeBuffer[0] = _tmp[0];
            writeBuffer[1] = _tmp[1];

            for (int i = 0; i < data.Length; i++)
                writeBuffer[i + 2] = data[i];

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
