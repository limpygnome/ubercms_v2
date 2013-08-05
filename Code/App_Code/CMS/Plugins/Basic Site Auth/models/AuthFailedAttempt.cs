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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/AuthFailedAttempt.cs
 * 
 *      Change-Log:
 *                      2013-07-28      Created initial class.
 * 
 * *********************************************************************************************************************
 * A model for representing failed authentication attempts.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using UberLib.Connector;
using CMS.Base;

namespace CMS.BasicSiteAuth.Models
{
    /// <summary>
    /// A model for representing failed authentication attempts.
    /// </summary>
    public class AuthFailedAttempt
    {
        // Enums *******************************************************************************************************
        public enum AuthType
        {
            Login = 10,
            LoginThirdParty = 11,
            Recovery = 20,
            API = 30,
            Other = 0
        }
        // Fields ******************************************************************************************************
        private bool        loaded,         // Indicates if the model was loaded.
                            modified;       // Indicates if the model has been modified.
        private string      ip;             // The IP address of the host which failed to authenticate.
        private DateTime    datetime;       // The date and time of the occurrence.
        private AuthType    type;           // The type of authentication failure.
        // Methods - Constructors **************************************************************************************
        /// <summary>
        /// Creates a new unpersisted authentication failed attempt model.
        /// </summary>
        public AuthFailedAttempt()
        {
            this.loaded = this.modified = false;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Creates and persists a model.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="type">The type of authentication failure.</param>
        /// <returns>Either the model if persisted or null.</returns>
        public static AuthFailedAttempt create(Data data, AuthType type)
        {
            return create(data.Connector, data.Request.UserHostAddress, DateTime.Now, type);
        }
        /// <summary>
        /// Creates and persists a model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ip">IP address of the host.</param>
        /// <param name="datetime">Date and time of the occurrence.</param>
        /// <param name="type">The type of authentication failure.</param>
        /// <returns>Either the model if persisted or null.</returns>
        public static AuthFailedAttempt create(Connector conn, string ip, DateTime datetime, AuthType type)
        {
            AuthFailedAttempt a = new AuthFailedAttempt();
            a.IP = ip;
            a.DateTime = datetime;
            a.Type = type;
            return a.save(conn) ? a : null;
        }
        /// <summary>
        /// Loads multiple models filtered by an IP.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ip">The IP of the failed authentication attempts.</param>
        /// <param name="amount">The maximum number of models to return.</param>
        /// <param name="offset">The page offset.</param>
        /// <returns>Array of models; never null.</returns>
        public static AuthFailedAttempt[] load(Connector conn, string ip, int amount, int offset)
        {
            Result data = conn.queryRead("SELECT * FROM bsa_authentication_failed_attempts WHERE ip='" + SQLUtils.escape(ip) + "' LIMIT " + SQLUtils.escape(amount.ToString()) + " OFFSET " + SQLUtils.escape(((amount * offset) - offset).ToString()) + ";");
            return load(data);
        }
        /// <summary>
        /// Loads multiple models.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="amount">Maximum number of models to return.</param>
        /// <param name="offset">The page offset.</param>
        /// <returns>Array of models; never null.</returns>
        public static AuthFailedAttempt[] load(Connector conn, int amount, int offset)
        {
            Result data = conn.queryRead("SELECT * FROM bsa_authentication_failed_attempts LIMIT " + SQLUtils.escape(amount.ToString()) + " OFFSET " + SQLUtils.escape(((amount * offset)-offset).ToString()) + ";");
            return load(data);
        }
        /// <summary>
        /// Loads multiple models from result data.
        /// </summary>
        /// <param name="data">Result data.</param>
        /// <returns>Array of models; never null.</returns>
        public static AuthFailedAttempt[] load(Result data)
        {
            List<AuthFailedAttempt> buffer = new List<AuthFailedAttempt>();
            AuthFailedAttempt t;
            foreach (ResultRow row in data)
            {
                t = load(row);
                if (t != null)
                    buffer.Add(t);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// Loads a model.
        /// </summary>
        /// <param name="row">Result-row/tuple-data.</param>
        /// <returns>Model or null.</returns>
        public static AuthFailedAttempt load(ResultRow row)
        {
            AuthFailedAttempt a = new AuthFailedAttempt();
            a.loaded = false;
            a.ip = row.get2<string>("ip");
            a.datetime = row.get2<DateTime>("datetime");
            a.type = getAuthType(row.get2<string>("type"));
            return a;
        }
        /// <summary>
        /// Removes all of the recovery-codes for an IP.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ip">IP of the host.</param>
        public static void remove(Connector conn, string ip, AuthType type)
        {
            conn.queryExecute("DELETE FROM bsa_authentication_failed_attempts WHERE ip='" + SQLUtils.escape(ip) + "';");
        }
        /// <summary>
        /// Removes all of the old failed authentication tuples.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public static void remove(Connector conn)
        {
            string dt = DateTime.Now.AddMilliseconds(-(double)Core.Settings[BasicSiteAuth.SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD].get<int>()).ToString("YYYY-MM-dd HH:mm:ss");
            conn.queryExecute("DELETE FROM bsa_authentication_failed_attempts WHERE datetime < '" + SQLUtils.escape(dt) + "';");
        }
        /// <summary>
        /// Removes all of the failed authentication tuples.
        /// </summary>
        /// <param name="conn"></param>
        public static void removeAll(Connector conn)
        {
            conn.queryExecute("DELETE FROM bsa_authentication_failed_attempts;");
        }
        /// <summary>
        /// Indicates if an IP address has exceeded the failed attempts threshold/limit.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ip">The IP of the host being tested.</param>
        /// <returns>True = exceeded, false = has not exceeded.</returns>
        public static bool isIpBanned(Connector conn, string ip)
        {
            string dt = DateTime.Now.AddMilliseconds(-(double)Core.Settings[BasicSiteAuth.SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD].get<int>()).ToString("YYYY-MM-dd HH:mm:ss");
            return conn.queryCount("SELECT COUNT('') FROM bsa_authentication_failed_attempts WHERE ip='" + SQLUtils.escape(ip) + "' AND datetime >= '" + SQLUtils.escape(dt) + "';") > Core.Settings[BasicSiteAuth.SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD].get<int>();
        }
        /// <summary>
        /// Persists a model to the database. Once a model has been persisted, it cannot be modified - thus you cannot
        /// load a model and then call this method.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True if persisted, false if not persisted.</returns>
        public bool save(Connector conn)
        {
            if (!modified || loaded)
                return false;
            // Compile query
            SQLCompiler sql = new SQLCompiler();
            sql["ip"] = ip;
            sql["datetime"] = datetime;
            sql["type"] = getAuthTypeStr(type);
            // Execute
            sql.executeInsert(conn, "bsa_authentication_failed_attempts");
            modified = false;
            return true;
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Converts a string to an auth-type enum value.
        /// </summary>
        /// <param name="data">The raw string data.</param>
        /// <returns>The auth-type enum value.</returns>
        public static AuthType getAuthType(string data)
        {
            return (AuthType)Enum.Parse(typeof(AuthType), data);
        }
        /// <summary>
        /// Converts an auth-type value to a string.
        /// </summary>
        /// <param name="type">The auth-type value to be converted/</param>
        /// <returns>The string representation.</returns>
        public static string getAuthTypeStr(AuthType type)
        {
            return ((int)type).ToString();
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The IP address of the host which failed to authenticate.
        /// 
        /// IPv4 and IPv6 (including IPv4 tunneling) are supported.
        /// </summary>
        public string IP
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
                modified = true;
            }
        }
        /// <summary>
        /// The date and time of when the host failed to authenticate.
        /// </summary>
        public DateTime DateTime
        {
            get
            {
                return datetime;
            }
            set
            {
                datetime = value;
                modified = true;
            }
        }
        /// <summary>
        /// The type of authentication failure.
        /// </summary>
        public AuthType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                modified = true;
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