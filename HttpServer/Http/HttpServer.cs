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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Feri.MS.Http.Timer;
using System.Linq;
using Feri.MS.Http.ContentSource;
using Feri.MS.Http.Log;
using Feri.MS.Http.Security;
using Feri.MS.Http.RootManager;
using Feri.MS.Http.Util;
using Feri.MS.Http.HttpSession;
using System.Text;

namespace Feri.MS.Http
{
    /// <summary>
    /// Class is main HTTP server class. It handles reciving conenctions, creating Tasks (and indirectly threads from thread pool), creating HttpRequest and HttpResponse objects, 
    /// adding and removing users, handling authentications, trigegring events based on the client requests, processing request errors, handling timers,
    /// registing and removing assemblies in which to look for embeded content, registing embedded content for reading.
    /// 
    /// IP filter is implemented in IPFilter class, user can replace it with custom class.
    /// User authentication and management is impleneted in UserManager class, user can replace it with custom class.
    /// Management of embedded content is managed by EmbeddedContent class.
    /// 
    /// Basic usage:
    /// HttpServer server = new HttpServer();
    /// server.start();
    /// 
    /// Normal usage:
    /// HttpServer server = new HttpServer();
    /// server.HttpRootManager.SetRootPath("PublicHtml");
    /// server.HttpRootManager.SetIndex(new string[] { "/index.html" });
    /// server.AddPath("/some_path.html", some_listener_method);
    /// server.start();
    /// </summary>
    public class HttpServer : IDisposable
    {
        #region Definitions
        Dictionary<string, serverPath> _serverPath = new Dictionary<string, serverPath>();               // Registrirane http poti (url) in metode ki se kličejo za obdelavo te zahteve
        Dictionary<string, HttpTimer> _timerji = new Dictionary<string, HttpTimer>();                            // registrirani timerji, ki so na sistemu in se prožijo

        Dictionary<string, StreamSocketListener> listeners = new Dictionary<string, StreamSocketListener>();                    // Socket listener za prejem zahtev, je flobalna spremenljivka zato da .net runtime ve da mora obdržati proces živ v kombinaciji z taskInstance.GetDeferral(); v glavnem razredu

        MimeTypes _mimeType = new MimeTypes();            // Interna instanca razreda MimeTypes za pretvorbo tipov. Rabi se v processroot, sicer pa služi kot helper class za uporabnika

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public delegate void serverPath(HttpRequest request, HttpResponse response);    // delegat za obdelavo http zahtev

        private IHttpRootManager _rootManager;

        private bool _debug = false;                      // Ali se naj izpisujejo debug informacije iz metod (precej spama)
        private bool _authenticationRequired = false;     // Ali server zahteva avtentikacijo za dostop do HTTP vmesnika. prevzeto je ne.
        private bool _ipFilterEnabled = false;

        private SessionManager _sessionManager = new SessionManager(); // Skrbi za seje. Poda se kot referenca novim HttRequest in HttpResponse objektom.

        private IHttpLog _log;             // Glavni razred, ki skrbi za logiranje dogodkov preko http protokola.

        private IUserManager _userManager;                // Razred skrbi za dodajanje, odvzemanje in avtentikacijo uporabnikov
        private IIPFilter _IPFilter;                      // Razred skrbi za preverjanje IP naslovov uporabnikov in vzdrženje White in Black list
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public bool IPFilterEnabled
        {
            get
            {
                return _ipFilterEnabled;
            }

            set
            {
                _ipFilterEnabled = value;
                if (_ipFilterEnabled)
                    if (_IPFilter == null)
                        IPFilter = new IPFilter();  // If it's null, we call thru IPFilter property, to perform lifecycle management.
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool AuthenticationRequired
        {
            get
            {
                return _authenticationRequired;
            }

            set
            {
                _authenticationRequired = value;
                if (_authenticationRequired)
                    if (_userManager == null)
                        UserManager = new UserManager();  // If it's null, we call thru UserManager property, to perform lifeclycle management.
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public MimeTypes GetMimeType
        {
            get
            {
                return _mimeType;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool SetDebug
        {
            get
            {
                return _debug;
            }

            set
            {
                _debug = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IUserManager UserManager
        {
            get
            {
                if (_userManager == null)
                {
                    _userManager = new UserManager();
                    _userManager.Start();
                }
                return _userManager;
            }

            set
            {
                if (value == null)
                {
                    _userManager?.Stop();
                    _userManager = new UserManager();
                    _userManager.Start();
                }
                else
                {
                    _userManager?.Stop();
                    _userManager = value;
                    _userManager.Start();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IIPFilter IPFilter
        {
            get
            {
                if (_IPFilter == null)
                {
                    _IPFilter = new IPFilter();
                    _IPFilter.Start();
                }
                return _IPFilter;
            }

            set
            {
                if (value == null)
                {
                    _IPFilter?.Stop();
                    _IPFilter = new IPFilter();
                    _IPFilter.Start();
                }
                else
                {
                    _IPFilter?.Stop();
                    _IPFilter = value;
                    _IPFilter.Start();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IHttpRootManager HttpRootManager
        {
            get
            {
                if (_rootManager == null)
                {
                    _rootManager = new DefaultHttpRootManager();
                    _rootManager.AddSource(new EmbeddedContent(this.GetType()));
                    _rootManager.Start(this);
                }
                return _rootManager;
            }
            set
            {
                if (_rootManager != null)
                {
                    _rootManager.Stop();
                }
                _rootManager = value;
                _rootManager.Start(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IHttpLog Log
        {
            get
            {
                return _log;
            }

            set
            {
                _log = value;
            }
        }

        #endregion

        #region IDisposable Support
        // To se kliče ob dispose, da se pravilno počisti listener.
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            foreach (KeyValuePair<string, StreamSocketListener> pair in listeners)
            {
                pair.Value.Dispose();
            }
            listeners.Clear();
            _userManager?.Stop();
            _IPFilter?.Stop();
        }
        #endregion

        /// <summary>
        /// Main constructor. It initializes needed classes and registers this class as assembly to look for information (default methods need html files to display errors).
        /// It allso registers Session cleanup timer to remove stale session objects (by default all session that were not accessed for more then 2 hours).
        /// </summary>
        public HttpServer()
        {
            _log = new HttpLog();
            _log.Open();
            _mimeType._debug = _debug;
            _sessionManager._debug = _debug;
            _log.SetDebug = _debug;
            AddTimer("SessionCleanupTimer", 60000, _sessionManager.SessionCleanupTimer);
        }

        /// <summary>
        /// this method just creates new StreamSocketListener and registers listener for new conenctions recived and then binds this listener to port specified by user.
        /// If you call the method without arguments, it defaults to port 8000.
        /// Must be called before server starts to listen to requests.
        /// </summary>
        public void Start(string serviceName = "8000")
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new InvalidDataException("Invalid service name in Start(string)");
            }
            if (!listeners.ContainsKey(serviceName))
            {
                StreamSocketListener _listener = new StreamSocketListener();
                _listener.ConnectionReceived += (sender, args) => ProcessRequestAsync(args.Socket);
#pragma warning disable CS4014
                _listener.BindServiceNameAsync(serviceName);
#pragma warning restore CS4014
                listeners.Add(serviceName, _listener);
            }
            else
            {
                throw new ArgumentException("Service " + serviceName + " is allready registred.");
            }

        }

        #region Request processing
        /// <summary>
        /// Main listener for requests. It creates new task for each request, to enshure that they are served as fast as possible (each task is run in thread from thread pool. Might be same, might be different)
        /// </summary>
        /// <param name="socket">StreamSocket with connection from the client.</param>
        private async void ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                await Task.Factory.StartNew(() => ProcessRequest(socket));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Actual method that processes connections in new Task. It creates new HttpRequest and HttpResponse objects, checks for errors and displays appropriate error and logs each request recived.
        /// It allso checks if request is of supported type.
        /// If everything is ok, it will trigger events for requested path.
        /// </summary>
        /// <param name="socket">StreamSocket with connection from the client.</param>
        private void ProcessRequest(StreamSocket socket)
        {
            bool __streamInit;
            HttpRequest __hrequest = null;
            HttpResponse __hresponse = null;
            Dictionary<string, string> __headers = new Dictionary<string, string>();
            string[] __supportedMethods = new string[] { "GET", "POST" };
            string __tmpKey = null;

            string __status = null;
            string __message = null;
            bool __ipFilterError = false;
            bool __streamInitError = false;
            bool __authenticationError = false;
            bool __methodError = false;


            // Access management
            if (_ipFilterEnabled)
            {
                // We do ip filtering before anyting, if access is denied, there is no point in processing data.
                if (_IPFilter.ProcessIPFilter(socket))
                {
                    __ipFilterError = true;
                }
            }

            if (!__ipFilterError)
            {
                // Create request and response objects
                __hrequest = new HttpRequest();
                __hrequest._debug = _debug;

                __streamInit = __hrequest.Init(socket);

                __hresponse = new HttpResponse(__hrequest);
                __hresponse._debug = _debug;

                __hrequest.SessionManager = _sessionManager;

                // Error management
                if (!__streamInit)
                {
                    // log error
                    __status = ProcessHttpError(__hrequest, __hresponse);
                    __streamInitError = true;
                    //return;
                }
                else
                {
                    // Authentication management
                    if (_authenticationRequired)
                    {
                        string loggedInUser = _userManager.AuthenticateUser(__hrequest);
                        if (string.IsNullOrEmpty(loggedInUser))    // Authentication failed for some reason, request that user authenticates.
                        {
                            // user authentication failed, display authentication request:                    
                            __headers.Add("WWW-Authenticate", "Basic realm=\"PI2 Web Access\"");
                            __status = "401";
                            __authenticationError = true;
                        }
                        else
                        {
                            // User is logged in, set the username property in HttpRequest object
                            __hrequest.AuthenticatedUser = loggedInUser;
                        }
                    }
                    if (!__authenticationError)
                        if (!__supportedMethods.Contains(__hrequest.RequestType))
                        {
                            Debug.WriteLineIf(_debug, "Method not allowed (Error 405) in ProcessRequest()");
                            StringBuilder _tmpMethods = new StringBuilder();
                            foreach (string tmp in __supportedMethods)
                            {
                                _tmpMethods.Append(tmp + ", ");
                            }
                            __headers.Add("Allow", _tmpMethods.ToString().TrimEnd(new char[] { ',' }));
                            __status = "405";
                            __methodError = true;
                        }
                }
            }

            if (__ipFilterError)
            {
                __message = TimeProvider.GetTime().ToString("R") + ": " + socket.Information.RemoteAddress.ToString() + ": " + socket.Information.LocalAddress.ToString() + ": " + socket.Information.LocalPort + ": ";
                __status = "403";
                _log.WriteLine(__message + ": " + __status);
                HttpRootManager.ReturnErrorMessage(socket, __status);
                return;
            }
            __message = TimeProvider.GetTime().ToString("R") + ": " + __hrequest.HttpConnection.RemoteHost + ": " + __hrequest.HttpConnection.LocalHost + ": " + __hrequest.HttpConnection.LocalPort + ": " + __hrequest.RequestString().TrimEnd();
            if (__streamInitError || __authenticationError || __methodError)
            {
                if (!string.IsNullOrEmpty(__status))
                {
                    _log.WriteLine(__message + ": " + __status);
                    HttpRootManager.ReturnErrorMessage(__hrequest, __hresponse, __headers, __status);
                }
                // Empty status message, can happen if we got empty request, ignore?
                return;
            }

            if (_serverPath.ContainsKey(__hrequest.RequestPath.ToLower()))
            {
                _serverPath[__hrequest.RequestPath.ToLower()](__hrequest, __hresponse);
                __status = "200";
            }
            else if (!string.IsNullOrEmpty(__tmpKey = IsPathRegistredGeneral(__hrequest.RequestPath)))
            {
                _serverPath[__tmpKey](__hrequest, __hresponse);
                __status = "200";
            }
            else
            {
                __status = _rootManager.Listen(__hrequest, __hresponse); //ni registrirane poti, kličemo rootmanager, ter preberemo status
            }

            // Write debug and log
            Debug.WriteLineIf(_debug, "TaskID: " + Task.CurrentId + " Pot: " + __hrequest.RequestPath + " ThreadID: " + System.Environment.CurrentManagedThreadId);
            _log.WriteLine(__message + ": " + __status);
        }

        /// <summary>
        /// Heler method for processing server or request errors-
        /// </summary>
        /// <param name="request">Current request</param>
        /// <param name="response">Current reponse assosiated with request object</param>
        private string ProcessHttpError(HttpRequest request, HttpResponse response)
        {
            if (request.Headers.ContainsKey("Content-Type"))
            {
                // Je napačen content-type?
                if (!request.Headers["Content-Type"].Equals("application/x-www-form-urlencoded"))
                {
                    Debug.WriteLineIf(_debug, "Unsupported POST: " + request.Headers["Content-Type"] + ".");
                    //HttpRootManager.ReturnErrorMessage(request, response, "415");
                    return "415";
                }
                else {
                    // če ni to, potem je napačna velikost....
                    string[] requestBody = request.ToString().Split('\n');
                    int lokacijaPodatkov = Array.IndexOf(requestBody, "\r") + 1;
                    int dataLength = requestBody[lokacijaPodatkov].Length;
                    Debug.WriteLineIf(_debug, "Data size mismatch. Attribute says: " + request.Headers["Content-Length"] + " Data says:" + dataLength + ".");
                    //HttpRootManager.ReturnErrorMessage(request, response, "415");
                    return "415";
                }
            }

            if (request.RequestSize < 10)
            {
                // We recived too small request. It happens. If in debug mode write debug line, else ignore
                Debug.WriteLineIf(_debug, "Stream malformed: " + request.RequestType + ": " + request.RequestString() + ": " + request.RequestSize + ".");
                return string.Empty;
            }
            else
            {
                // request is not too small, but something went wrong. we don't know what. print server error 500.
                //HttpRootManager.ReturnErrorMessage(request, response, "500");
                Debug.WriteLineIf(_debug, "Something went wrong. Stream type: " + request.RequestType + ": " + request.RequestString() + ": " + request.RequestSize + ".");
                return "500";
            }
        }
        #endregion

        #region Timers
        /// <summary>
        /// Method adds new periodic timer. It does not pass any arguments to listener.
        /// </summary>
        /// <param name="ime">Name of the timer. Name must be unique.</param>
        /// <param name="miliseconds">Time in miliseconds</param>
        /// <param name="ping">Listener to be called</param>
        /// <returns>True if timer was added and started, flase if it allready exists</returns>
        public bool AddTimer(string ime, int miliseconds, HttpTimer.timerEvent ping)
        {
            if (!_timerji.ContainsKey(ime))
            {
                _timerji.Add(ime, new HttpTimer(ime, miliseconds, ping));
                _timerji[ime]._debug = _debug;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Method stops and removes specified timer.
        /// </summary>
        /// <param name="ime">Name of the timer to stop and remove</param>
        /// <returns>True if the timer was removed or false if it does not exist.</returns>
        public bool RemoveTimer(string ime)
        {
            if (_timerji.ContainsKey(ime))
            {
                _timerji[ime].StopTimer();
                _timerji.Remove(ime);
                return true;
            }
            return false;
        }
        #endregion

        #region HTTP listeners
        private string IsPathRegistredGeneral(string path)
        {
            string _tmpPath = null;
            foreach (string _searchPath in _serverPath.Keys)
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
            if (!_serverPath.ContainsKey(pot.ToLower()))
            {
                _serverPath.Add(pot.ToLower(), metoda);
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
            if (_serverPath.ContainsKey(pot.ToLower()))
            {
                _serverPath.Remove(pot.ToLower());
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
            return _serverPath;
        }
        #endregion
    }
}
