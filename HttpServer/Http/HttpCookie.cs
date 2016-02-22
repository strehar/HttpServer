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
        public HttpCookie(string name)
        {
            _name = name;
            _value = null;
        }

        public HttpCookie(string name, string value)
        {
            _name = name;
            _value = value;
        }

        public HttpCookie(string name, string value, DateTime expire)
        {
            _name = name;
            _value = value;
            _expire = expire;
        }

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

        public bool HasKeys
        {
            get
            {
                return (_values.Count>1)?true:false;
            }
        }

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
