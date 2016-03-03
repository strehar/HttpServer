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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public HttpConnection(StreamSocket data)
        {
            // Podatki o hostu in klientu
            _localHost = data.Information.LocalAddress;
            _remoteHost = data.Information.RemoteAddress;
            _localPort = data.Information.LocalPort;
            _remotePort = data.Information.RemotePort;
        }

        /// <summary>
        /// 
        /// </summary>
        public string LocalHostName
        {
            get
            {
                return _localHost.DisplayName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string LocalHost
        {
            get
            {
                return _localHost.RawName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string LocalPort
        {
            get
            {
                return _localPort;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RemoteHostName
        {
            get
            {
                return _remoteHost.DisplayName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RemoteHost
        {
            get
            {
                return _remoteHost.RawName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RemotePort
        {
            get
            {
                return _remotePort;
            }
        }
    }
}
