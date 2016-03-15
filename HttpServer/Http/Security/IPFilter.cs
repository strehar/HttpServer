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
            HostName remoteHost = socket.Information.RemoteAddress;
            string[] _remoteHostString = remoteHost.ToString().Split('.');
            if (_remoteHostString.Length < 4)
            {
                // IP ni ok. nekaj je treba narediti...
            }
            byte[] _remoteIP = new byte[4];
            for (int i = 0; i < _remoteIP.Length; i++)
            {
                if (!byte.TryParse(_remoteHostString[i], out _remoteIP[i]))
                {
                    // konverzija ni uspela, spet je treba nekaj naredit.
                }
            }

            // process each IP from black and white list. if remote IP is not any of the black lists and if on atleast one white list then return ok, else return true for blocked.
            // 1st check whitelist, then blacklist, so blackist can override whitelist
            bool _result = true;
            if (_whiteList.Count > 0)
            {
                foreach (KeyValuePair<string, IpNumber> key in _whiteList)
                {
                    if (IpInRange(_remoteIP, key.Value))
                        _result = false;
                }
            }
            else
                _result = false;

            foreach (KeyValuePair<string, IpNumber> key in _blackList)
            {
                if (IpInRange(_remoteIP, key.Value))
                    _result = true;
            }

            return _result;

        }

        /// <summary>
        /// Internal helper method to check if provided ip is in range of provided filter.
        /// </summary>
        /// <param name="ip">IP address we are checking in byte format.</param>
        /// <param name="filter">Ip filter (IP range) we are checking against</param>
        /// <returns>true if ip is in range, false if it is not.</returns>
        private bool IpInRange(byte[] ip, IpNumber filter)
        {
            bool _result = true;
            for (int i = 0; i < ip.Length; i++)
            {
                if ((ip[i] >= filter.IPAddressLower[i]) && (ip[i] <= filter.IPAddressUpper[i]))
                {
                    //
                }
                else
                {
                    _result = false;
                }
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
            IpNumber _IP = GetIpRangeFromIpAddress(ip, bits);

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
            if (!_blackList.ContainsKey(ip.ToString()))
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
            if (!_blackList.ContainsKey(ip.ToString()))
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
            IpNumber _IP = GetIpRangeFromIpAddress(ip, bits);

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
            if (!_whiteList.ContainsKey(ip.ToString()))
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
            if (!_whiteList.ContainsKey(ip.ToString()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// internal helper method that calculates upper and lower ip address of ip range, from provided IP address and nework bit mask.
        /// ip if provided as IPAddress class and mask is provided as bits (for example: 0, 8, 16, 24, 32, ...)
        /// </summary>
        /// <param name="ip">IP to be used for range caclulation</param>
        /// <param name="bits">Network bits to be used for range calculation</param>
        /// <returns>IPNumber filter object contain provided ip address and upper and lowe ranges of the network</returns>
        private IpNumber GetIpRangeFromIpAddress(IPAddress ip, int bits)
        {
            uint _IPMask;
            if (bits != 32)
                _IPMask = ~(uint.MaxValue >> bits);
            else
                _IPMask = uint.MaxValue;

            byte[] _ipBytes = ip.GetAddressBytes();

            byte[] _maskBytes;
            if (BitConverter.IsLittleEndian)
                _maskBytes = BitConverter.GetBytes(_IPMask).Reverse().ToArray();
            else
                _maskBytes = BitConverter.GetBytes(_IPMask).ToArray();

            byte[] _lowerIPBytes = new byte[_ipBytes.Length];
            byte[] _upperIPBytes = new byte[_ipBytes.Length];
            for (int i = 0; i < _ipBytes.Length; i++)
            {
                _lowerIPBytes[i] = (byte)(_ipBytes[i] & _maskBytes[i]);
                _upperIPBytes[i] = (byte)(_ipBytes[i] | ~_maskBytes[i]);
            }

            IpNumber _IP = new IpNumber();
            _IP.IPAddress = _ipBytes;
            _IP.IPAddressLower = _lowerIPBytes;
            _IP.IPAddressUpper = _upperIPBytes;
            return _IP;
        }
        #endregion
    }
}
