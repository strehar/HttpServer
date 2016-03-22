using Feri.MS.Http.Util;
using Feri.MS.Parts.I2C.RealTimeClock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http.Util
{
    public class RTCClock : IClock
    {
        public void Enable(bool enable)
        {
            throw new NotImplementedException();
        }

        private DS1307 GetClock()
        {
            return DS1307.Create();
        }

        public DateTime GetTime()
        {
            return GetClock().GetTime();
        }

        public bool Ready()
        {
            return GetClock().Ready();
        }

        public void SetTime(DateTime time)
        {
            GetClock().SetTime(time);
        }
    }
}
