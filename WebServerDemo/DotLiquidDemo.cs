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

using DotLiquidCore;
using Feri.MS.Http;
using System;

namespace WebServerDemo
{
    /// <summary>
    /// This is SimpleTemplate demo. Since other classes display data on this template, we recive template instance from SimpleWebServer class.
    /// </summary>
    class DotLiquidDemo : IDisposable
    {
        HttpServer _ws;
        Template liquidtemplate;

        string _privatePath = "AppHtml";

        public void Start(HttpServer server)
        {
            _ws = server;
            _ws.AddPath("/dotLiquidTemplate.html", VrniTemplate);
            Template.RegisterSafeType(typeof(HttpRequest), new[] { "AuthenticatedUser", "RequestPath", "RequestType" });
            liquidtemplate = Template.Parse(_ws.EmbeddedContent.ReadEmbededToString(_privatePath + "/dotLiquidTemplateDemo.html"));
        }

        private void VrniTemplate(HttpRequest request, HttpResponse response)
        {
            try {
                Hash vars = Hash.FromAnonymousObject(request);
                string result = liquidtemplate.Render(vars);

                response.Write(System.Text.Encoding.UTF8.GetBytes(result.ToCharArray()), _ws.GetMimeType.GetMimeFromFile(_privatePath + "/templateDemo.html"));
            } catch (Exception e)
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
