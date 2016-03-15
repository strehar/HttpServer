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

using Feri.MS.Http.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Feri.MS.Http.HttpSession
{
    /// <summary>
    /// Handeling of Sessions. Supports creating, removal, retrival of the sessions.
    /// Allso contains Timer event listener to remove expired sessions from memmory.
    /// </summary>
    class SessionManager
    {
        // String UUID = Guid.NewGuid().ToString() za session id....
        Dictionary<string, Session> _sessions = new Dictionary<string, Session>();
        public bool _debug = false;

        public SessionManager()
        {
            //_sessionManager = this;
        }

        /// <summary>
        /// Creates new session and assigns unique identifier (guid) to it.
        /// </summary>
        /// <returns>New session.</returns>
        public Session CreateSession()
        {
            lock (_sessions)
            {
                string _sessionID = Guid.NewGuid().ToString();
                Session _tmpSession = new Session(_sessionID);
                _sessions.Add(_sessionID, _tmpSession);
                return _tmpSession;
            }
        }

        /// <summary>
        /// Removes session based on it's identifier. Does not remove session cookie, that is doen by HttpRequest.
        /// </summary>
        /// <param name="sessionID">ID to remove</param>
        public void RemoveSession(string sessionID)
        {
            lock (_sessions)
            {
                if (_sessions.ContainsKey(sessionID))
                {
                    _sessions.Remove(sessionID);
                }
            }
        }

        /// <summary>
        /// returnes session based in it's ID. ID is retrived form session cookie.
        /// </summary>
        /// <param name="sessionID">Session ID</param>
        /// <returns>retrived session or null if session expired.</returns>
        public Session GetSession(string sessionID)
        {
            lock (_sessions)
            {
                if (_sessions.ContainsKey(sessionID))
                {
                    return _sessions[sessionID];
                }
                return null;
            }
        }

        /// <summary>
        /// Timer listener that expires sessions after specified ammount of time.
        /// </summary>
        public void SessionCleanupTimer()
        {
            lock (_sessions)
            {
                Debug.WriteLineIf(_debug, "SessionCleanupTimer Thread ID: " + System.Environment.CurrentManagedThreadId);
                List<string> _toRemove = new List<string>();
                foreach (KeyValuePair<string, Session> par in _sessions)
                {
                    DateTime _timeNow = TimeProvider.GetTime();
                    if (par.Value.Expires.CompareTo(_timeNow) < 0)
                    {
                        // Timeout je potekel.
                        Debug.WriteLineIf(_debug, "Removing session: " + par.Value.SessionID);
                        Debug.WriteLineIf(_debug, "Time in session: " + par.Value.Expires + " time to kill: " + _timeNow);
                        _toRemove.Add(par.Key);
                    }
                }
                foreach (string remove in _toRemove)
                    _sessions.Remove(remove);
            }
        }
    }
}
