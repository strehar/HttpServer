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
using System.Collections;

namespace Feri.MS.Parts.I2C.MultiSensor
{
    public class BME280Data
    {
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double Humidity { get; set; }
    }
    public enum BME280Mode
    {
        SLEEP,
        ONESHOT,
        NORMAL
    };
    internal class BME280Helper
    {
        internal I2cDevice I2cController { get; set; }
        internal BME280 Part { get; set; }
        internal int Address { get; set; }
        internal int ReferenceCount { get; set; } = 0;
    }
    internal class BME280Calibration
    {
        public uint dig_T1 { get; set; }
        public int dig_T2 { get; set; }
        public int dig_T3 { get; set; }

        public uint dig_P1 { get; set; }
        public int dig_P2 { get; set; }
        public int dig_P3 { get; set; }
        public int dig_P4 { get; set; }
        public int dig_P5 { get; set; }
        public int dig_P6 { get; set; }
        public int dig_P7 { get; set; }
        public int dig_P8 { get; set; }
        public int dig_P9 { get; set; }

        public byte dig_H1 { get; set; }
        public int dig_H2 { get; set; }
        public byte dig_H3 { get; set; }
        public int dig_H4 { get; set; }
        public int dig_H5 { get; set; }
        public int dig_H6 { get; set; }
    }
    public class BME280 : IDisposable
    {
        private bool _isDisposed = false;
        private double t_fine;
        private BME280Calibration Calibration = new BME280Calibration();
        private BME280Mode mode = BME280Mode.NORMAL;
        public bool _debug = false;

        private static Dictionary<int, BME280Helper> _initialized { get; set; } = new Dictionary<int, BME280Helper>();
        public I2cDevice I2cController { get; private set; }
        private bool IsInitialized { get; set; }
        public int Address { get; private set; } = 0;
        public int CompensateTemperature { get; set; } = 0;
        public int CompensateHumidity { get; set; } = 0;
        public int CompensatePressure { get; set; } = -111;

        private BME280(int address)
        {
            Address = address;
        }

        public static BME280 Create(int address = 0x76, string i2cControllerDeviceId = null)
        {
            // Adresa je 1101000
            BME280 _part;
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

                _part = new BME280(address);

                BME280Helper helper = new BME280Helper();
                helper.Address = address;
                helper.Part = _part;
                helper.I2cController = _i2cController;

                _initialized.Add(address, helper);

                _part.Init();
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

        private void Init()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }
            Reset();
            Task.Delay(100);
            ReadCalibration();
            SetConfig();
            SetCtrlHum();
            SetCtrlMeas();
        }

        public void Reset()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[2];
            writeBuffer[0] = 0xE0;
            writeBuffer[1] = 0xB6;

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        public void SetMode(BME280Mode operationMode = BME280Mode.NORMAL)
        {
            byte[] _data = new byte[1] { GetCtrlMeas() };
            BitArray bitData;

            switch (mode)
            {
                case BME280Mode.NORMAL:
                    mode = operationMode;
                    bitData = new BitArray(_data[0]);
                    bitData[0] = true;
                    bitData[1] = true;
                    ((ICollection)bitData).CopyTo(_data, 0);
                    break;

                case BME280Mode.ONESHOT:
                    mode = operationMode;
                    bitData = new BitArray(_data[0]);
                    bitData[0] = true;
                    bitData[1] = false;
                    ((ICollection)bitData).CopyTo(_data, 0);
                    break;

                case BME280Mode.SLEEP:
                    mode = operationMode;
                    bitData = new BitArray(_data[0]);
                    bitData[0] = false;
                    bitData[1] = false;
                    ((ICollection)bitData).CopyTo(_data, 0);
                    break;
            }

            SetCtrlMeas(_data[0]);
        }

        private void SetConfig()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;

            BitArray bits = new BitArray(8);

            bits[0] = false;
            bits[2] = true;
            bits[3] = true;
            bits[4] = false;
            bits[5] = true;
            bits[6] = true;
            bits[7] = false;

            writeBuffer = new byte[1];

            ((ICollection)bits).CopyTo(writeBuffer, 0);

            SetConfig(writeBuffer[0]);
        }

        public void SetConfig(byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[2];
            writeBuffer[0] = 0xF5;
            writeBuffer[1] = data;

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        public byte GetConfig()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0xF5;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer[0];
        }

        private void SetCtrlHum()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;

            BitArray bits = new BitArray(8);

            bits[0] = true;
            bits[1] = false;
            bits[2] = true;

            writeBuffer = new byte[1];

            ((ICollection)bits).CopyTo(writeBuffer, 0);

            SetCtrlHum(writeBuffer[0]);
        }

        public void SetCtrlHum(byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0xF2;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            BitArray bits = new BitArray(readBuffer);
            writeBuffer[0] = data;
            BitArray dataBits = new BitArray(writeBuffer);

            bits[0] = dataBits[0];
            bits[1] = dataBits[1];
            bits[2] = dataBits[2];

            writeBuffer = new byte[2];
            writeBuffer[0] = 0xF2;

            ((ICollection)bits).CopyTo(writeBuffer, 1);

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        public byte GetCtrlHum()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0xF2;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer[0];
        }

        private void SetCtrlMeas()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;

            BitArray bits = new BitArray(8);

            bits[0] = true;
            bits[1] = true;
            bits[2] = true;
            bits[3] = false;
            bits[4] = true;
            bits[5] = true;
            bits[6] = false;
            bits[7] = true;

            writeBuffer = new byte[1];

            ((ICollection)bits).CopyTo(writeBuffer, 0);

            SetCtrlMeas(writeBuffer[0]);
        }

        public void SetCtrlMeas(byte data)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;

            writeBuffer = new byte[2];
            writeBuffer[0] = 0xF4;
            writeBuffer[1] = data;

            _initialized[Address].I2cController.Write(writeBuffer);
        }

        public byte GetCtrlMeas()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[1];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0xF4;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            return readBuffer[0];
        }

        public BME280Data Read()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            if (mode == BME280Mode.ONESHOT)
            {
                byte[] _data = new byte[1] { GetCtrlMeas() };

                BitArray bitData = new BitArray(_data[0]);

                bitData[0] = true;
                bitData[1] = false;

                ((ICollection)bitData).CopyTo(_data, 0);

                SetCtrlMeas(_data[0]);
            }

            int t_temperature;
            int t_humidity;
            int t_pressure;

            BME280Data data = new BME280Data();

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[8];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0xF7;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            t_pressure = (readBuffer[0] << 12) + (readBuffer[1] << 4) + (readBuffer[2] >> 4);
            t_temperature = (readBuffer[3] << 12) + (readBuffer[4] << 4) + (readBuffer[5] >> 4);
            t_humidity = (readBuffer[6] << 8) + readBuffer[7];

            data.Temperature = CalculateTemperature(t_temperature);
            data.Pressure = CalculatePressure(t_pressure);
            data.Humidity = CalculateHumidity(t_humidity);

            return data;
        }

        private void ReadCalibration()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("BME280");
            }

            byte[] writeBuffer;
            byte[] readBuffer;

            readBuffer = new byte[26];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0x88;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            // Manual, page 22
            Calibration.dig_T1 = (uint)(readBuffer[0] + (readBuffer[1] << 8));
            Calibration.dig_T2 = readBuffer[2] + (readBuffer[3] << 8);
            Calibration.dig_T3 = readBuffer[4] + (readBuffer[5] << 8);
            Calibration.dig_P1 = (uint)(readBuffer[6] + (readBuffer[7] << 8));
            Calibration.dig_P2 = readBuffer[8] + (readBuffer[9] << 8);
            Calibration.dig_P3 = readBuffer[10] + (readBuffer[11] << 8);
            Calibration.dig_P4 = readBuffer[12] + (readBuffer[13] << 8);
            Calibration.dig_P5 = readBuffer[14] + (readBuffer[15] << 8);
            Calibration.dig_P6 = readBuffer[16] + (readBuffer[17] << 8);
            Calibration.dig_P7 = readBuffer[18] + (readBuffer[19] << 8);
            Calibration.dig_P8 = readBuffer[20] + (readBuffer[21] << 8);
            Calibration.dig_P9 = readBuffer[22] + (readBuffer[23] << 8);
            Calibration.dig_H1 = readBuffer[25];

            readBuffer = new byte[7];
            writeBuffer = new byte[1];
            writeBuffer[0] = 0xE1;

            _initialized[Address].I2cController.WriteRead(writeBuffer, readBuffer);

            // Manual, page 22, 23
            Calibration.dig_H2 = readBuffer[0] + (readBuffer[1] << 8);
            Calibration.dig_H3 = readBuffer[2];
            Calibration.dig_H4 = (readBuffer[3] << 4) + (readBuffer[4] & 0xF);
            Calibration.dig_H5 = (readBuffer[4] >> 4) + (readBuffer[5] << 4);
            Calibration.dig_H6 = readBuffer[6];
        }

        private double CalculateTemperature(int temperature)
        {
            //Manual page 49
            double var1;
            double var2;
            double T;

            var1 = (temperature / 16384.0 - Calibration.dig_T1 / 1024.0) * Calibration.dig_T2;
            var2 = (temperature / 131072.0 - Calibration.dig_T1 / 8192.0) * (temperature / 131072.0 - Calibration.dig_T1 / 8192.0) * Calibration.dig_T3;
            t_fine = (var1 + var2);
            t_fine = t_fine + (CompensateTemperature * 5120);  // Sesnor seems to have offset of 2 C.
            T = (t_fine) / 5120.0;
            return Math.Round(T, 2, MidpointRounding.AwayFromZero);
        }

        private double CalculateHumidity(int humidity)
        {
            //Manual page 49
            double var_H;
            var_H = t_fine - 76800.0;
            var_H = (humidity - ((Calibration.dig_H4) * 64.0 + (Calibration.dig_H5) / 16384.0 * var_H)) * ((Calibration.dig_H2) / 65536.0 * (1.0 + (Calibration.dig_H6) / 67108864.0 * var_H * (1.0 + (Calibration.dig_H3) / 67108864.0 * var_H)));
            var_H = var_H * (1.0 - (Calibration.dig_H1) * var_H / 524288.0);
            var_H = var_H + CompensateHumidity;  // compensate sendor
            if (var_H > 100.0)
                var_H = 100.0;
            else if (var_H < 0.0)
                var_H = 0.0;
            return Math.Round(var_H, 2, MidpointRounding.AwayFromZero);
        }

        private double CalculatePressure(int pressure)
        {
            //Manual page 49
            double var1, var2, p;
            var1 = t_fine / 2.0 - 64000.0;
            var2 = var1 * var1 * (Calibration.dig_P6) / 32768.0;
            var2 = var2 + var1 * (Calibration.dig_P5) * 2.0;
            var2 = (var2 / 4.0) + ((Calibration.dig_P4) * 65536.0);
            var1 = ((Calibration.dig_P3) * var1 * var1 / 524288.0 + (Calibration.dig_P2) * var1) / 524288.0;
            var1 = (1.0 + var1 / 32768.0) * (Calibration.dig_P1);
            if (var1 == 0.0)
            {
                return 0; // avoid exception caused by division by zero
            }
            p = 1048576.0 - pressure;
            p = (p - (var2 / 4096.0)) * 6250.0 / var1;
            var1 = (Calibration.dig_P9) * p * p / 2147483648.0;
            var2 = p * (Calibration.dig_P8) / 32768.0;
            p = p + (var1 + var2 + (Calibration.dig_P7)) / 16.0;
            p = p + (CompensatePressure * 100); // Compensate sensor in hpa
            return Math.Round(p, 1, MidpointRounding.AwayFromZero);
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
