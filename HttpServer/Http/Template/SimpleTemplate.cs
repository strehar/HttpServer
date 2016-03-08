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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Feri.MS.Http.Template
{
    /// <summary>
    /// Very simple and basic templating engine. It takes file as input (as byte array or sting) and caches it. Then it runs user defined actions  on it and caches result.
    /// It will only do this if some actions have changed, else cached result is returned.
    /// If safe mode is set to true it will html encode resulting data, else data is returned as is. Safe mode is enabled by default.
    /// </summary>
    public class SimpleTemplate : ITemplate
    {
        #region Definitions
        Dictionary<string, TemplateAction> _actions = new Dictionary<string, TemplateAction>();

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

        public List<string> Keys
        {
            get
            {
                return _actions.Keys.ToList();
            }
        }

        public List<TemplateAction> Values
        {
            get
            {
                return _actions.Values.ToList();
            }
        }
        #endregion

        /// <summary>
        /// Method takes byte array as input and converts it to string for internal use.
        /// </summary>
        /// <param name="data">Data to process (html with custom tags, usually)</param>
        public void LoadString(byte[] data)
        {
            lock (_niz) lock (_originalniNiz) lock (_actions)
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
            lock (_niz) lock (_originalniNiz) lock (_actions)
                    {
                        _originalniNiz = data;
                        _posodobljenNiz = true;
                    }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TemplateAction this[string name]
        {
            get
            {
                lock (_niz) lock (_originalniNiz) lock (_actions)
                        {
                            if (_actions.ContainsKey(name))
                            {
                                return _actions[name];
                            }
                            return null;
                        }
            }
            set
            {
                lock (_niz) lock (_originalniNiz) lock (_actions)
                        {
                            //if (value == null)
                            //{
                            //    _actions.Remove(name);
                            //    return;
                            //}
                            TemplateAction _tmpAction = value;
                            if (SafeMode)
                                _tmpAction.Data = WebUtility.HtmlEncode(value.Data);
                            if (!_actions.ContainsKey(name))
                            {
                                _actions.Add(name, _tmpAction);
                                _posodobljenNiz = true;
                            }
                            else
                            {
                                _actions[name] = _tmpAction;
                                _posodobljenNiz = true;
                            }
                        }
            }
        }

        /// <summary>
        /// Method processes the string if there were any actions that changed. Can be called manually or it will be called automatically when processed data is requested.
        /// </summary>
        public void ProcessAction()
        {
            lock (_niz) lock (_originalniNiz) lock (_actions)
                    {
                        string _tmpString = null;
                        foreach (KeyValuePair<string, TemplateAction> par in _actions)
                        {
                            if (!string.IsNullOrEmpty(par.Value.RegexPattern))
                            {
                                // Zamenjamo najden string v izvornem nizu in ga damo v izhodnega
                                if (_tmpString == null)
                                    _tmpString = par.Value.RegexPattern.Replace(_originalniNiz, par.Value.Data);
                                else
                                    _tmpString = par.Value.RegexPattern.Replace(_tmpString, par.Value.Data);
                            }
                            else if (!string.IsNullOrEmpty(par.Value.Pattern))
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
                                //throw new NotSupportedException("Unsupported action type in ProcessAction; Should not happen.");
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
            lock (_niz) lock (_originalniNiz) lock (_actions)
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
            lock (_niz) lock (_originalniNiz) lock (_actions)
                    {
                        if (_posodobljenNiz)
                        {
                            ProcessAction();
                        }
                        return _niz;
                    }
        }

        public bool ContainsAction(string name)
        {
            lock (_niz) lock (_originalniNiz) lock (_actions)
                    {
                        if (_actions.ContainsKey(name))
                        {
                            return true;
                        }
                        return false;
                    }
        }

        public bool RemoveAction(string name)
        {
            if (_actions.ContainsKey(name))
            {
                _actions.Remove(name);
                return true;
            }
            return false;
        }
    }
}
