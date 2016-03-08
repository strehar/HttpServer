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
        void SetIndex(string[] index);
        void SetRootPath(string folder);

        bool AddExtensionListener(string extension, ITemplate template);
        bool RemoveExtensionListener(string name);
        ITemplate GetExtensionListener(string name);

        bool AddSource(string name, IContentSource provider);
        bool RemoveSource(string name);
        IContentSource GetSource(string name);

        bool AddErrorMessage(HttpError error);
        void ReturnErrorMessage(StreamSocket socket, string ErrorID);
        void ReturnErrorMessage(HttpRequest request, HttpResponse response, string ErrorID);
        void ReturnErrorMessage(HttpRequest request, HttpResponse response, Dictionary<string, string> headers, string ErrorID);
        bool UpdateErrorMessage(HttpError error);
        HttpError GetErrorMessage(string errorID);

        byte[] ReadToByte(string path);
        string ReadToString(string path);
        string UrlToPath(string url);
        bool Containes(string pot);
        List<string> GetNames();

        bool AddExtensionListenerData(string extension, string actionName, TemplateAction data);
        bool RemoveExtensionListenerData(string extension, string actionName);
        bool UpdateExtensionListenerData(string extension, string actionName, TemplateAction data);
        TemplateAction GetExtensionListenerData(string extension, string actionName);

        void Start(HttpServer server);
        void Stop();
        void Listen(HttpRequest request, HttpResponse response);
    }
}