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
using Feri.MS.Http.Json;
using Feri.MS.Http.Template;
using Feri.MS.Parts.I2C.PortExpander;
using System;
using System.Diagnostics;

namespace WebServerDemo
{
    /// <summary>
    /// Demo of using LED diode connected to the i2c port expander. It updates data in JSON and TEMPLATE objects recived from WebServerDemo class (needed for JSON and SimpleTemplate demo)
    /// </summary>
    class LEDDemo : IDisposable
    {
        HttpServer _ws;
        string _privatePath = "AppHtml";

        SimpleTemplate _LEDControl = new SimpleTemplate();
        PCF8574 _ports = PCF8574.Create();

        SimpleJsonListener _json;
        SimpleTemplate _templateDemo;

        string stateLed = "Unspecified";
        bool _debug = false;

        public void Start(HttpServer server, SimpleJsonListener json, SimpleTemplate templateDemo)
        {
            _ws = server;
            _json = json;
            _templateDemo = templateDemo;

            _ws.AddPath("/demoLED.html", ProcessDemoLED);

            _LEDControl.LoadString(_ws.HttpRootManager.ReadToByte(_privatePath + "/templateLED.html"));
            _LEDControl["ledOn"] = new TemplateAction() { Pattern = "LEDON" };
            _LEDControl["ledOff"] = new TemplateAction() { Pattern = "LEDOFF" };

            _templateDemo["maualLed"] = new TemplateAction() { Pattern = "MANUALLED", Data = "Off" };

            _ws.HttpRootManager.AddExtensionTemplateData("shtml", "manualLed", new TemplateAction() { Pattern = "MANUALLED", Data = "Off" });

            _json.AddData("MaualLed", "Off");

            //_ports._debug = true;
        }

        private void ProcessDemoLED(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.Parameters.ContainsKey("state"))
                {
                    if (request.Parameters["state"].Equals("On", StringComparison.OrdinalIgnoreCase))
                    {
                        stateLed = "On";
                        _json.UpdateData("MaualLed", "On");
                        _templateDemo["maualLed"].Data = "On";
                        _ws.HttpRootManager.UpdateExtensionTemplateData("shtml", "manualLed", new TemplateAction() { Pattern = "MANUALLED", Data = "On" });
                        //pin2.Write(GpioPinValue.High);
                        _ports.WritePin(PortNumber.PORT_TWO, true);   // Uncomment for sensors
                    }
                    else if (request.Parameters["state"].Equals("Off", StringComparison.OrdinalIgnoreCase))
                    {
                        stateLed = "Off";
                        _json.UpdateData("MaualLed", "Off");
                        _templateDemo["maualLed"].Data = "Off";
                        _ws.HttpRootManager.UpdateExtensionTemplateData("shtml", "manualLed", new TemplateAction() { Pattern = "MANUALLED", Data = "Off" });
                        //pin2.Write(GpioPinValue.Low);
                        _ports.WritePin(PortNumber.PORT_TWO, false);   // Uncomment for sensors
                    }
                    Debug.WriteLineIf(_debug, "State changed to: " + stateLed);
                }

                if (stateLed.Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    _LEDControl["ledOn"].Data = "checked";
                    _LEDControl["ledOff"].Data = string.Empty;
                }
                else
                {
                    _LEDControl["ledOn"].Data = string.Empty;
                    _LEDControl["ledOff"].Data = "checked";
                }

                _LEDControl.ProcessAction();
                response.Write(_LEDControl.GetByte(), _ws.GetMimeType.GetMimeFromFile("/templateLED.html"));

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
            _ws.RemovePath("/demoLED.html");
            _ports.Dispose();
        }
        #endregion

        //private void InitGPIO()
        //{
        //    gpio = GpioController.GetDefault();
        //    if (gpio == null)
        //        return; // GPIO ni na voljo na tem sistemu
        //    pin2 = gpio.OpenPin(5);
        //    pin2.Write(GpioPinValue.Low);
        //    pin2.SetDriveMode(GpioPinDriveMode.Output);
        //}
    }
}
