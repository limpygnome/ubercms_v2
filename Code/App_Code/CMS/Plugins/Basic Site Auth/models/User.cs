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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/User.cs
 * 
 *      Change-Log:
 *                      2013-07-06      Created initial class.
 *                      2013-07-24      Modified password mechanism for automatic hashing.
 *                                      Increased security with a third salt for every user.
 * 
 * *********************************************************************************************************************
 * A model to represent a user of the basic site authentication plugin. Modifying the settings of a user can be dirty,
 * with the data validated by the save method.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using CMS.BasicSiteAuth;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth.Models
{
    /// <summary>
    /// A model to represent a user of the basic site authentication plugin. Modifying the settings of a user
    /// can be dirty, with the data validated by the save method.
    /// </summary>
    public class User
    {
        // Enums ***************************************************************************************************
        /// <summary>
        /// The status of saving a user.
        /// </summary>
        public enum UserCreateSaveStatus
        {
            /// <summary>
            /// User successfully created.
            /// </summary>
            Success = 0,
            /// <summary>
            /// The user has been successfully created but needs to verify their account by e-mail.
            /// </summary>
            SuccessVerify = 1,
            /// <summary>
            /// Invalid username length.
            /// </summary>
            InvalidUsername_Length = 100,
            /// <summary>
            /// Username already taken.
            /// </summary>
            InvalidUsername_AlreadyExists = 101,
            /// <summary>
            /// Invalid password length.
            /// </summary>
            InvalidPassword_Length = 200,
            /// <summary>
            /// Password is too obvious.
            /// </summary>
            InvalidPassword_Security = 201,
            /// <summary>
            /// Invalid e-mail length.
            /// </summary>
            InvalidEmail_Length = 300,
            /// <summary>
            /// Invalid e-mail format.
            /// </summary>
            InvalidEmail_Format = 301,
            /// <summary>
            /// E-mail already in-use by another user.
            /// </summary>
            InvalidEmail_AlreadyExists = 302,
            /// <summary>
            /// Invalid secret question length.
            /// </summary>
            InvalidSecretQuestion_Length = 400,
            /// <summary>
            /// Invalid secret answer length.
            /// </summary>
            InvalidSecretAnswer_Length = 500,
            /// <summary>
            /// Invalid user-group.
            /// </summary>
            InvalidUserGroup = 600,
            /// <summary>
            /// An unknown exception occurred registering the user.
            /// </summary>
            Error_Regisration = 900
        };
        public enum AuthenticationStatus
        {
            /// <summary>
            /// Indicates a general exception occurred.
            /// </summary>
            Failed,
            /// <summary>
            /// Indicates too many attempts have been made from the IP and they're temporarily banned.
            /// </summary>
            FailedTempBanned,
            /// <summary>
            /// Indicates the account is banned.
            /// </summary>
            FailedBanned,
            /// <summary>
            /// Indicates the account is disabled from being able to login.
            /// </summary>
            FailedDisabled,
            /// <summary>
            /// Indicates the attempted password was incorrect.
            /// </summary>
            FailedIncorrect,
            /// <summary>
            /// The user has been successfully authenticated.
            /// </summary>
            Success
        };
        // Fields **************************************************************************************************
        int         userID;             // The identifier of the user.
        string      username,           // The username of the user.
                    password,           // The user'ss password (hashed).
                    passwordSalt,       // The unique salt for the user's password, used for additional hashing complexity.
                    email,              // The user's e-mail.
                    secretQuestion,     // Account recovery: secret question.
                    secretAnswer;       // Account recovery: secret answer.
        UserGroup   userGroup;          // The user's role/group.
        DateTime    registered;         // The date and time of when the user registered.
        bool        pendingDeletion;    // Indicates if the user is pending deletion.
        bool        persisted,          // Indicates if this model has been persisted to the database.
                    modified;           // Indicates if the data has been modified.
        // Methods - Constructors **********************************************************************************
        public User()
        {
            modified = persisted = false;
        }
        // Methods *************************************************************************************************
        /// <summary>
        /// Attempts to authenticate a request.
        /// 
        /// Note: this method does not set any ASP.NET authentication, this should be handled by the calling method.
        /// This is because this method could be used by APIs which may use session keys or some alternate system
        /// to ASP.NET's authentication.
        /// 
        /// Note 2: this method also has protection against brute-force authentication with banning; incorrect
        /// authentication attempts are also logged to account events.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="password">The plain-text password to be tested against this user.</param>
        /// <param name="requestData">The data for the current request.</param>
        /// <param name="ban">If FailedBan is returned, the ban is outputted to this parameter.</param>
        /// <returns>True if authenticated, false if authentication failed.</returns>
        public AuthenticationStatus authenticate(BasicSiteAuth bsa, string password, Data requestData, ref UserBan ban)
        {
            // Check the user has not exceeded the maximum amount of authentication attempts
            if (AuthFailedAttempt.isIpBanned(requestData.Connector, requestData.Request.UserHostAddress))
                return AuthenticationStatus.FailedTempBanned;
            // Check the password is correct
            else if (!validPassword(bsa, password))
            {
                // Log the failed attempt
                AccountEvent.create(requestData.Connector, bsa, BasicSiteAuth.ACCOUNT_EVENT__INCORRECT_AUTH__UUID, DateTime.Now, userID, requestData.Request.UserHostAddress, SettingsNode.DataType.String, requestData.Request.UserAgent, SettingsNode.DataType.String);
                AuthFailedAttempt.create(requestData, AuthFailedAttempt.AuthType.Login);
                return AuthenticationStatus.FailedIncorrect;
            }
            // Check the user is not banned
            UserBan ub = UserBan.getLatestBan(requestData.Connector, this);
            if (ub != null)
            {
                ban = ub;
                return AuthenticationStatus.FailedBanned;
            }
            // Check the user-group does not disable the user from logging-in or if the account is pending deletion
            else if (!userGroup.Login || pendingDeletion)
                return AuthenticationStatus.FailedDisabled;
            else
            {
                // Log the success
                AccountEvent.create(requestData.Connector, bsa, BasicSiteAuth.ACCOUNT_EVENT__AUTH__UUID, DateTime.Now, userID, requestData.Request.UserHostAddress, SettingsNode.DataType.String, requestData.Request.UserAgent, SettingsNode.DataType.String);
                return AuthenticationStatus.Success;
            }
        }
        /// <summary>
        /// Tests if a plain-text password is the same as the user's password, for authentication purposes.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="password">The plain-text password to be tested against this user.</param>
        /// <returns></returns>
        public bool validPassword(BasicSiteAuth bsa, string password)
        {
            if (password == null || password.Length == 0)
                return false;
            return Utils.generateHash(bsa, password, passwordSalt) == this.password;
        }
        // Mehods - Database Persistence ***************************************************************************
        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="data">The data for the current request; this must have an open and usable connector.</param>
        /// <param name="username">The username for the new user.</param>
        /// <param name="password">The plain-text password for the new user.</param>
        /// <param name="email">The e-mail of the new user.</param>
        /// <param name="secretQuestion">The secret question (account recovery) for the new user.</param>
        /// <param name="secretAnswer">The secret answer (account recovery) for the new user.</param>
        /// <param name="outputUser">The new user will be outputted to this reference if successfully created, else it will be set to null.</param>
        /// <returns>The creation status.</returns>
        public static UserCreateSaveStatus create(BasicSiteAuth bsa, Data data, string username, string password, string email, string secretQuestion, string secretAnswer, ref User outputUser)
        {
            try
            {
                bool emailVerification = Core.Settings[BasicSiteAuth.SETTINGS_EMAIL_VERIFICATION].get<bool>();
                User u = new User();
                u.Username = username;
                UserCreateSaveStatus t = u.setPassword(bsa, password);
                if (t != UserCreateSaveStatus.Success)
                    return t;
                u.
                u.Email = email;
                u.SecretQuestion = secretQuestion;
                u.SecretAnswer = secretAnswer;
                u.Registered = DateTime.Now;
                // Set the default group
                u.UserGroup = bsa.UserGroups[emailVerification ? Core.Settings[BasicSiteAuth.SETTINGS_GROUP_UNVERIFIED_GROUPID].get<int>() : Core.Settings[BasicSiteAuth.SETTINGS_GROUP_USER_GROUPID].get<int>()];
                // Attempt to persist
                t = u.save(bsa, data.Connector);
                if (t == UserCreateSaveStatus.Success)
                {
                    // Deploy either a welcome or verification e-mail
                    if (emailVerification)
                    {
                        // Create an e-mail verification recovery code and check we created it
                        RecoveryCode code = RecoveryCode.create(data.Connector, RecoveryCode.CodeType.AccountVerification, u);
                        if (code == null)
                        {
                            // Fall-back to a verified account
                            u.UserGroup = bsa.UserGroups[Core.Settings[BasicSiteAuth.SETTINGS_GROUP_USER_GROUPID].get<int>()];
                            // Check the verified user-group persisted
                            if (u.save(bsa, data.Connector) != UserCreateSaveStatus.Success)
                            { // Critical failure: abort  the registration process!
                                outputUser = null;
                                u.remove(data.Connector);
                                return UserCreateSaveStatus.Error_Regisration;
                            }
                            else
                            {
                                // Partial success (set from unverified to verified as fallback) - user is verified and setup!
                                Emails.sendWelcome(data, u);
                                outputUser = u;
                                return UserCreateSaveStatus.Success;
                            }
                        }
                        else
                        {
                            // Success - user requires verification!
                            Emails.sendVerify(data, u, code.Code);
                            outputUser = u;
                            return UserCreateSaveStatus.SuccessVerify;
                        }
                    }
                    else
                    {
                        // Success - user is verified!
                        Emails.sendWelcome(data, u);
                        outputUser = u;
                        return UserCreateSaveStatus.Success;
                    }
                }
                else
                {
                    // Failure occurred...
                    outputUser = null;
                    return t;
                }
            }
            catch
            {
                return UserCreateSaveStatus.Error_Regisration;
            }
        }
        /// <summary>
        /// Loads a user from the database by e-mail.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="email">The e-mail of the user.</param>
        /// <returns>A model or null.</returns>
        public static User loadByEmail(BasicSiteAuth bsa, Connector conn, string email)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_users WHERE email=?email;");
            ps["email"] = email;
            Result result = conn.queryRead(ps);
            return result.Count == 1 ? load(bsa, result[0]) : null;
        }
        /// <summary>
        /// Loads a user from the database.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="username">The username of the user.</param>
        /// <returns>A the model or null.</returns>
        public static User load(BasicSiteAuth bsa, Connector conn, string username)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_users WHERE username=?username;");
            ps["username"] = username;
            Result result = conn.queryRead(ps);
            return result.Count == 1 ? load(bsa, result[0]) : null;
        }
        /// <summary>
        /// Loads a user from the database.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="userID">The identifier of the user.</param>
        /// <returns>A the model or null.</returns>
        public static User load(BasicSiteAuth bsa, Connector conn, int userID)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_users WHERE userid=?userid;");
            ps["userid"] = userID;
            Result result = conn.queryRead(ps);
            return result.Count == 1 ? load(bsa, result[0]) : null;
        }
        /// <summary>
        /// Loads a user from database data.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="data">Database data.</param>
        /// <returns>A the model or null.</returns>
        public static User load(BasicSiteAuth bsa, ResultRow data)
        {
            UserGroup ug = bsa.UserGroups[data.get2<int>("groupid")];
            if (ug == null)
                return null;
            User usr = new User();
            usr.persisted = true;
            usr.userID = data.get2<int>("userid");
            usr.username = data["username"];
            usr.password = data["password"];
            usr.passwordSalt = data["password_salt"];
            usr.email = data["email"];
            usr.secretQuestion = data["secret_question"];
            usr.secretAnswer = data["secret_answer"];
            usr.userGroup = ug;
            usr.registered = data.get2<DateTime>("datetime_register");
            usr.pendingDeletion = data.get2<string>("pending_deletion").Equals("1");
            return usr;
        }
        /// <summary>
        /// Persists the user's data to the database.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>The state from attempting to persist the user.</returns>
        public UserCreateSaveStatus save(BasicSiteAuth bsa, Connector conn)
        {
            return save(bsa, conn, false);
        }
        /// <summary>
        /// Persists the user's data to the database.
        /// 
        /// Warning: this method is solely used (in some cases) for validating data and may accept dirty-data. Thus
        /// if you extend this method, ensure you validate any input data properly!
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="skipValidation">Skip validation of parameters; possibly quite dangerous - use with extreme caution!</param>
        /// <returns>The state from attempting to persist the user.</returns>
        public UserCreateSaveStatus save(BasicSiteAuth bsa, Connector conn, bool skipValidation)
        {
            lock(this)
            {
                try
                {
                    // Validate the data
                    if (!skipValidation)
                    {
                        if (username == null || username.Length < Core.Settings[BasicSiteAuth.SETTINGS_USERNAME_MIN].get<int>() || username.Length > Core.Settings[BasicSiteAuth.SETTINGS_USERNAME_MAX].get<int>())
                            return UserCreateSaveStatus.InvalidUsername_Length;
                        else if (password == null || password.Length < Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MIN].get<int>() || password.Length > Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MAX].get<int>())
                            return UserCreateSaveStatus.InvalidPassword_Length;
                        else if (email == null || email.Length < Core.Settings[BasicSiteAuth.SETTINGS_EMAIL_MIN].get<int>() || email.Length > Core.Settings[BasicSiteAuth.SETTINGS_EMAIL_MAX].get<int>())
                            return UserCreateSaveStatus.InvalidEmail_Length;
                        else if ((secretQuestion == null && Core.Settings[BasicSiteAuth.SETTINGS_SECRETQUESTION_MIN].get<int>() != 0) || (secretQuestion != null && (secretQuestion.Length < Core.Settings[BasicSiteAuth.SETTINGS_SECRETQUESTION_MIN].get<int>() || secretQuestion.Length > Core.Settings[BasicSiteAuth.SETTINGS_SECRETQUESTION_MAX].get<int>())))
                            return UserCreateSaveStatus.InvalidSecretQuestion_Length;
                        else if ((secretAnswer == null && Core.Settings[BasicSiteAuth.SETTINGS_SECRETANSWER_MIN].get<int>() != 0) || (secretAnswer != null && (secretAnswer.Length < Core.Settings[BasicSiteAuth.SETTINGS_SECRETANSWER_MIN].get<int>() || secretAnswer.Length > Core.Settings[BasicSiteAuth.SETTINGS_SECRETANSWER_MAX].get<int>())))
                            return UserCreateSaveStatus.InvalidSecretAnswer_Length;
                        else if (userGroup == null || !bsa.UserGroups.contains(userGroup))
                            return UserCreateSaveStatus.InvalidUserGroup;
                        else if (!Utils.validEmail(email))
                            return UserCreateSaveStatus.InvalidEmail_Format;
                    }
                    // Persist the data
                    SQLCompiler sql = new SQLCompiler();
                    sql["username"] = username;
                    sql["password"] = password;
                    sql["password_salt"] = passwordSalt;
                    sql["email"] = email;
                    sql["secret_question"] = secretQuestion;
                    sql["secret_answer"] = secretAnswer;
                    sql["groupid"] = userGroup.GroupID.ToString();
                    sql["datetime_register"] = registered;
                    if (persisted)
                    {
                        sql.UpdateAttribute = "userid";
                        sql.UpdateValue = userID;
                        sql.executeUpdate(conn, "bsa_users");
                    }
                    else
                    {
                        userID = (int)sql.executeInsert(conn, "bsa_users", "userid")[0].get2<long>("userid");
                        persisted = true;
                    }
                    // Success! Reset flags and return status
                    modified = false;
                    return UserCreateSaveStatus.Success;
                }
                catch (DuplicateEntryException ex)
                {
                    switch (ex.Attribute)
                    {
                        case "username":
                            return UserCreateSaveStatus.InvalidUsername_AlreadyExists;
                        case "email":
                            return UserCreateSaveStatus.InvalidEmail_AlreadyExists;
                        default:
                            return UserCreateSaveStatus.Error_Regisration;
                    }
                }
            }
        }
        /// <summary>
        /// Unpersists the data from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM bsa_users WHERE userid=?userid;");
            ps["userid"] = userID;
            conn.queryExecute(ps);
            persisted = false;
            modified = true;
        }
        // Methods - Mutators **************************************************************************************
        /// <summary>
        /// Sets the user's password. This method will generate a new unique salt for the user, as well as hash
        /// the specified new password.
        /// 
        /// Note: this does not persist the model or the password.
        /// </summary>
        /// <param name="newPassword">The plain-text new password, to be hashed.</param>
        /// <returns>The status of changing the password - either Success, InvalidPassword_Length or InvalidPassword_Security.</returns>
        public UserCreateSaveStatus setPassword(BasicSiteAuth bsa, string newPassword)
        {
            if (newPassword == null || newPassword.ToLower() == "password" || newPassword == "123456" || newPassword == "12345678" || newPassword == "abc123" || newPassword == "qwerty")
                return UserCreateSaveStatus.InvalidPassword_Security;
            else if (newPassword.Length < Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MIN].get<int>() || newPassword.Length > Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MAX].get<int>())
                return UserCreateSaveStatus.InvalidPassword_Length;
            else
            {
                Random rand = new Random((int)DateTime.Now.Ticks);
                this.passwordSalt = BaseUtils.generateRandomString(rand.Next(BasicSiteAuth.BSA_UNIQUE_USER_HASH_MIN, BasicSiteAuth.BSA_UNIQUE_USER_HASH_MAX));
                this.password = Utils.generateHash(bsa, newPassword, this.passwordSalt);
                return UserCreateSaveStatus.Success;
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
        }
        /// <summary>
        /// The user's unique salt for password hashing.
        /// </summary>
        public string PasswordSalt
        {
            get
            {
                return passwordSalt;
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
        /// The date and time of when the user registered.
        /// </summary>
        public DateTime Registered
        {
            get
            {
                return registered;
            }
            set
            {
                registered = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the account is pending deletion.
        /// </summary>
        public bool PendingDeletion
        {
            get
            {
                return pendingDeletion;
            }
            set
            {
                pendingDeletion = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if this model has been persisted to the database.
        /// </summary>
        public bool IsPersisted
        {
            get
            {
                return persisted;
            }
        }
        /// <summary>
        /// Indicates if the model has been modified.
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