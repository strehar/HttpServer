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

using Feri.MS.Http.ContentSource;
using Feri.MS.Http.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Windows.Networking.Sockets;

namespace Feri.MS.Http.RootManager
{
    internal class ExtensionTemplate
    {
        internal string Extension { get; set; }
        internal ITemplate Template { get; set; }
    }
    class DefaultHttpRootManager : IHttpRootManager
    {
        string _serverRootFolder;          // prevzeta "mapa" od koder se prikazujejo datoteke, če je bila zahtevana pot, ki je ne obdeluje nobena finkcija
        List<string> _serverRootFile = new List<string>();

        HttpServer _server;

        Dictionary<string, IContentSource> _sources = new Dictionary<string, IContentSource>();
        Dictionary<string, ExtensionTemplate> _extensionTemplates = new Dictionary<string, ExtensionTemplate>();
        Dictionary<string, HttpError> _errorMessages = new Dictionary<string, HttpError>();
        Dictionary<string, ExtensionTemplate> _activeTemplatesCache = new Dictionary<string, ExtensionTemplate>();

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
        public bool AddSource(IContentSource provider, string name = null)
        {
            // registriramo embededcontent, filesystemcontent, ...
            if (string.IsNullOrEmpty(name))
                name = provider.SourceName;
            if (!_sources.ContainsKey(name))
            {
                _sources.Add(name, provider);
                _sources[name].Start();
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
            if (_sources.ContainsKey(name))
            {
                _sources[name].Stop();
                _sources.Remove(name);
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
            if (_sources.ContainsKey(name))
            {
                return _sources[name];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IContentSource> GetAllSources()
        {
            return _sources;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReloadSourceFileList(string name = null)
        {
            if (name == null)
            {
                foreach (KeyValuePair<string, IContentSource> pair in _sources)
                {
                    pair.Value.ReloadFileList();
                }
            }
            else
            {
                GetSource(name).ReloadFileList();
            }
        }
        #endregion

        #region Extension listeners management
        public bool AddExtension(string extension, ITemplate template)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            if (!_extensionTemplates.ContainsKey(extension))
            {
                _extensionTemplates.Add(extension, new ExtensionTemplate() { Extension = extension, Template = template });
                return true;
            }
            return false;
        }

        public bool RemoveExtension(string extension)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            // Todo: kill all listeners in cache!
            if (_extensionTemplates.ContainsKey(extension))
            {
                _extensionTemplates.Remove(extension);
                return true;
            }
            return false;
        }

        public ITemplate GetExtension(string extension)
        {
            // recimo listener za DotLiquid templating engine na .chtml
            // Todo: don't allow if alive listeners in cache?
            if (_extensionTemplates.ContainsKey(extension))
            {
                return _extensionTemplates[extension].Template;
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

            ITemplate listener = GetExtension(_extension);
            if (listener == null)
                return false;

            return true;
        }

        private ITemplate CacheAddExtension(string path, string extension)
        {
            lock (_activeTemplatesCache)
            {
                if (!_activeTemplatesCache.ContainsKey(path))
                {
                    ITemplate _tmpTemplate = GetExtension(extension);
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
                    _activeTemplatesCache.Add(path, new ExtensionTemplate() { Extension = extension, Template = _clone });
                    return _clone;
                }
                return null;
            }
        }

        private bool CacheRemoveExtension(string path)
        {
            lock (_activeTemplatesCache)
            {
                if (_activeTemplatesCache.ContainsKey(path))
                {
                    _activeTemplatesCache.Remove(path);
                    return true;
                }
                return false;
            }
        }

        private void CacheClearExtension(string extension)
        {
            lock (_activeTemplatesCache)
            {
                List<string> _toRemove = new List<string>();
                foreach (KeyValuePair<string, ExtensionTemplate> pair in _activeTemplatesCache)
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
                        if (_activeTemplatesCache.ContainsKey(_path))
                            _activeTemplatesCache.Remove(_path);
                    }
                }
            }
        }

        private ITemplate CacheGetTemplate(string path)
        {
            lock (_activeTemplatesCache)
            {
                if (_activeTemplatesCache.ContainsKey(path))
                {
                    return _activeTemplatesCache[path].Template;
                }
                return null;
            }
        }

        private List<ITemplate> CacheGetExtensions(string extension)
        {
            lock (_activeTemplatesCache)
            {
                List<ITemplate> _toReturn = new List<ITemplate>();
                foreach (KeyValuePair<string, ExtensionTemplate> pair in _activeTemplatesCache)
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

            foreach (IContentSource provider in _sources.Values)
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

            foreach (IContentSource provider in _sources.Values)
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
            foreach (IContentSource provider in _sources.Values)
            {
                foreach (string _pot in provider.Names)
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

        #region Extension Template data
        public bool AddExtensionTemplateData(string extension, string actionName, TemplateAction data)
        {
            // Add object to main object and all cache objects
            bool status = true;
            ITemplate listener = GetExtension(extension);
            if (!listener.ContainsAction(actionName))
            {
                listener[actionName] = data;
            }
            else
            {
                status = false;
            }
            foreach (ITemplate _listener in CacheGetExtensions(extension))
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

        public bool RemoveExtensionTemplateData(string extension, string actionName)
        {
            // remove object from main class and all cache objects
            bool status = true;
            ITemplate listener = GetExtension(extension);
            if (listener.ContainsAction(actionName))
            {
                listener.RemoveAction(actionName);
            }
            else
            {
                status = false;
            }
            foreach (ITemplate _listener in CacheGetExtensions(extension))
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

        public TemplateAction GetExtensionTemplateData(string extension, string actionName)
        {
            ITemplate listener = GetExtension(extension);
            if (listener.ContainsAction(actionName))
            {
                return listener[actionName];
            }
            else
            {
                return null;
            }
        }

        public bool UpdateExtensionTemplateData(string extension, string actionName, TemplateAction data)
        {
            // Update main class and allcache objects
            bool status = true;
            ITemplate listener = GetExtension(extension);
            if (listener.ContainsAction(actionName))
            {
                listener[actionName] = data;
            }
            else
            {
                status = false;
            }
            foreach (ITemplate _listener in CacheGetExtensions(extension))
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
        private string ProcessExtensionListener(HttpRequest request, HttpResponse response, string extension)
        {
            // Send HTTP request and response to listener. all other must be handled by the user mannualy.
            ITemplate listener;

            if (GetExtension(extension) != null)
            {
                if ((listener = CacheGetTemplate(request.RequestPath.ToLower())) == null)
                {
                    // Check if file exists in 1st place...
                    if (!Containes(request.RequestPath))
                    {
                        ReturnErrorMessage(request, response, "404");
                        return "404";
                    }
                    listener = CacheAddExtension(request.RequestPath.ToLower(), extension);
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
                return "200";
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
        public string Listen(HttpRequest request, HttpResponse response)
        {
            try
            {
                // Ali je pot v registriranem listenerju in kliči listener ter return ;
                if (IsRegisteredExtension(request.RequestPath.ToLower()))
                {
                    string[] _tmp = request.RequestPath.ToLower().Split(new char[] { '.' });
                    if (_tmp.Length > 0)
                    {
                        return ProcessExtensionListener(request, response, _tmp[_tmp.Length - 1]);
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
                            return "200";
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
                        return "200";
                    }
                    else
                    {
                        ReturnErrorMessage(request, response, "404");
                        return "404";
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
                        return "200";
                    }
                    else
                    {
                        ReturnErrorMessage(request, response, "404");
                        return "404";
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                response.Write(e);
                return "500";
            }
        }
        #endregion
    }
}
