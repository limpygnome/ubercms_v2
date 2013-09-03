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
 *                      2013-08-22      Removed dispose method - superseded by pluginStart handler.
 *                                      More fields are now modifiable with bitwise for efficient updating.
 *                                      Added thread safety.
 * 
 * *********************************************************************************************************************
 * Base class for all plugins. This contains information about the plugin and methods to be implemented as handlers.
 * *********************************************************************************************************************
 */
using System;
using System.Reflection;
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
        };
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
        };
        private enum Fields
        {
            None = 0,
            State = 1,
            Title = 2,
            Directory = 4,
            Version = 8,
            Priority = 16,
            ClassPath = 32
        };
		// Fields ******************************************************************************************************
        private bool                    persisted;          // Indicates if this model has been persisted.
        private Fields                  modified;           // Indicates if any fields have been modified.
        private UUID                    uuid;               // The universally unique identifier of the plugin.
		private PluginState             state;              // The state of the plugin.
        private PluginHandlerInfo       handlerInfo;        // The handler information of the plugin.
        private DateTime                lastCycled;         // The last time the plugin cycled; DateTime.Min as value means the plugin has yet to cycle.
        private string                  title;              // The title of the plugin.
        private string                  directory;          // The relative path of the plugin's base directory.
        private Version                 version;            // The current version of the plugin.
        private int                     priority;           // The priority of this plugin; the higher, the more 
        private string                  classPath;          // The class-path of the plugin; this is only needed as an entry point to this very class when loading via the database.
        // Methods - Constructors **************************************************************************************
        public Plugin(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Version version, int priority, string classPath)
        {
            this.uuid = uuid;
            this.title = title;
            this.directory = directory;
            this.state = state;
            this.handlerInfo = handlerInfo;
            this.lastCycled = DateTime.MinValue;
            this.version = version;
            this.priority = priority;
            this.classPath = classPath;
            this.modified = Fields.None;
        }
        public Plugin() { }
		// Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads a plugin based on its identifier.
        /// </summary>
        /// <param name="uuid">The identifier of the plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>Model or null.</returns>
        public static Plugin load(UUID uuid, Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM cms_view_plugins_loadinfo WHERE uuid_raw=?uuid;");
            ps["uuid"] = uuid.Bytes;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a plugin from database data.
        /// </summary>
        /// <param name="data">The persisted data of the plugin.</param>
        /// <returns>Model or null.</returns>
        public static Plugin load(ResultRow data)
        {
            return load(Assembly.GetExecutingAssembly(), data);
        }
        /// <summary>
        /// Loads a plugin from database data.
        /// </summary>
        /// <param name="ass">The assembly of where the class of the plugin resides.</param>
        /// <param name="data">The persisted data of the plugin.</param>
        /// <returns>Model or null.</returns>
        public static Plugin load(Assembly ass, ResultRow data)
        {
            UUID uuid = UUID.parse(data["uuid"]);
            if (ass == null || data == null || uuid == null)
                return null;
            return load(ass, data, uuid);
        }
        /// <summary>
        /// Loads a plugin from database data.
        /// </summary>
        /// <param name="ass">The assembly of where the class of the plugin resides.</param>
        /// <param name="data">The persisted data of the plugin.</param>
        /// <param name="uuid">The plugin's identifier; this is to avoid reprocessing the uUID if we already have it.</param>
        /// <returns>Model or null.</returns>
        public static Plugin load(Assembly ass, ResultRow data, UUID uuid)
        {
            if (ass == null || data == null || uuid == null)
                return null;
            // Parse plugin parameters from data
            Plugin.PluginState state = (Plugin.PluginState)Enum.Parse(typeof(Plugin.PluginState), data["state"]);
            PluginHandlerInfo phi = PluginHandlerInfo.load(data);
            if (phi == null)
                return null;
            // Create an instance of the plugin
            try
            {
                Plugin p = (Plugin)ass.CreateInstance(data["classpath"], false, BindingFlags.CreateInstance, null, new object[] { uuid, data["title"], data["directory"], state, phi, new Version(data.get2<int>("version_major"), data.get2<int>("version_minor"), data.get2<int>("version_build")), data.get2<int>("priority"), data.get2<string>("classpath") }, null, null);
                p.persisted = true;
                return p;
            }
            catch (ArgumentException) { }
            catch (MissingMethodException) { }
            catch (NotSupportedException) { }
            catch (System.IO.FileNotFoundException) { }
            catch (BadImageFormatException) { }
            throw new Exception("Could not load class at path '" + data["classpath"] + "'!");
        }
        /// <summary>
        /// Invoked when the plugin should persist its data to the database.
        /// 
        /// Note: this will not persist the plugin handler information/data.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = nkt persisted.</returns>
        public virtual bool save(Connector conn)
        {
            lock (this)
            {
                if (modified == Fields.None)
                    return false;
                // Compile SQL
                SQLCompiler sql = new SQLCompiler();
                if ((modified & Fields.State) == Fields.State)
                    sql["state"] = (int)state;
                if ((modified & Fields.Title) == Fields.Title)
                    sql["title"] = title;
                if ((modified & Fields.Directory) == Fields.Directory)
                    sql["directory"] = directory;
                if ((modified & Fields.Version) == Fields.Version)
                {
                    sql["version_major"] = version.Major;
                    sql["version_minor"] = version.Minor;
                    sql["version_build"] = version.Build;
                }
                if ((modified & Fields.Priority) == Fields.Priority)
                    sql["priority"] = priority;
                if ((modified & Fields.ClassPath) == Fields.ClassPath)
                    sql["classpath"] = classPath;
                sql["directory"] = directory;
                // Add specific fields based on persistence and execute
                if (persisted)
                {
                    sql.UpdateAttribute = "uuid";
                    sql.UpdateValue = uuid.Bytes;
                    sql.executeUpdate(conn, "cms_plugins");
                }
                else
                {
                    sql["uuid"] = uuid.Bytes;
                    sql.executeInsert(conn, "cms_plugins");
                    persisted = true;
                }
                modified = Fields.None;
                return true;
            }
        }
        /// <summary>
        /// Unpersists the plugin from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public virtual void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM cms_plugins WHERE uuid=?uuid;");
            ps["uuid"] = uuid.Bytes;
            conn.queryExecute(ps);
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
        /// 
        /// Note: this is only invoked if the plugin is enabled.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns></returns>
        public virtual bool handler_pluginStart(Connector conn)
		{
			return true;
		}
        /// <summary>
        /// Invoked when the plugin is unloaded.
        /// 
        /// Note: this is only invoked if the plugin is enabled.
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
        /// 
        /// Note: this will be invoked on all (uninstalled/installed/enabled/disabled) plugins.
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
        /// 
        /// Note: if this model has already been persisted, setting this property will have no effect.
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
                        uuid = value;
                }
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
                lock (this)
                {
                    state = value;
                    modified |= Fields.State;
                }
            }
		}
        /// <summary>
        /// The display title of the plugin.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                lock (this)
                {
                    title = value;
                    modified |= Fields.Title;
                }
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
            set
            {
                lock (this)
                {
                    directory = value;
                    modified |= Fields.Directory;
                }
            }
        }
        /// <summary>
        /// THe version of the plugin.
        /// </summary>
        public Version Version
        {
            get
            {
                return version;
            }
            set
            {
                lock (this)
                {
                    version = value;
                    modified |= Fields.Version;
                }
            }
        }
        /// <summary>
        /// The priority of the plugin over other plugins for handlers;
        /// the higher the value, the higher priority.
        /// </summary>
        public int Priority
        {
            get
            {
                return priority;
            }
            set
            {
                lock (this)
                {
                    this.priority = value;
                    modified |= Fields.Priority;
                }
            }
        }
        /// <summary>
        /// The class-path of the plugin.
        /// </summary>
        public string ClassPath
        {
            get
            {
                return classPath;
            }
            set
            {
                lock (this)
                {
                    this.classPath = value;
                    modified |= Fields.ClassPath;
                }
            }
        }
        /// <summary>
        /// The handler information about the plugin.
        /// 
        /// Note: this is a seperate model; calling persist on this plugin model will not persist the plugin handler
        /// data!
        /// </summary>
        public PluginHandlerInfo HandlerInfo
        {
            get
            {
                return handlerInfo;
            }
            set
            {
                handlerInfo = value;
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
