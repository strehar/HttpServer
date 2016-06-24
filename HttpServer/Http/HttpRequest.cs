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
using System.Text;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Net;

namespace Feri.MS.Http
{
    /// <summary>
    /// Class handles everything related to recived HTTP request.
    /// 
    /// - Parses the stream, passed from HttpServer class
    /// - Creates parameters
    /// - Creates attributes
    /// - Parses and creates cookies
    /// - Retrives session from cookie and SessionManager class.
    /// </summary>
    public class HttpRequest
    {
        #region Declarations
        private const int BufferSize = 8192;

        private Stream _output;

        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private Dictionary<string, HttpCookie> _cookies = new Dictionary<string, HttpCookie>();
        private string _requestType;
        private string _requestPath;

        private string _requestString;

        private string _rawrequest;

        private int _requestSize;

        private HttpConnection _httpConnection;

        private SessionManager _sessionManager;

        int _sessionLifetime = 2;  // Session Expire in hours

        bool _sessionUpdated = false;

        /// <summary>
        /// 
        /// </summary>
        public bool _debug = false;
        #endregion

        #region Properties
        internal Stream Output
        {
            get
            {
                return _output;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RequestType
        {
            get
            {
                return _requestType;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RequestPath
        {
            get
            {
                return _requestPath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get
            {
                return _headers;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int RequestSize
        {
            get
            {
                return _requestSize;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public HttpConnection HttpConnection
        {
            get
            {
                return _httpConnection;
            }
        }

        internal SessionManager SessionManager
        {
            get
            {
                return _sessionManager;
            }

            set
            {
                _sessionManager = value;
            }
        }

        internal bool SessionUpdated
        {
            get
            {
                return _sessionUpdated;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int SessionLifetime
        {
            get
            {
                return _sessionLifetime;
            }

            set
            {
                _sessionLifetime = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsCookie(string name)
        {
            return _cookies.ContainsKey(name);
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, HttpCookie> Cookies
        {
            get
            {
                return _cookies;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string RequestString()
        {
            return _requestString;
        }

        /// <summary>
        /// 
        /// </summary>
        public string AuthenticatedUser { get; internal set; }
        #endregion

        /// <summary>
        /// Overrides default ToString to return unparsed request as string.
        /// </summary>
        /// <returns>String representation of unparsed request.</returns>
        override public string ToString()
        {
            return _rawrequest;
        }


        /// <summary>
        /// Class checks if Session exists in SessionManager and in Cookies. If it exists, it returnes the session. If not, it creates new by default. This can be overriden by setting create parameter to false.
        /// Then it will return null on non existant session.
        /// </summary>
        /// <param name="create">Optional parameter. If not defined it defaults to true and means create session if it does not exist. if false it overrides this behaviour and does not create session.</param>
        /// <returns>Session or null</returns>
        public Session GetSession(bool create = true)
        {
            DateTime _tmpTime = (DateTime.Now).AddHours(_sessionLifetime);
            Session _tmpSession;
            if (create)
            {

                if (_cookies.ContainsKey("SessionID"))
                {
                    // Seja že obstaja, samo dobimo jo iz managerja, razen, če vrne null, v tem primeru ustvarimo novo.
                    _tmpSession = _sessionManager.GetSession(_cookies["SessionID"].Value);
                    if (_tmpSession == null)
                    {
                        _tmpSession = _sessionManager.CreateSession();
                        if (!_cookies.ContainsKey("SessionID"))
                        {
                            _cookies.Add("SessionID", new HttpCookie("SessionID", _tmpSession.SessionID));
                        }
                        else
                        {
                            _cookies["SessionID"] = new HttpCookie("SessionID", _tmpSession.SessionID);
                        }
                    }
                }
                else
                {
                    //Kot kaže seja še ne obstaja (če obstaja, pa se je zgubil ID bo tako ali tako potekla v roku ene ure in se izbrisala) zato ustvarimo novo.
                    _tmpSession = _sessionManager.CreateSession();
                    _cookies.Add("SessionID", new HttpCookie("SessionID", _tmpSession.SessionID));
                }
                _cookies["SessionID"].Expire = _tmpTime;
                _cookies["SessionID"].Path = "/";
                _tmpSession.Expires = _tmpTime;
                _sessionUpdated = true;
                return _tmpSession;
            }
            else
            {
                if (_cookies.ContainsKey("SessionID"))
                {
                    _tmpSession = _sessionManager.GetSession(_cookies["SessionID"].Value);
                    if (_tmpSession == null)
                        return null;
                    else
                    {
                        _cookies["SessionID"].Expire = _tmpTime;
                        _cookies["SessionID"].Path = "/";
                        _tmpSession.Expires = _tmpTime;
                        _sessionUpdated = true;
                        return _tmpSession;
                    }
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Metod delets current session if it exists and expires related session cookie.
        /// </summary>
        /// <returns>true if session was deleted, false if session did not exists.</returns>
        public bool RemoveSession()
        {
            if (_cookies.ContainsKey("SessionID"))
            {
                _sessionManager.RemoveSession(_cookies["SessionID"].Value);
                _cookies["SessionID"].Expire = DateTime.MinValue;
                _cookies["SessionID"].Path = "/";
                _sessionUpdated = true;
            }
            return true;
        }

        /// <summary>
        /// Metod parses the provided Http request stream and creates parameters, headers and cookies.
        /// It is internal method for use by HttpServre class.
        /// </summary>
        /// <param name="data">Raw http stream</param>
        /// <returns>True if stream parsed OK, false if something went wrong. Problem can be determined from class state.</returns>
        internal bool Init(StreamSocket data)
        {
            // preberemo stream, zgradimo array haderjev in parametrov, ter input Stream in output stream ter response objekt.
            try
            {
                string[] tmpParam;

                // this works for text only
                StringBuilder requestBuilder = new StringBuilder();
                try
                {
                    using (Stream _input = data.InputStream.AsStreamForRead())
                    {
                        byte[] _data = new byte[BufferSize];
                        IBuffer buffer = _data.AsBuffer();
                        int dataRead = BufferSize;
                        while (dataRead == BufferSize)
                        {
                            dataRead = _input.Read(_data, 0, BufferSize);
                            requestBuilder.Append(Encoding.UTF8.GetString(_data, 0, _data.Length));
                        }
                    }
                    // end this

                    _output = data.OutputStream.AsStreamForWrite();

                    _httpConnection = new HttpConnection(data);
                }
                catch (Exception e)
                {
                    Debug.WriteLineIf(_debug, "Client closed stream during read, some data could be lost (" + e.Message + ").");
                    _output = null;
                    _httpConnection = new HttpConnection();

                    if (data != null)
                        if (data.Information != null)
                        {
                            if (data.Information.LocalAddress != null)
                                _httpConnection._localHost = data.Information.LocalAddress;
                            else
                                _httpConnection._localHost = new Windows.Networking.HostName("Read Error");

                            if (data.Information.RemoteAddress != null)
                                _httpConnection._remoteHost = data.Information.RemoteAddress;
                            else
                                _httpConnection._remoteHost = new Windows.Networking.HostName("Read Error");

                            if (data.Information.LocalPort != null)
                                _httpConnection._localPort = data.Information.LocalPort;
                            else
                                _httpConnection._localPort = "0";

                            if (data.Information.RemotePort != null)
                                _httpConnection._remotePort = data.Information.RemotePort;
                            else
                                _httpConnection._remotePort = "0";
                        }
                        else
                        {
                            _httpConnection._localHost = new Windows.Networking.HostName("Read Error");
                            _httpConnection._remoteHost = new Windows.Networking.HostName("Read Error");
                            _httpConnection._localPort = "0";
                            _httpConnection._remotePort = "0";
                        }
                    else
                    {
                        _httpConnection._localHost = new Windows.Networking.HostName("Read Error");
                        _httpConnection._remoteHost = new Windows.Networking.HostName("Read Error");
                        _httpConnection._localPort = "0";
                        _httpConnection._remotePort = "0";
                    }
                    return false;
                }

                _rawrequest = requestBuilder.ToString().TrimEnd('\0');

                _requestSize = _rawrequest.Length;

                string[] requestBody = _rawrequest.Split('\n');

                _requestString = requestBody[0];

                string[] requestHeaderParts = requestBody[0].Split(' ');
                if (!(requestHeaderParts.Length > 1))
                {
                    Debug.WriteLineIf(_debug, "Malformed request.");
                    Debug.WriteLineIf(_debug, "Request: " + requestBuilder + ".");
                    return false;
                }

                _requestType = requestHeaderParts[0];
                string[] _requestParts = requestHeaderParts[1].Split('?');

                // parametri
                _requestPath = _requestParts[0];
                if (_requestParts.Length > 1)
                {
                    foreach (string par in _requestParts[1].Split('&'))
                    {
                        tmpParam = par.Split(new char[] { '=' }, 2);

                        if (!_parameters.ContainsKey(tmpParam[0].Trim()))
                        {
                            if (tmpParam.Length > 1)
                            {
                                _parameters.Add(WebUtility.UrlDecode(tmpParam[0].Trim()), WebUtility.UrlDecode(tmpParam[1]));
                            }
                            else
                            {
                                _parameters.Add(WebUtility.UrlDecode(tmpParam[0].Trim()), null);
                            }
                        }
                    }
                }

                // aributi
                foreach (string par in requestBody)
                {
                    string[] headers = par.Split(new char[] { ':' }, 2);
                    if (headers.Length > 1)
                    {
                        if (!_headers.ContainsKey(headers[0].Trim()))
                        {
                            _headers.Add(WebUtility.UrlDecode(headers[0].Trim()), WebUtility.UrlDecode(headers[1].Trim()));
                            if (headers[0].Trim().Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                            {
                                ProcessCookie(headers[1].Trim());
                            }
                        }
                    }
                }

                // If type = POST, find string \r, then parse parameters if supportsed type, else report unsupported type.

                if (_requestType.Equals("POST"))
                {
                    if (_headers.ContainsKey("Content-Type"))
                    {
                        if (_headers["Content-Type"].Equals("application/x-www-form-urlencoded"))
                        {
                            int lokacijaPodatkov = Array.IndexOf(requestBody, "\r") + 1;
                            int dataLength = requestBody[lokacijaPodatkov].Length;
                            if (_headers.ContainsKey("Content-Length"))
                            {
                                if (!_headers["Content-Length"].Equals(dataLength.ToString()))
                                {
                                    Debug.WriteLineIf(_debug, "Data size mismatch. Attribute says: " + _headers["Content-Length"] + " Data says:" + dataLength + ".");
                                    return false;
                                }
                            }
                            foreach (string par in requestBody[lokacijaPodatkov].TrimEnd().Split('&'))
                            {
                                tmpParam = par.Split(new char[] { '=' }, 2);

                                if (!_parameters.ContainsKey(tmpParam[0].Trim()))
                                {
                                    if (tmpParam.Length > 1)
                                    {
                                        _parameters.Add(WebUtility.UrlDecode(tmpParam[0].Trim()), WebUtility.UrlDecode(tmpParam[1]));
                                    }
                                    else
                                    {
                                        _parameters.Add(WebUtility.UrlDecode(tmpParam[0].Trim()), null);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Unsupported POST request type
                            Debug.WriteLineIf(_debug, "Unsupported POST: " + _headers["Content-Type"] + ".");
                            return false;
                        }
                    }
                }

                return true;

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }

        /// <summary>
        /// Internal method of HttpRequest class to parse cookies from the cookie string provided by the client.
        /// </summary>
        /// <param name="cookie">Raw string with cookie data.</param>
        private void ProcessCookie(string cookie)
        {
            try
            {
                string decodedCookie = WebUtility.UrlDecode(cookie);
                char[] _tmpChars = decodedCookie.ToCharArray();
                int _tmpLength = _tmpChars.Length;
                // Preverimo koliko ; se pojavi v nizu. to pove koliko cookijev je za to zahtevo.
                int cookieCount = 0;
                for (int n = 0; n < _tmpLength; n++)
                {
                    if (_tmpChars[n] == ';')
                        cookieCount++;
                }

                string[] _tmpCookieArry = decodedCookie.Split(';');

                // Obdelamo vsak poslan cookie...
                foreach (string _cookie in _tmpCookieArry)
                {
                    // preverimo koliko = se pojavi v nizu. Glede na to lahko sestavimo ravilni cookie.
                    int subCookieCount = 0;
                    char[] _subCookieChars = _cookie.ToCharArray();
                    _tmpLength = _subCookieChars.Length;
                    for (int n = 0; n < _tmpLength; n++)
                    {
                        if (_subCookieChars[n] == '=')
                            subCookieCount++;
                    }
                    if (subCookieCount == 0)
                    {
                        // value
                        _cookies.Add(_cookie, new HttpCookie(_cookie));
                    }
                    else if (subCookieCount == 1)
                    {
                        // name=value
                        string[] _cookieParts = _cookie.Split(new char[] { '=' }, 2);
                        _cookies.Add(WebUtility.UrlDecode(_cookieParts[0].Trim()), new HttpCookie(_cookieParts[0].Trim(), _cookieParts[1].Trim()));
                    }
                    else
                    {
                        // name1=value1&name2=value2&...
                        string[] _cookieParts = _cookie.Split(new char[] { '=' }, 2);
                        HttpCookie _tmpHttpCookie = new HttpCookie(_cookieParts[0].Trim());
                        string[] _subCookieParts = _cookieParts[1].Split('&'); //subcookie string.
                        foreach (string _subCookieCookieParts in _subCookieParts)
                        {
                            string[] _subCookie = _subCookieCookieParts.Split(new char[] { '=' }, 2);
                            if (_subCookie.Length > 1)
                            {
                                if (!_tmpHttpCookie.Values.ContainsKey(_subCookie[0].Trim()))  // Ignoriramo podvojene vrednosti, upoštevamo samo prvo.
                                {
                                    _tmpHttpCookie.Values.Add(_subCookie[0].Trim(), _subCookie[1].Trim());
                                }
                            }
                            else
                            {
                                _tmpHttpCookie.Values.Add(_subCookie[0].Trim(), null);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
