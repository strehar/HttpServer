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

namespace Feri.MS.Http.ContentSource
{
    class FileContent : IContentSource
    {
        public bool EnableDebug
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string SourceName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Containes(string pot)
        {
            throw new NotImplementedException();
        }

        public List<string> Names
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsWritable
        {
            get
            {
                return true;
            }
        }

        public byte[] ReadToByte(string path)
        {
            throw new NotImplementedException();
        }

        public string ReadToString(string path)
        {
            throw new NotImplementedException();
        }

        public void WriteFromByte(string path, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteFromtring(string path, string data)
        {
            throw new NotImplementedException();
        }

        public void ReloadFileList()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public string UrlToPath(string url)
        {
            throw new NotImplementedException();
        }
    }
}
