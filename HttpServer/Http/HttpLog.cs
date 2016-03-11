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

namespace Feri.MS.Http
{
    /// <summary>
    /// Class is used to log information from library. Currently it only writes debug from information recived.
    /// </summary>
    public class HttpLog : IHttpLog
    {
        private List<string> _log = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        public string Pot { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool InMemory { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool SetDebug { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public List<string> Cached
        {
            get
            {
                return _log;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Open()
        {
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="niz"></param>
        public void WriteLine(string niz)
        {
            Debug.WriteLineIf(SetDebug, niz);
            if (_log.Count > 1000)
            {
                _log.RemoveAt(0);
            }
            _log.Add(niz);
            return;
        }
    }
}
