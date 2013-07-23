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
        private bool        modified;       // A flag for indicating if the data has been modified.
        private int         banid,          // The identifier of the ban; -1 if new. This should only have read-only access.
                            userid,         // User identifier.
                            bannedBy;       // User identifier of the user who created the ban.
        private string      reason;         // The reason for the ban.
        private DateTime    datetimeStart,  // The datetime of when the ban was created.
                            datetimeEnd;    // The datetime of when the ban ends.
        // Methods - Constructors **************************************************************************************
        private UserBan()
        { }
        public UserBan(int bannedBy, string reason, DateTime datetimeStart, DateTime datetimeEnd)
        {
            this.modified = false;
            this.banid = -1;
            this.bannedBy = bannedBy;
            this.reason = reason;
            this.datetimeStart = datetimeStart;
            this.datetimeEnd = datetimeEnd;
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
            ub.banid = row.get2<int>("banid");
            ub.userid = row.get2<int>("userid");
            ub.reason = row["reason"];
            ub.datetimeStart = row.get2<DateTime>("datetime_start");
            ub.datetimeEnd = row.get2<DateTime>("datetime_end");
            ub.bannedBy = row.get2<int>("banned_by");
            return ub;
        }
        /// <summary>
        /// Persists the data to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void save(Connector conn)
        {
            SQLCompiler compiler = new SQLCompiler();
            compiler["banned_by"] = bannedBy.ToString();
            compiler["reason"] = reason;
            compiler["datetime_start"] = datetimeStart.ToString("YYYY-MM-dd HH:mm:ss");
            compiler["datetime_end"] = datetimeEnd.ToString("YYYY-MM-dd HH:mm:ss");
            compiler["userid"] = userid.ToString();
            if (banid < 0)
                conn.queryExecute("INSERT INTO bsa_user_bans (userid, reason, datetime_start, datetime_end, banned_by) VALUES('" + SQLUtils.escape(userid.ToString()) + "', '" + SQLUtils.escape(reason) + "', '" + SQLUtils.escape(datetimeStart.ToString()) + "', '" + SQLUtils.escape(datetimeEnd.ToString()) + "', " + (bannedBy < 0 ? "NULL" : "'" + SQLUtils.escape(bannedBy.ToString()) + "'") + ");");
            else
                conn.queryExecute("UPDATE bsa_user_bans SET userid='" + SQLUtils.escape(userid.ToString()) + "', reason='" + SQLUtils.escape(reason) + "', datetime_start='" + SQLUtils.escape(datetimeStart.ToString()) + "', datetime_end='" + SQLUtils.escape(datetimeEnd.ToString()) + "', banned_by='" + SQLUtils.escape(bannedBy.ToString()) + "' WHERE banid='" + SQLUtils.escape(banid.ToString()) + "';");
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
    }
}