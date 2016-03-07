using Feri.MS.Http.Template;
using Windows.Networking.Sockets;

namespace Feri.MS.Http
{
    public interface IHttpRootManager
    {
        bool AddExtensionListener(string name, string extension, ITemplate template);
        void SetIndex(string[] index);
        bool AddSource(string name, IContentSource provider);
        void GetErrorMessage(string errorID, string fileToDisplay);
        ITemplate GetExtensionListener(string name);
        IContentSource GetSource(string name);
        void Listen(HttpRequest request, HttpResponse response);
        bool RemoveExtensionListener(string name);
        bool RemoveSource(string name);
        void SendError(StreamSocket socket, string ErrorID);
        void SendError(HttpRequest request, HttpResponse response, string ErrorID);
        void SetErrorMessage(string errorID, string fileToDisplay);
        void SetRootPath(string folder);
        void Start(HttpServer server);
        void Stop();
    }
}