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

using Feri.MS.Http.HttpSession;
using Feri.MS.Http.Log;
using Feri.MS.Http.RootManager;
using Feri.MS.Http.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http
{
    internal class HttpVirtualServer
    {
        #region Definicije
        
        public delegate void serverPath(HttpRequest request, HttpResponse response);    // delegat za obdelavo http zahtev
        private Dictionary<string, serverPath> RegisteredServerPath = new Dictionary<string, serverPath>();               // Registrirane http poti (url) in metode ki se kličejo za obdelavo te zahteve
        
        private bool _authenticationRequired = false;     // Ali server zahteva avtentikacijo za dostop do HTTP vmesnika. prevzeto je ne.

        private SessionManager _sessionManager = new SessionManager(); // Skrbi za seje. Poda se kot referenca novim HttRequest in HttpResponse objektom.

        private IUserManager _userManager;                // Razred skrbi za dodajanje, odvzemanje in avtentikacijo uporabnikov
        private HttpServer _httpServer;
         
        #endregion
        #region Properties
        public List<string> ServerName { get; set; }
        public string ServerRootPath { get; set; }
        public IHttpRootManager RootManager { get; set; }
        public bool DebugEnabled { get; set; } = false;                      // Ali se naj izpisujejo debug informacije iz metod (precej spama)
        public IHttpLog Log { get; set; }             // Glavni razred, ki skrbi za logiranje dogodkov preko http protokola.
        #endregion

        public void Start(HttpServer listenerClass)
        {
            _httpServer = listenerClass;

        }

        public void Stop()
        {

        }

        public string ProcessRequest (HttpRequest request, HttpResponse response)
        {
            string __tmpKey = null;
            string __status = null;

            // Process requests
            try
            {
                if (RegisteredServerPath.ContainsKey(request.RequestPath.ToLower()))
                {
                    RegisteredServerPath[request.RequestPath.ToLower()](request, response);
                    __status = "200";
                }
                else if (!string.IsNullOrEmpty(__tmpKey = IsPathRegistredGeneral(request.RequestPath)))
                {
                    RegisteredServerPath[__tmpKey](request, response);
                    __status = "200";
                }
                else
                {
                    __status = RootManager.Listen(request, response); //ni registrirane poti, kličemo rootmanager, ter preberemo status
                }
            }
            catch (Exception e)
            {
                // There was unhandeled exception in the user method, we return stack trace and return 500 to HttpServer
                response.Write(e);
                __status = "500";
            }

            return __status;
        }

        #region HTTP listeners
        private string IsPathRegistredGeneral(string path)
        {
            string _tmpPath = null;
            foreach (string _searchPath in RegisteredServerPath.Keys)
            {
                if (_searchPath.EndsWith("*"))
                {
                    _tmpPath = _searchPath.Remove(_searchPath.Length - 1);
                    if (path.ToLower().StartsWith(_tmpPath))
                    {
                        return _searchPath;
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Method registers new event listener for specified part recived in HttpREquest.
        /// For example, if path recived was /HelloWorld it could call registered listener ProcessHelloWorld(HttpRequest, HttpResponse)
        /// </summary>
        /// <param name="pot">HTTP Path to listen for. Must be unique.</param>
        /// <param name="metoda">Event listener to call if user requested registered path</param>
        /// <returns>True if event listener was registered or false if it allready exists.</returns>
        public bool AddPath(string pot, serverPath metoda)
        {
            if (!RegisteredServerPath.ContainsKey(pot.ToLower()))
            {
                RegisteredServerPath.Add(pot.ToLower(), metoda);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method removes event listener and unregisters the HTTP path.
        /// </summary>
        /// <param name="pot">Path and related listener to remove.</param>
        /// <returns>True if it was removed or flase if path did not exist.</returns>
        public bool RemovePath(string pot)
        {
            if (RegisteredServerPath.ContainsKey(pot.ToLower()))
            {
                RegisteredServerPath.Remove(pot.ToLower());
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, serverPath> GetPath()
        {
            return RegisteredServerPath;
        }
        #endregion

        #region IDisposable Support
        // To se kliče ob dispose, da se pravilno počisti listener.
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            /*foreach (KeyValuePair<string, StreamSocketListener> pair in listeners)
            {
                pair.Value.Dispose();
            }
            listeners.Clear();
            _userManager?.Stop();
            _IPFilter?.Stop();*/
        }
        #endregion
    }
}
