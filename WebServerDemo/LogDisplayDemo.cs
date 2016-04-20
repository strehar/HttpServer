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
using Feri.MS.Http.Log;
using Feri.MS.Http.Template;
using System;
using System.Diagnostics;

namespace WebServerDemo
{
    /// <summary>
    /// This class shows how to use cokies in your project.
    /// </summary>
    class LogDisplayDemo
    {
        HttpServer _ws;
        string _privatePath = "AppHtml";
        IHttpLog _log;

        SimpleTemplate _logTemplate = new SimpleTemplate();

        public void Start(HttpServer server)
        {
            _ws = server;
            _log = _ws.Log;
            _ws.AddPath("/DemoDispayLog.html", ProcessLog);

            _logTemplate.LoadString(_ws.HttpRootManager.ReadToByte(_privatePath + "/templateLog.html"));
            _logTemplate["log"] = new TemplateAction() { Pattern = "LOG" };
        }

        private void ProcessLog(HttpRequest request, HttpResponse response)
        {
            try
            {
                //Feri.MS.Parts.I2C.MultiSensor.BME280.Create().SetCtrlMeas();
                Feri.MS.Parts.I2C.MultiSensor.BME280.Create().Read();


                _logTemplate["log"].Data = "";
                string[] _toDisplay = new string[_log.Cached.Count];
                _log.Cached.CopyTo(_toDisplay);
                for (int i = _toDisplay.Length-1; i >= 0; i--)
                    _logTemplate["log"].Data += _toDisplay[i] + "<br>\n";
                _logTemplate.ProcessAction();
                response.Write(_logTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/templateLog.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                response.Write(e);
            }
        }
    }
}
