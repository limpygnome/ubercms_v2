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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/UserGroups.cs
 * 
 *      Change-Log:
 *                      2013-07-07      Created initial class.
 *                      2013-07-23      Changed way user-groups are added, added contains method.
 * 
 * *********************************************************************************************************************
 * A data-structure for managing the collection of user-groups for the basic site authentication plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UberLib.Connector;
using CMS.Base;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A data-structure for managing the collection of user-groups for the basic site authentication plugin.
    /// </summary>
    public class UserGroups
    {
        // Fields ******************************************************************************************************
        private Dictionary<int, UserGroup> groups;      // Cached list of user-groups.
        // Methods - Constructors **************************************************************************************
        private UserGroups()
        {
            groups = new Dictionary<int, UserGroup>();
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Adds a new user-group; note: the specified group-id will be ignored!
        /// </summary>
        /// <param name="ug">The user-group to be added.</param>
        /// <returns>True if successful, false if failed.</returns>
        public bool add(UserGroup ug)
        {
            lock (this)
            {
                if (!ug.IsPersisted)
                    return false;
                else if (groups.ContainsKey(ug.GroupID))
                    return false;
                groups.Add(ug.GroupID, ug);
                return true;
            }
        }
        /// <summary>
        /// Removes a user-group. Also unpersists the data from the database.
        /// </summary>
        /// <param name="ug">The user-group to be removed.</param>
        public void remove(UserGroup ug)
        {
            lock (this)
            {
                Core.Connector.queryExecute("DELETE FROM bsa_user_groups WHERE groupid='" + SQLUtils.escape(ug.GroupID.ToString()) + "';");
                if (groups.ContainsKey(ug.GroupID))
                    groups.Remove(ug.GroupID);
            }
        }
        /// <summary>
        /// Checks if the group, based on the group ID, exists.
        /// </summary>
        /// <param name="ug">The user group to check.</param>
        /// <returns>True = user-group is within the collection, false = the user group is not in the collection.</returns>
        public bool contains(UserGroup ug)
        {
            return groups.ContainsKey(ug.GroupID);
        }
        /// <summary>
        /// Reloads the collection of user-groups from the database.
        /// </summary>
        /// <param name="conn"></param>
        public void reload(Connector conn)
        {
            lock (this)
            {
                groups.Clear();
                UserGroup ug;
                foreach (ResultRow data in conn.queryRead("SELECT * FROM bsa_user_groups;"))
                {
                    ug = UserGroup.load(data);
                    if (ug != null)
                        groups.Add(ug.GroupID, ug);
                }
            }
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Loads a new instance of the user-groups collection.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>An instance of this collection with the user-groups loaded from the database.</returns>
        public static UserGroups load(Connector conn)
        {
            UserGroups ugs = new UserGroups();
            ugs.reload(conn);
            return ugs;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Fetch a user-group based on its identifier/groupid.
        /// </summary>
        /// <param name="groupID">The user-group's identifier.</param>
        /// <returns>The user-group if it exists, else null is returned.</returns>
        public UserGroup this[int groupID]
        {
            get
            {
                lock(this)
                    return groups.ContainsKey(groupID) ? groups[groupID] : null;
            }
        }
    }
}