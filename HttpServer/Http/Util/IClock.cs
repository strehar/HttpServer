using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http.Util
{
    public interface IClock
    {
        bool Ready();
        DateTime GetTime();
        void SetTime(DateTime time);
        void Enable(bool enable);
    }
}
