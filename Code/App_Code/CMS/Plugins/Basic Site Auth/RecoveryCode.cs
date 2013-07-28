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
 * A model for account recovery codes.
 * *********************************************************************************************************************
 */
using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A model for account recovery codes.
    /// </summary>
    public class RecoveryCode
    {
        // Fields ******************************************************************************************************
        private bool        persisted,          // Indicates if the model has been persisted to the database.
                            modified;           // Indicates if this model has been modified.
        private string      oldCode,            // The old recovery code (a copy of the code when loaded from the database).
                            code;               // The current recovery code.
        private int         userID;             // The user which owns this recovery code.
        private DateTime    datetimeCreated;    // The date and time this recovery code was created; used for cleaning-up recovery codes, since they're only valid for a period of time.
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
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads a recovery-code model.
        /// </summary>
        /// <param name="code">The code of the recovery-code.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>Model or null.</returns>
        public static RecoveryCode load(string code, Connector conn)
        {
            Result data = conn.queryRead("SELECT * FROM bsa_recovery_codes WHERE code='" + SQLUtils.escape(code) + "';");
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
        /// <param name="conn">Database connector.</param>
        /// <returns>Model or null.</returns>
        public static RecoveryCode load(string code, string email, Connector conn)
        {
            Result data = conn.queryRead("SELECT * FROM bsa_recovery_codes AS rc LEFT OUTER JOIN bsa_users AS u ON u.userid=rc.userid WHERE rc.code='" + SQLUtils.escape(code) + "' AND u.email='" + SQLUtils.escape(email) + "';");
            if (data.Count == 1)
                return load(data[0]);
            else
                return null;
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
            return code;
        }
        /// <summary>
        /// Persists the model to the database if it has been modified.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void save(Connector conn)
        {
            if (!modified)
                return;
            SQLCompiler c = new SQLCompiler();
            c["code"] = code;
            c["userid"] = userID.ToString();
            c["datetime_created"] = datetimeCreated.ToString("YYYY-MM-dd HH:mm:ss");
            if(persisted)
                conn.queryExecute(c.compileUpdate("bsa_recovery_codes", "code='" + SQLUtils.escape(oldCode) + "'");
            else
            {
                int attempts = 0;
                while(attempts < 5)
                {
                    try
                    {
                        conn.queryExecute(c.compileInsert("bsa_recovery_codes"));
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
            }
            oldCode = code;
            modified = false;
        }
        /// <summary>
        /// Removes a recovery code from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="code">The recovery code to be removed.</param>
        public static void remove(Connector conn, ref RecoveryCode code)
        {
            conn.queryExecute("DELETE FROM bsa_recovery_codes WHERE code='" + SQLUtils.escape(code.code) + "'");
            code = null;
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
        public static void remove(Connector conn)
        {
            string dt = DateTime.Now.AddMilliseconds(-(double)Core.Settings[BasicSiteAuth.SETTINGS_RECOVERYCODES_EXPIRE].get<int>()).ToString("YYYY-MM-dd HH:mm:ss");
            conn.queryExecute("DELETE FROM bsa_recovery_codes WHERE datetime_created < '" + SQLUtils.escape(dt) + "'");
        }
        /// <summary>
        /// Removes all recovery codes.
        /// </summary>
        public static void removeAll(Connector conn)
        {
            conn.queryExecute("DELETE FROM bsa_recovery_codes;");
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
    }
}