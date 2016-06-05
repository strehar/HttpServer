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
using System.Diagnostics;
using System.Collections.Generic;

namespace Feri.MS.Parts.I2C.Experimental
{
    internal class OV7670Helper
    {
        internal I2cDevice I2cController { get; set; }
        internal OV7670 Part { get; set; }
        internal int Address { get; set; }
        internal int ReferenceCount { get; set; } = 0;
    }
    class OV7670 : IDisposable
    {
        public bool _debug = false;

        private bool IsInitialized { get; set; }

        public I2cDevice I2cController { get; private set; }
        private bool _isDisposed = false;
        private static Dictionary<int, OV7670Helper> _initialized { get; set; } = new Dictionary<int, OV7670Helper>();
        public int Address { get; set; } = 0;

        private OV7670(int address)
        {
            Address = address;
        }

        public static OV7670 Create(int address = 0x48, string i2cControllerDeviceId = null)
        {
            // Adresa je 1001+A2+A1+A0
            OV7670 _part;
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

                _part = new OV7670(address);
                OV7670Helper helper = new OV7670Helper();
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
        }
        #endregion
    }
}
