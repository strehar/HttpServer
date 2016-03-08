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
using Feri.MS.Http.Template;
using Feri.MS.Http;
using System.Collections.Generic;
using System.Net;
using System.Linq;

/// <summary>
/// Optional helper classes for HttpServer, that integrate it with DotLiquidCore library.
/// </summary>
namespace Feri.MS.Integration.Http.Template
{
    public class DotLiquidCoreTemplate : ITemplate
    {
        Dictionary<string, TemplateAction> _actions = new Dictionary<string, TemplateAction>();
        DotLiquidCore.Template template;
        bool _safeMode = true;

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

        public byte[] GetByte()
        {
            return System.Text.Encoding.UTF8.GetBytes(template.Render());
        }

        public string GetString()
        {
            //TODO: stuff for passing to templating engine!
            return template.Render();
        }

        public void LoadString(byte[] data)
        {
            template = DotLiquidCore.Template.Parse(System.Text.Encoding.UTF8.GetString(data));
        }

        public void LoadString(string data)
        {
            template = DotLiquidCore.Template.Parse(data);
        }

        public void ProcessAction()
        {
            return;
        }

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
