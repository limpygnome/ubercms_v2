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
 *                      2013-09-23      Finished initial class.
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
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            UUID = 1,
            RequestStart = 2,
            RequestEnd = 4,
            PageError = 8,
            PageNotFound = 16,
            PluginStart = 32,
            PluginStop = 64,
            PluginAction = 128,
            CycleInterval = 256
        };
        // Fields ******************************************************************************************************
        private UUID    uuid;               // The identifier of the plugin; used internally only.
        private bool    persisted;          // Indicates if the model has been persisted to the database.
        private Fields  modified;           // Indicates if the model has been modified.
        private bool    requestStart,
                        requestEnd,
                        pageError,
                        pageNotFound,
                        pluginStart,
                        pluginStop,
                        pluginAction;
        int             cycleInterval;
        // Methods - Constructors **************************************************************************************
        /// <summary>
        /// Creates a new model to represent a plugin's handler information.
        /// </summary>
        public PluginHandlerInfo()
        {
            this.modified = Fields.None;
            this.persisted = false;
        }
        /// <summary>
        /// Creates a new model to represent a plugin's handler information.
        /// </summary>
        /// <param name="uuid">Universally unqiue identifier (UUID) of the plugin.</param>
        public PluginHandlerInfo(UUID uuid)
        {
            this.modified = Fields.UUID;
            this.persisted = false;
            this.uuid = uuid;
        }
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
            this.modified = Fields.None;
            this.persisted = false;
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

            phi.uuid = UUID.parse(data["uuid"]);
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
        /// <param name="forcePersist">Indicates if the model should be forcibly persisted, regardless of being modified. </param>
        /// <returns>True = persisted, false = not persisted.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (modified == Fields.None)
                    return false;
                // COmpile SQL
                SQLCompiler sql = new SQLCompiler();
                if((modified & Fields.RequestStart) == Fields.RequestStart)
                    sql["request_start"] = requestStart ? "1" : "0";
                if ((modified & Fields.RequestEnd) == Fields.RequestEnd)
                    sql["request_end"] = requestEnd ? "1" : "0";
                if ((modified & Fields.PageError) == Fields.PageError)
                    sql["page_error"] = pageError ? "1" : "0";
                if ((modified & Fields.PageNotFound) == Fields.PageNotFound)
                    sql["page_not_found"] = pageNotFound ? "1" : "0";
                if ((modified & Fields.PluginStart) == Fields.PluginStart)
                    sql["plugin_start"] = pluginStart ? "1" : "0";
                if ((modified & Fields.PluginStop) == Fields.PluginStop)
                    sql["plugin_stop"] = pluginStop ? "1" : "0";
                if ((modified & Fields.PluginAction) == Fields.PluginAction)
                    sql["plugin_action"] = pluginAction ? "1" : "0";
                if ((modified & Fields.CycleInterval) == Fields.CycleInterval)
                    sql["cycle_interval"] = cycleInterval;
                // Execute SQL
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
                modified = Fields.None;
                return true;
            }
        }
        /// <summary>
        /// Unpersists the model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            lock (this)
            {
                PreparedStatement ps = new PreparedStatement("DELETE FROM cms_plugin_handlers WHERE uuid=?uuid;");
                ps["uuid"] = uuid.Bytes;
                conn.queryExecute(ps);
                persisted = false;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The UUID of the plugin of this handler information.
        /// 
        /// Note: setting this property when the model has been persisted will have no effect.
        /// </summary>
        public UUID UUID
        {
            get
            {
                return uuid;
            }
            set
            {
                lock (this)
                {
                    if (!persisted)
                    {
                        uuid = value;
                        modified |= Fields.UUID;
                    }
                }
            }
        }
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
                lock (this)
                {
                    requestStart = value;
                    modified |= Fields.RequestStart;
                }
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
                lock (this)
                {
                    requestEnd = value;
                    modified |= Fields.RequestEnd;
                }
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
                lock (this)
                {
                    pageError = value;
                    modified |= Fields.PageError;
                }
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
                lock (this)
                {
                    pageNotFound = value;
                    modified |= Fields.PageNotFound;
                }
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
                lock (this)
                {
                    pluginStart = value;
                    modified |= Fields.PluginStart;
                }
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
                lock (this)
                {
                    pluginStop = value;
                    modified |= Fields.PluginStop;
                }
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
                lock (this)
                {
                    pluginAction = value;
                    modified |= Fields.PluginAction;
                }
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
                lock (this)
                {
                    cycleInterval = value;
                    modified |= Fields.CycleInterval;
                }
            }
        }
        /// <summary>
        /// Indicates if the model has been modified.
        /// </summary>
        public bool IsModified
        {
            get
            {
                return modified != Fields.None;
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
