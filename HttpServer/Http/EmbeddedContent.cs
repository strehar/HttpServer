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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Feri.MS.Http
{
    public class EmbeddedContent
    {
        Dictionary<string, AssemblyData> _RegistriraniAssebly = new Dictionary<string, AssemblyData>();  // Asembliji (dll-i) po katerih iščemo embeded vire, ki jih lahko prikažemo uporabnikom. Rabi se za reflection
        Dictionary<string, string> _NajdeneDatoteke = new Dictionary<string, string>();                  // Datoteke, ki so vključene v asemblije kot embededresource in jih lahko pošljemo uporabniku ter v katerem assembliju so.

        Assembly _sistemskiAssembly = null;               // asembly od tega dll-a, da se lahko sklicujemo na sistemske vire

        private bool _debug = false;                      // Ali se naj izpisujejo debug informacije iz metod (precej spama)

        public bool EnableDebug
        {
            get
            {
                return _debug;
            }

            set
            {
                _debug = value;
            }
        }

        internal EmbeddedContent(Type val)
        {
            _sistemskiAssembly = val.GetTypeInfo().Assembly;
            string _namespace = GetType().Namespace;
            if (_namespace.Length < 1)
                _RegistriraniAssebly.Add(_sistemskiAssembly.GetName().Name, new AssemblyData() { Name = _sistemskiAssembly.GetName().Name, Assembly = _sistemskiAssembly });
            else
                _RegistriraniAssebly.Add(_sistemskiAssembly.GetName().Name, new AssemblyData() { Name = _sistemskiAssembly.GetName().Name, Assembly = _sistemskiAssembly, NameSpace = _namespace });
            RefreshFileList();
        }

        #region Assembly processing
        /// <summary>
        /// Method registers new assembly to look for embeded content. (in Visual studio in file properties build action is embedded resource)
        /// </summary>
        /// <param name="assembly">typeof(object) or object.GetType(); must be unique</param>
        /// <returns>true if assembly was added, false if it was allready registerd.</returns>
        public bool RegisterAssembly(Type assembly)
        {
            if (assembly != null)
            {
                if (!_RegistriraniAssebly.ContainsKey(assembly.GetTypeInfo().Assembly.GetName().Name))
                {
                    string _namespace = assembly.Namespace;
                    if (_namespace.Length < 1)
                        _RegistriraniAssebly.Add(assembly.GetTypeInfo().Assembly.GetName().Name, new AssemblyData() { Name = assembly.GetTypeInfo().Assembly.GetName().Name, Assembly = assembly.GetTypeInfo().Assembly });
                    else
                        _RegistriraniAssebly.Add(assembly.GetTypeInfo().Assembly.GetName().Name, new AssemblyData() { Name = assembly.GetTypeInfo().Assembly.GetName().Name, Assembly = assembly.GetTypeInfo().Assembly, NameSpace = _namespace });
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method removes assembly from the list of assemblies to look for embeded content.
        /// </summary>
        /// <param name="assembly">typeof(object) or object.GetType()</param>
        /// <returns>True if assembly was removed, false it it was not. It can return false if trying to unregister assembly containing HttpServer class.</returns>
        public bool UnregisterAssembly(Type assembly)
        {
            if (assembly != null)
            {
                // Ne dovolimo odstraniti sistemskega assemblija iz seznama.
                if (!assembly.GetTypeInfo().Assembly.GetName().Name.Equals(_sistemskiAssembly.GetName().Name))
                {
                    //preverimo ali je ta assebly registriran
                    if (_RegistriraniAssebly.ContainsKey(assembly.GetTypeInfo().Assembly.GetName().Name))
                    {
                        _RegistriraniAssebly.Remove(assembly.GetTypeInfo().Assembly.GetName().Name);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Embeded files
        /// <summary>
        /// Method scans for new embedded content that can be read form registered assemblies. It must be called whenever new assemblies are added or removed.
        /// </summary>
        public void RefreshFileList()
        {

            _NajdeneDatoteke.Clear();
            List<string> _datoteke = new List<string>();
            foreach (KeyValuePair<string, AssemblyData> par in _RegistriraniAssebly)
            {
                _datoteke.AddRange(par.Value.Assembly.GetManifestResourceNames());
                foreach (string pot in _datoteke)
                {
                    //_potKljuc = pot.Substring(par.Value.NameSpace.Length + 1);
                    if (!_NajdeneDatoteke.ContainsKey(pot))
                    {
                        _NajdeneDatoteke.Add(pot, par.Key);
                    }
                    Debug.WriteLineIf(_debug, "Najdena pot: " + pot + " Assembly: " + par.Key);
                }
                _datoteke.Clear();
            }
        }

        /// <summary>
        /// Method takes url provided by user methods and returns full path to be read, with all package names.
        /// It uses registered files to scan. It does partitial file scan and returnes first found file. that's why it's important that calling methods add atleast part of namespace before path.
        /// For example "publicHtml" + path
        /// </summary>
        /// <param name="url">url or part of file name to look for</param>
        /// <returns>full file name (with namespace names) or null if not found.</returns>
        public string UrlToPath(string url)
        {
            string _url = url.Replace('/', '.').ToLower();
            string _polnaPot = "";
            bool najdeno = false;
            foreach (string _pot in _NajdeneDatoteke.Keys)
            {
                if (_pot.ToLower().Contains(_url) && (!najdeno))
                {
                    _polnaPot = _pot;
                    najdeno = true;
                }
            }
            return najdeno ? _polnaPot : null;
        }

        /// <summary>
        /// Method reads embedded file and returnes byte array with the data.
        /// </summary>
        /// <param name="pot">Full path to file, that is returned from UrlToPath method.</param>
        /// <returns>byte array with file data</returns>
        public byte[] ReadEmbededToByte(string pot)
        {
            string _potTmp = pot.Replace('/', '.');
            string _pot = UrlToPath(_potTmp);
            if (_pot == null)
            {
                throw new FileNotFoundException("File " + pot + " not found.");
            }
            Debug.WriteLineIf(_debug, "Open File: " + _pot);
            //string _assemblyName = _pot.Substring(0, _pot.IndexOf('.'));
            string _assemblyName = _NajdeneDatoteke[_pot];
            if (_RegistriraniAssebly.ContainsKey(_assemblyName))
            {
                using (Stream stream = _RegistriraniAssebly[_assemblyName].Assembly.GetManifestResourceStream(_pot))
                {
                    MemoryStream buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    byte[] _dataArray = buffer.ToArray();
                    return _dataArray;
                }
            }
            else
            {
                throw new FileNotFoundException("File " + pot + " not found in Assembly " + _assemblyName);
            }
        }

        /// <summary>
        /// Helper method that returnes List of all names of embedded resources 
        /// </summary>
        /// <returns>List with names of embedded resources.</returns>
        public List<string> GetEmbededNames()
        {
            List<string> _tmpList = new List<string>();
            foreach (string pot in _NajdeneDatoteke.Keys)
                _tmpList.Add(pot);
            return _tmpList;
        }

        /// <summary>
        /// Helper method to check if embedded resource ecists
        /// </summary>
        /// <param name="pot">name of embedded resource</param>
        /// <returns>True if it exists and false if it does not.</returns>
        public bool GetEmbededContaines(string pot)
        {
            if (UrlToPath(pot.Replace('/', '.')) != null)
                return true;
            else
                return false;
        }
        #endregion
    }
}
