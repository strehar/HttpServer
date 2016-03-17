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
using System.Net;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace Feri.MS.Http.Security
{
    /// <summary>
    /// 
    /// </summary>
    public class IPFilter : IIPFilter
    {
        Dictionary<string, IpNumber> _blackList = new Dictionary<string, IpNumber>();
        Dictionary<string, IpNumber> _whiteList = new Dictionary<string, IpNumber>();

        #region Lifecycle
        /// <summary>
        /// This manager really does not do anything here.
        /// </summary>
        public void Start()
        {

        }

        /// <summary>
        /// This manager really does not do anything here.
        /// </summary>
        public void Stop()
        {

        }
        #endregion

        #region IP Filtering
        /// <summary>
        /// Check if IP of remote user matches any of the lists.
        /// If it exists in blacklist or does not exist in whitelist access is denied.
        /// BlackList allways override the whitelist!
        /// 
        /// WhiteList: If no record exist, it's ignored. If atleast one record exists, then we check against the list, else set result to false. If it is found, the return is set to false, else it's set to true.
        /// BlackList: If no record exist do not modify whitelist result. Else, check the IP against the list. If it exists in the list, set the result to true, overriding whatever was result from whitelist.
        /// </summary>
        /// <param name="socket">connectiong socket. we get remote IP address from this.</param>
        /// <returns>True if user should be blocked or false, if access should be granted. Note that this does not override user access verification in any way.</returns>
        public bool ProcessIPFilter(StreamSocket socket)
        {
            // Get remote IP
            IPAddress _remoteIP = IPAddress.Parse(socket.Information.RemoteAddress.ToString());

            // process each IP from black and white list. if remote IP is not any of the black lists and if on atleast one white list then return ok, else return true for blocked.
            // 1st check whitelist, then blacklist, so blackist can override whitelist
            bool _result = true;
            if (_whiteList.Count > 0)
            {
                foreach (KeyValuePair<string, IpNumber> key in _whiteList)
                {
                    if (IPNetwork.Contains(key.Value.IPNetwork, _remoteIP))
                        _result = false;
                }
            }
            else
                _result = false;

            foreach (KeyValuePair<string, IpNumber> key in _blackList)
            {
                if (IPNetwork.Contains(key.Value.IPNetwork, _remoteIP))
                    _result = true;
            }

            return _result;

        }

        /// <summary>
        /// Method adds provided IP address and range to the blacklist
        /// </summary>
        /// <param name="ip">IP address we are adding to the blacklist</param>
        /// <param name="bits">network bits of the address. This is used to calculate upper and lower IP address of the range.</param>
        /// <returns>true if ip was added, false if it allready exist in the blacklist</returns>
        public bool AddBlackList(IPAddress ip, int bits)
        {
            IpNumber _IP = new IpNumber();
            string _ipAddress;
            if (ip.ToString().Contains("%"))
            {
                _ipAddress = ip.ToString().Split(new char[] { '%' })[0];
            } else
            {
                _ipAddress = ip.ToString();
            }
            _IP.IPAddress = ip;
            _IP.IPNetwork = IPNetwork.Parse(_ipAddress + "/" + bits);

            if (!_blackList.ContainsKey(ip.ToString()))
            {
                _blackList.Add(ip.ToString(), _IP);
                return true;
            }
            return false;
        }

        /// <summary>
        /// method removes provided ip from the blacklist
        /// </summary>
        /// <param name="ip">IP to remove from blacklist.</param>
        /// <returns>true if ip was removed or else if it did not exist in blacklist</returns>
        public bool RemoveBlackList(IPAddress ip)
        {
            if (_blackList.ContainsKey(ip.ToString()))
            {
                _blackList.Remove(ip.ToString());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Method checks if provided ip exist in blacklist.
        /// </summary>
        /// <param name="ip">IP we are checking for in blacklist</param>
        /// <returns>true if it exist or false if it does not exist in blacklist</returns>
        public bool IsBlackListed(IPAddress ip)
        {
            if (_blackList.ContainsKey(ip.ToString()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Method adds provided IP address and range to the whitelist
        /// </summary>
        /// <param name="ip">IP address we are adding to the whitelist</param>
        /// <param name="bits">network bits of the address. This is used to calculate upper and lower IP address of the range.</param>
        /// <returns>true if ip was added, false if it allready exist in the whitelist</returns>
        public bool AddWhiteList(IPAddress ip, int bits)
        {
            IpNumber _IP = new IpNumber();
            string _ipAddress;
            _IP.IPAddress = ip;
            if (ip.ToString().Contains("%"))
            {
                _ipAddress = ip.ToString().Split(new char[] { '%' })[0];
            }
            else
            {
                _ipAddress = ip.ToString();
            }
            _IP.IPNetwork = IPNetwork.Parse(_ipAddress + "/" + bits);

            if (!_whiteList.ContainsKey(ip.ToString()))
            {
                _whiteList.Add(ip.ToString(), _IP);
                return true;
            }
            return false;
        }

        /// <summary>
        /// method removes provided ip from the whitelist
        /// </summary>
        /// <param name="ip">IP to remove from whitelist.</param>
        /// <returns>true if ip was removed or else if it did not exist in whitelist</returns>
        public bool RemoveWhiteList(IPAddress ip)
        {
            if (_whiteList.ContainsKey(ip.ToString()))
            {
                _blackList.Remove(ip.ToString());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Method checks if provided ip exist in whitelist.
        /// </summary>
        /// <param name="ip">IP we are checking for in whitelist</param>
        /// <returns>true if it exist or false if it does not exist in whitelist</returns>
        public bool IsWhiteListed(IPAddress ip)
        {
            if (_whiteList.ContainsKey(ip.ToString()))
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
