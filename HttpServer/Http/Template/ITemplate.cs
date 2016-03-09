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

using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Feri.MS.Http.Template
{
    /// <summary>
    /// Interface that any templating engine must implement. Without it template cannot be registred for listening to extensions.
    /// </summary>
    public interface ITemplate
    {
        TemplateAction this[string name] { get; set; }
        List<string> Keys { get; }
        List<TemplateAction> Values { get; }

        bool SafeMode { get; set; }

        byte[] GetByte();
        string GetString();
        void LoadString(string data);
        void LoadString(byte[] data);
        void ProcessAction();
        bool ContainsAction(string name);
        bool RemoveAction(string name);

    }
}