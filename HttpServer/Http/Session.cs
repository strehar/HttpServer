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
using System.Linq;

namespace Feri.MS.Http
{

    /// <summary>
    /// Data object for storring session information.
    /// </summary>
    public class Session
    {
        Dictionary<string, object> _values = new Dictionary<string, object>();
        DateTime _expires;
        string _sessionID;

        /// <summary>
        /// 
        /// </summary>
        public string SessionID
        {
            get
            {
                return _sessionID;
            }

            set
            {
                _sessionID = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Expires
        {
            get
            {
                return _expires;
            }

            set
            {
                _expires = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<string> Keys
        {
            get
            {
                return _values.Keys.ToList();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<object> Values
        {
            get
            {
                return _values.Values.ToList();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        public Session(string sessionId)
        {
            _expires = TimeProvider.GetTime();
            _expires.AddHours(2);
            _sessionID = sessionId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsParameter(string key)
        {
            return _values.ContainsKey(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveParameter(string key)
        {
            if (_values.ContainsKey(key))
            {
                _values.Remove(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                if (_values.ContainsKey(key))
                    return _values[key];
                else
                    return null;
            }
            set
            {
                if (!_values.ContainsKey(key))
                {
                    _values.Add(key, value);
                }
                else
                {
                    _values[key] = value;
                }
            }
        }
    }
}
