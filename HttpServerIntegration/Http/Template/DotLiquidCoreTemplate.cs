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

/// <summary>
/// Optional helper classes for HttpServer, that integrate it with DotLiquidCore library.
/// </summary>
namespace Feri.MS.Integration.Http.Template
{
    public class Action
    {
        public object Data { get; set; }
        public string Name { get; set; }
        public string Pattern { get; set; }
    }

    public class DotLiquidCoreTemplate : ITemplate
    {
        Dictionary<string, Action> _akcije = new Dictionary<string, Action>();
        DotLiquidCore.Template template;

        public Dictionary<string, Action> Akcije
        {
            get
            {
                return _akcije;
            }
            set
            {
                _akcije = value;
            }
        }

        public bool AddAction(string name, string pattern, string data)
        {
            throw new NotImplementedException();
        }

        public void AddAction(string name, object action)
        {

        }

        public bool DeleteAction(string name)
        {
            throw new NotImplementedException();
        }

        public byte[] GetByte()
        {
            return System.Text.Encoding.UTF8.GetBytes(template.Render());
        }

        public string GetString()
        {
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

        public bool UpdateAction(string name, string data)
        {
            throw new NotImplementedException();
        }
        public bool UpdateAction(HttpRequest request, HttpResponse response)
        {
            //throw new NotImplementedException();
            return true;
        }

        bool ITemplate.AddAction(string name, object data)
        {
            throw new NotImplementedException();
        }

        public bool UpdateAction(string name, object data)
        {
            throw new NotImplementedException();
        }

        public bool ContainsAction(string name)
        {
            if (_akcije.ContainsKey(name))
            {
                return true;
            }
            return false;
        }

        public object GetAction(string name)
        {
            throw new NotImplementedException();
        }
    }
}
