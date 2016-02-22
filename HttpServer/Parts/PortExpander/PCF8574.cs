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
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Feri.MS.Parts.PortExpander
{
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

    public class PCF8574 : IDisposable
    {
        private I2cDevice _i2cController;
        private bool _isDisposed = false;

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

            // Adresa je PCF8574: 0100+A2+A1+A0  PCF8574A: 0111+A2+A1+A0
            if (Address == 0) Address = 0x38;
            I2cConnectionSettings i2cSettings = new I2cConnectionSettings(Address);
            i2cSettings.BusSpeed = I2cBusSpeed.StandardMode;

            Task<I2cDevice> controlerInitTask = Task<I2cDevice>.Run(async () => await I2cDevice.FromIdAsync(i2cControllerDeviceId, i2cSettings));
            _i2cController = controlerInitTask.Result;

            IsInitialized = true;
        }

        public void Write(byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }
            if (!IsInitialized)
            {
                Initialize();
            }
            byte[] writeBuffer;

            writeBuffer = new byte[] { data };
            _i2cController.Write(writeBuffer);
        }

        public byte Read()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }
            if (!IsInitialized)
            {
                Initialize();
            }
            byte[] readBuffer;

            readBuffer = new byte[1];
            _i2cController.Read(readBuffer);

            return readBuffer[0];
        }

        public void WritePin(PortNumber pin, bool data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }
            if (!IsInitialized)
            {
                Initialize();
            }
            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            _i2cController.Read(readBuffer);

            writeBuffer = new byte[1];

            BitArray bits = new BitArray(readBuffer);

            bits[(int)pin] = data;

            ((ICollection)bits).CopyTo(writeBuffer, 0);

            _i2cController.Write(writeBuffer);

            Debug.WriteLine("Pin " + pin + " na " + writeBuffer[0]);

        }

        public bool ReadPin(PortNumber pin)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PCF8574");
            }
            if (!IsInitialized)
            {
                Initialize();
            }
            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            _i2cController.Read(readBuffer);

            writeBuffer = new byte[1];
            readBuffer.CopyTo(writeBuffer, 0);

            BitArray bits = new BitArray(readBuffer);

            return bits[(int)pin];
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
