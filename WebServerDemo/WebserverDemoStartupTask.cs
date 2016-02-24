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
using Windows.ApplicationModel.Background;
using Windows.System.Threading;
using Windows.Foundation;
using System.Diagnostics;
//using Windows.Devices.Gpio;
using WebResources;
using Feri.MS.Http;
using Feri.MS.Http.Template;
using Feri.MS.Http.Json;
using Feri.MS.Parts.I2C.Thermometer;
using System.Text;
using Feri.MS.Parts.I2C.PortExpander;
using System.Net;

namespace WebserverDemo
{
    public sealed class WebServerDemo : IDisposable
    {

        //GpioController gpio;
        //GpioPin pin;

        HttpServer _ws = new HttpServer();
        WebHolder _wh = new WebHolder();

        DS1621 _termometer = new DS1621();
        PCF8574 _ports = new PCF8574();

        SimpleTemplate _templateDemo = new SimpleTemplate();
        SimpleTemplate _temperatureTemplate = new SimpleTemplate();
        SimpleTemplate _LEDControl = new SimpleTemplate();
        SimpleTemplate _timerControl = new SimpleTemplate();
        SimpleTemplate _cookieTemplate = new SimpleTemplate();
        SimpleTemplate _cookieSetTemplate = new SimpleTemplate();
        SimpleTemplate _sessionTemplate = new SimpleTemplate();
        SimpleTemplate _sessionSetTemplate = new SimpleTemplate();

        SimpleJsonListener _json = new SimpleJsonListener();

        string _privatePath = "AppHtml";
        string state = "Unspecified";
        string stateLed = "Unspecified";
        bool enableBlink = false;
        bool _debug = false;

        public void Start()
        {
            //InitGPIO();
            _ws.SetDebug = true;

            _ws.RegisterAssembly(this.GetType());
            _ws.RegisterAssembly(_wh.GetType());

            _ws.RefreshFileList();

            _ws.AuthenticationRequired = true;
            _ws.AddUser("user", "password");

            _ws.AddPath("/demoLED.html", ProcessDemoLED);
            _ws.AddPath("/demoTemperature.html", ProcessTemperature);
            _ws.AddPath("/demoTimer.html", ProcessTimer);
            _ws.AddPath("/demoCookieSet.html", ProcessCookieSet);
            _ws.AddPath("/demoCookieRead.html", ProcessCookieRead);
            _ws.AddPath("/demoCookieRemove.html", ProcessCookieRemove);
            _ws.AddPath("/demoSessionSet.html", ProcessSessionSet);
            _ws.AddPath("/demoSessionRead.html", ProcessSessionRead);
            _ws.AddPath("/demoSessionRemove.html", ProcessSessionRemove);
            _ws.AddPath("/template.html", VrniTemplate);
            _ws.AddPath("/data.json", _json.Listen);

            _ws.SetRootPath("PublicHtml", "/index.html");

            // IP Filtering
            _ws.AddBlackList(new IPAddress(new byte[] { 192, 168, 1, 64 }), 24);
            _ws.AddWhiteList(new IPAddress(new byte[] { 192, 168, 2, 64 }), 32);

            _ws.Start();

            _ws.AddTimer("TestTimer", 10000, TimerDemo);

            _json.AddData("Timer", "Off");
            _json.AddData("Led", "Off");
            _json.AddData("MaualLed", "Off");

            _templateDemo.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateDemo.html"));
            _templateDemo.AddAction("led", "LED", "Off");
            _templateDemo.AddAction("maualLed", "MANUALLED", "Off");
            _templateDemo.AddAction("timer", "TIMER", "Off");

            _LEDControl.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateLED.html"));
            _LEDControl.AddAction("ledOn", "LEDON", "");
            _LEDControl.AddAction("ledOff", "LEDOFF", "");

            _timerControl.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateTimer.html"));
            _timerControl.AddAction("timerOn", "TIMERON", "");
            _timerControl.AddAction("timerOff", "TIMEROFF", "");

            _cookieTemplate.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateCookieRead.html"));
            _cookieTemplate.AddAction("cookie", "COOKIE", "");

            _cookieSetTemplate.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateCookieSet.html"));
            _cookieSetTemplate.AddAction("cookie", "COOKIE", "");

            _sessionTemplate.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateSessionRead.html"));
            _sessionTemplate.AddAction("session", "SESSION", "");

            _sessionSetTemplate.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateSessionSet.html"));
            _sessionSetTemplate.AddAction("session", "SESSION", "");

            _temperatureTemplate.LoadString(_ws.ReadEmbededToByte(_privatePath + "/templateTermometer.html"));
            _temperatureTemplate.AddAction("temperature", "TEMP", "");

            _termometer.HighPrecision = true;
            _termometer.Initialize();

            _ports._debug = true;
            _ports.Initialize();
        }

        //private void InitGPIO()
        //{
        //    gpio = GpioController.GetDefault();
        //    if (gpio == null)
        //        return; // GPIO ni na voljo na tem sistemu
        //    pin = gpio.OpenPin(4);
        //    pin.Write(GpioPinValue.Low);
        //    pin.SetDriveMode(GpioPinDriveMode.Output);
        //}

        public void TimerDemo()
        {
            if (enableBlink)
            {
                if (state.Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    //pin.Write(GpioPinValue.Low);
                    //_ports.Write(0);
                    _ports.WritePin(PortNumber.PORT_ONE, false);
                    _json.UpdateData("Led", "Off");
                    _templateDemo.UpdateAction("led", "Off");
                    state = "Off";
                }
                else
                {
                    //pin.Write(GpioPinValue.High);
                    //_ports.Write(1);
                    _ports.WritePin(PortNumber.PORT_ONE, true);
                    _json.UpdateData("Led", "On");
                    _templateDemo.UpdateAction("led", "On");
                    state = "On";
                }
                Debug.WriteLineIf(_debug, "State changed to: " + state);
            }
        }

        private void VrniTemplate(HttpRequest reqiest, HttpResponse response)
        {
            _templateDemo.ProcessAction();
            byte[] rezultat = _templateDemo.GetByte();
            response.Write(rezultat, _ws.GetMimeType.GetMimeFromFile(_privatePath + "/templateDemo.html"));
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
                        _templateDemo.UpdateAction("maualLed", "On");
                        //pin.Write(GpioPinValue.High);
                        _ports.WritePin(PortNumber.PORT_TWO, true);
                    }
                    else if (request.Parameters["state"].Equals("Off", StringComparison.OrdinalIgnoreCase))
                    {
                        stateLed = "Off";
                        _json.UpdateData("MaualLed", "Off");
                        _templateDemo.UpdateAction("maualLed", "Off");
                        //pin.Write(GpioPinValue.Low);
                        _ports.WritePin(PortNumber.PORT_TWO, false);
                    }
                    Debug.WriteLineIf(_debug, "State changed to: " + state);
                }

                if (stateLed.Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    _LEDControl.UpdateAction("ledOn", "checked");
                    _LEDControl.UpdateAction("ledOff", "");
                }
                else
                {
                    _LEDControl.UpdateAction("ledOn", "");
                    _LEDControl.UpdateAction("ledOff", "checked");
                }

                _LEDControl.ProcessAction();
                response.Write(_LEDControl.GetByte(), _ws.GetMimeType.GetMimeFromFile("/templateLED.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessTimer(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.Parameters.ContainsKey("state"))
                {
                    if (request.Parameters["state"].Equals("On", StringComparison.OrdinalIgnoreCase))
                    {
                        enableBlink = true;
                        Debug.WriteLineIf(_debug, "Timer state changed to: On");
                    }
                    else if (request.Parameters["state"].Equals("Off", StringComparison.OrdinalIgnoreCase))
                    {
                        enableBlink = false;
                        Debug.WriteLineIf(_debug, "Timer state changed to: Off");
                    }
                }

                if (enableBlink)
                {
                    _timerControl.UpdateAction("timerOn", "checked");
                    _timerControl.UpdateAction("timerOff", "");
                    _json.UpdateData("Timer", "On");
                    _templateDemo.UpdateAction("timer", "On");
                }
                else
                {
                    _timerControl.UpdateAction("timerOn", "");
                    _timerControl.UpdateAction("timerOff", "checked");
                    _json.UpdateData("Timer", "Off");
                    _templateDemo.UpdateAction("timer", "Off");
                }

                _timerControl.ProcessAction();
                response.Write(_timerControl.GetByte(), _ws.GetMimeType.GetMimeFromFile("/templateTimer.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessCookieSet(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.Parameters.ContainsKey("niz"))
                {
                    response.AddCookie(new HttpCookie("DemoCookie", request.Parameters["niz"], (DateTime.Now).AddHours(1)));

                    response.Write(_ws.ReadEmbededToByte(_privatePath + "/cookieSetPotrdi.html"), _ws.GetMimeType.GetMimeFromFile("/cookieSetPotrdi.html"));
                }
                else
                {
                    if (request.ContainsCookie("DemoCookie"))
                    {
                        bool rezultat = _cookieSetTemplate.UpdateAction("cookie", request.GetCookie("DemoCookie").Value);
                    }
                    else
                    {
                        _cookieSetTemplate.UpdateAction("cookie", "");
                    }
                    _cookieSetTemplate.ProcessAction();
                    response.Write(_cookieSetTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/teplateCookieSet.html"));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

        }

        private void ProcessCookieRead(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.ContainsCookie("DemoCookie"))
                {
                    _cookieTemplate.UpdateAction("cookie", request.GetCookie("DemoCookie").Value);
                }
                else
                {
                    _cookieTemplate.UpdateAction("cookie", "ni podatkov.");
                }
                _cookieTemplate.ProcessAction();
                response.Write(_cookieTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/teplateCookieRead.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessCookieRemove(HttpRequest request, HttpResponse response)
        {
            try
            {
                
                response.AddCookie(new HttpCookie("DemoCookie", "", DateTime.MinValue));
                response.Write(_ws.ReadEmbededToByte(_privatePath + "/sessionRemove.html"), _ws.GetMimeType.GetMimeFromFile("/sessionRemove.html"));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessSessionSet(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.Parameters.ContainsKey("niz"))
                {
                    string niz = (request.GetSession()["Demo"] = request.Parameters["niz"]) as string;
                    response.Write(_ws.ReadEmbededToByte(_privatePath + "/sessionSetPotrdi.html"), _ws.GetMimeType.GetMimeFromFile("/sessionSetPotrdi.html"));
                }
                else
                {
                    Session _session = request.GetSession(false);
                    if (_session != null)
                    {
                        _sessionSetTemplate.UpdateAction("session", (string)_session["Demo"]);
                    }
                    else
                    {
                        _sessionSetTemplate.UpdateAction("session", "");
                    }
                    _sessionSetTemplate.ProcessAction();
                    response.Write(_sessionSetTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/teplateSessionSet.html"));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessSessionRead(HttpRequest request, HttpResponse response)
        {
            try
            {
                Session _session = request.GetSession(false);
                //string niz = (request.GetSession(false)?["Demo"]) as string;
                if (_session != null)
                {
                    _sessionTemplate.UpdateAction("session", (string)_session["Demo"]);
                }
                else
                {
                    _sessionTemplate.UpdateAction("session", "ni podatkov.");
                }
                _sessionTemplate.ProcessAction();
                response.Write(_sessionTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/teplateSessionRead.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessSessionRemove(HttpRequest request, HttpResponse response)
        {
            try
            {
                request.RemoveSession();
                response.Write(_ws.ReadEmbededToByte(_privatePath + "/cookieRemove.html"), _ws.GetMimeType.GetMimeFromFile("/cookieRemove.html"));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void ProcessTemperature(HttpRequest request, HttpResponse response)
        {
            try
            {
                byte[] _temp = _termometer.TemperatureRead();
                StringBuilder temp = new StringBuilder();
                if (_temp[0] > 125)  // Termometer gre do 125, če je več, potem pomeni da je negativna temperatura. (Thermometer reads up to 125C, everything above it is negative)
                    temp.Append((_temp[0] - 256).ToString());
                else
                    temp.Append(_temp[0].ToString());
                temp.Append(".");
                temp.Append(_temp[1].ToString());

                _temperatureTemplate.UpdateAction("temperature", temp.ToString());
                _temperatureTemplate.ProcessAction();
                response.Write(_temperatureTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/templateTermometer.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }


        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            //pin.Dispose();
            _ports.Dispose();
            _termometer.Dispose();
        }
        #endregion
    }
    public sealed class WebserverDemoStartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _serviceDeferral;
        WebServerDemo server;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskInstance.Canceled += OnCanceled;
            _serviceDeferral = taskInstance.GetDeferral();
            server = new WebServerDemo();
            IAsyncAction asyncAction = ThreadPool.RunAsync((workItem) =>
            {
                server.Start();
            });
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _serviceDeferral.Complete();
        }
    }
}
