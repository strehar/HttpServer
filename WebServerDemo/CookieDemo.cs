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
    /// This class shows how to use cokies in your project.
    /// </summary>
    class CookieDemo : IDisposable
    {
        HttpServer _ws;
        string _privatePath = "AppHtml";

        SimpleTemplate _cookieTemplate = new SimpleTemplate();
        SimpleTemplate _cookieSetTemplate = new SimpleTemplate();

        public void Start(HttpServer server)
        {
            _ws = server;

            _ws.AddPath("/demoCookieSet.html", ProcessCookieSet);
            _ws.AddPath("/demoCookieRead.html", ProcessCookieRead);
            _ws.AddPath("/demoCookieRemove.html", ProcessCookieRemove);

            _cookieTemplate.LoadString(_ws.EmbeddedContent.ReadToByte(_privatePath + "/templateCookieRead.html"));
            _cookieTemplate.AddAction("cookie", "COOKIE", "");

            _cookieSetTemplate.LoadString(_ws.EmbeddedContent.ReadToByte(_privatePath + "/templateCookieSet.html"));
            _cookieSetTemplate.AddAction("cookie", "COOKIE", "");
        }

        private void ProcessCookieSet(HttpRequest request, HttpResponse response)
        {
            try
            {
                if (request.Parameters.ContainsKey("niz"))
                {
                    response.AddCookie(new HttpCookie("DemoCookie", request.Parameters["niz"], (DateTime.Now).AddHours(1)));

                    response.Write(_ws.EmbeddedContent.ReadToByte(_privatePath + "/cookieSetPotrdi.html"), _ws.GetMimeType.GetMimeFromFile("/cookieSetPotrdi.html"));
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
                Debug.WriteLine(e);
                response.Write(e);
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
                    _cookieTemplate.UpdateAction("cookie", "no data.");
                }
                _cookieTemplate.ProcessAction();
                response.Write(_cookieTemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile("/teplateCookieRead.html"));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                response.Write(e);
            }
        }

        private void ProcessCookieRemove(HttpRequest request, HttpResponse response)
        {
            try
            {

                response.AddCookie(new HttpCookie("DemoCookie", "", DateTime.MinValue));
                response.Write(_ws.EmbeddedContent.ReadToByte(_privatePath + "/sessionRemove.html"), _ws.GetMimeType.GetMimeFromFile("/sessionRemove.html"));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                response.Write(e);
            }
        }

        #region Disposable support
        public void Dispose()
        {
            _ws.RemovePath("/demoCookieSet.html");
            _ws.RemovePath("/demoCookieRead.html");
            _ws.RemovePath("/demoCookieRemove.html");
        }
        #endregion
    }
}
