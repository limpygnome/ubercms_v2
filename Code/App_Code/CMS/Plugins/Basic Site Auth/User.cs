/*                       ____               ____________
 *                      |    |             |            |
 *                      |    |             |    ________|
 *                      |    |             |   |
 *                      |    |             |   |    
 *                      |    |             |   |    ____
 *                      |    |             |   |   |    |
 *                      |    |_______      |   |___|    |
 *                      |            |  _  |            |
 *                      |____________| |_| |____________|
 *                        
 *      Author(s):      limpygnome (Marcus Craske)              limpygnome@gmail.com
 * 
 *      License:        Creative Commons Attribution-ShareAlike 3.0 Unported
 *                      http://creativecommons.org/licenses/by-sa/3.0/
 * 
 *      File:           User.cs
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/User.cs
 * 
 *      Change-Log:
 *                      2013-07-06      Created initial class.
 * 
 * *********************************************************************************************************************
 * A model to represent a user of the basic site authentication plugin.
 * *********************************************************************************************************************
 */
using System;
using CMS.BasicSiteAuth;
using CMS.Base;
using UberLib.Connector;

namespace CMS
{
    namespace BasicSiteAuth
    {
        /// <summary>
        /// A model to represent a user of the basic site authentication plugin.
        /// </summary>
        public class User
        {
            // Fields **************************************************************************************************
            int         userID;             // The identifier of the user.
            string      username,           // The username of the user.
                        password,           // The user'ss password (hashed).
                        email,              // The user's e-mail.
                        secretQuestion,     // Account recovery: secret question.
                        secretAnswer;       // Account recovery: secret answer.
            UserGroup   userGroup;          // The user's role/group.
            bool        loaded,             // Indicates if this instance has been loaded (true) or is a new user and does not exist (false).
                        modified;           // Indicates if the data has been modified.
            // Methods - Constructors **********************************************************************************
            private User()
            {
                modified = loaded = false;
            }
            // Mehods - Database ***************************************************************************************
            /// <summary>
            /// Loads a user from the database.
            /// </summary>
            /// <param name="bsa">BSA plugin.</param>
            /// <param name="conn">Database connector.</param>
            /// <param name="userID">The identifier of the user.</param>
            /// <returns>The user if found/valid, else null.</returns>
            public static User load(BasicSiteAuth bsa, Connector conn, int userID)
            {
                Result result = conn.queryRead("SELECT * FROM bsa_users WHERE userid='" + SQLUtils.escape(userID.ToString()) + "';");
                if (result.Count == 1)
                    return load(bsa, result[0]);
                else
                    return null;
            }
            /// <summary>
            /// Loads a user from database data.
            /// </summary>
            /// <param name="bsa">BSA plugin.</param>
            /// <param name="data">Database data.</param>
            /// <returns>The user if found/valid, else null.</returns>
            public static User load(BasicSiteAuth bsa, ResultRow data)
            {
                UserGroup ug = bsa.UserGroups[int.Parse(data["groupid"])];
                if (ug == null)
                    return null;
                User usr = new User();
                usr.loaded = true;
                usr.userID = int.Parse(data["userid"]);
                usr.username = data["username"];
                usr.password = data["password"];
                usr.email = data["email"];
                usr.secretQuestion = data["secret_question"];
                usr.secretAnswer = data["secret_answer"];
                usr.userGroup = ug;
                return usr;
            }
            /// <summary>
            /// Persists the user's data to the database.
            /// </summary>
            /// <param name="conn">Database connector.</param>
            public void save(Connector conn)
            {
                lock(this)
                {
                    SQLCompiler sql = new SQLCompiler();
                    sql["username"] = username;
                    sql["password"] = password;
                    sql["email"] = email;
                    sql["secret_question"] = secretQuestion;
                    sql["secret_answer"] = secretAnswer;
                    sql["groupid"] = userGroup.GroupID.ToString();
                    if(loaded)
                        conn.queryExecute(sql.compileUpdate("bsa_users", "userid='" + SQLUtils.escape(userID.ToString()) + "'"));
                    else
                        userID = int.Parse(conn.queryScalar(sql.compileInsert("bsa_users", "userid")).ToString());
                    modified = false;
                }
            }
            // Methods - Properties ************************************************************************************
            /// <summary>
            /// The user's identifier.
            /// </summary>
            public int UserID
            {
                get
                {
                    return userID;
                }
            }
            /// <summary>
            /// The user's username.
            /// </summary>
            public string Username
            {
                get
                {
                    return username;
                }
                set
                {
                    modified = true;
                    username = value;
                }
            }
            /// <summary>
            /// The user's password, hashed.
            /// </summary>
            public string Password
            {
                get
                {
                    return password;
                }
                set
                {
                    modified = true;
                    password = value;
                }
            }
            /// <summary>
            /// The user's e-mail address.
            /// </summary>
            public string Email
            {
                get
                {
                    return email;
                }
                set
                {
                    modified = true;
                    email = value;
                }
            }
            /// <summary>
            /// The user's secret-question; may be empty.
            /// </summary>
            public string SecretQuestion
            {
                get
                {
                    return secretQuestion;
                }
                set
                {
                    modified = true;
                    secretQuestion = value;
                }
            }
            /// <summary>
            /// The user's secret-answer; may be empty.
            /// </summary>
            public string SecretAnswer
            {
                get
                {
                    return secretAnswer;
                }
                set
                {
                    modified = true;
                    secretAnswer = value;
                }
            }
            /// <summary>
            /// The user's role/group.
            /// </summary>
            public UserGroup UserGroup
            {
                get
                {
                    return userGroup;
                }
                set
                {
                    modified = true;
                    userGroup = value;
                }
            }
            /// <summary>
            /// Indicates if the data has been modified.
            /// </summary>
            public bool IsModified
            {
                get
                {
                    return modified;
                }
            }
        }
    }
}