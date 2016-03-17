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
using Feri.MS.Http.ContentSource;
using Feri.MS.Http.Json;
using Feri.MS.Http.Template;
using System.Net;
using WebResources;

namespace WebServerDemo
{
    /// <summary>
    /// This class creates instances of various examples, creates WebServer class, registers Assemblies, set ip filters, user authentication and starts the HttpServer.
    /// </summary>
    class StartDemo
    {

        HttpServer _ws = new HttpServer();
        WebHolder _wh = new WebHolder();
        SimpleTemplate _template = new SimpleTemplate();
        SimpleJsonListener _json = new SimpleJsonListener();

        CookieDemo _cookieDemo = new CookieDemo();
        SessionDemo _sessionDemo = new SessionDemo();
        LEDDemo _LEDDemo = new LEDDemo();
        RedirectDemo _redirectDemo = new RedirectDemo();
        TemperatureDemo _temperatureDemo = new TemperatureDemo();
        TimerDemo _timerDemo = new TimerDemo();
        TemplateDemo _templateDemo = new TemplateDemo();
        DotLiquidDemo _liquidDemo = new DotLiquidDemo();
        LogDisplayDemo _logDemo = new LogDisplayDemo();

        public void Start()
        {
            // Disable debug display from HttpServer
            _ws.SetDebug = false;

            // Register assemblies for content and refresh file list
            ((EmbeddedContent)_ws.HttpRootManager.GetSource("EmbeddedContent")).RegisterAssembly(this.GetType()); // Register it in specific source
            EmbeddedContent.RegisterAssembly(_ws, _wh.GetType());  // Register it in all sources of matching type (helper method specific to this source)
            _ws.HttpRootManager.ReloadSourceFileList();   // Reload all file lists in all sources

            // IP Filtering
            //_ws.IPFilterEnabled = true;  // Change this to true to enable IP filter!
            //_ws.IPFilter.AddBlackList(new IPAddress(new byte[] { 192, 168, 1, 64 }), 24);
            //_ws.IPFilter.AddWhiteList(new IPAddress(new byte[] { 192, 168, 2, 64 }), 32);
            //_ws.IPFilter.AddBlackList(IPAddress.Parse("164.8.251.107"), 24);
            //_ws.IPFilter.AddWhiteList(IPAddress.Parse("164.8.251.107"), 32);

            // User Authentication
            _ws.AuthenticationRequired = true;
            _ws.UserManager.AddUser("user", "password1");

            // JSON
            _ws.AddPath("/data.json", _json.Listen);

            // Server root path for static content
            _ws.HttpRootManager.SetRootPath("PublicHtml");
            _ws.HttpRootManager.SetIndex(new string[] { "/index.html" });
            _ws.HttpRootManager.AddExtension("chtml", new DotLiquidCoreTemplate());
            _ws.HttpRootManager.AddExtension("shtml", new SimpleTemplate());

            // Initialize demos
            _cookieDemo.Start(_ws);
            _sessionDemo.Start(_ws);
            _LEDDemo.Start(_ws, _json, _template);
            _redirectDemo.Start(_ws);
            _temperatureDemo.Start(_ws);
            _timerDemo.Start(_ws, _json, _template);
            _templateDemo.Start(_ws, _template);
            _liquidDemo.Start(_ws);
            _logDemo.Start(_ws);

            // Start server on default port (8000)
            //_ws.Start();

            // Start server on alternate port 80
            _ws.Start("80");
        }
    }
}
