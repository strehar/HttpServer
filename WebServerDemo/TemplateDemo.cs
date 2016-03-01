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

namespace WebServerDemo
{
    /// <summary>
    /// This is SimpleTemplate demo. Since other classes display data on this template, we recive template instance from SimpleWebServer class.
    /// </summary>
    class TemplateDemo
    {
        HttpServer _ws;
        SimpleTemplate _templateDemo;

        string _privatePath = "AppHtml";

        public void Start(HttpServer server, SimpleTemplate template)
        {
            _ws = server;
            _templateDemo = template;
            _ws.AddPath("/template.html", VrniTemplate);
            _templateDemo.LoadString(_ws.EmbeddedContent.ReadEmbededToByte(_privatePath + "/templateDemo.html"));
            _templateDemo.AddAction("userName", "USERNAME", "");
        }

        private void VrniTemplate(HttpRequest reqiest, HttpResponse response)
        {
            _templateDemo.UpdateAction("userName", reqiest.AuthenticatedUser);
            _templateDemo.ProcessAction();
            byte[] rezultat = _templateDemo.GetByte();
            response.Write(rezultat, _ws.GetMimeType.GetMimeFromFile(_privatePath + "/templateDemo.html"));
        }
    }
}
