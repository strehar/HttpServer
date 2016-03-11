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
using System;

namespace WebServerDemo
{
    /// <summary>
    /// This class shows how to redirect user request to a different address.
    /// </summary>
    class RedirectDemo : IDisposable
    {
        HttpServer _ws;

        public void Start(HttpServer server)
        {
            _ws = server;

            _ws.AddPath("/demoRedirect*", ProcessDemoRedirect);
        }

        private void ProcessDemoRedirect(HttpRequest request, HttpResponse response)
        {
            response.Redirect("/redirectTarget.html");
        }

        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            _ws.RemovePath("/demoRedirect.html");
        }
        #endregion
    }
}
