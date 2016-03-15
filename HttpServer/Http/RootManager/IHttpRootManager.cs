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

using Feri.MS.Http.ContentSource;
using Feri.MS.Http.Template;
using System.Collections.Generic;
using Windows.Networking.Sockets;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Feri.MS.Http.RootManager
{
    /// <summary>
    /// Interface for implementing main manager for processing root requests. includes content sources, error messages, reading from sources and template registrations.
    /// </summary>
    public interface IHttpRootManager
    {
        void SetIndex(string[] index);
        void SetRootPath(string folder);

        bool AddExtension(string extension, ITemplate template);
        bool RemoveExtension(string name);
        ITemplate GetExtension(string name);

        bool AddSource(IContentSource provider, string name = null);
        bool RemoveSource(string name);
        IContentSource GetSource(string name);
        Dictionary<string, IContentSource> GetAllSources();
        void ReloadSourceFileList(string name = null);

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

        bool AddExtensionTemplateData(string extension, string actionName, TemplateAction data);
        bool RemoveExtensionTemplateData(string extension, string actionName);
        bool UpdateExtensionTemplateData(string extension, string actionName, TemplateAction data);
        TemplateAction GetExtensionTemplateData(string extension, string actionName);

        void Start(HttpServer server);
        void Stop();
        string Listen(HttpRequest request, HttpResponse response);
    }
}