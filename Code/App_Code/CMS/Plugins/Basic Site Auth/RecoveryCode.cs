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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/RecoveryCode.cs
 * 
 *      Change-Log:
 *                      2013-07-28      Created initial class.
 * 
 * *********************************************************************************************************************
 * A model for a verification or account recovery code.
 * *********************************************************************************************************************
 */
using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A model for a verification or account recovery code.
    /// </summary>
    public class RecoveryCode
    {
        // Enums *******************************************************************************************************
        /// <summary>
        /// Used to indicate the type of recovery-code model.
        /// </summary>
        public enum CodeType
        {
            Recovery = 0,
            AccountVerification = 1
        }
        /// <summary>
        /// Used for the e-mail recovery method as the status of the operation.
        /// </summary>
        public enum RecoveryCodeEmail
        {
            /// <summary>
            /// Indicates the recovery-code exists.
            /// </summary>
            Exists,
            /// <summary>
            /// The recovery code does not exist or an error occurred.
            /// </summary>
            Failed,
            /// <summary>
            /// The new password could not be applied; check the status reference output of the method using this enum.
            /// </summary>
            FailedUserPersist,
            /// <summary>
            /// Successfully changed password and removed recovery code.
            /// </summary>
            Success,
        }
        // Fields ******************************************************************************************************
        private bool        persisted,          // Indicates if the model has been persisted to the database.
                            modified;           // Indicates if this model has been modified.
        private string      oldCode,            // The old recovery code (a copy of the code when loaded from the database).
                            code;               // The current recovery code.
        private int         userID;             // The user which owns this recovery code.
        private DateTime    datetimeCreated;    // The date and time this recovery code was created; used for cleaning-up recovery codes, since they're only valid for a period of time.
        private CodeType    type;               // The type of recovery-code.
        // Methods - Constructors **************************************************************************************
        /// <summary>
        /// Creates a new unpersisted recovery code.
        /// </summary>
        public RecoveryCode()
        {
            this.modified = this.persisted = false;
            this.userID = -1;
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Generates and sets a new recovery code.
        /// </summary>
        public void generateNewCode()
        {
            this.code = BaseUtils.generateRandomString(32);
            this.modified = true;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Creates a new recovery-code model and persists it.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="code">Recovery code.</param>
        /// <param name="type">Type of recovery code.</param>
        /// <param name="user">The user which owns the code.</param>
        /// <param name="datetimeCreated">The creation date and time, used for cleaning up/expiration.</param>
        /// <returns>A model or null.</returns>
        public static RecoveryCode create(Connector conn, string code, CodeType type, User user, DateTime datetimeCreated)
        {
            return create(conn, code, type, user.UserID, datetimeCreated);
        }
        /// <summary>
        /// Creates a new recovery-code model and persists it.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="code">Recovery code.</param>
        /// <param name="type">Type of recovery code.</param>
        /// <param name="userID">The identifier of the user which owns the code.</param>
        /// <param name="datetimeCreated">The creation date and time, used for cleaning up/expiration.</param>
        /// <returns>A model or null.</returns>
        public static RecoveryCode create(Connector conn, string code, CodeType type, int userID, DateTime datetimeCreated)
        {
            try
            {
                RecoveryCode c = new RecoveryCode();
                c.Code = code;
                c.Type = type;
                c.UserID = userID;
                c.DateTimeCreated = datetimeCreated;
                return c.save(conn) ? c : null;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Loads a recovery-code model.
        /// </summary>
        /// <param name="code">The code of the recovery-code.</param>
        /// <param name="type">The type of recovery code.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>A model or null.</returns>
        public static RecoveryCode load(string code, CodeType type, Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_recovery_codes WHERE code=?code;");
            ps["code"] = code;
            ps["type"] = (int)type;
            Result data = conn.queryRead(ps);
            if (data.Count == 1)
                return load(data[0]);
            else
                return null;
        }
        /// <summary>
        /// Loads a recovery-code model.
        /// </summary>
        /// <param name="code">The code of the recovery-code.</param>
        /// <param name="email">The e-mail of the owner of the recovery-code.</param>
        /// <param name="type">The type of recovery code.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>A model or null.</returns>
        public static RecoveryCode load(string code, string email, CodeType type, Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_recovery_codes AS rc LEFT OUTER JOIN bsa_users AS u ON u.userid=rc.userid WHERE rc.code=?code AND u.email=?email;");
            ps["code"] = code;
            ps["email"] = email;
            ps["type"] = (int)type;
            Result data = conn.queryRead(ps);
            return data.Count == 1 ? load(data[0]) : null;
        }
        /// <summary>
        /// Loads a recovery-code model using tuple data.
        /// </summary>
        /// <param name="row">The result-row/tuple of data.</param>
        /// <returns>A model or null.</returns>
        public static RecoveryCode load(ResultRow row)
        {
            RecoveryCode code = new RecoveryCode();
            code.code = code.oldCode = row.get2<string>("code");
            code.userID = row.get2<int>("userid");
            code.datetimeCreated = row.get2<DateTime>("datetime_created");
            code.type = (CodeType)row.get2<int>("type");
            return code;
        }
        /// <summary>
        /// Persists the model to the database if it has been modified.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = no data persisted.</returns>
        public bool save(Connector conn)
        {
            if (!modified)
                return false;
            SQLCompiler c = new SQLCompiler();
            c["code"] = code;
            c["userid"] = userID.ToString();
            c["datetime_created"] = datetimeCreated;
            c["type"] = (int)type;
            if(persisted)
            {
                c.UpdateAttribute = "code";
                c.UpdateValue = oldCode;
                c.executeUpdate(conn, "bsa_recovery_codes");
            }
            else
            {
                int attempts = 0;
                while(attempts < 5)
                {
                    try
                    {
                        c.executeInsert(conn, "bsa_recovery_codes");
                        break;
                    }
                    catch(DuplicateEntryException)
                    {
                        attempts++;
                        // Generate new recovery code
                        generateNewCode();
                        c["code"] = code;
                    }
                }
                if (attempts < 5)
                    persisted = true;
            }
            oldCode = code;
            modified = false;
            return true;
        }
        /// <summary>
        /// Removes all of the recovery codes belonging to a user.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="userID">The user's identifier.</param>
        public static void remove(Connector conn, int userID)
        {
            conn.queryExecute("DELETE FROM bsa_recovery_codes WHERE userid='" + SQLUtils.escape(userID.ToString()) + "'");
        }
        /// <summary>
        /// Removes all of the invalid/expired recovery codes.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public static void removeExpired(Connector conn)
        {
            string dt = DateTime.Now.AddMilliseconds(-(double)Core.Settings[BasicSiteAuth.SETTINGS_RECOVERYCODES_EXPIRE].get<int>()).ToString("YYYY-MM-dd HH:mm:ss");
            conn.queryExecute("DELETE FROM bsa_recovery_codes WHERE datetime_created < '" + SQLUtils.escape(dt) + "' AND type='" + (int)CodeType.Recovery + "'");
        }
        /// <summary>
        /// Removes all recovery codes.
        /// </summary>
        public static void removeAll(Connector conn)
        {
            conn.queryExecute("DELETE FROM bsa_recovery_codes;");
        }
        /// <summary>
        /// Unpersists the data from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM bsa_recovery_codes WHERE code=?code;");
            ps["code"] = code;
            conn.queryExecute(ps);
        }
        // Methods - Usage *********************************************************************************************
        /// <summary>
        /// Used for handling an account verification, upgrading the group from unverified to verified.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">The BSA plugin.</param>
        /// <param name="code">The code of the recovery code.</param>
        /// <param name="email">The e-mail of the account which owns the code, an additional layer of security against brute-force.</param>
        /// <returns>True if successfully verified, false if not found.</returns>
        public static bool usage_accountVerify(Data data, BasicSiteAuth bsa, string code, string email)
        {
            // Check the IP is not banned (brute-force protection)
            if (AuthFailedAttempt.isIpBanned(data.Connector, data.Request.UserHostAddress))
                return false;
            // Lookup the code
            RecoveryCode c = RecoveryCode.load(code, email, RecoveryCode.CodeType.Recovery, data.Connector);
            if (c != null)
            {
                User usr = User.load(bsa, data.Connector, c.UserID);
                if (usr != null && usr.UserGroup.GroupID == Core.Settings[BasicSiteAuth.SETTINGS_GROUP_UNVERIFIED_GROUPID].get<int>())
                {
                    // Switch the user-group
                    usr.UserGroup = bsa.UserGroups[Core.Settings[BasicSiteAuth.SETTINGS_GROUP_USER_GROUPID].get<int>()];
                    if (usr.save(bsa, data.Connector) != User.UserCreateSaveStatus.Success)
                        return false;
                }
                // Remove the code
                c.remove(data.Connector);
                return true;
            }
            else
                // Log as an attempt to authenticate - brute-force protection
                AuthFailedAttempt.create(data.Connector, data.Request.UserHostAddress, DateTime.Now, AuthFailedAttempt.AuthType.RecoveryCode);
            return false;
        }
        /// <summary>
        /// Used for handling a recovery-code used for account recovery/password-reset by e-mail. The newPassword
        /// parameter can be left null to indicate if a recovery code exists.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">The BSA plugin.</param>
        /// <param name="code">The code of the recovery-code.</param>
        /// <param name="newPassword">The new password; can be null.</param>
        /// <param name="status">This parameter is set with the status of persisting the new password.</param>
        /// <returns>The status of the operation.</returns>
        public static RecoveryCodeEmail usage_recoveryEmail(Data data, BasicSiteAuth bsa, string code, string newPassword, ref User.UserCreateSaveStatus status)
        {
            // Check the IP is not banned (brute-force protection)
            if (AuthFailedAttempt.isIpBanned(data.Connector, data.Request.UserHostAddress))
                return RecoveryCodeEmail.Failed;
            // Lookup the code
            RecoveryCode c = RecoveryCode.load(code, RecoveryCode.CodeType.Recovery, data.Connector);
            if (c != null)
            {
                // Check if to just confirm the code exists
                if (newPassword == null)
                    return RecoveryCodeEmail.Exists;
                // Change the user's password
                User usr = User.load(bsa, data.Connector, c.UserID);
                usr.setPassword(bsa, newPassword);
                User.UserCreateSaveStatus s = usr.save(bsa, data.Connector);
                if (s != User.UserCreateSaveStatus.Success)
                {
                    status = s;
                    return RecoveryCodeEmail.FailedUserPersist;
                }
                // Remove the code
                c.remove(data.Connector);
                return RecoveryCodeEmail.Success;
            }
            else
                // Log as an attempt to authenticate - brute-force protection
                AuthFailedAttempt.create(data.Connector, data.Request.UserHostAddress, DateTime.Now, AuthFailedAttempt.AuthType.RecoveryCode);
            return RecoveryCodeEmail.Failed;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The code of the recovery code.
        /// </summary>
        public string Code
        {
            get
            {
                return code;
            }
            set
            {
                code = value;
                modified = true;
            }
        }
        /// <summary>
        /// The user's identifier.
        /// </summary>
        public int UserID
        {
            get
            {
                return userID;
            }
            set
            {
                userID = value;
                modified = true;
            }
        }
        /// <summary>
        /// The date and time of when the recovery code was created.
        /// </summary>
        public DateTime DateTimeCreated
        {
            get
            {
                return datetimeCreated;
            }
            set
            {
                datetimeCreated = value;
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
        /// <summary>
        /// The type of recovery code.
        /// </summary>
        public CodeType Type
        {
            get
            {
                return type;
            }
            set
            {
                this.type = value;
                modified = true;
            }
        }
    }
}