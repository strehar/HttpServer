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

namespace Feri.MS.Http.Json
{
    /// <summary>
    /// class implements simple json listener that can be registered as listener in HttpServer class. it returnes simple array of data.
    /// </summary>
    public class SimpleJsonListener
    {
        Dictionary<string, string> _data = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public void Listen(HttpRequest request, HttpResponse response)
        {
            string _jsonString;
            Json _parser = new Json();
            lock (_data)
            {
                _jsonString = _parser.ToJson(_data);
            }
            response.Write(System.Text.Encoding.UTF8.GetBytes(_jsonString), "application/json");
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> GetAllData
        {
            get
            {
                return _data;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddData(string key, string value)
        {
            lock (_data)
            {
                if (!_data.ContainsKey(key))
                {

                    _data.Add(key, value);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveData(string key)
        {
            lock (_data)
            {
                if (_data.ContainsKey(key))
                {

                    _data.Remove(key);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool UpdateData(string key, string value)
        {
            lock (_data)
            {
                if (_data.ContainsKey(key))
                {
                    _data[key] = value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetData(string key)
        {
            lock (_data)
            {
                if (_data.ContainsKey(key))
                {
                    return _data[key];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsInData(string key)
        {
            if (_data.ContainsKey(key))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
