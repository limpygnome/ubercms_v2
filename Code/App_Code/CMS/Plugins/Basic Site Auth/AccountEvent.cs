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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/AccountEvent.cs
 * 
 *      Change-Log:
 *                      2013-07-24      Created initial class.
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

namespace CMS.BasicSiteAuth
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
        /// Loads an event model based on its identifier.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="eventid">Identifier of persisted model.</param>
        /// <returns>An instance or null.</returns>
        public AccountEvent load(BasicSiteAuth bsa, Connector conn, int eventid)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_account_events WHERE eventid='" + SQLUtils.escape(eventid.ToString()) + "'");
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
        /// <param name="offset">The offset in pages.</param>
        /// <param name="sorting">The sorting of events.</param>
        /// <returns>Array of models; may be empty - never null.</returns>
        public AccountEvent[] loadByUser(BasicSiteAuth bsa, Connector conn, User usr, int amount, int offset, Sorting sorting)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_account_events WHERE userid='" + SQLUtils.escape(usr.UserID.ToString()) + "' LIMIT " + amount + " OFFSET " + ((offset * amount) - amount) + " ORDER BY " + buildSorting(sorting));
            return load(bsa, result);
        }
        /// <summary>
        /// Loads a set of events for a user between a range with sorting.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="usr">The user.</param>
        /// <param name="amount">The number of models to load.</param>
        /// <param name="offset">The offset in pages.</param>
        /// <param name="sorting">The sorting of events.</param>
        /// <param name="periodStart">The inclusive starting date of events.</param>
        /// <param name="periodEnd">The inclusive ending date of events.</param>
        /// <returns>Array of models; may be empty - never null.</returns>
        public AccountEvent[] loadByUser(BasicSiteAuth bsa, Connector conn, User usr, int amount, int offset, Sorting sorting, DateTime periodStart, DateTime periodEnd)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_account_events WHERE userid='" + SQLUtils.escape(usr.UserID.ToString()) + "' AND datetime >= '" + SQLUtils.escape(periodStart.ToString("YYYY-MM-dd HH:mm:ss")) + "' AND datetime <= '" + SQLUtils.escape(periodEnd.ToString("YYYY-MM-dd HH:mm:ss")) + "' LIMIT " + amount + " OFFSET " + ((offset * amount) - amount) + " ORDER BY " + buildSorting(sorting));
            return load(bsa, result);
        }
        /// <summary>
        /// Loads multiple account events using a series of tuples/result-data.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="result">Series of tuples/result-data.</param>
        /// <returns>An array of account events.</returns>
        private AccountEvent[] load(BasicSiteAuth bsa, Result result)
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
        private AccountEvent load(BasicSiteAuth bsa, ResultRow data)
        {
            AccountEvent a = new AccountEvent();
            a.eventid = data.get2<int>("eventid");
            a.userid = data.get2<int>("userid");
            a.eventType = bsa.AccountEventTypes[data.get2<int>("eventtypeid")];
            a.param1 = SettingsNode.parseType(data.get2<string>("param1"));
            return a;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void save(Connector conn)
        {
            if (!modified)
                return;
            SQLCompiler c = new SQLCompiler();
            c["userid"] = userid.ToString();
            c["eventtypeid"] = eventType.EventTypeID.ToString();
            c["datetime"] = datetime.ToString("YYYY-MM-dd HH:mm:ss");
            c["param1"] = param1 == null ? null : param1.ToString();
            c["param2"] = param2 == null ? null : param2.ToString();
            c["param1_datatype"] = ((int)param1DataType).ToString();
            c["param2_datatype"] = ((int)param2DataType).ToString();
            if (persisted)
                conn.queryExecute(c.compileUpdate("bsa_account_events", "eventid='" + SQLUtils.escape(eventid.ToString()) + "'"));
            else
                eventid = (int)conn.queryScalar(c.compileInsert("bsa_account_events", "eventid"));
        }
        // Methods *****************************************************************************************************
        private string buildSorting(Sorting sorting)
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