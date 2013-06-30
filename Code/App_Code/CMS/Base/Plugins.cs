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
 *      Path:           /App_Code/CMS/Core/Plugins.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Finished initial class.
 * 
 * *********************************************************************************************************************
 * Used to store and interact with plugins.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using CMS.Plugins;
using UberLib.Connector;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Text;
using System.IO;

namespace CMS
{
	namespace Base
	{
		public class Plugins
		{
			// Fields **************************************************************************************************
			private Dictionary<int,Plugin> plugins;			    // Map of pluginid to plugin.
            private Thread cycler;                              // The thread used for running cycles of plugins.
            // Fields - Handler List Caching ***************************************************************************
            private Plugin[] cacheRequestStart;                 // Cache of plugins capable of handling the start of a request.
            private Plugin[] cacheRequestEnd;                   // Cache of plugins capable of handling the end of a request.
            private Plugin[] cachePageError;				    // Cache of plugins capable of handling a page error.
            private Plugin[] cachePageNotFound;			        // Cache of plugins capable of handling a page not found.
			// Methods - Constructors **********************************************************************************
			private Plugins()
			{
				this.plugins = new Dictionary<int, Plugin>();
                cycler = null;
                cacheRequestStart = cacheRequestEnd = cachePageError = cachePageNotFound = new Plugin[0];
			}
			// Methods *************************************************************************************************
			/// <summary>
			/// Finds the available plugins to serve a request. Returns null if a consistency issue has occurred
			/// (a plugin cannot be found).
			/// </summary>
			/// <returns>The request handlers.</returns>
			/// <param name="pathInfo">Path info.</param>
			/// <param name="conn">Conn.</param>
			public Plugin[] findRequestHandlers(PathInfo pathInfo, Connector conn)
			{
				Result r = conn.Query_Read("SELECT DISTINCT ur.pluginid FROM cms_urlrewriting AS ur LEFT OUTER JOIN cms_plugins AS p ON p.pluginid=ur.pluginid  WHERE p.state='" + (int)Plugin.PluginState.Enabled + "' AND (ur.full_path='" + Utils.Escape(pathInfo.FullPath) + "' OR ur.full_path ='" + Utils.Escape(pathInfo.ModuleHandler) + "') ORDER BY ur.priority DESC");
				Plugin[] result = new Plugin[r.Rows.Count];
				int c = 0;
				Plugin p;
				foreach(ResultRow plugin in r)
				{
					p = this[int.Parse(plugin["pluginid"])];
					if(p != null)
						result[c++] = p;
					else
						return null;
				}
				return result;
			}
            /// <summary>
            /// Invoked when a page exception occurs.
            /// </summary>
            /// <param name="data">The data of the request.</param>
            public void handlePageError(Data data, Exception ex)
            {
                bool handled = false;
                foreach (Plugin p in cachePageError)
                    if (handled = p.handler_handlePageError(data, ex))
                        break;
                if (!handled)
                    data["Content"] = "An error occurred whilst handling your request and the error could not be handled!";
            }
            /// <summary>
            /// Rebuilds the internal handler-caches. This is used to speedup invoking handlers, rather than iterating
            /// every plugin (constant as opposed to N complexity).
            /// </summary>
            public void rebuildHandlerCaches()
            {
                lock (this)
                {
                    List<Plugin> cacheRequestStart = new List<Plugin>();
                    List<Plugin> cacheRequestEnd = new List<Plugin>();
                    List<Plugin> cachePageError = new List<Plugin>();
                    List<Plugin> cachePageNotFound = new List<Plugin>();
                    foreach (Plugin p in plugins.Values)
                    {
                        if (p.HandlerInfo.RequestStart)
                            cacheRequestStart.Add(p);
                        if (p.HandlerInfo.RequestEnd)
                            cacheRequestEnd.Add(p);
                        if (p.HandlerInfo.PageError)
                            cachePageError.Add(p);
                        if (p.HandlerInfo.PageNotFound)
                            cachePageNotFound.Add(p);
                    }
                    this.cacheRequestStart = cacheRequestStart.ToArray();
                    this.cacheRequestEnd = cacheRequestEnd.ToArray();
                    this.cachePageError = cachePageError.ToArray();
                    this.cachePageNotFound = cachePageNotFound.ToArray();
                }
            }
            /// <summary>
            /// Unloads a plugin from the runtime; this does not affect the plugin's state.
            /// </summary>
            /// <param name="plugin"></param>
            public void unload(Plugin plugin)
            {
                lock (this)
                {
                    plugins.Remove(plugin.PluginID);
                }
            }
            /// <summary>
            /// Creates a plugin from a zip-file.
            /// </summary>
            /// <param name="pathToZip">The path to the zip file.</param>
            /// <param name="messageOutput">Message output.</param>
            /// <returns></returns>
            public bool createFromZip(string pathToZip, ref StringBuilder messageOutput)
            {
                lock(this)
                {
                    // Find a suitable output directory for the zip extraction
                    Random rand = new Random((int)DateTime.Now.ToBinary());
                    string outputDir;
                    int attempts = 0;
                    while(Directory.Exists(outputDir = Core.TempPath + "/pu_" + rand.Next(0, int.MaxValue) + "_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + DateTime.Now.Millisecond) && attempts++ < 5)
                        ;
                    if(attempts == 5)
                    {
                        messageOutput.Append("Failed to create a temporary directory for the zip upload '" + pathToZip + "'!");
                        return false;
                    }
                    // Extract the zip to the temporary path
                    BaseUtils.extractZip(pathToZip, outputDir);
                    // Read where the plugin should be based and move the temporary directory
                    bool success = true;
                    string directory = null;
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(outputDir + "/Plugin.config");
                        directory = Core.BasePath + "/" + doc["plugin"]["directory"].InnerText;
                        if (Directory.Exists(directory))
                        {
                            success = false;
                            messageOutput.Append("Destination directory '" + directory + "' for plugin already exists, aborted!");
                        }
                        else
                        {
                            try
                            {
                                // Move to destination
                                Directory.Move(outputDir, directory);
                            }
                            catch (Exception ex)
                            {
                                messageOutput.Append("Failed to move plugin to destination directory '" + directory + "' for plugin; exception: '" + ex.Message + "'!");
                                success = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        messageOutput.Append("Failed to handle temporary directory: '" + ex.Message + "'!");
                    }
                    // Create a new plugin from the directory
                    if (success)
                        success = createFromDirectory(directory, ref messageOutput);
                    // If the installation failed, remove the directory
                    if (!success)
                    {
                        try
                        {
                            Directory.Delete(outputDir, true);
                        }
                        catch {}
                        return false;
                    }
                    else
                        return true;
                }
            }
            /// <summary>
            /// Creates a new plugin from the specified directory; this new plugin is not loaded into the runtime.
            /// You should invoke the method 'reload' to reload all the plugins after a call to this method, unless
            /// the plugin files have only been recently added (the class won't exist in the runtime and the core will
            /// fail).
            /// </summary>
            /// <param name="directoryPath">The directory of the new plugin.</param>
            /// <param name="messageOutput">Message output.</param>
            /// <returns></returns>
            public bool createFromDirectory(string directoryPath, ref StringBuilder messageOutput)
            {
                lock (this)
                {
                    int priority;
                    string title, directory, classPath;
                    // Read base configuration
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(directoryPath + "/Plugin.config");
                        priority = int.Parse(doc["plugin"]["priority"].InnerText);
                        title = doc["plugin"]["title"].InnerText;
                        directory = doc["plugin"]["directory"].InnerText;
                        classPath = doc["plugin"]["class_path"].InnerText;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Failed to read configuration file; exception: '" + ex.Message + "'!");
                        return false;
                    }
                    // Check the path is correct
                    if ((Core.BasePath + "/" + directory) != directoryPath)
                    {
                        messageOutput.Append("Incorrect directory location! Should be at '" + Core.BasePath + "/" + directory + "', however directory is at '" + directoryPath + "'! If you need to change the install location of a plugin, modify its 'Plugin.config' file, however the new path MUST be relative.");
                        return false;
                    }
                    // Update the database
                    try
                    {
                        Core.Connector.Query_Execute("INSERT INTO cms_plugins (title, directory, classpath, priority) VALUES('" + Utils.Escape(title) + "', '" + Utils.Escape(directory) + "', '" + Utils.Escape(classPath) + "', '" + Utils.Escape(priority.ToString()) + "'); INSERT INTO cms_plugin_handlers (pluginid) VALUES((SELECT MAX(pluginid) FROM cms_plugins));");
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Failed to insert the plugin into the database: '" + ex.Message + "'!");
                        return false;
                    }
                    return true;
                }
            }
            public bool install(Plugin plugin, ref StringBuilder messageOutput)
            {
                lock (this)
                {
                    if (plugin.State != Plugin.PluginState.NotInstalled)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (ID: '" + plugin.PluginID+ "') is already installed!");
                        return false;
                    }
                    // Invoke pre-action handlers
                    foreach (Plugin p in Fetch)
                    {
                        if (p.HandlerInfo.CmsPluginAction && !p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PreInstall, plugin))
                        {
                            messageOutput.Append("Aborted by plugin '" + p.Title + "' (ID: '" + p.PluginID + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    if (!plugin.install(Core.Connector, ref messageOutput))
                        return false;
                    else
                    {
                        plugin.State = Plugin.PluginState.Disabled;
                        plugin.save(Core.Connector);
                    }
                    // Invoke post-action handlers
                    foreach (Plugin p in Fetch)
                        if (p.HandlerInfo.CmsPluginAction)
                            p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PostInstall, plugin);
                }
                return true;
            }
            public bool uninstall(Plugin plugin, ref StringBuilder messageOutput)
            {

                return true;
            }
            public bool enable(Plugin plugin, ref StringBuilder messageOutput)
            {

                return true;
            }
            public bool disable(Plugin plugin, ref StringBuilder messageOutput)
            {

                return true;
            }
            // Methods - Cycles ****************************************************************************************
            /// <summary>
            /// Starts the internal thread for invoking the cycle handler of plugins periodically.
            /// </summary>
            public void cyclerStart()
            {
                lock (this)
                {
                    if (cycler != null)
                        return;
                    cycler = new Thread(delegate()
                        {
                            cyclePlugins();
                        });
                }
            }
            /// <summary>
            /// Stops the internal thread for invoking the cycle handler of plugins.
            /// </summary>
            public void cyclerStop()
            {
                lock (this)
                {
                    if (cycler == null)
                        return;
                    cycler.Abort();
                    cycler = null;
                }
            }
            private void cyclePlugins()
            {
                int cyclingInterval = Core.SettingsDisk.getInteger("settings/core/cycling_interval");
                // Build cache of plugins to be cycled
                List<Plugin> pluginsCycling = new List<Plugin>();
                foreach (Plugin p in plugins.Values)
                {
                    if (p.State == Plugin.PluginState.Enabled && p.HandlerInfo.CycleInterval > 0)
                        pluginsCycling.Add(p);
                }
                // Loop...
                while (true)
                {
                    foreach (Plugin p in pluginsCycling)
                        if ((DateTime.Now - p.LastCycled).Milliseconds > p.HandlerInfo.CycleInterval)
                            p.handler_cmsCycle();
                    Thread.Sleep(cyclingInterval);
                }
            }
            // Methods - Reloading *************************************************************************************
            /// <summary>
            /// Reloads the collection of plugins from the database; dispose will be invoked on the plugin.
            /// </summary>
            public void reload()
            {
                reload(false);
            }
            private bool reload(bool callByCreator)
            {
                lock (this)
                {
                    try
                    {
                        // Stop cycler
                        cyclerStop();
                        // Inform each plugin they're being disposed
                        foreach (Plugin p in Fetch)
                            p.dispose();
                        // Clear old plugins
                        plugins.Clear();
                        // Load each plugin
                        Assembly ass = Assembly.GetExecutingAssembly();
                        int pluginid;
                        Plugin.PluginState state;
                        PluginHandlerInfo phi;
                        Plugin plugin;
                        foreach (ResultRow t in Core.Connector.Query_Read("SELECT * FROM cms_view_plugins_loadinfo"))
                        {
                            // Parse plugin params
                            pluginid = int.Parse(t["pluginid"]);
                            state = (Plugin.PluginState)Enum.Parse(typeof(Plugin.PluginState), t["state"]);
                            try
                            {
                                phi = new PluginHandlerInfo(pluginid, t["request_start"] == "1", t["request_end"] == "1", t["page_error"] == "1", t["page_not_found"] == "1", t["cms_start"] == "1", t["cms_end"] == "1", t["cms_plugin_action"] == "1", pluginid);
                            }
                            catch (Exception ex)
                            {
                                if (callByCreator)
                                    Core.ErrorMessage = "Failed to load plugin handler information for plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!";
                                else
                                    Core.fail("Failed to load plugin handler information for plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!");
                                return false;
                            }
                            // Create an instance of the class and add it
                            try
                            {
                                plugin = (Plugin)ass.CreateInstance(t["classpath"], false, BindingFlags.CreateInstance, null, new object[] { pluginid, t["title"], state, phi }, null, null);
                                if (plugin != null)
                                    plugins.Add(pluginid, plugin);
                                else
                                {
                                    if (callByCreator)
                                        Core.ErrorMessage = "Failed to load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - could not find class-path or an issue occurred creating an instance!";
                                    else
                                        Core.fail("Failed to load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - could not find class-path or an issue occurred creating an instance!");
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (callByCreator)
                                    Core.ErrorMessage = "Could not load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!";
                                else
                                    Core.fail("Could not load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!");
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (callByCreator)
                            Core.ErrorMessage = "Unknown exception when loading plugins: '" + ex.Message + "'!";
                        else
                            Core.fail("Unknown exception when loading plugins: '" + ex.Message + "'!");
                        return false;
                    }
                    // Build plugin handler cache's
                    try
                    {
                        rebuildHandlerCaches();
                    }
                    catch (Exception ex)
                    {
                        if (callByCreator)
                            Core.ErrorMessage = "Unknown exception when rebuilding handler cache's: '" + ex.Message + "'!";
                        else
                            Core.fail("Unknown exception when rebuilding handler cache's: '" + ex.Message + "'!");
                        return false;
                    }
                    // Start cycler service
                    try
                    {
                        cyclerStart();
                    }
                    catch (Exception ex)
                    {
                        if (callByCreator)
                            Core.ErrorMessage = "Unknown exception when starting plugin cycler: '" + ex.Message + "'!";
                        else
                            Core.fail("Unknown exception when starting plugin cycler: '" + ex.Message + "'!");
                        return false;
                    }
                }
                return true;
            }
			// Methods - Static ****************************************************************************************
            /// <summary>
            /// Creates a new instance of the Plugins manager, with all the plugins loaded and configured.
            /// </summary>
            /// <returns></returns>
			public static Plugins load()
			{
                Plugins plugins = new Plugins();
                plugins.reload(true);
                return plugins;
			}
            // Methods - Properties ************************************************************************************
            /// <summary>
            /// Returns a plugin by its pluginid, else null if a plugin with the specified ID cannot be found.
            /// </summary>
            /// <param name="pluginid"></param>
            /// <returns></returns>
            public Plugin this[int pluginid]
            {
                get
                {
                    return plugins.ContainsKey(pluginid) ? plugins[pluginid] : null;
                }
            }
            /// <summary>
            /// A cached-list of plugins with a request-start handler.
            /// </summary>
            public Plugin[] HandlerCache_RequestStart
            {
                get
                {
                    return cacheRequestStart;
                }
            }
            /// <summary>
            /// A cached-list of plugins with a request-end handler.
            /// </summary>
            public Plugin[] HandlerCache_RequestEnd
            {
                get
                {
                    return cacheRequestEnd;
                }
            }
            /// <summary>
            /// A cached-list of plugins with a page-error handler.
            /// </summary>
            public Plugin[] HandlerCache_PageError
            {
                get
                {
                    return cachePageError;
                }
            }
            /// <summary>
            /// A cached-list of plugins with a page-not-found handler.
            /// </summary>
            public Plugin[] HandlerCache_PageNotFound
            {
                get
                {
                    return cachePageNotFound;
                }
            }
            /// <summary>
            /// Returns an array of the loaded plugins.
            /// </summary>
            public Plugin[] Fetch
            {
                get
                {
                    lock (plugins)
                    {
                        if (plugins.Count == 0) // Skip processing ahead.
                            return new Plugin[0];
                        else
                        {
                            Plugin[] t = new Plugin[plugins.Count];
                            plugins.Values.CopyTo(t, 0);
                            return t;
                        }
                    }
                }
            }
		}
	}
}
