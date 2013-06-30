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
			// Methods - Static ****************************************************************************************
            /// <summary>
            /// Creates a new instance of the Plugins manager, with all the plugins loaded and configured.
            /// </summary>
            /// <returns></returns>
			public static Plugins load()
			{
                Plugins plugins = new Plugins();
				try
				{
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
                            phi = new PluginHandlerInfo(t["request_start"] == "1", t["request_end"] == "1", t["page_error"] == "1", t["page_not_found"] == "1", t["cms_start"] == "1", t["cms_end"] == "1", t["cms_plugin_action"] == "1", pluginid);
                        }
                        catch (Exception ex)
                        {
                            Core.ErrorMessage = "Failed to load plugin handler information for plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!";
                            return null;
                        }
                        // Create an instance of the class and add it
                        try
                        {
                            plugin = (Plugin)ass.CreateInstance(t["classpath"], false, BindingFlags.CreateInstance, null, new object[] { pluginid, t["title"], state, phi }, null, null);
                            if (plugin != null)
                                plugins.plugins.Add(pluginid, plugin);
                            else
                            {
                                Core.ErrorMessage = "Failed to load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - could not find class-path or an issue occurred creating an instance!";
                                return null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Core.ErrorMessage = "Could not load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!";
                            return null;
                        }
                    }
				}
				catch(Exception ex)
				{
					Core.ErrorMessage = "Unknown exception when loading plugins: '" + ex.Message + "'!";
                    return null;
				}
                // Build plugin handler cache's
                try
                {
                    plugins.rebuildHandlerCaches();
                }
                catch (Exception ex)
                {
                    Core.ErrorMessage = "Unknown exception when rebuilding handler cache's: '" + ex.Message + "'!";
                    return null;
                }
                // Start cycler service
                try
                {
                    
                    plugins.cyclerStart();
                }
                catch (Exception ex)
                {
                    Core.ErrorMessage = "Unknown exception when starting plugin cycler: '" + ex.Message + "'!";
                    return null;
                }
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
