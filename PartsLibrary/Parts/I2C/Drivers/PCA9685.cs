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

namespace Feri.MS.Parts.I2C.Drivers
{
    class PCA9685 : IDisposable
    {
        private bool IsInitialized { get; set; }
        public int Address { get; set; } = 0;

        private I2cDevice _i2cController;

        private DeviceInformationCollection FindI2cControllers()
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

        public void Initialize()
        {
            Initialize(FindI2cControllers()[0].Id);
            throw new NotImplementedException();
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

            Task<I2cDevice> controlerInitTask = Task.Run(async () => await I2cDevice.FromIdAsync(i2cControllerDeviceId, i2cSettings));
            _i2cController = controlerInitTask.Result;

            IsInitialized = true;
        }

        #region IDisposable Support
        public void Dispose()
        {
            //GC.SuppressFinalize(this);
        }
        #endregion
    }
}
