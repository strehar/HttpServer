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

using System.Net;
using Windows.Networking.Sockets;

namespace Feri.MS.Http
{
    /// <summary>
    /// 
    /// </summary>
    public interface IIPFilter
    {
        /// <summary>
        /// 
        /// </summary>
        void Start();
        /// <summary>
        /// 
        /// </summary>
        void Stop();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        bool AddBlackList(IPAddress ip, int bits);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        bool AddWhiteList(IPAddress ip, int bits);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        bool IsBlackListed(IPAddress ip);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        bool IsWhiteListed(IPAddress ip);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        bool ProcessIPFilter(StreamSocket socket);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        bool RemoveBlackList(IPAddress ip);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        bool RemoveWhiteList(IPAddress ip);
    }
}