using System;
using Windows.System.Threading;

namespace Feri.MS.Http.Timer
{
    /// <summary>
    /// Timer object, contains information about timers and takes care of creating it. It is used by HttpServer class.
    /// </summary>
    public class HttpTimer : IDisposable
    {
        string _ime;
        int _milisekunde;
        ThreadPoolTimer _timer;
        internal bool _debug = false;
        public delegate void timerEvent();

        timerEvent _timerEvent;

        /// <summary>
        /// When creating timer, we need to provide all information needed.
        /// </summary>
        /// <param name="ime">Name of timer</param>
        /// <param name="milisekunde">Time in miliseconds</param>
        /// <param name="ping">Listener to call</param>
        public HttpTimer(string ime, int milisekunde, timerEvent ping)
        {
            _timerEvent = ping;
            _ime = ime;
            if (milisekunde < 2)
                milisekunde = 2;
            _timer = ThreadPoolTimer.CreatePeriodicTimer(TimerHandler, TimeSpan.FromMilliseconds(milisekunde));
            _milisekunde = milisekunde;
        }

        public void TimerHandler(ThreadPoolTimer timer)
        {
            _timerEvent();
        }

        public void StopTimer()
        {
            _timer.Cancel();
        }

        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            _timer.Cancel();
        }
        #endregion
    }
}
