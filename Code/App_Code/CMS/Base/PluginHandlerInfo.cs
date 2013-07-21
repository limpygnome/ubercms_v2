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
 *      File:           Plugins.cs
 *      Path:           /App_Code/CMS/Plugins/PluginHandlerInfo.cs
 * 
 *      Change-Log:
 *                      2013-06-28      Created initial class.
 *                      2013-06-30      Added setting and saving.
 *                      2013-07-01      Properties can now be set.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Changed plugin identifier to support UUID.
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
        // Fields **************************************************************************************************
        private UUID    uuid;               // The identifier of the plugin; used internally only.
        private bool    requestStart,
                        requestEnd,
                        pageError,
                        pageNotFound,
                        cmsStart,
                        cmsEnd,
                        cmsPluginAction;
        int             cycleInterval;
        // Methods - Constructors **********************************************************************************
        public PluginHandlerInfo() { }
        /// <summary>
        /// Creates a new model to represent a plugin's handler information.
        /// </summary>
        /// <param name="uuid">Universally unqiue identifier (UUID) of the plugin.</param>
        /// <param name="requestStart">Indicates if the request-start handler is invoked.</param>
        /// <param name="requestEnd">Indicates if the request-end handler is invoked.</param>
        /// <param name="pageError">Indicates if the page-error handler is invoked.</param>
        /// <param name="pageNotFound">Indicates if the page-not-found handler is invoked.</param>
        /// <param name="cmsStart">Indicates if the cms-start handler is invoked.</param>
        /// <param name="cmsEnd">Indicates if the cms-end handler is invoked.</param>
        /// <param name="cmsPluginAction">Indicates if the cms-plugin-action handler is invoked.</param>
        /// <param name="cycleInterval">Indicates if the cycle handler is invoked if greater than zero; the amount greater than zero is the delay in milliseconds between invoking the handler.</param>
        public PluginHandlerInfo(UUID uuid, bool requestStart, bool requestEnd, bool pageError, bool pageNotFound, bool cmsStart, bool cmsEnd, bool cmsPluginAction, int cycleInterval)
        {
            this.uuid = uuid;
            this.requestStart = requestStart;
            this.requestEnd = requestEnd;
            this.pageError = pageError;
            this.pageNotFound = pageNotFound;
            this.cmsStart = cmsStart;
            this.cmsEnd = cmsEnd;
            this.cmsPluginAction = cmsPluginAction;
            this.cycleInterval = cycleInterval;
        }
        // Methods *************************************************************************************************
        public void save(Connector conn)
        {
            lock(this) // Lock this handler information instance until we've updated the database
                conn.queryExecute("UPDATE cms_plugin_handlers SET request_start='" + (requestStart ? "!" : "0") + "', request_end='" + (requestEnd ? "1" : "0") + "', page_error='" + (pageError ? "1" : "0") + "', page_not_found='" + (pageNotFound ? "1" : "0") + "', cms_start='" + (cmsStart ? "1" : "0") + "', cms_end='" + (cmsEnd ? "1" : "0") + "', cms_plugin_action='" + (cmsPluginAction ? "1" : "0") + "', cycle_interval='" + SQLUtils.escape(cycleInterval.ToString()) + "' WHERE uuid=" + uuid.SQLValue + ";");
        }
        // Methods - Properties ************************************************************************************
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
            }
        }
        /// <summary>
        /// Indicates if the plugin's cms-start handler should be invoked after the cms's core has started.
        /// </summary>
        public bool CmsStart
        {
            get
            {
                return cmsStart;
            }
            set
            {
                cmsStart = value;
            }
        }
        /// <summary>
        /// Indicates if the plugin's cms-end handler should be invoked before the cms's core is stopped.
        /// </summary>
        public bool CmsEnd
        {
            get
            {
                return cmsEnd;
            }
            set
            {
                cmsEnd = value;
            }
        }
        /// <summary>
        /// Indicates if the plugin's cms-action handler should be invoked when any plugin action occurs
        /// (enable/disable/uninstall/install).
        /// </summary>
        public bool CmsPluginAction
        {
            get
            {
                return cmsPluginAction;
            }
            set
            {
                cmsPluginAction = value;
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
            }
        }
    }
}
