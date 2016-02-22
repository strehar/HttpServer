using System.Collections.Generic;

namespace Feri.MS.Http.Json
{
    /// <summary>
    /// class implements simple json listener that can be registered as listener in HttpServer class. it returnes simple array of data.
    /// </summary>
    public class SimpleJsonListener
    {
        Dictionary<string, string> _data = new Dictionary<string, string>();

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

        public Dictionary<string, string> GetAllData
        {
            get
            {
                return _data;
            }
        }

        public bool AddData(string key, string value)
        {
            //try
            //{
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
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        public bool RemoveData(string key)
        {
            //try
            //{
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
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        public bool UpdateData(string key, string value)
        {
            //try
            //{
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
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }

        public string GetData(string key)
        {
            //try
            //{
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
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return null;
            //}
        }

        public bool IsInData(string key)
        {
            //try
            //{
            if (_data.ContainsKey(key))
            {
                return true;
            }
            else
            {
                return false;
            }
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    return false;
            //}
        }
    }
}
