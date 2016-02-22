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
using System.Collections;
using System.Diagnostics;

namespace Feri.MS.Http
{
    /// <summary>
    /// Helper class that handeles MIME types.
    /// </summary>
    public class MimeTypes
    {
        internal bool _debug = false;
        private Hashtable _mimeTypes = new Hashtable();

        /// <summary>
        /// Known mime types
        /// </summary>
        public MimeTypes()
        {
            // default vrednosti za mimetype: .html, .htm, .gif, .jpg , .js .css
            _mimeTypes.Add(".html", "text/html");
            _mimeTypes.Add(".htm", "text/html");
            _mimeTypes.Add(".gif", "image/gif");
            _mimeTypes.Add(".jpg", "image/jpeg");
            _mimeTypes.Add(".js", "application/javascript");
            _mimeTypes.Add(".css", "text/css");
            _mimeTypes.Add(".ico", "image/x-icon");
        }

        /// <summary>
        /// Get mime type from file extension
        /// </summary>
        /// <param name="koncnica"></param>
        /// <returns></returns>
        public string this [string koncnica]
        {
            get
            {
                if (_mimeTypes.Contains(koncnica.ToLower()))
                {
                    return _mimeTypes[koncnica.ToLower()].ToString();
                } else
                {
                    // Ce ni prepoznano, po prevzetem vrnemo html
                    return "text/html";
                }
            }
        }

        public Hashtable Types
        {
            get
            {
                return _mimeTypes;
            }
            set
            {
                _mimeTypes = value;
            }
        }

        /// <summary>
        /// Get mime type from provided file name (looks at the file extension)
        /// </summary>
        /// <param name="fileName">Filename to look at</param>
        /// <returns>Mime type of the file or text/html if unknown.</returns>
        public string GetMimeFromFile(String fileName)
        {
            string _koncnica = "text/html";
            if (fileName.Contains("."))
            {
                _koncnica = fileName.Substring(fileName.LastIndexOf('.'));
            } else
            {
                _koncnica = fileName;
            }
            Debug.WriteLineIf(_debug, "Mime Type: " + this[_koncnica]);
            return this[_koncnica];
        }
    }
}
