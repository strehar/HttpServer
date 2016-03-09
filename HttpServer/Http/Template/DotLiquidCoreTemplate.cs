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

using System.Collections.Generic;
using System.Net;
using System.Linq;
using DotLiquidCore;

namespace Feri.MS.Http.Template
{
    /// <summary>
    /// Class to encapsulate HttpRequest for passing to the Templating engine
    /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class DotLiquidCoreTemplateRequest : Drop
    {
        public DotLiquidCoreTemplateDictionaryDrop Headers { get; set; } = new DotLiquidCoreTemplateDictionaryDrop();
        public DotLiquidCoreTemplateDictionaryDrop Parameters { get; set; } = new DotLiquidCoreTemplateDictionaryDrop();
        public DotLiquidCoreTemplateDictionaryDrop Session { get; set; } = new DotLiquidCoreTemplateDictionaryDrop();
        public DotLiquidCoreTemplateDictionaryDrop Cookies { get; set; } = new DotLiquidCoreTemplateDictionaryDrop();
        public string User { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Key/Value pairs that we pass to the templating engine. I't part of DotLiquidCoreTemplateRequest
    /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class DotLiquidCoreTemplateDictionaryDrop : Drop {
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public List<string> Keys
        {
            get
            {
                return Data.Keys.ToList();
            }
        }
        public List<string> Values
        {
            get
            {
                return Data.Values.ToList();
            }
        }
        public int Count
        {
            get
            {
                return Data.Count;
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// 
    /// </summary>
    public class DotLiquidCoreTemplate : ITemplate
    {
        Dictionary<string, TemplateAction> _actions = new Dictionary<string, TemplateAction>();
        DotLiquidCore.Template template;
        bool _safeMode = true;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, TemplateAction> Actions
        {
            get
            {
                return _actions;
            }
            set
            {
                _actions = value;
            }
        }

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

        /// <summary>
        /// 
        /// </summary>
        public List<string> Keys
        {
            get
            {
                return _actions.Keys.ToList();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<TemplateAction> Values
        {
            get
            {
                return _actions.Values.ToList();
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
                lock (_actions)
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
                lock (_actions)
                {
                    if (value == null)
                    {
                        _actions.Remove(name);
                        return;
                    }
                    TemplateAction _tmpAction = value;
                    if (SafeMode)
                        _tmpAction.Data = WebUtility.HtmlEncode(value.Data);
                    if (!_actions.ContainsKey(name))
                    {
                        _actions.Add(name, _tmpAction);
                    }
                    else
                    {
                        _actions[name] = _tmpAction;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] GetByte()
        {
            return System.Text.Encoding.UTF8.GetBytes(GetString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            //TODO: stuff for passing to templating engine!
            Hash vars = new Hash();
            foreach (KeyValuePair<string, TemplateAction> pair in _actions)
            {
                if (!string.IsNullOrEmpty(pair.Value.Data))
                {
                    vars.Add(pair.Key, pair.Value);
                }
                else if (pair.Value.ObjectData != null)
                {
                    if (pair.Value.ObjectData is HttpRequest)
                    {
                        // map parameters, user, cookies, session to the template, so it can read and set appropriate data.
                        HttpRequest request = pair.Value.ObjectData as HttpRequest;
                        DotLiquidCoreTemplateRequest _request = new DotLiquidCoreTemplateRequest();

                        _request.User = request.AuthenticatedUser;
                        _request.Path = request.RequestPath;
                        _request.Type = request.RequestType;
                        _request.Size = request.RequestSize;

                        foreach (string key in request.Parameters.Keys)
                            _request.Parameters.Data.Add(key, request.Parameters[key]);

                        foreach (string key in request.Headers.Keys)
                            _request.Headers.Data.Add(key, request.Headers[key]);

                        foreach (string key in request.Cookies.Keys)
                            _request.Cookies.Data.Add(key, request.Cookies[key].Value);

                        Session _session = request.GetSession(false);
                        if (_session != null)
                            foreach (string _name in _session.Keys)
                            {
                                if (request.GetSession()[_name] is string)
                                    _request.Session.Data.Add(_name, _session[_name] as string);
                            }

                        //...
                        vars.Add("Request", _request);

                    }
                    else if (pair.Value.ObjectData is HttpResponse)
                    {
                        HttpResponse response = pair.Value.ObjectData as HttpResponse;
                        // map the cookie to function, so the script can set cookies.
                    }
                    else
                    {
                        // We just pass the object to template. if it's not valid, it will get ignored by template.
                        vars.Add(pair.Key, pair.Value.ObjectData);
                    }

                }
                else
                {
                    // Empty action, ae are going to ignore it.
                }
            }
            return template.Render(vars);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void LoadString(byte[] data)
        {
            template = DotLiquidCore.Template.Parse(System.Text.Encoding.UTF8.GetString(data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void LoadString(string data)
        {
            template = DotLiquidCore.Template.Parse(data);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ProcessAction()
        {
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsAction(string name)
        {
            lock (_actions)
            {
                if (_actions.ContainsKey(name))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
