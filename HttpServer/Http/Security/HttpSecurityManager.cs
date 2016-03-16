using Feri.MS.Http.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http.Security
{
    class HttpSecurityManager
    {
        Dictionary<string, HttpSecurityManagerData> _firstStage = new Dictionary<string, HttpSecurityManagerData>();
        Dictionary<string, HttpSecurityManagerData> _secondStage = new Dictionary<string, HttpSecurityManagerData>();

        HttpServer _server;

        public int FirstStageBanTimerMinutes { get; set; }  
        public int SecondStageBanTimerMinutes { get; set; } 
        public bool Enabled { get; set; } = true;

        public bool IsBanned(IPAddress address)
        {
            return false;
        }

        public void Start(HttpServer server)
        {
            _server = server;
        }

        public void Stop()
        {

        }
        private IPAddress StringToIPAddress(string address)
        {
            string[] parts = address.Split(new char[] { '.' });
            byte[] byteParts = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                byteParts[i] = byte.Parse(parts[i]);
            }
            IPAddress tempAddress = new IPAddress(byteParts);
            return tempAddress;
        }

        public bool UnauthenticatedAccess(HttpRequest request, HttpResponse response)
        {
            if (_firstStage.ContainsKey(request.HttpConnection.RemoteHost))
            {
                if (_firstStage[request.HttpConnection.RemoteHost].Counter >= 5)
                {
                    // TODO: BAN
                    _secondStage.Add(request.HttpConnection.RemoteHost, _firstStage[request.HttpConnection.RemoteHost]);
                    _secondStage[request.HttpConnection.RemoteHost].Time = TimeProvider.GetTime().AddMilliseconds(SecondStageBanTimerMinutes);
                    _secondStage[request.HttpConnection.RemoteHost].Counter = 0;
                    _firstStage.Remove(request.HttpConnection.RemoteHost);
                }
                else
                {
                    // Preverimo, če je potekel čas. Če je, potem zbrišemo. sicer povečamo counter.
                    if (_firstStage[request.HttpConnection.RemoteHost].Time.CompareTo(TimeProvider.GetTime()) < 0)
                    {

                    }
                    else {
                        _firstStage[request.HttpConnection.RemoteHost].Counter++;
                    }
                }
                // user is on 1st stage list, increment counter. if couner is greater then max, ban user move him to the second stage
            }
            else if (_secondStage.ContainsKey(request.HttpConnection.RemoteHost))
            {
                if (_secondStage[request.HttpConnection.RemoteHost].Counter >= 5)
                {
                    // TODO: BAN
                    _secondStage[request.HttpConnection.RemoteHost].Time = TimeProvider.GetTime().AddMilliseconds(SecondStageBanTimerMinutes);
                    _secondStage[request.HttpConnection.RemoteHost].Counter = 0;
                }
                else
                {
                    _secondStage[request.HttpConnection.RemoteHost].Counter++;
                }
                // user is on 2nd stage, increment counter. if counter is greater then max, ban user for temp time.
            }
            else if (_banExpired.ContainsKey(request.HttpConnection.RemoteHost))
            {
                // user's ban expired, but ban timer clear didn't yet, user get's banned and moved to 2nd stage.
            }
            else
            {
                _firstStage.Add(request.HttpConnection.RemoteHost, new HttpSecurityManagerData() { Counter = 0, IPAddress = StringToIPAddress(request.HttpConnection.RemoteHost).GetAddressBytes(), Time = TimeProvider.GetTime().AddMilliseconds(FirstStageBanTimerMinutes) });
                // User is on no list, add it to 1st stage list
            }
            StringToIPAddress(request.HttpConnection.RemoteHost);

            return false;
        }

        public void AuthenticatedAccess(HttpRequest request, HttpResponse response)
        {
            if (_firstStage.ContainsKey(request.HttpConnection.RemoteHost))
                _firstStage.Remove(request.HttpConnection.RemoteHost);
            if (_secondStage.ContainsKey(request.HttpConnection.RemoteHost))
                _secondStage.Remove(request.HttpConnection.RemoteHost);
            if (_banExpired.ContainsKey(request.HttpConnection.RemoteHost))
                _banExpired.Remove(request.HttpConnection.RemoteHost);
        }

        public void BanTimer()
        {
            // check the ban timer lists for any expired timers. if they are, remove them form those lists and from ipfilter's ban
        }
    }
}
