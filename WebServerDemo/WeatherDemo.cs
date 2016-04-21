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

using Feri.MS.Http;
using Feri.MS.Http.Template;
using Feri.MS.Parts.I2C.MultiSensor;
using Feri.MS.Parts.I2C.Thermometer;
using System;
using System.Diagnostics;
using System.Text;

namespace WebServerDemo
{
    /// <summary>
    /// Demo of reading temperature from DS1621 connected to i2c bus. It uses High precision mode of the DS1621.
    /// </summary>
    class WeatherDemo : IDisposable
    {
        HttpServer _ws;
        string _privatePath = "AppHtml";

        DS1621 _termometer = DS1621.Create();

        SimpleTemplate _temperatureTemplate = new SimpleTemplate();


        public void Start(HttpServer server)
        {
            _ws = server;

            _ws.AddPath("/demoWeather.html", ProcessWeather);

            _temperatureTemplate.LoadString(_ws.HttpRootManager.ReadToByte(_privatePath + "/templateWeather.html"));
            _temperatureTemplate["temperature"] = new TemplateAction() { Pattern = "TEMP" };
            _temperatureTemplate["humidity"] = new TemplateAction() { Pattern = "HUMIDITY" };
            _temperatureTemplate["pressure"] = new TemplateAction() { Pattern = "PRESSURE" };

            _termometer.HighPrecision = true;
        }

        private void ProcessWeather(HttpRequest request, HttpResponse response)
        {
            try
            {
                BME280Data data = BME280.Create().Read();

                //Debug.WriteLine("Temperatura: " + data.Temperature + " C.");
                //Debug.WriteLine("Pritisk: " + data.Pressure / 100 + " hpa.");
                //Debug.WriteLine("Vlažnost: " + data.Humidity + " %.");

                _temperatureTemplate["temperature"].Data = data.Temperature.ToString();
                _temperatureTemplate["humidity"].Data = data.Humidity.ToString();
                _temperatureTemplate["pressure"].Data = (data.Pressure/100).ToString();

                _temperatureTemplate.ProcessAction();
                response.Write(_temperatureTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/templateWeather.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                response.Write(e);
            }
        }

        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            _ws.RemovePath("/demoTemperature.html");
            _termometer.Dispose();
        }
        #endregion
    }
}
