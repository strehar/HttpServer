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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Feri.MS.Http
{
    /// <summary>
    /// Interface for implementing source of content (from where data is read, cn be anything local or remote)
    /// </summary>
    public interface IContentSource
    {
        bool EnableDebug { get; set; }
        string SourceName { get; }

        void Start();
        void Stop();

        void ReloadFileList();
        string UrlToPath(string url);
        byte[] ReadToByte(string pot);
        string ReadToString(string path);
        List<string> GetNames();
        bool Containes(string pot);
    }
}
