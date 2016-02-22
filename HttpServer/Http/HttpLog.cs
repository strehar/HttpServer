using System.Diagnostics;

namespace Feri.MS.Http
{
    /// <summary>
    /// Class is used to log information from library. Currently it only writes debug from information recived.
    /// </summary>
    public class HttpLog
    {
        public string Pot { get; set; }
        public bool InMemory { get; set; }

        public bool _debug = false;

        public void Open()
        {
            return;
        }

        public void Close()
        {
            return;
        }

        public void WriteLine(string niz)
        {
            Debug.WriteLineIf(_debug, niz);
            return;
        }
    }
}
