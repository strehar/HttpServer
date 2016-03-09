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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Feri.MS.Parts.I2C.PortExpander
{
    /// <summary>
    /// 
    /// </summary>
    public enum PortNumber
    {
        PORT_ONE,
        PORT_TWO,
        PORT_THREE,
        PORT_FOUR,
        PORT_FIVE,
        PORT_SIX,
        PORT_SEVEN,
        PORT_EIGHT
    };
    internal class PCF8574Helper
    {
        internal I2cDevice I2cController { get; set; }
        internal PCF8574 Part { get; set; }
        internal int Address { get; set; }
        internal int ReferenceCount { get; set; } = 0;
    }
    /// <summary>
    /// 
    /// </summary>
    public class PCF8574 : IDisposable
    {
        private static Dictionary<int, PCF8574Helper> _initialized { get; set; } = new Dictionary<int, PCF8574Helper>();
        public I2cDevice I2cController { get; private set; }
        private bool _isDisposed = false;
        public bool _debug = false;

        private bool IsInitialized { get; set; }
        public int Address { get; private set; } = 0;

        private PCF8574(int address)
        {
            Address = address;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="i2cControllerDeviceId"></param>
        /// <returns></returns>
        public static PCF8574 Create(int address = 0x38, string i2cControllerDeviceId = null)
        {
            // Adresa je PCF8574: 0100+A2+A1+A0  PCF8574A: 0111+A2+A1+A0
            PCF8574 _part;
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

                _part = new PCF8574(address);
                PCF8574Helper helper = new PCF8574Helper();
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
        /// <param name="data"></param>
        public void Write(byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[] { data };
            _initialized[Address].I2cController.Write(writeBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte Read()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }

            byte[] readBuffer;

            readBuffer = new byte[1];
            _initialized[Address].I2cController.Read(readBuffer);

            return readBuffer[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="data"></param>
        public void WritePin(PortNumber pin, bool data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            _initialized[Address].I2cController.Read(readBuffer);

            writeBuffer = new byte[1];

            BitArray bits = new BitArray(readBuffer);

            bits[(int)pin] = data;

            ((ICollection)bits).CopyTo(writeBuffer, 0);

            _initialized[Address].I2cController.Write(writeBuffer);

            Debug.WriteLineIf(_debug, "Pin " + pin + " na " + writeBuffer[0]);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        public bool ReadPin(PortNumber pin)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            _initialized[Address].I2cController.Read(readBuffer);

            writeBuffer = new byte[1];
            readBuffer.CopyTo(writeBuffer, 0);

            BitArray bits = new BitArray(readBuffer);

            return bits[(int)pin];
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
