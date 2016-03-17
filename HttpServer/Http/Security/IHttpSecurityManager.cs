using System.Net;

namespace Feri.MS.Http.Security
{
    interface IHttpSecurityManager
    {
        bool Enabled { get; set; }
        int FirstStageBanTimerMinutes { get; set; }
        int SecondStageBanTimerMinutes { get; set; }
        bool SetDebug { get; set; }

        void AuthenticatedAccess(HttpRequest request, HttpResponse response);
        void BanTimer();
        bool IsBanned(IPAddress address);
        void Start(HttpServer server);
        void Stop();
        void UnauthenticatedAccess(HttpRequest request, HttpResponse response);
    }
}