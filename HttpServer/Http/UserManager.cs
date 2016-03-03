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

namespace Feri.MS.Http
{
    /// <summary>
    /// 
    /// </summary>
    public class UserManager : IUserManager
    {
        Dictionary<string, string> _users = new Dictionary<string, string>();                            // Registriranu userji z gesli, ki imajo dostop do sistema

        #region Lifecycle
        /// <summary>
        /// This manager really does not do anything here.
        /// </summary>
        public void Start()
        {

        }

        /// <summary>
        /// This manager really does not do anything here.
        /// </summary>
        public void Stop()
        {

        }
        #endregion

        #region User management
        /// <summary>
        /// method adds user to the list of registered users (users that are allowed access to the application over http)
        /// WARNING: server uses basic authentication.
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <param name="password">Password for the user</param>
        /// <returns>true if user was added and flase if user allready exists.</returns>
        public bool AddUser(string username, string password)
        {
            //Username ni treba da je caps sensitive, password pa mora biti...
            string _username = username.ToLower();
            if (!_users.ContainsKey(_username))
            {
                _users.Add(_username, password);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method removes user from list of authorized users.
        /// </summary>
        /// <param name="username">Username to remove</param>
        /// <returns>true if user was removed of false if user does not exist</returns>
        public bool RemoveUser(string username)
        {
            //Username ni treba da je caps sensitive, password pa mora biti...
            string _username = username.ToLower();
            if (_users.ContainsKey(_username))
            {
                _users.Remove(_username);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method checks if user information provided by the client mathces the information stored on server
        /// </summary>
        /// <param name="request">request we will use to authenticate user (extract user data from headers).</param>
        /// <returns>true if information matches and lase if it does not.</returns>
        public string AuthenticateUser(HttpRequest request)
        {
            // Preveri kaj je user vnesel...
            if (!request.Headers.ContainsKey("Authorization"))
                return null;

            string _encoded = request.Headers["Authorization"].Split(' ')[1].Trim();
            string _Vnos = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(_encoded));
            string[] _user = _Vnos.Split(new char[] { ':' }, 2);


            //Username ni treba da je caps sensitive, password pa mora biti...
            string _username = _user[0].Trim().ToLower();
            if (_users.ContainsKey(_username))
                if (_users[_username].Equals(_user[1].Trim()))
                    return _username;
                else
                    return null;
            else
                return null;
        }
        #endregion

    }
}
