using Windows.Networking;
using Windows.Networking.Sockets;

namespace Feri.MS.Http
{
    /// <summary>
    /// Class containes information about current HTTP Connection. Available are local host, local port, remote host, remote port.
    /// </summary>
    public class HttpConnection
    {
        private HostName _localHost;
        private HostName _remoteHost;
        private string _remotePort;
        private string _localPort;

        public HttpConnection(StreamSocket data)
        {
            // Podatki o hostu in klientu
            _localHost = data.Information.LocalAddress;
            _remoteHost = data.Information.RemoteAddress;
            _localPort = data.Information.LocalPort;
            _remotePort = data.Information.RemotePort;
        }

        public string LocalHostName
        {
            get
            {
                return _localHost.DisplayName;
            }
        }

        public string LocalHost
        {
            get
            {
                return _localHost.RawName;
            }
        }

        public string LocalPort
        {
            get
            {
                return _localPort;
            }
        }

        public string RemoteHostName
        {
            get
            {
                return _remoteHost.DisplayName;
            }
        }

        public string RemoteHost
        {
            get
            {
                return _remoteHost.RawName;
            }
        }

        public string RemotePort
        {
            get
            {
                return _remotePort;
            }
        }
    }
}
