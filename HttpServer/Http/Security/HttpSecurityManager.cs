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

using Feri.MS.Http.Util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Feri.MS.Http.Security
{
    class HttpSecurityManager : IHttpSecurityManager
    {
        Dictionary<string, HttpSecurityManagerData> _firstStage = new Dictionary<string, HttpSecurityManagerData>();
        Dictionary<string, HttpSecurityManagerData> _secondStage = new Dictionary<string, HttpSecurityManagerData>();

        HttpServer _server;

        public int FirstStageBanTimerMinutes { get; set; } = 5;
        public int SecondStageBanTimerMinutes { get; set; } = 60;
        public bool Enabled { get; set; } = true;
        List<string> _toRemoveFirstStage = new List<string>();
        List<string> _toRemoveSecondStage = new List<string>();
        bool _originalIPFilterState;
        public bool SetDebug { get; set; } = false;

        public bool IsBanned(IPAddress address)
        {
            bool __found = false;
            lock (_firstStage) lock (_secondStage)
                {

                    foreach (string key in _firstStage.Keys)
                    {
                        if (key.Equals(address.ToString()))
                            __found = true;
                    }
                    foreach (string key in _secondStage.Keys)
                    {
                        if (key.Equals(address.ToString()))
                            __found = true;
                    }
                }
            return __found;
        }

        public void Start(HttpServer server)
        {
            _server = server;
            _originalIPFilterState = _server.IPFilterEnabled;
            if (!_server.IPFilterEnabled) _server.IPFilterEnabled = true;
        }

        public void Stop()
        {
            _server.IPFilterEnabled = _originalIPFilterState;
        }

        public void UnauthenticatedAccess(HttpRequest request, HttpResponse response)
        {
            if (!Enabled)
                return;
            if (!_server.IPFilterEnabled) _server.IPFilterEnabled = true;
            lock (_firstStage) lock (_secondStage)
                {
                    if (_firstStage.ContainsKey(request.HttpConnection.RemoteHost))
                    {
                        // user is on 1st stage list, increment counter. if couner is greater then max, ban user move him to the second stage
                        if (_firstStage[request.HttpConnection.RemoteHost].Counter >= 5)
                        {
                            // TODO: BAN                 
                            if (_firstStage[request.HttpConnection.RemoteHost].IPAddress.AddressFamily == AddressFamily.InterNetwork)
                                _server.IPFilter.AddBlackList(_firstStage[request.HttpConnection.RemoteHost].IPAddress, 32);
                            else
                                _server.IPFilter.AddBlackList(_firstStage[request.HttpConnection.RemoteHost].IPAddress, 128);

                            _firstStage[request.HttpConnection.RemoteHost].Time = TimeProvider.GetTime().AddMinutes(FirstStageBanTimerMinutes);
                            Debug.WriteLineIf(SetDebug, "First stage ban ip " + request.HttpConnection.RemoteHost + " on " + (_firstStage[request.HttpConnection.RemoteHost].Counter + 1) + " try. Time is " + _firstStage[request.HttpConnection.RemoteHost].Time + ".");
                            _firstStage[request.HttpConnection.RemoteHost].Counter = 0;
                            _firstStage[request.HttpConnection.RemoteHost].FromStage = 0;
                        }
                        else
                        {
                            _firstStage[request.HttpConnection.RemoteHost].Counter++;
                            Debug.WriteLineIf(SetDebug, "First stage ban counter for ip " + request.HttpConnection.RemoteHost + " incremented to " + _firstStage[request.HttpConnection.RemoteHost].Counter + ".");
                        }
                    }
                    else if (_secondStage.ContainsKey(request.HttpConnection.RemoteHost))
                    {
                        // user is on 2nd stage, increment counter. if counter is greater then max, ban user for temp time.
                        if (_secondStage[request.HttpConnection.RemoteHost].Counter >= 5)
                        {
                            // TODO: BAN
                            if (_secondStage[request.HttpConnection.RemoteHost].IPAddress.AddressFamily == AddressFamily.InterNetwork)
                                _server.IPFilter.AddBlackList(_secondStage[request.HttpConnection.RemoteHost].IPAddress, 32);
                            else
                                _server.IPFilter.AddBlackList(_secondStage[request.HttpConnection.RemoteHost].IPAddress, 128);

                            _secondStage[request.HttpConnection.RemoteHost].Time = TimeProvider.GetTime().AddMinutes(SecondStageBanTimerMinutes);
                            Debug.WriteLineIf(SetDebug, "Second stage ban ip " + request.HttpConnection.RemoteHost + " on " + _secondStage[request.HttpConnection.RemoteHost].Counter + " try. Time is " + _secondStage[request.HttpConnection.RemoteHost].Time + ".");
                            _secondStage[request.HttpConnection.RemoteHost].Counter = 0;
                        }
                        else
                        {
                            _secondStage[request.HttpConnection.RemoteHost].Counter++;
                            Debug.WriteLineIf(SetDebug, "Second stage ban counter ip " + request.HttpConnection.RemoteHost + " incremented to " + _secondStage[request.HttpConnection.RemoteHost].Counter + ".");
                        }
                    }
                    else
                    {
                        // User is on no list, add it to 1st stage list
                        _firstStage.Add(request.HttpConnection.RemoteHost, new HttpSecurityManagerData() { Counter = 0, IPAddress = IPAddress.Parse(request.HttpConnection.RemoteHost), Time = TimeProvider.GetTime().AddMinutes(FirstStageBanTimerMinutes) });
                        Debug.WriteLineIf(SetDebug, "Adding ban counter for ip " + request.HttpConnection.RemoteHost + " setting it to " + _firstStage[request.HttpConnection.RemoteHost].Counter + ". Time is " + _firstStage[request.HttpConnection.RemoteHost].Time + ".");
                    }
                }
        }

        public void AuthenticatedAccess(HttpRequest request, HttpResponse response)
        {
            if (!Enabled)
                return;
            if (!_server.IPFilterEnabled) _server.IPFilterEnabled = true;
            lock (_firstStage) lock (_secondStage)
                {
                    Debug.WriteLineIf(SetDebug, "Authentication OK from ip " + request.HttpConnection.RemoteHost + " removing from first and second stage.");
                    if (_firstStage.ContainsKey(request.HttpConnection.RemoteHost))
                        _firstStage.Remove(request.HttpConnection.RemoteHost);
                    if (_secondStage.ContainsKey(request.HttpConnection.RemoteHost))
                        _secondStage.Remove(request.HttpConnection.RemoteHost);
                }
        }

        public void BanTimer()
        {
            if (!Enabled)
                return;
            if (!_server.IPFilterEnabled) _server.IPFilterEnabled = true;
            lock (_firstStage) lock (_secondStage)
                {
                    // check the ban timer lists for any expired timers. if they are, remove them form those lists and from ipfilter's ban
                    _toRemoveFirstStage.Clear();
                    _toRemoveSecondStage.Clear();
                    foreach (KeyValuePair<string, HttpSecurityManagerData> pair in _firstStage)
                    {
                        if (pair.Value.Time.CompareTo(TimeProvider.GetTime()) < 0)
                        {
                            _toRemoveFirstStage.Add(pair.Key);
                        }
                    }
                    foreach (KeyValuePair<string, HttpSecurityManagerData> pair in _secondStage)
                    {
                        if (pair.Value.Time.CompareTo(TimeProvider.GetTime()) < 0)
                        {
                            _toRemoveSecondStage.Add(pair.Key);
                        }
                    }
                    foreach (string key in _toRemoveFirstStage)
                    {
                        if (_firstStage[key].FromStage == 0)
                        {
                            _server.IPFilter.RemoveBlackList(_firstStage[key].IPAddress);

                            _secondStage.Add(key, _firstStage[key]);
                            _secondStage[key].Time = TimeProvider.GetTime().AddMinutes(SecondStageBanTimerMinutes);
                            Debug.WriteLineIf(SetDebug, "Removing first stage ban for ip " + key + ". Moving to stage 2. Time is " + _secondStage[key].Time + ".");
                            _secondStage[key].Counter = 0;
                            _secondStage[key].FromStage = 1;
                            _firstStage.Remove(key);
                        }
                        else
                        {
                            _server.IPFilter.RemoveBlackList(_firstStage[key].IPAddress);

                            Debug.WriteLineIf(SetDebug, "Removing first stage ban for ip " + _firstStage[key].IPAddress.ToString() + ".");
                            _server.IPFilter.RemoveBlackList(_firstStage[key].IPAddress);
                            _firstStage.Remove(key);
                        }
                    }
                    foreach (string key in _toRemoveSecondStage)
                    {
                        _server.IPFilter.RemoveBlackList(_secondStage[key].IPAddress);

                        _firstStage.Add(key, _secondStage[key]);
                        _firstStage[key].Time = TimeProvider.GetTime().AddMinutes(FirstStageBanTimerMinutes);
                        _firstStage[key].Counter = 0;
                        _firstStage[key].FromStage = 2;
                        Debug.WriteLineIf(SetDebug, "Removing second stage ban for ip " + _secondStage[key].IPAddress.ToString() + ", moving it to first stage, setting counter to " + _secondStage[key].Counter + ". Time is " + _secondStage[key].Time + ".");
                        _secondStage.Remove(key);
                    }
                }
        }
    }
}
