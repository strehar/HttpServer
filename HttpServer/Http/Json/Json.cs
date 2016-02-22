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

using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Feri.MS.Http.Json
{
    /// <summary>
    ///  Helper vlass used to transform objects into JSON objects and back.
    /// </summary>
    class Json
    {
        /// <summary>
        /// Method transforms object into json object.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string ToJson(object data)
        {
            using (MemoryStream _Stream = new MemoryStream())
            {
                DataContractJsonSerializer _Serializer = new DataContractJsonSerializer(data.GetType());
                _Serializer.WriteObject(_Stream, data);
                _Stream.Position = 0;
                using (StreamReader _Reader = new StreamReader(_Stream))
                {
                    return _Reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Method transforms JSON object back into .net objects. Must be provided data and sample object.
        /// </summary>
        /// <param name="json">JSON data</param>
        /// <param name="data">object that this data represents</param>
        /// <returns>Objects created form JSON data</returns>
        public object FromJson(string json, object data)
        {
            var _Bytes = Encoding.Unicode.GetBytes(json);
            using (MemoryStream _Stream = new MemoryStream(_Bytes))
            {
                var _Serializer = new DataContractJsonSerializer(data.GetType());
                return _Serializer.ReadObject(_Stream);
            }
        }
    }
}
