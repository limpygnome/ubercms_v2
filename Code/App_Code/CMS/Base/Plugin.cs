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
 *      Path:           /App_Code/CMS/Base/Plugin.cs
 * 
 *      Change-Log:
 *                      2013-06-27      Created initial class.
 *                      2013-06-28      Added handler information.
 *                      2013-06-29      Finished initial class.
 *                      2013-06-30      Made changes to message param type of install/uninstall/enable/disable.
 *                                      Added directory data from database.
 *                                      Added properties for some default paths.
 *                                      Renamed Plugin property to avoid conflict with System.IO.
 *                      2013-07-05      Modified default directory paths to be lower-case.
 *                      2013-06-07      Added plugin versioning.
 *                      2013-07-20      Changed pluginid to uuid (universally unique identifier).
 *                                      Moved namespace to Core.Base.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                      2013-07-29      Refactored FullPath to Path.
 *                      2013-08-06      Versioning redone to be database based, less effort when updating.
 * 
 * *********************************************************************************************************************
 * Base class for all plugins. This contains information about the plugin and methods to be implemented as handlers.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Base
{
    /// <summary
    /// Base class for all plugins. This contains information about the plugin and methods to be implemented as
    /// handlers.
    /// </summary>
	public abstract class Plugin
	{
		// Enums *******************************************************************************************************
        /// <summary>
        /// The state of a plugin.
        /// 
        /// Warning: the integer values of this enum are hard-written in the database.
        /// </summary>
		public enum PluginState
		{
			NotInstalled = 0,
			Disabled = 1,
			Enabled = 2
		}
        /// <summary>
        /// The type of action performed on a plugin.
        /// </summary>
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
		// Fields ******************************************************************************************************
        private bool                    stateChanged;       // Indicates if the state of the plugin has changed.
        private UUID                    uuid;               // The universally unique identifier of the plugin.
		private PluginState             state;              // The state of the plugin.
        private PluginHandlerInfo       handlerInfo;        // The handler information of the plugin.
        private DateTime                lastCycled;         // The last time the plugin cycled; DateTime.Min as value means the plugin has yet to cycle.
        private string                  title;              // The title of the plugin.
        private string                  directory;          // The relative path of the plugin's base directory.
        private Version                 version;            // The current version of the plugin.
        // Methods - Constructors **************************************************************************************
        public Plugin(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Version version)
        {
            this.uuid = uuid;
            this.title = title;
            this.directory = directory;
            this.state = state;
            this.handlerInfo = handlerInfo;
            this.lastCycled = DateTime.MinValue;
            this.version = version;
        }
		// Methods - Abstract - State **********************************************************************************
        /// <summary>
        /// Invoked when the plugin should persist its data to the database.
        /// </summary>
        /// <param name="conn"></param>
        public virtual void save(Connector conn)
        {
            if(stateChanged)
                conn.queryExecute("UPDATE cms_plugins SET state='" + SQLUtils.escape(((int)state).ToString()) + "' WHERE uuid=" + uuid.NumericHexString + "; ");
        }
        /// <summary>
        /// Invoked when the plugin is being unloaded; this may not occur only when the CMS ends.
        /// </summary>
        public virtual void dispose()
        {
        }
        /// <summary>
		/// Invoked when the plugin is to be enabled; no checking of the plugin state is required. Return
		/// true if successful or false if the plugin cannot be enabled.
		/// </summary>
		/// <param name="conn">Database connector.</param>
        /// <param name="messageOutput">Message output.</param>
        public virtual bool enable(Connector conn, ref StringBuilder messageOutput)
		{
            messageOutput.AppendLine("Not implemented.");
			return false;
		}
		/// <summary>
		/// Invoked when the current plugin is to be disabled; no checking of the plugin state is required.
		/// </summary>
		/// <param name="conn">Database connector.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful or false if the plugin failed to be disabled.</returns>
        public virtual bool disable(Connector conn, ref StringBuilder messageOutput)
		{
            messageOutput.AppendLine("Not implemented.");
			return false;
		}
        /// <summary>
        /// Invoked wehn the current plugin is to be uninstalled; no checking of the plugin state is required.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful or false if the plugin failed to be uninstalled.</returns>
        public virtual bool uninstall(Connector conn, ref StringBuilder messageOutput)
		{
            messageOutput.AppendLine("Not implemented.");
			return false;
		}
        /// <summary>
        /// Invoked when the current plugin is to be installed; no checking of the plugin state is required.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful or false if the plugin failed to be disabled.</returns>
        public virtual bool install(Connector conn, ref StringBuilder messageOutput)
		{
            messageOutput.AppendLine("Not implemented.");
			return false;
		}
		// Methods - Abstract - Handlers - Plugins *********************************************************************
        /// <summary>
        /// Invoked when the plugin is loaded.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns></returns>
        public virtual bool handler_pluginStart(Connector conn)
		{
			return true;
		}
        /// <summary>
        /// Invoked when the plugin is unloaded.
        /// </summary>
        /// <param name="conn">Database connector.</param>
		public virtual void handler_pluginStop(Connector conn)
		{
		}
        /// <summary>
        /// Invoked at intervals, as defined by the cycle-interval in the plugin's handler information.
        /// </summary>
        public virtual void handler_pluginCycle()
		{
		}
        /// <summary>
        /// Invoked before/post enable/disable/install/uninstall of any plugin apart of the CMS.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="action">The action being, or has been, performed.</param>
        /// <returns>If this is a pre-action, you can return false to abort the process. No affect on post actions.</returns>
        public virtual bool handler_pluginAction(Connector conn, PluginAction action, Plugin plugin)
        {
            return false;
        }
		// Methods - Abstract - Handlers - Requests ********************************************************************
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
		// Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of the plugin.
        /// </summary>
		public UUID UUID
		{
			get
			{
                return uuid;
			}
		}
        /// <summary>
        /// The version of this plugin.
        /// </summary>
        public Version Version
        {
            get
            {
                return version;
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
        /// The relative path of the plugin's base-directory.
        /// </summary>
        public string RelativeDirectory
        {
            get
            {
                return directory;
            }
        }
        /// <summary>
        /// Returns the full path to the plugin's base-directory.
        /// </summary>
        public string Path
        {
            get
            {
                return Core.BasePath + "/" + directory;
            }
        }
        /// <summary>
        /// The full physical path to this plugin's content directory.
        /// </summary>
        public virtual string PathContent
        {
            get
            {
                return Path + "/content";
            }
        }
        /// <summary>
        /// The full physical path to this plugin's templates directory.
        /// </summary>
        public virtual string PathTemplates
        {
            get
            {
                return Path + "/templates";
            }
        }
        /// <summary>
        /// The full physical path to this plugin's SQL directory.
        /// </summary>
        public virtual string PathSQL
        {
            get
            {
                return Path + "/sql";
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
            set
            {
                state = value;
                stateChanged = true;
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
