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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/AccountEventTypes.cs
 * 
 *      Change-Log:
 *                      2013-07-29      Finished initial class.
 * 
 * *********************************************************************************************************************
 * Stores a collection of account event type models.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// Stores a collection of account event type models.
    /// </summary>
    public class AccountEventTypes
    {
        // Fields ******************************************************************************************************
        private Dictionary<int, AccountEventType> types;    // Cached list of account event types.
        // Methods - Constructors **************************************************************************************
        private AccountEventTypes()
        { }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Adds a persisted account event type to the collection.
        /// </summary>
        /// <param name="type">The account event type to be added.</param>
        /// <returns></returns>
        public bool add(AccountEventType type)
        {
            lock (this)
            {
                if (!type.IsPersisted)
                    return false;
                else if (types.ContainsKey(type.EventTypeID))
                    return false;
                types.Add(type.EventTypeID, type);
                return true;
            }
        }
        /// <summary>
        /// Removes an event type from both the collection and the database.
        /// </summary>
        /// <param name="type">The account event type to be removed.</param>
        public void remove(AccountEventType type)
        {
            lock (this)
            {
                Core.Connector.queryExecute("DELETE FROM bsa_account_event_types WHERE eventtypeid='" + SQLUtils.escape(type.EventTypeID.ToString()) + "';");
                if (types.ContainsKey(type.EventTypeID))
                    types.Remove(type.EventTypeID);
            }
        }
        /// <summary>
        /// Indicates if an account even type exists in the collection. This is based on the eventTypeID.
        /// </summary>
        /// <param name="type">The account event type to be tested.</param>
        /// <returns>True = exists, false = does not exist.</returns>
        public bool contains(AccountEventType type)
        {
            return types.ContainsKey(type.EventTypeID);
        }
        /// <summary>
        /// Reloads the collection of persisted models from the database; this does not save any changes made to any
        /// items in the collection!
        /// </summary>
        /// <param name="conn"></param>
        public void reload(Connector conn)
        {
            lock (this)
            {
                types.Clear();
                AccountEventType t;
                foreach (ResultRow row in conn.queryRead("SELECT * FROM bsa_acount_event_types;"))
                {
                    t = AccountEventType.load(row);
                    if (t != null)
                        types.Add(t.EventTypeID, t);
                }
            }
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Creates a new instance of this collection, loaded with all the persisted account event types from the
        /// database.
        /// </summary>
        /// <param name="conn">Database connection.</param>
        /// <returns>An instance of this collection.</returns>
        public static AccountEventTypes load(Connector conn)
        {
            AccountEventTypes at = new AccountEventTypes();
            at.reload(conn);
            return at;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Retrieves an account event type based on the eventTypeID; returns null if a model with the identifier cannot
        /// be found.
        /// </summary>
        /// <param name="eventTypeID">The identifier of the account event type to return.</param>
        /// <returns>The account event type or null.</returns>
        public AccountEventType this[int eventTypeID]
        {
            get
            {
                lock (this)
                    return types.ContainsKey(eventTypeID) ? types[eventTypeID] : null;
            }
        }
    }
}