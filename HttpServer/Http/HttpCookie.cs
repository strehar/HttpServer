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
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http
{
    /// <summary>
    /// CLass stores cookie information. Expire date, value and path are optional.
    /// </summary>
    public class HttpCookie
    {
        string _name;
        string _value;
        string _path;
        DateTime? _expire;
        
        Dictionary<string, string> _values = new Dictionary<string, string>();

        //TODO: Ostala funkcionalnost za cookie, https://msdn.microsoft.com/en-us/library/system.web.httpcookie(v=vs.110).aspx
        // Manjka domain= atribut
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public HttpCookie(string name)
        {
            _name = name;
            _value = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public HttpCookie(string name, string value)
        {
            _name = name;
            _value = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public HttpCookie(string name, string value, DateTime expire)
        {
            _name = name;
            _value = value;
            _expire = expire;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? Expire
        {
            get
            {
                return _expire;
            }

            set
            {
                _expire = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasKeys
        {
            get
            {
                return (_values.Count>1)?true:false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Values
        {
            get
            {
                return _values;
            }

            set
            {
                _values = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
            }
        }
    }
}
