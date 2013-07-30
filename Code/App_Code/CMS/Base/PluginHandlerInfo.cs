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
 *      Path:           /App_Code/CMS/Base/PluginHandlerInfo.cs
 * 
 *      Change-Log:
 *                      2013-06-28      Created initial class.
 *                      2013-06-30      Added setting and saving.
 *                      2013-07-01      Properties can now be set.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Changed plugin identifier to support UUID.
 *                      2013-07-30      Changed handler properties to reflect changes in plugin class (CMS to plguin
 *                                      handler changes).
 *                                      General major overhaul of class.
 * 
 * *********************************************************************************************************************
 * Stores information about a plugin's handler's, indicating if they should invoked and any parameters.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using UberLib.Connector;

namespace CMS.Base
{
    /// <summary>
    /// Stores information about a plugin's handler's, indicating if they should invoked and any parameters.
    /// </summary>
    public class PluginHandlerInfo
    {
        // Fields ******************************************************************************************************
        private UUID    uuid;               // The identifier of the plugin; used internally only.
        private bool    persisted,          // Indicates if the model has been persisted to the database.
                        modified;           // Indicates if the model has been modified.
        private bool    requestStart,
                        requestEnd,
                        pageError,
                        pageNotFound,
                        pluginStart,
                        pluginStop,
                        pluginAction;
        int             cycleInterval;
        // Methods - Constructors **************************************************************************************
        public PluginHandlerInfo() { }
        /// <summary>
        /// Creates a new model to represent a plugin's handler information.
        /// </summary>
        /// <param name="uuid">Universally unqiue identifier (UUID) of the plugin.</param>
        /// <param name="requestStart">Indicates if the request-start handler is invoked.</param>
        /// <param name="requestEnd">Indicates if the request-end handler is invoked.</param>
        /// <param name="pageError">Indicates if the page-error handler is invoked.</param>
        /// <param name="pageNotFound">Indicates if the page-not-found handler is invoked.</param>
        /// <param name="pluginStart">Indicates if the cms-start handler is invoked.</param>
        /// <param name="pluginStop">Indicates if the cms-end handler is invoked.</param>
        /// <param name="pluginAction">Indicates if the cms-plugin-action handler is invoked.</param>
        /// <param name="cycleInterval">Indicates if the cycle handler is invoked if greater than zero; the amount greater than zero is the delay in milliseconds between invoking the handler.</param>
        public PluginHandlerInfo(UUID uuid, bool requestStart, bool requestEnd, bool pageError, bool pageNotFound, bool pluginStart, bool pluginStop, bool pluginAction, int cycleInterval)
        {
            this.modified = this.persisted = false;
            this.uuid = uuid;
            this.requestStart = requestStart;
            this.requestEnd = requestEnd;
            this.pageError = pageError;
            this.pageNotFound = pageNotFound;
            this.pluginStart = pluginStart;
            this.pluginStop = pluginStop;
            this.pluginAction = pluginAction;
            this.cycleInterval = cycleInterval;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads the plugin handler information for a plugin.
        /// </summary>
        /// <param name="conn">Database connector./param>
        /// <param name="uuid">The identifier of the plugin.</param>
        /// <returns>A model or null.</returns>
        public static PluginHandlerInfo load(Connector conn, UUID uuid)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM cms_plugin_handlers WHERE uuid=?uuid;");
            ps["uuid"] = uuid.Bytes;
            Result result = conn.queryRead(ps);
            if (result.Count == 1)
                return load(result[0]);
            else
                return null;
        }
        /// <summary>
        /// Loads the plugin handler information for a plugin.
        /// </summary>
        /// <param name="data">The database tuple/row of data for the plugin handler.</param>
        /// <returns>A model or null.</returns>
        public static PluginHandlerInfo load(ResultRow data)
        {
            PluginHandlerInfo phi = new PluginHandlerInfo();

            phi.uuid = UUID.createFromHex(data["uuid"]);
            phi.requestStart = data["request_start"] == "1";
            phi.requestEnd = data["request_end"] == "1";
            phi.pageError = data["page_error"] == "1";
            phi.pageNotFound = data["page_not_found"] == "1";
            phi.pluginStart = data["plugin_start"] == "1";
            phi.pluginStop = data["plugin_stop"] == "1";
            phi.pluginAction = data["plugin_action"] == "1";
            phi.cycleInterval = int.Parse(data["cycle_interval"]);

            phi.persisted = true;
            return phi;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void save(Connector conn)
        {
            save(conn, false);
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="forcePersist">Indicates if the model should be forcibly persisted, regardless of being modified. </param>
        public void save(Connector conn, bool forcePersist)
        {
            lock (this)
            {
                if (!modified && !forcePersist)
                    return;
                SQLCompiler sql = new SQLCompiler();
                sql["request_start"] = requestStart ? "1" : "0";
                sql["request_end"] = requestEnd ? "1" : "0";
                sql["page_error"] = pageError ? "1" : "0";
                sql["page_not_found"] = pageNotFound ? "1" : "0";
                sql["plugin_start"] = pluginStart ? "1" : "0";
                sql["plugin_stop"] = pluginStop ? "1" : "0";
                sql["plugin_action"] = pluginAction ? "1" : "0";
                sql["cycle_interval"] = cycleInterval;
                if (persisted)
                {
                    sql.UpdateAttribute = "uuid";
                    sql.UpdateValue = uuid.Bytes;
                    sql.executeUpdate(conn, "cms_plugin_handlers");
                }
                else
                {
                    sql["uuid"] = uuid.Bytes;
                    sql.executeInsert(conn, "cms_plugin_handlers");
                    persisted = true;
                }
                modified = false;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Indicates if the plugin's request-start handler should be invoked for every request.
        /// </summary>
        public bool RequestStart
        {
            get
            {
                return requestStart;
            }
            set
            {
                requestStart = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the plugins' request-end handler should be invoked for every request.
        /// </summary>
        public bool RequestEnd
        {
            get
            {
                return requestEnd;
            }
            set
            {
                requestEnd = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the plugin's request-error handler should be considered for invocation when a page error
        /// occurs.
        /// </summary>
        public bool PageError
        {
            get
            {
                return pageError;
            }
            set
            {
                pageError = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the plugin's page-not-found handler should be considered for invocation when a page not found
        /// error occurs.
        /// </summary>
        public bool PageNotFound
        {
            get
            {
                return pageNotFound;
            }
            set
            {
                pageNotFound = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the plugin's plugin-start handler should be invoked when the plugin is loaded.
        /// </summary>
        public bool PluginStart
        {
            get
            {
                return pluginStart;
            }
            set
            {
                pluginStart = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the plugin's plugin-stop handler should be invoked when the plugin is unloaded.
        /// </summary>
        public bool PluginStop
        {
            get
            {
                return pluginStop;
            }
            set
            {
                pluginStop = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates if the plugin's plugin-action handler should be invoked when any plugin action occurs
        /// (enable/disable/uninstall/install).
        /// </summary>
        public bool PluginAction
        {
            get
            {
                return pluginAction;
            }
            set
            {
                pluginAction = value;
                modified = true;
            }
        }
        /// <summary>
        /// Indicates how often the plugin's cycler handler should be invoked; if this is greater than zero, it will
        /// not be invoked.
        /// </summary>
        public int CycleInterval
        {
            get
            {
                return cycleInterval;
            }
            set
            {
                cycleInterval = value;
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
