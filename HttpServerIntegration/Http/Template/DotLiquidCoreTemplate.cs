using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Feri.MS.Http.Template;
using Feri.MS.Http;

/// <summary>
/// Optional helper classes for HttpServer, that integrate it with DotLiquidCore library.
/// </summary>
namespace HttpServer.Integration.Http.Template
{
    class DotLiquidCoreTemplate : ITemplate
    {
        public bool AddAction(string name, string pattern, string data)
        {
            throw new NotImplementedException();
        }

        public bool DeleteAction(string name)
        {
            throw new NotImplementedException();
        }

        public byte[] GetByte()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public void LoadString(byte[] data)
        {
            throw new NotImplementedException();
        }

        public void LoadString(string data)
        {
            throw new NotImplementedException();
        }

        public void ProcessAction()
        {
            throw new NotImplementedException();
        }

        public bool UpdateAction(string name, string data)
        {
            throw new NotImplementedException();
        }
        public bool UpdateAction(HttpRequest request, HttpResponse response)
        {
            throw new NotImplementedException();
        }
    }
}
