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
using System;
using System.Diagnostics;

namespace WebServerDemo
{
    /// <summary>
    /// Demo of session object in the HttpServer.
    /// </summary>
    class SessionDemo : IDisposable
    {
        HttpServer _ws;
        string _privatePath = "AppHtml";

        SimpleTemplate _sessionTemplate = new SimpleTemplate();
        SimpleTemplate _sessionSetTemplate = new SimpleTemplate();
        
        public void Start(HttpServer server)
        {
            _ws = server;

            _ws.AddPath("/demoSessionSet.html", ProcessSessionSet);
            _ws.AddPath("/demoSessionRead.html", ProcessSessionRead);
            _ws.AddPath("/demoSessionRemove.html", ProcessSessionRemove);

            _sessionTemplate.LoadString(_ws.EmbeddedContent.ReadEmbededToByte(_privatePath + "/templateSessionRead.html"));
            _sessionTemplate.AddAction("session", "SESSION", "");

            _sessionSetTemplate.LoadString(_ws.EmbeddedContent.ReadEmbededToByte(_privatePath + "/templateSessionSet.html"));
            _sessionSetTemplate.AddAction("session", "SESSION", "");

        }

        private void ProcessSessionSet(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.Parameters.ContainsKey("niz"))
                {
                    string niz = (request.GetSession()["Demo"] = request.Parameters["niz"]) as string;
                    response.Write(_ws.EmbeddedContent.ReadEmbededToByte(_privatePath + "/sessionSetPotrdi.html"), _ws.GetMimeType.GetMimeFromFile("/sessionSetPotrdi.html"));
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
                response.Write(e);
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
                    _sessionTemplate.UpdateAction("session", "no data.");
                }
                _sessionTemplate.ProcessAction();
                response.Write(_sessionTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/teplateSessionRead.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                response.Write(e);
            }
        }

        private void ProcessSessionRemove(HttpRequest request, HttpResponse response)
        {
            try
            {
                request.RemoveSession();
                response.Write(_ws.EmbeddedContent.ReadEmbededToByte(_privatePath + "/sessionRemove.html"), _ws.GetMimeType.GetMimeFromFile("/sessionRemove.html"));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                response.Write(e);
            }
        }

        #region Disposable support
        public void Dispose()
        {
            _ws.RemovePath("/demoSessionSet.html");
            _ws.RemovePath("/demoSessionRead.html");
            _ws.RemovePath("/demoSessionRemove.html");
        }
        #endregion
    }
}
