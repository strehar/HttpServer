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
using System.Net;
using System.Text.RegularExpressions;

namespace Feri.MS.Http.Template
{
    /// <summary>
    /// 
    /// </summary>
    class Akcija
    {
        string _ime;
        string _data;
        object _pattern;

        public string Ime
        {
            get
            {
                return _ime;
            }

            set
            {
                _ime = value;
            }
        }

        public string Data
        {
            get
            {
                return _data;
            }

            set
            {
                _data = value;
            }
        }

        public object Pattern
        {
            get
            {
                return _pattern;
            }

            set
            {
                _pattern = value;
            }
        }
    }
    /// <summary>
    /// Very simple and basic templating engine. It takes file as input (as byte array or sting) and caches it. Then it runs user defined actions  on it and caches result.
    /// It will only do this if some actions have changed, else cached result is returned.
    /// If safe mode is set to true it will html encode resulting data, else data is returned as is. Safe mode is enabled by default.
    /// </summary>
    public class SimpleTemplate
    {
        #region Definitions
        Dictionary<string, Akcija> _akcije = new Dictionary<string, Akcija>();

        string _originalniNiz = "";
        string _niz = "";

        bool _posodobljenNiz = false;
        bool _safeMode = true;   // Če je true, naredi HTML ENCODE na vsebini vseh akcij.
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public bool SafeMode
        {
            get
            {
                return _safeMode;
            }

            set
            {
                _safeMode = value;
            }
        }
        #endregion

        /// <summary>
        /// Method takes byte array as input and converts it to string for internal use.
        /// </summary>
        /// <param name="data">Data to process (html with custom tags, usually)</param>
        public void LoadString(byte[] data)
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        _originalniNiz = System.Text.Encoding.UTF8.GetString(data);
                        _posodobljenNiz = true;
                    }
        }

        /// <summary>
        /// Method takes string for processing.
        /// </summary>
        /// <param name="data">Data to process (html with custom tags, usually)</param>
        public void LoadString(string data)
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        _originalniNiz = data;
                        _posodobljenNiz = true;
                    }
        }

        /// <summary>
        /// Internal method that handeles action registration.
        /// </summary>
        /// <param name="name">Name of action</param>
        /// <param name="pattern">Pattern to look for in data</param>
        /// <param name="data">Data that will replace pattern (User tag) in the string</param>
        /// <returns></returns>
        private bool InternalAddAction(string name, object pattern, string data)
        {
            //name je niz ki se menja, pattern je vzorec, ki ga menjavamo, data je za kaj se menja
            try
            {
                lock (_niz) lock (_originalniNiz) lock (_akcije)
                        {
                            if (!_akcije.ContainsKey(name))
                            {
                                Akcija _tmp = new Akcija();
                                _tmp.Ime = name;
                                _tmp.Pattern = pattern;
                                _tmp.Data = data;
                                _akcije.Add(name, _tmp);
                                _posodobljenNiz = true;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Method for registing actions. if class is in safe mode it allso html encodes the data.
        /// </summary>
        /// <param name="name">Name of action</param>
        /// <param name="pattern">Pattern to look for in data</param>
        /// <param name="data">Data that will replace pattern (User tag) in the string</param>
        /// <returns>result from InternalAddAction</returns>
        public bool AddAction(string name, string pattern, string data)
        {
            //name je niz ki se menja, pattern je vzorec, ki ga menjavamo, data je za kaj se menja
            if (_safeMode)
            {
                return InternalAddAction(name, pattern, WebUtility.HtmlEncode(data));
            }
            else {
                return InternalAddAction(name, pattern, data);
            }
        }

        /// <summary>
        /// Method for registing actions. if class is in safe mode it allso html encodes the data.
        /// </summary>
        /// <param name="name">Name of action</param>
        /// <param name="pattern">REGEX to look for in data</param>
        /// <param name="data">Data that will replace pattern (User tag) in the string</param>
        /// <returns></returns>
        public bool AddAction(string name, Regex pattern, string data)
        {
            if (_safeMode)
            {
                return InternalAddAction(name, pattern, WebUtility.HtmlEncode(data));
            }
            else
            {
                return InternalAddAction(name, pattern, data);
            }
        }

        /// <summary>
        /// Method for updating actions.
        /// </summary>
        /// <param name="name">Name of action</param>
        /// <param name="data">New data to display with this action</param>
        /// <returns></returns>
        public bool UpdateAction(string name, string data)
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        if (_akcije.ContainsKey(name))
                        {
                            if (_safeMode)
                            {
                                _akcije[name].Data = WebUtility.HtmlEncode(data);
                            }
                            else {
                                _akcije[name].Data = data;
                            }
                            _posodobljenNiz = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
        }

        /// <summary>
        /// Remove action from action list.
        /// </summary>
        /// <param name="name">Name of the action to remove.</param>
        /// <returns>true if action was deleted, false if it was not.</returns>
        public bool DeleteAction(string name)
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        if (_akcije.ContainsKey(name))
                        {
                            _akcije.Remove(name);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
        }

        /// <summary>
        /// Method processes the string if there were any actions that changed. Can be called manually or it will be called automatically when processed data is requested.
        /// </summary>
        public void ProcessAction()
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        string _tmpString = null;
                        foreach (KeyValuePair<string, Akcija> par in _akcije)
                        {
                            if (par.Value.Pattern is Regex)
                            {
                                // Zamenjamo najden string v izvornem nizu in ga damo v izhodnega
                                if (_tmpString == null)
                                    _tmpString = ((Regex)par.Value.Pattern).Replace(_originalniNiz, par.Value.Data);
                                else
                                    _tmpString = ((Regex)par.Value.Pattern).Replace(_tmpString, par.Value.Data);

                            }
                            else if (par.Value.Pattern is string)
                            {
                                // Pri stringu je začetek ključne besede @#, s tem da je v patternu ni treba določit, v templateu pa.
                                if (_tmpString == null)
                                    _tmpString = _originalniNiz.Replace("@#" + (string)par.Value.Pattern, par.Value.Data);
                                else
                                    _tmpString = _tmpString.Replace("@#" + (string)par.Value.Pattern, par.Value.Data);
                            }
                            else
                            {
                                //Neveljaven tip akcije...
                                throw new NotSupportedException("Unsupported action type in ProcessAction; Should not happen.");
                                //return false;
                            }
                        }
                        _niz = _tmpString;
                        _posodobljenNiz = false;
                    }

        }

        /// <summary>
        /// Method returnes processed data, if there are unprocessed actions it will allso call process action, else it will return cached result.
        /// </summary>
        /// <returns>Byte array of the data.</returns>
        public byte[] GetByte()
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        if (_posodobljenNiz)
                        {
                            ProcessAction();
                        }

                        return System.Text.Encoding.UTF8.GetBytes(_niz);
                    }
        }

        /// <summary>
        /// Method returnes processed data, if there are unprocessed actions it will allso call process action, else it will return cached result.
        /// </summary>
        /// <returns>String representation of the data.</returns>
        public string GetString()
        {
            lock (_niz) lock (_originalniNiz) lock (_akcije)
                    {
                        if (_posodobljenNiz)
                        {
                            ProcessAction();
                        }
                        return _niz;
                    }
        }
    }
}
