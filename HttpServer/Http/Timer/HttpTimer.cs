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
        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timer"></param>
        public void TimerHandler(ThreadPoolTimer timer)
        {
            _timerEvent();
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopTimer()
        {
            _timer.Cancel();
        }

        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _timer.Cancel();
        }
        #endregion
    }
}
