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

namespace Feri.MS.Http.Template
{
    public interface ITemplate
    {
        bool AddAction(string name, string pattern, string data);
        bool DeleteAction(string name);
        byte[] GetByte();
        string GetString();
        void LoadString(string data);
        void LoadString(byte[] data);
        void ProcessAction();
        bool UpdateAction(string name, string data);
        bool UpdateAction(HttpRequest rquest, HttpResponse response);
    }
}