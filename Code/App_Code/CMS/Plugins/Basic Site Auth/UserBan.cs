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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/UserBan.cs
 * 
 *      Change-Log:
 *                      2013-07-24      Finished initial class.
 * 
 * *********************************************************************************************************************
 * Used as a model to represent user bans. Infinite bans should set the datetime-end to null; this will be set to
 * C#'s DateTime.MaxValue.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    public class UserBan
    {
        // Fields ******************************************************************************************************
        private bool        persisted,      // Indicates if this model has been persisted.
                            modified;       // Indicates if this model has been modified.
        private int         banid,          // The identifier of the ban; -1 if new. This should only have read-only access.
                            userid,         // User identifier.
                            bannedBy;       // User identifier of the user who created the ban.
        private string      reason;         // The reason for the ban.
        private DateTime    datetimeStart,  // The datetime of when the ban was created.
                            datetimeEnd;    // The datetime of when the ban ends.
        // Methods - Constructors **************************************************************************************
        public UserBan()
        {
            this.persisted = this.modified = false;
        }
        // Methods - Database ******************************************************************************************
        /// <summary>
        /// Loads all the bans for a user.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="user">The user of the bans.</param>
        /// <returns>An array of all the bans belonging to a user.</returns>
        public static UserBan[] loadByUser(Connector conn, User user)
        {
            List<UserBan> bans = new List<UserBan>();
            UserBan ub;
            foreach (ResultRow row in conn.queryRead("SELECT * FROM bsa_user_bans WHERE userid='" + SQLUtils.escape(user.UserID.ToString()) + "';"))
            {
                ub = load(row);
                if (ub != null)
                    bans.Add(ub);
            }
            return bans.ToArray();
        }
        /// <summary>
        /// Loads all of the active bans for a user.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="user">The user of the bans.</param>
        /// <returns>An array of all the bans currently enforced.</returns>
        public static UserBan[] loadByUserActive(Connector conn, User user)
        {
            List<UserBan> bans = new List<UserBan>();
            UserBan ub;
            foreach (ResultRow row in conn.queryRead("SELECT * FROM bsa_user_bans WHERE userid='" + SQLUtils.escape(user.UserID.ToString()) + "' AND (datetime_end > CURRENT_TIMESTAMP OR datetime_end=NULL);"))
            {
                ub = load(row);
                if (ub != null)
                    bans.Add(ub);
            }
            return bans.ToArray();
        }
        /// <summary>
        /// Loads a user-ban from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="banid">Ban identifier.</param>
        /// <returns>Returns the model for the ban or returns null.</returns>
        public static UserBan load(Connector conn, int banid)
        {
            Result data = conn.queryRead("SELECT * FROM bsa_user_bans WHERE banid='" + SQLUtils.escape(banid.ToString()) + "';");
            if (data.Count == 1)
                return load(data[0]);
            else
                return null;
        }
        /// <summary>
        /// Loads a user-ban from the database.
        /// </summary>
        /// <param name="row">Database row.</param>
        /// <returns>Returns the model for the ban or returns null.</returns>
        public static UserBan load(ResultRow row)
        {
            UserBan ub = new UserBan();
            ub.persisted = true;
            ub.banid = row.get2<int>("banid");
            ub.userid = row.get2<int>("userid");
            ub.reason = row.isNull("reason") ? string.Empty : row["reason"];
            ub.datetimeStart = row.get2<DateTime>("datetime_start");
            ub.datetimeEnd = row.isNull("datetime_end") ? DateTime.MaxValue : row.get2<DateTime>("datetime_end");
            ub.bannedBy = row.isNull("banned_by") ? -1 : row.get2<int>("banned_by");
            return ub;
        }
        /// <summary>
        /// Persists the data to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void save(Connector conn)
        {
            SQLCompiler sql = new SQLCompiler();
            sql["userid"] = userid.ToString();
            sql["banned_by"] = bannedBy == -1 ? null : bannedBy.ToString();
            sql["reason"] = reason.Length == 0 ? null : reason;
            sql["datetime_start"] = datetimeStart.ToString("YYYY-MM-dd HH:mm:ss");
            sql["datetime_end"] = datetimeEnd == DateTime.MaxValue ? null : datetimeEnd.ToString("YYYY-MM-dd HH:mm:ss");
            if (persisted)
            {
                sql.UpdateAttribute = "banid";
                sql.UpdateValue = banid;
                sql.executeUpdate(conn, "bsa_user_bans");
            }
            else
            {
                banid = sql.executeInsert(conn, "bsa_user_bans", "banid")[0].get2<int>("banid");
                persisted = true;
            }
            modified = false;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The datetime of when the ban was created.
        /// </summary>
        public DateTime DateTimeStart
        {
            get
            {
                return datetimeStart;
            }
            set
            {
                datetimeStart = value;
                modified = true;
            }
        }
        /// <summary>
        /// The datetime of when the ban ends.
        /// </summary>
        public DateTime DateTimeEnd
        {
            get
            {
                return datetimeEnd;
            }
            set
            {
                datetimeEnd = value;
                modified = true;
            }
        }
        /// <summary>
        /// The reason the user was banned.
        /// </summary>
        public string Reason
        {
            get
            {
                return reason;
            }
            set
            {
                reason = value;
                modified = true;
            }
        }
        /// <summary>
        /// The identifier of the user who created the ban; -1 if the userid is null.
        /// </summary>
        public int BannedBy
        {
            get
            {
                return bannedBy;
            }
            set
            {
                bannedBy = value;
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