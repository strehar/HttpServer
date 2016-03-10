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

namespace WebServerDemo
{
    /// <summary>
    /// This is SimpleTemplate demo. Since other classes display data on this template, we recive template instance from SimpleWebServer class.
    /// </summary>
    class DotLiquidDemo : IDisposable
    {
        HttpServer _ws;
        DotLiquidCoreTemplate liquidtemplate = new DotLiquidCoreTemplate();

        string _privatePath = "AppHtml";

        public void Start(HttpServer server)
        {
            _ws = server;
            _ws.AddPath("/dotLiquidTemplate.html", VrniTemplate);
            liquidtemplate.LoadString(_ws.HttpRootManager.ReadToString(_privatePath + "/dotLiquidTemplateDemo.html"));
        }

        private void VrniTemplate(HttpRequest request, HttpResponse response)
        {
            try
            {
                liquidtemplate["Request"] = new TemplateAction() { ObjectData=request };

                response.Write(liquidtemplate.GetByte(), _ws.GetMimeType.GetMimeFromFile(_privatePath + "/templateDemo.html"));
            }
            catch (Exception e)
            {
                response.Write(e);
            }
        }

        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            _ws.RemovePath("/template.html");
        }
        #endregion
    }
}
