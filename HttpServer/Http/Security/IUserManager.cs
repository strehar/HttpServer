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

namespace Feri.MS.Http.Security
{
    /// <summary>
    /// Interface that defines API for user management. If you wish to use your own user management class and not build in one, you need to implement this interface and
    /// register your class with HttpServer by calling server.UserManager=yourclass;.
    /// </summary>
    public interface IUserManager
    {
        /// <summary>
        /// 
        /// </summary>
        void Start();
        /// <summary>
        /// 
        /// </summary>
        void Stop();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        bool AddUser(string username, string password);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        string AuthenticateUser(HttpRequest request);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        bool RemoveUser(string username);
    }
}