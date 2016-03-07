using Feri.MS.Http.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Feri.MS.Http
{
    internal class ExtensionListener
    {
        internal string Extension { get; set; }
        internal ITemplate Template { get; set; }
    }
    class DefaultHttpRootManager : IHttpRootManager
    {
        string _serverRootFolder = "SystemHtml";          // prevzeta "mapa" od koder se prikazujejo datoteke, če je bila zahtevana pot, ki je ne obdeluje nobena finkcija
        List<string> _serverRootFile = new List<string>();

        HttpServer _server;

        Dictionary<string, IContentSource> _providers = new Dictionary<string, IContentSource>();
        Dictionary<string, ExtensionListener> _extensionListeners = new Dictionary<string, ExtensionListener>();


        public DefaultHttpRootManager()
        {
            _serverRootFile.Add("serverDefault.html");    // Preveta datoteka za prikaz
        }

        public void Start(HttpServer server)
        {
            _server = server;
        }
        public void Stop()
        {

        }
        public bool AddSource(string name, IContentSource provider)
        {
            if (!_providers.ContainsKey(name))
            {
                _providers.Add(name, provider);
                return true;
            }
            return false;
            // registriramo embededcontent, filesystemcontent, ...
        }
        public bool RemoveSource(string name)
        {
            return true;
            // registriramo embededcontent, filesystemcontent, ...
        }
        public IContentSource GetSource(string name)
        {
            // registriramo embededcontent, filesystemcontent, ...
            return null;
        }
        public bool AddExtensionListener(string name, string extension, ITemplate template)
        {
            return true;
            // recimo listener za DotLiquid templating engine na .chtml
        }
        public bool RemoveExtensionListener(string name)
        {
            return true;
            // recimo listener za DotLiquid templating engine na .chtml
        }
        public ITemplate GetExtensionListener(string name)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            return null;
        }
        public void SetIndex(string[] index)
        {
            _serverRootFile.Clear();
            // index.html, index,chtml, ...
            foreach (string file in index)
            {
                if (file.StartsWith("/"))
                    _serverRootFile.Add(file.Replace("/", ""));
                else
                    _serverRootFile.Add(file);
            }
        }

        public void SetRootPath(string folder)
        {
            //Pot kjer se naj iščejo datoteke
            _serverRootFolder = folder;
        }

        public void SetErrorMessage(string errorID, string fileToDisplay)
        {
            // Da lahko preusmerimo error message na custom errorje.
        }

        public void GetErrorMessage(string errorID, string fileToDisplay)
        {
            // Da lahko preusmerimo error message na custom errorje.
        }

        public void SendError(HttpRequest request, HttpResponse response, string ErrorID)
        {

        }

        public void SendError(StreamSocket socket, string ErrorID)
        {

        }

        public void Listen(HttpRequest request, HttpResponse response)
        {
            byte[] _dataArray = null;
            string pot = null;
            IContentSource _foundSource = null;



            if (request.RequestPath.EndsWith("/"))
            {
                foreach (string file in _serverRootFile)
                {
                    foreach (IContentSource provider in _providers.Values)
                    {
                        if (provider.Containes(_serverRootFolder + request.RequestPath.ToLower() + file))
                        {
                            pot = _serverRootFolder + request.RequestPath.ToLower() + file;
                            _foundSource = provider;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(pot))
                {
                    _dataArray = _foundSource.ReadToByte(pot);
                    response.Write(_dataArray, _server.GetMimeType.GetMimeFromFile(pot));
                    return;
                }
                else
                {
                    List<string> _vsePoti = new List<string>();
                    List<string> _ustreznePoti = new List<string>();
                    foreach (IContentSource provider in _providers.Values)
                    {
                        foreach (string _pot in provider.GetNames())
                        {
                            _vsePoti.Add(_pot);
                        }
                    }
                    foreach (string _pot in _vsePoti)
                    {
                        if (_pot.ToLower().Contains(_serverRootFolder.ToLower() + request.RequestPath.ToLower().Replace('/', '.')))
                        {
                            // Popravi, da bo samo prabilni URL!!!
                            _ustreznePoti.Add(_pot);
                        }
                    }
                    if (_ustreznePoti.Count >= 1)
                    {
                        StringBuilder rezultat = new StringBuilder();
                        rezultat.Append("<html><head><title>Index of: " + request.RequestPath + "</title></head><body><h1>Index of: " + request.RequestPath + "<hr>");
                        foreach (string _pot in _ustreznePoti)
                        {
                            rezultat.Append("<a href=\"" + _pot + "\">" + _pot + "</a><br>");
                        }
                        rezultat.Append("</body></html>");
                        response.Write(System.Text.Encoding.UTF8.GetBytes(rezultat.ToString()), "text/html");
                        return;
                    }
                    else
                    {
                        //404
                        _dataArray = _server.EmbeddedContent.ReadToByte("SystemHtml/404.html");
                        response.Write(_dataArray, _server.GetMimeType.GetMimeFromFile(pot), "404 Not Found");
                        return;
                    }
                    // Nismo našli novenega indexa v tej poti, izpišemo folder listing....
                    /*
                    1) pogledamo, če ima kateri provider folder, ki se konča z zahtevano potjo
                    2) če je, izpišemo vse datoteke, ki imajo to pot v imenu, za vse provijderje.
                    3) če ne najdemo, je treba izpisat 404.
                    */
                }
            }
            else
            {
                //pot = _serverRootFolder + "/" + request.RequestPath;
                foreach (IContentSource provider in _providers.Values)
                {
                    if (provider.Containes(_serverRootFolder + request.RequestPath.ToLower()))
                    {
                        pot = _serverRootFolder + request.RequestPath.ToLower();
                        _foundSource = provider;
                    }
                }
                if (!string.IsNullOrEmpty(pot))
                {
                    _dataArray = _foundSource.ReadToByte(pot);
                    response.Write(_dataArray, _server.GetMimeType.GetMimeFromFile(pot));
                    return;
                }
                else
                {
                    // TODO: fix so it checks with registered erors.
                    _dataArray = _server.EmbeddedContent.ReadToByte("SystemHtml/404.html");
                    response.Write(_dataArray, _server.GetMimeType.GetMimeFromFile(pot), "404 Not Found");
                    return;
                }
            }
        }
    }
}
