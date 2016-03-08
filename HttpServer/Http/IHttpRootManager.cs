using Feri.MS.Http.Template;
using System.Collections.Generic;
using Windows.Networking.Sockets;

namespace Feri.MS.Http
{
    public interface IHttpRootManager
    {
        bool AddExtensionListener(string name, string extension, ITemplate template);
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


        void Start(HttpServer server);
        void Stop();
    }
}