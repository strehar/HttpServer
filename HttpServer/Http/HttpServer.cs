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
    /// server.SetRootPath("PublicHtml", "/index.html");
    /// server.AddPath("/some_path.html", some_listener_method);
    /// server.start();
    /// </summary>
    public class HttpServer : IDisposable
    {
        #region Definitions
        Dictionary<string, serverPath> _serverPath = new Dictionary<string, serverPath>();               // Registrirane http poti (url) in metode ki se kličejo za obdelavo te zahteve
        Dictionary<string, HttpTimer> _timerji = new Dictionary<string, HttpTimer>();                            // registrirani timerji, ki so na sistemu in se prožijo

        StreamSocketListener listener;                    // Socket listener za prejem zahtev, je flobalna spremenljivka zato da .net runtime ve da mora obdržati proces živ v kombinaciji z taskInstance.GetDeferral(); v glavnem razredu

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

        private HttpLog _log = new HttpLog();             // Glavni razred, ki skrbi za logiranje dogodkov preko http protokola.

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

        #endregion

        #region IDisposable Support
        // To se kliče ob dispose, da se pravilno počisti listener.
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            listener.Dispose();
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
            _log.Open();
            _log._debug = true;
            _mimeType._debug = _debug;
            _sessionManager._debug = _debug;
            //_log._debug = _debug;
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
            listener = new StreamSocketListener();
            listener.ConnectionReceived += (sender, args) => ProcessRequestAsync(args.Socket);
#pragma warning disable CS4014
            listener.BindServiceNameAsync(serviceName);
#pragma warning restore CS4014

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
            // Access management
            if (_ipFilterEnabled)
            {
                // We do ip filtering before anyting, if access is denied, there is no point in processing data.
                if (_IPFilter.ProcessIPFilter(socket))
                {
                    _log.WriteLine("Blocked IP:" + TimeProvider.GetTime().ToString("R") + ": " + socket.Information.RemoteAddress.ToString() + ": " + socket.Information.LocalAddress.ToString());
                    HttpRootManager.ReturnErrorMessage(socket, "403");
                    return;
                }
            }

            // Create request and response objects
            HttpRequest _hrequest = new HttpRequest();
            _hrequest._debug = _debug;

            bool _streamInit = _hrequest.Init(socket);

            HttpResponse _hresponse = new HttpResponse(_hrequest);
            _hresponse._debug = _debug;

            _hrequest.SessionManager = _sessionManager;

            // Error management
            if (!_streamInit)
            {
                // log error
                _log.WriteLine("Error:" + TimeProvider.GetTime().ToString("R") + ": " + _hrequest.HttpConnection.RemoteHost + ": " + _hrequest.HttpConnection.LocalHost + ": " + _hrequest.RequestString().TrimEnd());
                ProcessHttpError(_hrequest, _hresponse);
                return;
            }

            //Log request
            _log.WriteLine(TimeProvider.GetTime().ToString("R") + ": " + _hrequest.HttpConnection.RemoteHost + ": " + _hrequest.HttpConnection.LocalHost + ": " + _hrequest.RequestString().TrimEnd());

            // Authentication management
            if (_authenticationRequired)
            {
                string loggedInUser = _userManager.AuthenticateUser(_hrequest);
                if (string.IsNullOrEmpty(loggedInUser))    // Authentication failed for some reason, request that user authenticates.
                {
                    // user authentication failed, display authentication request:
                    Dictionary<string, string> _headers = new Dictionary<string, string>();
                    _headers.Add("WWW-Authenticate", "Basic realm=\"PI2 Web Access\"");
                    HttpRootManager.ReturnErrorMessage(_hrequest, _hresponse, _headers, "401");
                }
                else
                {
                    // User is logged in, set the username property in HttpRequest object
                    _hrequest.AuthenticatedUser = loggedInUser;
                }
            }

            Debug.WriteLineIf(_debug, "TaskID: " + Task.CurrentId + " Pot: " + _hrequest.RequestPath + " ThreadID: " + System.Environment.CurrentManagedThreadId);

            // check request type, and if it's supported, call listeners.
            string[] supportedMethods = new string[] { "GET", "POST" };

            if (supportedMethods.Contains(_hrequest.RequestType))
            {
                if (_serverPath.ContainsKey(_hrequest.RequestPath.ToLower()))
                {
                    _serverPath[_hrequest.RequestPath.ToLower()](_hrequest, _hresponse);
                }
                else
                {
                    _rootManager.Listen(_hrequest, _hresponse); //ni registrirane poti, kličemo processtoorasync
                }
            }
            else
            {
                Dictionary<string, string> _headers = new Dictionary<string, string>();
                _headers.Add("Allow", "GET");
                HttpRootManager.ReturnErrorMessage(_hrequest, _hresponse, _headers, "405");
                Debug.WriteLineIf(_debug, "Method not allowed (Error 405) in ProcessRequest()");
            }
        }

        /// <summary>
        /// Heler method for processing server or request errors-
        /// </summary>
        /// <param name="request">Current request</param>
        /// <param name="response">Current reponse assosiated with request object</param>
        private void ProcessHttpError(HttpRequest request, HttpResponse response)
        {
            if (request.Headers.ContainsKey("Content-Type"))
            {
                // Je napačen content-type?
                if (!request.Headers["Content-Type"].Equals("application/x-www-form-urlencoded"))
                {
                    Debug.WriteLineIf(_debug, "Unsupported POST: " + request.Headers["Content-Type"] + ".");
                    HttpRootManager.ReturnErrorMessage(request, response, "415");
                    return;
                }
                else {
                    // če ni to, potem je napačna velikost....
                    string[] requestBody = request.ToString().Split('\n');
                    int lokacijaPodatkov = Array.IndexOf(requestBody, "\r") + 1;
                    int dataLength = requestBody[lokacijaPodatkov].Length;
                    Debug.WriteLineIf(_debug, "Data size mismatch. Attribute says: " + request.Headers["Content-Length"] + " Data says:" + dataLength + ".");
                    HttpRootManager.ReturnErrorMessage(request, response, "415");
                    return;
                }
            }

            if (request.RequestSize < 10)
            {
                // We recived too small request. It happens. If in debug mode write debug line, else ignore
                Debug.WriteLineIf(_debug, "Stream malformed: " + request.RequestType + ": " + request.RequestString() + ": " + request.RequestSize + ".");
                return;
            }
            else
            {
                // request is not too small, but something went wrong. we don't know what. print server error 500.
                HttpRootManager.ReturnErrorMessage(request, response, "500");
                Debug.WriteLineIf(_debug, "Something went wrong. Stream type: " + request.RequestType + ": " + request.RequestString() + ": " + request.RequestSize + ".");
                return;
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
