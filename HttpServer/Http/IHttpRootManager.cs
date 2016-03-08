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

using Feri.MS.Http.Template;
using System.Collections.Generic;
using Windows.Networking.Sockets;

namespace Feri.MS.Http
{
    public interface IHttpRootManager
    {
        bool AddExtensionListener(string extension, ITemplate template);
        void SetIndex(string[] index);
        bool AddSource(string name, IContentSource provider);
        HttpError GetErrorMessage(string errorID);
        ITemplate GetExtensionListener(string name);
        IContentSource GetSource(string name);
        void Listen(HttpRequest request, HttpResponse response);
        bool RemoveExtensionListener(string name);
        bool RemoveSource(string name);
        void ReturnErrorMessage(StreamSocket socket, string ErrorID);
        void ReturnErrorMessage(HttpRequest request, HttpResponse response, string ErrorID);
        void ReturnErrorMessage(HttpRequest request, HttpResponse response, Dictionary<string, string> headers, string ErrorID);
        bool AddErrorMessage(HttpError error);
        bool UpdateErrorMessage(HttpError error);
        void SetRootPath(string folder);

        string UrlToPath(string url);
        byte[] ReadToByte(string path);
        string ReadToString(string path);
        List<string> GetNames();
        bool Containes(string pot);

        bool AddExtensionListenerData(string extension, string actionName, object data);
        bool RemoveExtensionListenerData(string extension, string actionName);
        object GetExtensionListenerData(string extension, string actionName);
        bool UpdateExtensionListenerData(string extension, string actionName, object data);

        void Start(HttpServer server);
        void Stop();
    }
}