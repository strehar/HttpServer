using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feri.MS.Http
{
    public interface IContentSource
    {
        bool EnableDebug { get; set; }

        void Start();
        void Stop();
        string UrlToPath(string url);
        byte[] ReadToByte(string pot);
        string ReadToString(string path);
        List<string> GetNames();
        bool Containes(string pot);
    }
}
