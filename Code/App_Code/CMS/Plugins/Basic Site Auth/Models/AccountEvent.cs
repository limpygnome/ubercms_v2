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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/models/AccountEvent.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model for account events, used for account auditing.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth.Models
{
    public class AccountEvent
    {
        // Enums *******************************************************************************************************
        public enum Sorting
        {
            EventTypeAscending,
            EventTypeDescending,
            DateTimeAscending,
            DateTimeDescending
        }
        // Fields ******************************************************************************************************
        bool                    persisted,      // Indicates if this model has been persisted to the database.
                                modified;       // Indicates if any data has been modified.
        int                     eventid;        // The identifier of this model in the database.
        AccountEventType        eventType;      // The type of event.
        DateTime                datetime;       // The date and time of the event.
        int                     userid;         // The identifier of the account primarily involved in the event (the owner).
        object                  param1,         // Anonymous data parameter.
                                param2;         // Anonymous data parameter.
        SettingsNode.DataType   param1DataType, // The data-type of param1.
                                param2DataType; // The data-type of param2.
        // Methods - Constructors **************************************************************************************
        public AccountEvent()
        {
            this.persisted = this.modified = false;
            this.eventType = null;
            this.eventid = -1;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Creates and persists a new account event.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="bsa">The BSA plugin.</param>
        /// <param name="typeUUID">The account event type UUID as a string with no hyphen's in upper-case.</param>
        /// <param name="datetime">The date and time of the event.</param>
        /// <param name="userID">The identifier of the user affected.</param>
        /// <param name="param1">Parameter 1, can be null.</param>
        /// <param name="param1DT">The data-type of parameter 1.</param>
        /// <param name="param2">Parameter 2, can be null.</param>
        /// <param name="param2DT">The data-type of parameter 2.</param>
        /// <returns>Either the model or null if it failed to persist.</returns>
        public static AccountEvent create(Connector conn, BasicSiteAuth bsa, string typeUUID, DateTime datetime, int userID, object param1, SettingsNode.DataType param1DT, object param2, SettingsNode.DataType param2DT)
        {
            try
            {
                AccountEvent at = new AccountEvent();
                at.Type = bsa.AccountEventTypes[typeUUID];
                at.DateTime = datetime;
                at.UserID = userID;
                at.Param1 = param1;
                at.Param1DataType = param1DT;
                at.Param2 = param2;
                at.Param2DataType = param2DT;
                return at.save(conn) ? at : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// Loads an event model based on its identifier.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="eventid">Identifier of persisted model.</param>
        /// <returns>An instance or null.</returns>
        public static AccountEvent load(BasicSiteAuth bsa, Connector conn, int eventid)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_view_account_events WHERE eventid='" + SQLUtils.escape(eventid.ToString()) + "'");
            if (result.Count == 1)
                return load(bsa, result[0]);
            else
                return null;
        }
        /// <summary>
        /// Loads a set of events for a user between a range with sorting.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="usr">The user.</param>
        /// <param name="amount">The number of models to load.</param>
        /// <param name="offset">The offset in pages; starting at one.</param>
        /// <param name="sorting">The sorting of events.</param>
        /// <returns>Array of models; may be empty - never null.</returns>
        public static AccountEvent[] loadByUser(BasicSiteAuth bsa, Connector conn, User usr, int amount, int offset, Sorting sorting)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_view_account_events WHERE userid='" + SQLUtils.escape(usr.UserID.ToString()) + "' ORDER BY " + buildSorting(sorting) + " LIMIT " + amount + " OFFSET " + ((offset * amount) - amount));
            return load(bsa, result);
        }
        /// <summary>
        /// Loads a set of events for a user between a range with sorting.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="usr">The user.</param>
        /// <param name="amount">The number of models to load.</param>
        /// <param name="offset">The offset in pages; starting at one.</param>
        /// <param name="sorting">The sorting of events.</param>
        /// <param name="periodStart">The inclusive starting date of events.</param>
        /// <param name="periodEnd">The inclusive ending date of events.</param>
        /// <returns>Array of models; may be empty - never null.</returns>
        public static AccountEvent[] loadByUser(BasicSiteAuth bsa, Connector conn, User usr, int amount, int offset, Sorting sorting, DateTime periodStart, DateTime periodEnd)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_view_account_events WHERE userid='" + SQLUtils.escape(usr.UserID.ToString()) + "' AND datetime >= '" + SQLUtils.escape(periodStart.ToString("YYYY-MM-dd HH:mm:ss")) + "' AND datetime <= '" + SQLUtils.escape(periodEnd.ToString("YYYY-MM-dd HH:mm:ss")) + "' ORDER BY " + buildSorting(sorting) + " LIMIT " + amount + " OFFSET " + ((offset * amount) - amount));
            return load(bsa, result);
        }
        /// <summary>
        /// Loads multiple account events using a series of tuples/result-data.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="result">Series of tuples/result-data.</param>
        /// <returns>An array of account events.</returns>
        private static AccountEvent[] load(BasicSiteAuth bsa, Result result)
        {
            List<AccountEvent> events = new List<AccountEvent>();
            AccountEvent t;
            foreach (ResultRow row in result)
            {
                t = load(bsa, row);
                if (t != null)
                    events.Add(t);
            }
            return events.ToArray();
        }
        /// <summary>
        /// Loads an account event using a database result-row/tuple.
        /// </summary>
        /// <param name="bsa">Basic site authentication.</param>
        /// <param name="data">Result-row/tuple of data.</param>
        /// <returns>Either an instance or null.</returns>
        private static AccountEvent load(BasicSiteAuth bsa, ResultRow data)
        {
            AccountEvent a = new AccountEvent();
            a.eventid = data.get2<int>("eventid");
            a.userid = data.get2<int>("userid");
            a.eventType = bsa.AccountEventTypes[data.get2<string>("type_uuid")];
            a.param1DataType = SettingsNode.parseType(data.get2<string>("param1_datatype"));
            a.param1 = SettingsNode.parseTypeValue(a.param1DataType, data.get2<string>("param1"));
            a.param2DataType = SettingsNode.parseType(data.get2<string>("param2_datatype"));
            a.param2 = SettingsNode.parseTypeValue(a.param2DataType, data.get2<string>("param2"));
            a.datetime = data.get2<DateTime>("datetime");
            return a;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True if persisted, false if failed.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return false;
                // Compile SQL
                SQLCompiler c = new SQLCompiler();
                c["userid"] = userid.ToString();
                c["type_uuid"] = eventType.TypeUUID.Bytes;
                c["datetime"] = datetime;
                c["param1"] = param1 == null ? null : param1.ToString();
                c["param2"] = param2 == null ? null : param2.ToString();
                c["param1_datatype"] = ((int)param1DataType).ToString();
                c["param2_datatype"] = ((int)param2DataType).ToString();
                if (persisted)
                {
                    c.UpdateAttribute = "eventid";
                    c.UpdateValue = eventid;
                    c.executeUpdate(conn, "bsa_account_events");
                }
                else
                {
                    eventid = int.Parse(c.executeInsert(conn, "bsa_account_events", "eventid")[0]["eventid"]);
                    persisted = true;
                }
                modified = false;
                return true;
            }
        }
        /// <summary>
        /// Unpersists the model from the database.
        /// </summary>
        /// <param name="conn">Datbaase connector</param>
        public void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM bsa_account_events WHERE eventid=?eventid;");
            ps["eventid"] = eventid;
            conn.queryExecute(ps);
        }
        /// <summary>
        /// Removes all of the events belonging to a user.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="user">The user of the account events.</param>
        public static void removeAll(Connector conn, User user)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM bsa_account_events WHERE userid=?userid;");
            ps["userid"] = user.UserID;
            conn.queryExecute(ps);
        }
        // Methods *****************************************************************************************************
        private static string buildSorting(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.DateTimeAscending:
                    return "datetime ASC";
                case Sorting.DateTimeDescending:
                    return "datetime DESC";
                case Sorting.EventTypeAscending:
                    return "eventtypeid ASC";
                case Sorting.EventTypeDescending:
                    return "eventtypeid DESC";
                default:
                    throw new InvalidOperationException("Unknown sorting parameter specified!");
            }
        }
        // Methods - Accessors *****************************************************************************************
        /// <summary>
        /// Gets param1 as a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>Param1 typecasted to the specified type; may be default value of type.</returns>
        public T getParam1<T>()
        {
            return param1 == null ? default(T) : (T)param1;
        }
        /// <summary>
        /// Gets param2 as a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>Param2 typecasted to the specified type; may be default value of type.</returns>
        public T getParam2<T>()
        {
            return param2 == null ? default(T) : (T)param2;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of this model in the database.
        /// </summary>
        public int EventID
        {
            get
            {
                return eventid;
            }
        }
        /// <summary>
        /// The type of event.
        /// </summary>
        public AccountEventType Type
        {
            get
            {
                return eventType;
            }
            set
            {
                eventType = value;
                modified = true;
            }
        }
        /// <summary>
        /// The date and time of the event.
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
        /// // The identifier of the account primarily involved in the event (the owner).
        /// </summary>
        public int UserID
        {
            get
            {
                return userid;
            }
            set
            {
                userid = value;
                modified = true;
            }
        }
        /// <summary>
        /// Anonymous data parameter 1.
        /// </summary>
        public object Param1
        {
            get
            {
                return param1;
            }
            set
            {
                param1 = value;
                modified = true;
            }
        }
        public SettingsNode.DataType Param1DataType
        {
            get
            {
                return param1DataType;
            }
            set
            {
                param1DataType = value;
                modified = true;
            }
        }
        /// <summary>
        /// Anonymous data parameter 2.
        /// </summary>
        public object Param2
        {
            get
            {
                return param2;
            }
            set
            {
                param2 = value;
                modified = true;
            }
        }
        public SettingsNode.DataType Param2DataType
        {
            get
            {
                return param2DataType;
            }
            set
            {
                param2DataType = value;
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
        /// <summary>
        /// Indicates if the model has been persisted.
        /// </summary>
        public bool IsPersisted
        {
            get
            {
                return persisted;
            }
        }
    }
}