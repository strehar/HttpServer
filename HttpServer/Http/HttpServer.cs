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
using System.Reflection;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Feri.MS.Http.Timer;

namespace Feri.MS.Http
{
    /// <summary>
    /// Class is min HTTP server class. It handles reciving conenctions, creating Tasks (and indirectly threads from thread pool), creating HttpRequest and HttpResponse objects, 
    /// adding and removing users, handling authentications, trigegring events based on the client requests, processing request errors, handling timers,
    /// registing and removing assemblies in which to look for embeded content, registing embedded content for reading.
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

        Dictionary<string, AssemblyData> _RegistriraniAssebly = new Dictionary<string, AssemblyData>();  // Asembliji (dll-i) po katerih iščemo embeded vire, ki jih lahko prikažemo uporabnikom. Rabi se za reflection
        Dictionary<string, string> _NajdeneDatoteke = new Dictionary<string, string>();                  // Datoteke, ki so vključene v asemblije kot embededresource in jih lahko pošljemo uporabniku ter v katerem assembliju so.
        Dictionary<string, serverPath> _serverPath = new Dictionary<string, serverPath>();               // Registrirane http poti (url) in metode ki se kličejo za obdelavo te zahteve
        Dictionary<string, HttpTimer> _timerji = new Dictionary<string, HttpTimer>();                            // registrirani timerji, ki so na sistemu in se prožijo
        Dictionary<string, string> _users = new Dictionary<string, string>();                            // Registriranu userji z gesli, ki imajo dostop do sistema

        Assembly _sistemskiAssembly = null;               // asembly od tega dll-a, da se lahko sklicujemo na sistemske vire

        //private const int BufferSize = 8192;            // Prevzeta največja velikost predpomnilnika za obdelavo HTTP zahtev.
        StreamSocketListener listener;                    // Socket listener za prejem zahtev, je flobalna spremenljivka zato da .net runtime ve da mora obdržati proces živ v kombinaciji z taskInstance.GetDeferral(); v glavnem razredu

        MimeTypes _mimeType = new MimeTypes();            // Interna instanca razreda MimeTypes za pretvorbo tipov. Rabi se v processroot, sicer pa služi kot helper class za uporabnika

        public delegate void serverPath(HttpRequest request, HttpResponse response);    // delegat za obdelavo http zahtev

        serverPath _serverRoot;                           // Delegat za prevzeto pot, v primeru da se zahteva url, ki ga ne obdeluje nobena registrirana funkcija

        string _serverRootFolder = "SystemHtml";          // prevzeta "mapa" od koder se prikazujejo datoteke, če je bila zahtevana pot, ki je ne obdeluje nobena finkcija
        string _serverRootFile = "serverDefault.html";    // Preveta datoteka za prikaz
        private bool _debug = false;                      // Ali se naj izpisujejo debug informacije iz metod (precej spama)
        private bool _authenticationRequired = false;     // Ali server zahteva avtentikacijo za dostop do HTTP vmesnika. prevzeto je ne.

        private SessionManager _sessionManager = new SessionManager(); // Skrbi za seje. Poda se kot referenca novim HttRequest in HttpResponse objektom.

        private HttpLog _log = new HttpLog();

        #endregion

        #region Properties
        public bool AuthenticationRequired
        {
            get
            {
                return _authenticationRequired;
            }

            set
            {
                _authenticationRequired = value;
            }
        }

        public MimeTypes GetMimeType
        {
            get
            {
                return _mimeType;
            }
        }

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

        #endregion

        #region IDisposable Support
        // To se kliče ob dispose, da se pravilno počisti listener.
        public void Dispose()
        {
            listener.Dispose();
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
            _sistemskiAssembly = GetType().GetTypeInfo().Assembly;
            string _namespace = GetType().Namespace;
            if (_namespace.Length < 1)
                _RegistriraniAssebly.Add(_sistemskiAssembly.GetName().Name, new AssemblyData() { Name = _sistemskiAssembly.GetName().Name, Assembly = _sistemskiAssembly });
            else
                _RegistriraniAssebly.Add(_sistemskiAssembly.GetName().Name, new AssemblyData() { Name = _sistemskiAssembly.GetName().Name, Assembly = _sistemskiAssembly, NameSpace = _namespace });
            RefreshFileList();
            _serverRoot = ProcessRoot;
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
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Actual method that processes connections in new Task. It creates new HttpRequest and HttpResponse objects, checks for errors and displays appropriate error and logs each request recived.
        /// It allso checks if request is of supported type.
        /// </summary>
        /// <param name="socket">StreamSocket with connection from the client.</param>
        private void ProcessRequest(StreamSocket socket)
        {
            //try
            //{
            HttpRequest _hrequest = new HttpRequest();
            _hrequest._debug = _debug;

            bool _streamInit = _hrequest.Init(socket);

            HttpResponse _hresponse = new HttpResponse(_hrequest);
            _hresponse._debug = _debug;

            _hrequest.SessionManager = _sessionManager;

            Debug.WriteLineIf(_debug, "ProcessRequestAsync Task ID: " + Task.CurrentId + ".");

            // Error management
            if (!_streamInit)
            {
                // log error
                _log.WriteLine("Error:" + DateTime.Now.ToString("R") + ": " + _hrequest.HttpConnection.RemoteHost + ": " + _hrequest.HttpConnection.LocalHost + ": " + _hrequest.RequestString().TrimEnd());
                ProcessHttpError(_hrequest, _hresponse);
                return;
            }

            //Log request
            _log.WriteLine(DateTime.Now.ToString("R") + ": " + _hrequest.HttpConnection.RemoteHost + ": " + _hrequest.HttpConnection.LocalHost + ": " + _hrequest.RequestString().TrimEnd());

            if (_hrequest.RequestType.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                ProcessHttpRequest(_hrequest, _hresponse);
            }
            else if (_hrequest.RequestType.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                ProcessHttpRequest(_hrequest, _hresponse);
            }
            else {
                byte[] _dataArray = ReadEmbededToByte("SystemHtml/405.html");
                Dictionary<string, string> _405header = new Dictionary<string, string>();
                _405header.Add("Allow", "GET");
                _hresponse.Write(_dataArray, _mimeType.GetMimeFromFile("/405.html"), "405 Method not allowed", _405header);

                Debug.WriteLineIf(_debug, "Method not allowed (Error 405) in ProcessRequest()");
            }
            return;  // Do tu živita objekta request in response
            //}
            //catch (Exception e)
            //{
            //    // Something went boom. :)
            //    //Debug.WriteLine(e.Message);
            //    //Debug.WriteLine(e.ToString());
            //    Debug.WriteLine(e);
            //}
        }

        /// <summary>
        /// Heler method for processing server or request errors-
        /// </summary>
        /// <param name="request">Current request</param>
        /// <param name="response">Current reponse assosiated with request object</param>
        private void ProcessHttpError(HttpRequest request, HttpResponse response)
        {
            //try
            //{
            if (request.Headers.ContainsKey("Content-Type"))
            {
                // Je napačen content-type?
                if (!request.Headers["Content-Type"].Equals("application/x-www-form-urlencoded"))
                {
                    Debug.WriteLineIf(_debug, "Unsupported POST: " + request.Headers["Content-Type"] + ".");

                    byte[] _dataArray = ReadEmbededToByte("SystemHtml/415.html");
                    response.Write(_dataArray, _mimeType.GetMimeFromFile("/415.html"), "415 Unsupported Media Type");

                    return;

                }
                else {
                    // če ni to, potem je napačna velikost....
                    string[] requestBody = request.ToString().Split('\n');
                    int lokacijaPodatkov = Array.IndexOf(requestBody, "\r") + 1;
                    int dataLength = requestBody[lokacijaPodatkov].Length;
                    Debug.WriteLineIf(_debug, "Data size mismatch. Attribute says: " + request.Headers["Content-Length"] + " Data says:" + dataLength + ".");

                    byte[] _dataArray = ReadEmbededToByte("SystemHtml/415.html");
                    response.Write(_dataArray, _mimeType.GetMimeFromFile("/415.html"), "415 Unsupported Media Type");

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

                byte[] _dataArray = ReadEmbededToByte("SystemHtml/500.html");
                response.Write(_dataArray, _mimeType.GetMimeFromFile("/500.html"), "500 Internal Server Error");

                Debug.WriteLineIf(_debug, "Something went wrong. Stream type: " + request.RequestType + ": " + request.RequestString() + ": " + request.RequestSize + ".");
                return;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //}
        }

        /// <summary>
        /// Helper method for processing Requests and triggering events. Recives HttpRequest and HttpResponse objecs from ProcessRequest()
        /// </summary>
        /// <param name="request">Current request</param>
        /// <param name="response">Current reponse assosiated with request object</param>
        private void ProcessHttpRequest(HttpRequest request, HttpResponse response)
        {
            Debug.WriteLineIf(_debug, "ProcessGET Task ID: " + Task.CurrentId + ".");

            //try
            //{
            Debug.WriteLineIf(_debug, "TaskID: " + Task.CurrentId + " Pot: " + request.RequestPath + " ThreadID: " + System.Environment.CurrentManagedThreadId);

            if (_authenticationRequired)
            {
                if (!request.Headers.ContainsKey("Authorization"))
                {
                    // Autentikacija še ni bila izvedena.
                    NeedAuthenticationResponse(response);
                    return;
                }
                else
                {
                    // Preveri kaj je user vnesel...
                    string _encoded = request.Headers["Authorization"].Split(' ')[1].Trim();
                    string _Vnos = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(_encoded));
                    string[] _user = _Vnos.Split(new char[] { ':' }, 2);
                    if (!AuthenticateUser(_user[0].Trim(), _user[1].Trim()))
                    {
                        // Autentikacija ni uspela, prikaži login ekran.
                        NeedAuthenticationResponse(response);
                        return;
                    }
                }
            }


            //Obdelaj registrirane poti preko delegatov

            if (_serverPath.ContainsKey(request.RequestPath.ToLower()))
            {
                _serverPath[request.RequestPath.ToLower()](request, response);
            }
            else
            {
                _serverRoot(request, response); //ni registrirane poti, kličemo processtoorasync
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //}
        }

        /// <summary>
        /// Default method for displaying static html content from the server.
        /// Path to look for content is defined by SetRootPath method that allso defines default file to display.
        /// This method is registered as listener when server is first created and can be overriten by the AddRootPath method.
        /// 
        /// It dispalys file if found in the path, else it displays 404 message.
        /// </summary>
        /// <param name="request">Current request</param>
        /// <param name="response">Current reponse assosiated with request object</param>
        private void ProcessRoot(HttpRequest request, HttpResponse response)
        {
            //try
            //{

            byte[] _dataArray = null;
            string pot;
            if (request.RequestPath.Equals("/"))
                pot = "/" + _serverRootFile;
            else
                pot = request.RequestPath;

            if (GetEmbededContaines(_serverRootFolder + pot))
            {
                _dataArray = ReadEmbededToByte(_serverRootFolder + pot);
            }
            else {
                _dataArray = ReadEmbededToByte("SystemHtml/404.html");
                response.Write(_dataArray, _mimeType.GetMimeFromFile(pot), "404 Not Found");
                return;
            }

            response.Write(_dataArray, _mimeType.GetMimeFromFile(pot));
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //}
        }
        #endregion

        #region Assembly processing
        /// <summary>
        /// Method registers new assembly to look for embeded content. (in Visual studio in file properties build action is embedded resource)
        /// </summary>
        /// <param name="assembly">typeof(object) or object.GetType(); must be unique</param>
        /// <returns>true if assembly was added, false if it was allready registerd.</returns>
        public bool RegisterAssembly(Type assembly)
        {
            //try
            //{
            if (assembly != null)
            {
                if (!_RegistriraniAssebly.ContainsKey(assembly.GetTypeInfo().Assembly.GetName().Name))
                {
                    string _namespace = assembly.Namespace;
                    if (_namespace.Length < 1)
                        _RegistriraniAssebly.Add(assembly.GetTypeInfo().Assembly.GetName().Name, new AssemblyData() { Name = assembly.GetTypeInfo().Assembly.GetName().Name, Assembly = assembly.GetTypeInfo().Assembly });
                    else
                        _RegistriraniAssebly.Add(assembly.GetTypeInfo().Assembly.GetName().Name, new AssemblyData() { Name = assembly.GetTypeInfo().Assembly.GetName().Name, Assembly = assembly.GetTypeInfo().Assembly, NameSpace = _namespace });
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Method removes assembly from the list of assemblies to look for embeded content.
        /// </summary>
        /// <param name="assembly">typeof(object) or object.GetType()</param>
        /// <returns>True if assembly was removed, false it it was not. It can return false if trying to unregister assembly containing HttpServer class.</returns>
        public bool UnregisterAssembly(Type assembly)
        {
            //try
            //{
            if (assembly != null)
            {
                // Ne dovolimo odstraniti sistemskega assemblija iz seznama.
                if (!assembly.GetTypeInfo().Assembly.GetName().Name.Equals(_sistemskiAssembly.GetName().Name))
                {
                    //preverimo ali je ta assebly registriran
                    if (_RegistriraniAssebly.ContainsKey(assembly.GetTypeInfo().Assembly.GetName().Name))
                    {
                        _RegistriraniAssebly.Remove(assembly.GetTypeInfo().Assembly.GetName().Name);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
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
            //try
            //{
            if (!_timerji.ContainsKey(ime))
            {
                _timerji.Add(ime, new HttpTimer(ime, miliseconds, ping));
                _timerji[ime]._debug = _debug;
                return true;
            }
            return false;
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Method stops and removes specified timer.
        /// </summary>
        /// <param name="ime">Name of the timer to stop and remove</param>
        /// <returns>True if the timer was removed or false if it does not exist.</returns>
        public bool RemoveTimer(string ime)
        {
            //try
            //{
            if (_timerji.ContainsKey(ime))
            {
                _timerji[ime].StopTimer();
                _timerji.Remove(ime);
                return true;
            }
            return false;
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }
        #endregion

        #region HTTP paths
        /// <summary>
        /// Method registers new event listener for specified part recived in HttpREquest.
        /// For example, if path recived was /HelloWorld it could call registered listener ProcessHelloWorld(HttpRequest, HttpResponse)
        /// </summary>
        /// <param name="pot">HTTP Path to listen for. Must be unique.</param>
        /// <param name="metoda">Event listener to call if user requested registered path</param>
        /// <returns>True if event listener was registered or false if it allready exists.</returns>
        public bool AddPath(string pot, serverPath metoda)
        {
            //try
            //{
            if (!_serverPath.ContainsKey(pot.ToLower()))
            {
                _serverPath.Add(pot.ToLower(), metoda);
                return true;
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Method removes event listener and unregisters the HTTP path.
        /// </summary>
        /// <param name="pot">Path and related listener to remove.</param>
        /// <returns>True if it was removed or flase if path did not exist.</returns>
        public bool RemovePath(string pot)
        {
            //    try
            //    {
            if (_serverPath.ContainsKey(pot.ToLower()))
            {
                _serverPath.Remove(pot.ToLower());
                return true;
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Method sets root path for built in static html listener. It defines where to look for static content and what is default file to look for.
        /// </summary>
        /// <param name="folder">Folder to look for, for example PublicHtml</param>
        /// <param name="file">File to look for, for example index.html</param>
        public void SetRootPath(string folder, string file)
        {
            _serverRootFolder = folder;
            if (file.StartsWith("/"))
                _serverRootFile = file.Replace("/", "");
            else
                _serverRootFile = file;
        }

        /// <summary>
        /// Method replaces default listener for static and unregistered content with user specified one.
        /// Unregistered content meand all url requests that are not handeled by other listeners.
        /// </summary>
        /// <param name="metoda">Enevt listener that will be called whenever unregistered path is called.</param>
        public void AddRootPath(serverPath metoda)
        {
            //try
            //{
            _serverRoot = metoda;
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }
        #endregion

        #region Embeded files
        /// <summary>
        /// Method scans for new embedded content that can be read form registered assemblies. It must be called whenever new assemblies are added or removed.
        /// </summary>
        public void RefreshFileList()
        {
            //try
            //{
            _NajdeneDatoteke.Clear();
            List<string> _datoteke = new List<string>();
            foreach (KeyValuePair<string, AssemblyData> par in _RegistriraniAssebly)
            {
                _datoteke.AddRange(par.Value.Assembly.GetManifestResourceNames());
                foreach (string pot in _datoteke)
                {
                    //_potKljuc = pot.Substring(par.Value.NameSpace.Length + 1);
                    if (!_NajdeneDatoteke.ContainsKey(pot))
                    {
                        _NajdeneDatoteke.Add(pot, par.Key);
                    }
                    Debug.WriteLineIf(_debug, "Najdena pot: " + pot + " Assembly: " + par.Key);
                }
                _datoteke.Clear();
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Method takes url provided by user methods and returns full path to be read, with all package names.
        /// It uses registered files to scan. It does partitial file scan and returnes first found file. that's why it's important that calling methods add atleast part of namespace before path.
        /// For example "publicHtml" + path
        /// </summary>
        /// <param name="url">url or part of file name to look for</param>
        /// <returns>full file name (with namespace names) or null if not found.</returns>
        public string UrlToPath(string url)
        {
            //try
            //{
            string _url = url.Replace('/', '.').ToLower();
            string _polnaPot = "";
            bool najdeno = false;
            foreach (string _pot in _NajdeneDatoteke.Keys)
            {
                if (_pot.ToLower().Contains(_url) && (!najdeno))
                {
                    _polnaPot = _pot;
                    najdeno = true;
                }
            }
            return najdeno ? _polnaPot : null;
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return null;
            //}
        }

        /// <summary>
        /// Method reads embedded file and returnes byte array with the data.
        /// </summary>
        /// <param name="pot">Full path to file, that is returned from UrlToPath method.</param>
        /// <returns>byte array with file data</returns>
        public byte[] ReadEmbededToByte(string pot)
        {
            //try
            //{
            string _potTmp = pot.Replace('/', '.');
            string _pot = UrlToPath(_potTmp);
            if (_pot == null)
            {
                throw new FileNotFoundException("File " + pot + " not found.");
            }
            Debug.WriteLineIf(_debug, "Open File: " + _pot);
            //string _assemblyName = _pot.Substring(0, _pot.IndexOf('.'));
            string _assemblyName = _NajdeneDatoteke[_pot];
            if (_RegistriraniAssebly.ContainsKey(_assemblyName))
            {
                using (Stream stream = _RegistriraniAssebly[_assemblyName].Assembly.GetManifestResourceStream(_pot))
                {
                    MemoryStream buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    byte[] _dataArray = buffer.ToArray();
                    return _dataArray;
                }
            }
            else
            {
                throw new FileNotFoundException("File " + pot + " not found in Assembly " + _assemblyName);
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //}
            //return null;
        }

        /// <summary>
        /// Helper method that returnes List of all names of embedded resources 
        /// </summary>
        /// <returns>List with names of embedded resources.</returns>
        public List<string> GetEmbededNames()
        {
            //try
            //{
            List<string> _tmpList = new List<string>();
            foreach (string pot in _NajdeneDatoteke.Keys)
                _tmpList.Add(pot);
            return _tmpList;
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //}
            //return null;
        }

        /// <summary>
        /// Helper method to check if embedded resource ecists
        /// </summary>
        /// <param name="pot">name of embedded resource</param>
        /// <returns>True if it exists and false if it does not.</returns>
        public bool GetEmbededContaines(string pot)
        {
            if (UrlToPath(pot.Replace('/', '.')) != null)
                return true;
            else
                return false;
        }
        #endregion

        #region User management
        /// <summary>
        /// method adds user to the list of registered users (users that are allowed access to the application over http)
        /// WARNING: server uses basic authentication.
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <param name="password">Password for the user</param>
        /// <returns>true if user was added and flase if user allready exists.</returns>
        public bool AddUser(string username, string password)
        {
            //try
            //{
            //Username ni treba da je caps sensitive, password pa mora biti...
            string _username = username.ToLower();
            if (!_users.ContainsKey(_username))
            {
                _users.Add(_username, password);
                return true;
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Method removes user from list of authorized users.
        /// </summary>
        /// <param name="username">Username to remove</param>
        /// <returns>true if user was removed of false if user does not exist</returns>
        public bool RemoveUser(string username)
        {
            //try
            //{
            //Username ni treba da je caps sensitive, password pa mora biti...
            string _username = username.ToLower();
            if (_users.ContainsKey(_username))
            {
                _users.Remove(_username);
                return true;
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        /// <summary>
        /// Helper method that requests basic authentication from the client. It's for internal use only.
        /// </summary>
        /// <param name="response"></param>
        private void NeedAuthenticationResponse(HttpResponse response)
        {
            try
            {
                byte[] _dataArray = ReadEmbededToByte("SystemHtml/401.html");

                Dictionary<string, string> _headers = new Dictionary<string, string>();
                _headers.Add("WWW-Authenticate", "Basic realm=\"PI2 Web Access\"");
                string _StatusCode = "401 Unauthorized";
                response.Write(_dataArray, _mimeType.GetMimeFromFile("/401.html"), _StatusCode, _headers);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Method checks if user information provided by the client mathces the information stored on server
        /// </summary>
        /// <param name="username">Provided username</param>
        /// <param name="password">Provided password</param>
        /// <returns>true if information matches and lase if it does not.</returns>
        private bool AuthenticateUser(string username, string password)
        {
            try
            {
                //Username ni treba da je caps sensitive, password pa mora biti...
                string _username = username.ToLower();
                if (_users.ContainsKey(_username))
                    if (_users[_username].Equals(password))
                        return true;
                    else
                        return false;
                else
                    return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.ToString());
                return false;
            }
        }
        #endregion

    }
}
