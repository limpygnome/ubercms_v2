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
 *      Path:           /App_Code/CMS/Plugin/Plugin.cs
 * 
 *      Change-Log:
 *                      2013-06-27      Created initial class.
 *                      2013-06-28      Added handler information.
 *                      2013-06-29      Finished initial class.
 * 
 * *********************************************************************************************************************
 * Base class for all plugins. This contains information about the plugin and methods to be implemented as handlers.
 * *********************************************************************************************************************
 */
using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS
{
	namespace Plugins
	{
        /// <summary
        /// Base class for all plugins. This contains information about the plugin and methods to be implemented as
        /// handlers.
        /// </summary>
		public abstract class Plugin
		{
			// Enums ***************************************************************************************************
			public enum PluginState
			{
				NotInstalled = 0,
				Disabled = 1,
				Enabled = 2
			}
            public enum PluginAction
            {
                PreInstall,
                PostInstall,
                PreUninstall,
                PostUninstall,
                PreDisable,
                PostDisable,
                PreEnable,
                PostEnable
            }
			// Fields **************************************************************************************************
			private int pluginid;                       // The identifier of the plugin.
			private PluginState state;                  // The state of the plugin.
            private PluginHandlerInfo handlerInfo;      // The handler information of the plugin.
            private DateTime lastCycled;                // The last time the plugin cycled; DateTime.Min as value means the plugin has yet to cycle.
            private string title;                       // The title of the plugin.
            // Methods - Constructors **********************************************************************************
            public Plugin(int pluginid, string title, PluginState state, PluginHandlerInfo handlerInfo)
            {
                this.pluginid = pluginid;
                this.title = title;
                this.state = state;
                this.handlerInfo = handlerInfo;
                this.lastCycled = DateTime.MinValue;
            }
			// Methods - Abstract - State ******************************************************************************
			/// <summary>
			/// Invoked when the plugin is to be enabled; no checking of the plugin state is required. Return
			/// true if successful or false if the plugin cannot be enabled.
			/// </summary>
			/// <param name="conn">Database connector.</param>
			/// <param name="message">Message output.</param>
            public virtual bool enable(Connector conn, ref string errorMessage)
			{
				errorMessage = "Not implemented.";
				return false;
			}
			/// <summary>
			/// Invoked when the current plugin is to be disabled; no checking of the plugin state is required.
			/// </summary>
			/// <param name="conn">Database connector.</param>
			/// <param name="message">Message output.</param>
            /// <returns>True if successful or false if the plugin failed to be disabled.</returns>
            public virtual bool disable(Connector conn, ref string message)
			{
				message = "Not implemented.";
				return false;
			}
            /// <summary>
            /// Invoked wehn the current plugin is to be uninstalled; no checking of the plugin state is required.
            /// </summary>
            /// <param name="conn">Database connector.</param>
            /// <param name="message">Message output.</param>
            /// <returns>True if successful or false if the plugin failed to be uninstalled.</returns>
            public virtual bool uninstall(Connector conn, ref string message)
			{
				message = "Not implemented.";
				return false;
			}
            /// <summary>
            /// Invoked when the current plugin is to be installed; no checking of the plugin state is required.
            /// </summary>
            /// <param name="conn">Database connector.</param>
            /// <param name="message">Message output.</param>
            /// <returns>True if successful or false if the plugin failed to be disabled.</returns>
            public virtual bool install(Connector conn, ref string message)
			{
				message = "Not implemented.";
				return false;
			}
			// Methods - Abstract - Handlers - CMS *********************************************************************
            /// <summary>
            /// Invoked when the CMS's core has started.
            /// </summary>
            /// <param name="conn">Database connector.</param>
            /// <returns></returns>
            public virtual bool handler_cmsStart(Connector conn)
			{
				return true;
			}
            /// <summary>
            /// Invoked before the CMS's core has stopped.
            /// </summary>
            /// <param name="conn">Database connector.</param>
			public virtual void handler_cmsEnd(Connector conn)
			{
			}
            /// <summary>
            /// Invoked at intervals, as defined by the cycle-interval in the plugin's handler information.
            /// </summary>
            public virtual void handler_cmsCycle()
			{
			}
            /// <summary>
            /// Invoked before/post enable/disable/install/uninstall of any plugin apart of the CMS.
            /// </summary>
            /// <param name="conn">Database connector.</param>
            /// <param name="action">The action being, or has been, performed.</param>
            /// <returns>If this is a pre-action, you can return false to abort the process. No affect on post actions.</returns>
            public virtual bool handler_cmsPluginAction(Connector conn, PluginAction action)
            {
                return false;
            }
			// Methods - Abstract - Handlers - Requests ****************************************************************
            /// <summary>
            /// Invoked at the start of a request.
            /// </summary>
            /// <param name="data">The data of the request being served.</param>
            public virtual void handler_requestStart(Data data)
			{
			}
            /// <summary>
            /// Invoked at the end of a request, before (possibly) writing content to the client.
            /// </summary>
            /// <param name="data"></param>
            public virtual void handler_requestEnd(Data data)
			{
			}
            /// <summary>
            /// Invoked when a URL has been invoked of which this plugin is to handle, as dictated by either URL
            /// rewriting or the CMS's home-page/default URL.
            /// </summary>
            /// <param name="data">Database connector.</param>
            /// <returns>True if handled by this method, false if not handled.</returns>
            public virtual bool handler_handleRequest(Data data)
			{
				return false;
			}
            /// <summary>
            /// Invoked when a page-error occurs.
            /// </summary>
            /// <param name="data">Database connector.</param>
            /// <returns>True if handled by this method, false if not handled.</returns>
            public virtual bool handler_handlePageError(Data data, Exception ex)
			{
				return false;
			}
            /// <summary>
            /// Invoked when a page cannot be found/
            /// </summary>
            /// <param name="data">Database connector.</param>
            /// <returns>True if handled by this method, false if not handled.</returns>
            public virtual bool handler_handlePageNotFound(Data data)
			{
				return false;
			}
			// Methods - Properties ************************************************************************************
            /// <summary>
            /// The identifier of the plugin.
            /// </summary>
			public int PluginID
			{
				get
				{
					return pluginid;
				}
			}
            /// <summary>
            /// The plugin's title.
            /// </summary>
            public string Title
            {
                get
                {
                    return title;
                }
            }
            /// <summary>
            /// The state of the plugin.
            /// </summary>
			public PluginState State 
			{
				get
				{
					return state;
				}
			}
            /// <summary>
            /// The handler information about the plugin.
            /// </summary>
            public PluginHandlerInfo HandlerInfo
            {
                get
                {
                    return handlerInfo;
                }
            }
            /// <summary>
            /// The date-time at which the plugin's cycler handler was last invoked. If this value is DateTime.Min,
            /// the handler has not yet been invoked.
            /// </summary>
            public DateTime LastCycled
            {
                get
                {
                    return lastCycled;
                }
            }
		}
	}
}