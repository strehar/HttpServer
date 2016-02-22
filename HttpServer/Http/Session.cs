using System;
using System.Collections.Generic;

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

        public Session(string sessionId)
        {
            _expires = DateTime.Now;
            _expires.AddHours(2);
            _sessionID = sessionId;
        }

        public bool ContainsParameter(string key)
        {
            return _values.ContainsKey(key);
        }

        public bool RemoveParameter(string key)
        {
            if (_values.ContainsKey(key))
            {
                _values.Remove(key);
                return true;
            }
            return false;
        }

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
