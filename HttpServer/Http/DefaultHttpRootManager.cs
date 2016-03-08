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

using Feri.MS.Http.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        string _serverRootFolder;          // prevzeta "mapa" od koder se prikazujejo datoteke, če je bila zahtevana pot, ki je ne obdeluje nobena finkcija
        List<string> _serverRootFile = new List<string>();

        HttpServer _server;

        Dictionary<string, IContentSource> _providers = new Dictionary<string, IContentSource>();
        Dictionary<string, ExtensionListener> _extensionListeners = new Dictionary<string, ExtensionListener>();
        Dictionary<string, HttpError> _errorMessages = new Dictionary<string, HttpError>();
        Dictionary<string, object> _listenerActions = new Dictionary<string, object>();

        Dictionary<string, ExtensionListener> _activeListenersCache = new Dictionary<string, ExtensionListener>();

        public DefaultHttpRootManager()
        {
            _serverRootFile.Add("serverDefault.html");    // Preveta datoteka za prikaz
            _serverRootFolder = "SystemHtml";

            // Registracija prevzetih napak
            AddErrorMessage(new HttpError() { ErrorCode = "401", ErrorStatus = "401 Unauthorized", ErrorFile = "SystemHtml/401.html" });
            AddErrorMessage(new HttpError() { ErrorCode = "403", ErrorStatus = "403 Forbidden", ErrorFile = "SystemHtml/403.html" });
            AddErrorMessage(new HttpError() { ErrorCode = "404", ErrorStatus = "404 Not Found", ErrorFile = "SystemHtml/404.html" });
            AddErrorMessage(new HttpError() { ErrorCode = "405", ErrorStatus = "405 Method not allowed", ErrorFile = "SystemHtml/405.html" });
            AddErrorMessage(new HttpError() { ErrorCode = "415", ErrorStatus = "415 Unsupported Media Type", ErrorFile = "SystemHtml/415.html" });
            AddErrorMessage(new HttpError() { ErrorCode = "500", ErrorStatus = "500 Internal Server Error", ErrorFile = "SystemHtml/500.html" });
        }

        #region Lifecycle management
        public void Start(HttpServer server)
        {
            _server = server;
        }

        public void Stop()
        {

        }
        #endregion

        #region Source management
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public bool AddSource(string name, IContentSource provider)
        {
            // registriramo embededcontent, filesystemcontent, ...
            if (!_providers.ContainsKey(name))
            {
                _providers.Add(name, provider);
                _providers[name].Start();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveSource(string name)
        {
            // registriramo embededcontent, filesystemcontent, ...
            if (_providers.ContainsKey(name))
            {
                _providers[name].Stop();
                _providers.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IContentSource GetSource(string name)
        {
            // registriramo embededcontent, filesystemcontent, ...
            if (_providers.ContainsKey(name))
            {
                return _providers[name];
            }
            return null;
        }
        #endregion

        #region Extension listeners management
        public bool AddExtensionListener(string extension, ITemplate template)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            if (!_extensionListeners.ContainsKey(extension))
            {
                _extensionListeners.Add(extension, new ExtensionListener() { Extension = extension, Template = template });
                return true;
            }
            return false;
        }

        public bool RemoveExtensionListener(string extension)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            // Todo: kill all listeners in cache!
            if (_extensionListeners.ContainsKey(extension))
            {
                _extensionListeners.Remove(extension);
                return true;
            }
            return false;
        }

        public ITemplate GetExtensionListener(string extension)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            // Todo: don't allow if alive listeners in cache?
            if (_extensionListeners.ContainsKey(extension))
            {
                return _extensionListeners[extension].Template;
            }
            return null;
        }

        private bool IsRegisteredExtension(string path)
        {
            string[] _tmp = path.Split(new char[] { '.' });
            string _extension;
            if (_tmp.Length > 0)
                _extension = _tmp[_tmp.Length - 1];
            else
                return false;

            ITemplate listener = GetExtensionListener(_extension);
            if (listener == null)
                return false;

            return true;
        }

        private ITemplate AddListenerToCache(string path, string extension)
        {
            lock (_activeListenersCache)
            {
                if (!_activeListenersCache.ContainsKey(path))
                {
                    ITemplate _tmpTemplate = GetExtensionListener(extension);
                    Type _tmpType = _tmpTemplate.GetType();
                    IEnumerable<PropertyInfo> properties = _tmpType.GetTypeInfo().DeclaredProperties;

                    ITemplate _clone = _tmpType.GetTypeInfo().DeclaredConstructors.FirstOrDefault().Invoke(null) as ITemplate;

                    foreach (PropertyInfo _property in properties)
                    {
                        if (_property.CanWrite)
                        {
                            if (_property.Name == "Item")
                            {
                                foreach (string name in _tmpTemplate.Keys)
                                    _clone[name] = _tmpTemplate[name];
                            }
                            else {
                                _property.SetValue(_clone, _property.GetValue(_tmpTemplate, null), null);
                            }
                        }
                    }

                    _clone.LoadString(ReadToByte(path));
                    _activeListenersCache.Add(path, new ExtensionListener() { Extension = extension, Template = _clone });
                    return _clone;
                }
                return null;
            }
        }

        private bool RemoveListenerFromCache(string path)
        {
            lock (_activeListenersCache)
            {
                if (_activeListenersCache.ContainsKey(path))
                {
                    _activeListenersCache.Remove(path);
                    return true;
                }
                return false;
            }
        }

        private void ClearListenersFromCache(string extension)
        {
            lock (_activeListenersCache)
            {
                List<string> _toRemove = new List<string>();
                foreach (KeyValuePair<string, ExtensionListener> pair in _activeListenersCache)
                {
                    if (pair.Value.Extension.Equals(extension))
                    {
                        _toRemove.Add(pair.Key);
                    }
                }
                if (_toRemove.Count > 0)
                {
                    foreach (string _path in _toRemove)
                    {
                        if (_activeListenersCache.ContainsKey(_path))
                            _activeListenersCache.Remove(_path);
                    }
                }
            }
        }

        private ITemplate GetListenerFromCache(string path)
        {
            lock (_activeListenersCache)
            {
                if (_activeListenersCache.ContainsKey(path))
                {
                    return _activeListenersCache[path].Template;
                }
                return null;
            }
        }

        private List<ITemplate> GetListenersFromCache(string extension)
        {
            lock (_activeListenersCache)
            {
                List<ITemplate> _toReturn = new List<ITemplate>();
                foreach (KeyValuePair<string, ExtensionListener> pair in _activeListenersCache)
                {
                    if (pair.Value.Extension.Equals(extension))
                    {
                        _toReturn.Add(pair.Value.Template);
                    }
                }
                return _toReturn;
            }

        }
        #endregion

        #region Default content location
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        public void SetRootPath(string folder)
        {
            //Pot kjer se naj iščejo datoteke
            _serverRootFolder = folder;
        }
        #endregion

        #region Data management
        public string UrlToPath(string url)
        {
            string _path = null;
            bool _found = false;

            foreach (IContentSource provider in _providers.Values)
            {
                if (provider.Containes(url) && (!_found))
                {
                    _path = provider.UrlToPath(url);
                    _found = true;
                }
            }
            return _found ? _path : null;
        }

        public byte[] ReadToByte(string path)
        {
            byte[] _dataArray = null;
            IContentSource _foundSource = null;

            foreach (IContentSource provider in _providers.Values)
            {
                if (provider.Containes(path.ToLower()))
                {
                    _foundSource = provider;
                }
            }
            if (_foundSource != null)
            {
                _dataArray = _foundSource.ReadToByte(path);
                return _dataArray;
            }
            else
            {
                throw new FileNotFoundException("File " + path + " not found in any of the providers");
            }
        }

        public string ReadToString(string path)
        {
            return System.Text.Encoding.UTF8.GetString(ReadToByte(path));
        }

        public List<string> GetNames()
        {
            List<string> _vsePoti = new List<string>();
            foreach (IContentSource provider in _providers.Values)
            {
                foreach (string _pot in provider.GetNames())
                {
                    _vsePoti.Add(_pot);
                }
            }
            if (_vsePoti.Count > 0)
                return _vsePoti;
            else
                return null;
        }

        public bool Containes(string pot)
        {
            List<string> _vsePoti = GetNames();
            if (_vsePoti == null)
                return false;

            bool _found = false;

            foreach (string _pot in _vsePoti)
            {
                if (_pot.ToLower().EndsWith(pot.ToLower().Replace('/', '.')))
                {
                    _found = true;
                }
            }
            return _found;
        }
        #endregion

        #region Error messages
        public bool AddErrorMessage(HttpError error)
        {
            // Da lahko preusmerimo error message na custom errorje.
            if (!_errorMessages.ContainsKey(error.ErrorCode))
            {
                _errorMessages.Add(error.ErrorCode, error);
                return true;
            }
            return false;
        }

        public bool UpdateErrorMessage(HttpError error)
        {
            // Da lahko preusmerimo error message na custom errorje.
            if (_errorMessages.ContainsKey(error.ErrorCode))
            {
                _errorMessages[error.ErrorCode] = error;
                return true;
            }
            return false;

        }

        public HttpError GetErrorMessage(string errorID)
        {
            // Da lahko preusmerimo error message na custom errorje.
            if (_errorMessages.ContainsKey(errorID))
            {
                return _errorMessages[errorID];
            }
            return null;
        }

        public void ReturnErrorMessage(HttpRequest request, HttpResponse response, Dictionary<string, string> headers, string ErrorID)
        {
            byte[] _dataArray = null;
            HttpError error = GetErrorMessage(ErrorID);
            if (error != null)
            {
                _dataArray = ReadToByte(error.ErrorFile);
                response.Write(_dataArray, _server.GetMimeType.GetMimeFromFile(error.ErrorFile), error.ErrorStatus, headers);
            }
            else
            {
                throw new KeyNotFoundException("Cannot find error message for code: " + ErrorID);
            }
            return;
        }

        public void ReturnErrorMessage(HttpRequest request, HttpResponse response, string ErrorID)
        {
            ReturnErrorMessage(request, response, null, ErrorID);
            return;
        }

        public void ReturnErrorMessage(StreamSocket socket, string ErrorID)
        {
            byte[] _dataArray = null;
            HttpResponse _error = null;
            HttpError error = GetErrorMessage(ErrorID);
            if (error != null)
            {
                _error = new HttpResponse(socket.OutputStream.AsStreamForWrite());
                _dataArray = ReadToByte(error.ErrorFile);
                _error.Write(_dataArray, _server.GetMimeType.GetMimeFromFile(error.ErrorFile), error.ErrorStatus);
            }
            else
            {
                throw new KeyNotFoundException("Cannot find error message for code: " + ErrorID);
            }
            return;
        }
        #endregion

        #region Extension listener data
        public bool AddExtensionListenerData(string extension, string actionName, TemplateAction data)
        {
            // Add object to main object and all cache objects
            bool status = true;
            ITemplate listener = GetExtensionListener(extension);
            if (!listener.ContainsAction(actionName))
            {
                listener[actionName] = data;
            }
            else
            {
                status = false;
            }
            foreach (ITemplate _listener in GetListenersFromCache(extension))
            {
                if (!_listener.ContainsAction(actionName))
                {
                    _listener[actionName] = data;
                }
                else
                {
                    status = false;
                }
            }
            return status;
        }

        public bool RemoveExtensionListenerData(string extension, string actionName)
        {
            // remove object from main class and all cache objects
            bool status = true;
            ITemplate listener = GetExtensionListener(extension);
            if (listener.ContainsAction(actionName))
            {
                listener.RemoveAction(actionName);
            }
            else
            {
                status = false;
            }
            foreach (ITemplate _listener in GetListenersFromCache(extension))
            {
                if (_listener.ContainsAction(actionName))
                {
                    _listener.RemoveAction(actionName);
                }
                else
                {
                    status = false;
                }
            }
            return status;
        }

        public TemplateAction GetExtensionListenerData(string extension, string actionName)
        {
            ITemplate listener = GetExtensionListener(extension);
            if (listener.ContainsAction(actionName))
            {
                return listener[actionName];
            }
            else
            {
                return null;
            }
        }

        public bool UpdateExtensionListenerData(string extension, string actionName, TemplateAction data)
        {
            // Update main class and allcache objects
            bool status = true;
            ITemplate listener = GetExtensionListener(extension);
            if (listener.ContainsAction(actionName))
            {
                listener[actionName] = data;
            }
            else
            {
                status = false;
            }
            foreach (ITemplate _listener in GetListenersFromCache(extension))
            {
                if (_listener.ContainsAction(actionName))
                {
                    _listener[actionName] = data;
                }
                else
                {
                    status = false;
                }
            }
            return status;
        }
        #endregion

        #region Listener
        private void ProcessExtensionListener(HttpRequest request, HttpResponse response, string extension)
        {
            // Send HTTP request and response to listener. all other must be handled by the user mannualy.
            ITemplate listener;

            if (GetExtensionListener(extension) != null)
            {
                if ((listener = GetListenerFromCache(request.RequestPath.ToLower())) == null)
                {
                    // Check if file exists in 1st place...
                    if (!Containes(request.RequestPath))
                    {
                        ReturnErrorMessage(request, response, "404");
                        return;
                    }
                    listener = AddListenerToCache(request.RequestPath.ToLower(), extension);
                }
                if (listener == null)
                    throw new InvalidOperationException("Unable to get listener for extension " + extension);

                if (listener.ContainsAction("request"))
                    listener["request"].ObjectData = request;
                else
                    listener["request"] = new TemplateAction() { ObjectData = request };
                if (listener.ContainsAction("response"))
                    listener["response"].ObjectData = response;
                else
                    listener["response"] = new TemplateAction() { ObjectData = response };

                listener.ProcessAction();
                response.Write(listener.GetByte(), "text/html");
                return;
            }
            else
            {
                throw new InvalidOperationException("GetExtensionListener returned null for extension: " + extension);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public void Listen(HttpRequest request, HttpResponse response)
        {
            try
            {
                // Ali je pot v registriranem listenerju in kliči listener ter return ;
                if (IsRegisteredExtension(request.RequestPath.ToLower()))
                {
                    string[] _tmp = request.RequestPath.ToLower().Split(new char[] { '.' });
                    if (_tmp.Length > 0)
                    {
                        ProcessExtensionListener(request, response, _tmp[_tmp.Length - 1]);
                        return;
                    }
                    else
                    {
                        throw new InvalidDataException("Cannot determine extension from " + request.RequestPath);
                    }
                }

                if (request.RequestPath.EndsWith("/"))
                {
                    // Preverimo, ali je v folderju index file.
                    foreach (string file in _serverRootFile)
                    {
                        if (Containes(_serverRootFolder + request.RequestPath + file))
                        {
                            response.Write(ReadToByte(_serverRootFolder + request.RequestPath + file), _server.GetMimeType.GetMimeFromFile(request.RequestPath + file));
                            return;
                        }
                    }
                    // Ni index fajla,  izpišemo folder.
                    /*
                    1) Skopiramo vse poti v začasno datoteko
                    2) vse poti, ki ustrezajo ustrezni mapi, skopiramo in pripravimo za izpis
                    3) če ne najdemo, je treba izpisat 404.
                    */
                    List<string> _ustreznePoti = new List<string>();

                    foreach (string _pot in GetNames())
                    {
                        if (_pot.ToLower().Contains((_serverRootFolder + request.RequestPath).ToLower().Replace('/', '.')))
                        {
                            // Dodamo samo pravilne url-je za trenutno mapo, brez polne poti in v pravilni obliki.
                            int cut = _pot.ToLower().Split(new string[] { _serverRootFolder.ToLower() + request.RequestPath.ToLower().Replace('/', '.') }, StringSplitOptions.None)[1].Length;
                            string _tmpPath = _pot.Replace('.', '/');
                            int Place = _tmpPath.LastIndexOf("/");
                            _tmpPath = _tmpPath.Remove(Place, 1).Insert(Place, ".");
                            if (!_tmpPath.Substring(_tmpPath.Length - cut).Contains("/"))
                                _ustreznePoti.Add(_tmpPath.Substring(_tmpPath.Length - cut));
                        }
                    }
                    if (_ustreznePoti.Count > 0)
                    {
                        SimpleTemplate _template = new SimpleTemplate();
                        _template.LoadString(ReadToByte("SystemHtml/templateFolderListing.html"));
                        _template.SafeMode = false;

                        StringBuilder rezultat = new StringBuilder();
                        foreach (string _pot in _ustreznePoti)
                        {
                            rezultat.Append("<a href=\"" + _pot + "\">" + _pot + "</a><br>\n");
                        }

                        _template["path"] = new TemplateAction() { Pattern = "PATH", Data = request.RequestPath };
                        _template["content"] = new TemplateAction() { Pattern = "CONTENT", Data = rezultat.ToString() };
                        _template.ProcessAction();
                        response.Write(_template.GetByte(), "text/html");
                        return;
                    }
                    else
                    {
                        ReturnErrorMessage(request, response, "404");
                        return;
                    }
                }
                else
                {
                    /*
                    Ni folder, izpišemo zahtevano datoteko.
                    */
                    if (Containes(_serverRootFolder + request.RequestPath))
                    {
                        response.Write(ReadToByte(_serverRootFolder + request.RequestPath), _server.GetMimeType.GetMimeFromFile(request.RequestPath));
                        return;
                    }
                    else
                    {
                        ReturnErrorMessage(request, response, "404");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                response.Write(e);
            }
        }
        #endregion
    }
}
