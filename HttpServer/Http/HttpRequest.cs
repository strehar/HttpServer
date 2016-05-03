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
using Feri.MS.Http.Util;
using Feri.MS.Http.HttpSession;

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
            set
            {
                _output = value;
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
            internal set
            {
                _requestType = value;
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
            internal set
            {
                _requestPath = value;
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
            internal set
            {
                _parameters = value;
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
            internal set
            {
                _headers = value;
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
            internal set
            {
                _requestSize = value;
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
            internal set
            {
                _httpConnection = value;
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
            internal set
            {
                _cookies = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string RequestString
        {
            get
            {
                return _requestString;
            }
            internal set
            {
                _requestString = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AuthenticatedUser { get; internal set; }

        internal string RawRequest
        {
            get
            {
                return _rawrequest;
            }
            set
            {
                _rawrequest = value;
            }
        }
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
            DateTime _tmpTime = TimeProvider.GetTime().AddHours(_sessionLifetime);
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


    }
}
