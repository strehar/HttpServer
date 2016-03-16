using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http.Security
{
    internal class HttpSecurityManagerData
    {
        internal byte[] IPAddress { get; set; }
        internal DateTime Time { get; set; }
        internal int Counter { get; set; } = 0;
    }
}
