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

using Feri.MS.Parts.I2C.RealTimeClock;
using System;

namespace Feri.MS.Http.Util
{
    /// <summary>
    /// Class checks if there is RTC prsent and provides time from it. If it is not found, it returns the system time (can be problematic on boards without built in RTC)
    /// TODO: check for RTC and reading time from it. Do it so it does not depend on GPIO library.
    /// </summary>
    public class TimeProvider
    {
        static bool _RTCPresent = false;
        /// <summary>
        /// 
        /// </summary>
        static TimeProvider()
        {
            CheckForRTC();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static DS1307 GetRTC()
        {
            return DS1307.Create();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CheckForRTC()
        {
            DS1307 _rtc = DS1307.Create();
            try
            {
                _rtc.GetTime();
                _RTCPresent = true;
            }
            catch
            {
                _RTCPresent = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static DateTime GetTime()
        {
            if (_RTCPresent)
            {
                DS1307 _rtc = DS1307.Create();
                if (_rtc.Ready())
                {
                    DateTime _time = _rtc.GetTime();
                    return _time.ToUniversalTime();
                }
                else
                {
                    return DateTime.UtcNow;
                }
            }
            else
            {
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public static void SetTime(DateTime time)
        {
            if (_RTCPresent)
            {
                DS1307 _rtc = DS1307.Create();
                _rtc.SetTime(time);
            }
            else
            {
                //SET System time. somehow.
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RegisterTimeSource()
        {

        }
    }
}
