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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/models/AccountEventTypes.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * Stores a collection of account event type models.
 * 
 * Thread-safe.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth.Models
{
    /// <summary>
    /// Stores a collection of account event type models.
    /// 
    /// Thread-safe.
    /// </summary>
    public class AccountEventTypes
    {
        // Fields ******************************************************************************************************
        private Dictionary<string,AccountEventType> types;    // Cached list of account event types - UUID string with no hyphens,model.
        // Methods - Constructors **************************************************************************************
        private AccountEventTypes()
        {
            this.types = new Dictionary<string,AccountEventType>();
        }
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
                else if (types.ContainsKey(type.TypeUUID.Hex))
                    return false;
                types.Add(type.TypeUUID.Hex, type);
                return true;
            }
        }
        /// <summary>
        /// Removes an event type from both the collection and the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="type">The account event type to be removed.</param>
        public void remove(Connector conn, AccountEventType type)
        {
            lock (this)
            {
                PreparedStatement ps = new PreparedStatement("DELETE FROM bsa_account_event_types WHERE type_uuid=?typeid;");
                ps["typeid"] = type.TypeUUID.Bytes;
                conn.queryExecute(ps);
                if (types.ContainsKey(type.TypeUUID.Hex))
                    types.Remove(type.TypeUUID.Hex);
            }
        }
        /// <summary>
        /// Indicates if an account even type exists in the collection. This is based on the type UUID.
        /// </summary>
        /// <param name="type">The account event type to be tested.</param>
        /// <returns>True = exists, false = does not exist.</returns>
        public bool contains(AccountEventType type)
        {
            return types.ContainsKey(type.TypeUUID.Hex);
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
                foreach (ResultRow row in conn.queryRead("SELECT * FROM bsa_view_aet;"))
                {
                    t = AccountEventType.load(row);
                    if (t != null)
                        types.Add(t.TypeUUID.Hex.ToUpper(), t);
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
        /// Retrieves an account event type based on the typeUUID; returns null if a model with the identifier cannot
        /// be found.
        /// </summary>
        /// <param name="typeUUID">The identifier of the account event type to return, as a string with no hyphen's.</param>
        /// <returns>The account event type or null.</returns>
        public AccountEventType this[string typeUUID]
        {
            get
            {
                lock (this)
                    return types.ContainsKey(typeUUID) ? types[typeUUID] : null;
            }
        }
    }
}