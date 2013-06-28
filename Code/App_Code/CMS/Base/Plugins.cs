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
 *                      2013-06-25     Created initial class.
 * 
 * *****************************************************************************
 * Used to store and interact with plugins.
 * *****************************************************************************
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
			// Fields
			private Dictionary<int,Plugin> plugins;			    // Map of pluginid to plugin.
            private Thread cycler;                              // The thread used for running cycles of plugins.
            // Fields - Handler List Caching (to reduce complexity of checking each plugin at runtime - constant as opposed to N)
            private Plugin[] cacheRequestStart;                 // Cache of plugins capable of handling the start of a request.
            private Plugin[] cacheRequestEnd;                   // Cache of plugins capable of handling the end of a request.
            private Plugin[] cachePageError;				    // Cache of plugins capable of handling a page error.
            private Plugin[] cachePageNotFound;			        // Cache of plugins capable of handling a page not found
			// Methods - Constructors
			private Plugins()
			{
				this.plugins = new Dictionary<int, Plugin>();
                cycler = null;
			}
			// Methods
			/// <summary>
			/// Finds the available plugins to serve a request. Returns null if a consistency issue has occurred
			/// (a plugin cannot be found).
			/// </summary>
			/// <returns>The request handlers.</returns>
			/// <param name="pathInfo">Path info.</param>
			/// <param name="conn">Conn.</param>
			public Plugin[] findRequestHandlers(PathInfo pathInfo, Connector conn)
			{
				Result r = conn.Query_Read("SELECT DISTINCT pluginid FROM cms_urlrewriting WHERE full_path='" + Utils.Escape(pathInfo.FullPath) + "' OR full_path ='" + Utils.Escape(pathInfo.ModuleHandler) + "' ORDER BY priority DESC");
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
            private void rebuildHandlerCaches()
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
            // Methods - Cycles
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
                    if (p.HandlerInfo.CycleInterval > 0)
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
			// Methods - Accessors
			public Plugin this[int pluginid]
			{
				get
				{
					return plugins[pluginid];
				}
			}
			// Methods - Static
			public static Plugins load(Connector conn, ref string errorMessage)
			{
				try
				{
					Plugins plugins = new Plugins();
                    // Load each plugin
                    Assembly ass = Assembly.GetExecutingAssembly();
                    int pluginid;
                    Plugin.PluginState state;
                    PluginHandlerInfo phi;
                    Plugin plugin;
                    foreach (ResultRow t in Core.Connector.Query_Read("SELECT p.*, ph.* FROM cms_plugins AS p LEFT OUTER JOIN cms_plugin_handlers AS ph ON ph.pluginid=p.pluginid ORDER BY p.priority DESC"))
                    {
                        // Parse plugin params
                        pluginid = int.Parse(t["cycle_interval"]);
                        state = (Plugin.PluginState)Enum.Parse(typeof(Plugin.PluginState), t["state"]);
                        phi = new PluginHandlerInfo(t["request_start"] == "1", t["request_end"] == "1", t["page_error"] == "1", t["page_not_found"] == "1", pluginid);
                        // Create an instance of the class and add it
                        try
                        {
                            plugin = (Plugin)ass.CreateInstance(t["classpath"], false, BindingFlags.CreateInstance, null, new object[] { pluginid, phi }, null, null);
                        }
                        catch (Exception ex)
                        {
                            errorMessage = "Could not load plugin '" + t["pluginid"] + "' (" + t["title"] + ") - '" + ex.Message + "'!";
                            return null;
                        }
                    }
                    // Build plugin handler cache's
                    plugins.rebuildHandlerCaches();
                    // Start cycler service
                    plugins.cyclerStart();
					return plugins;
				}
				catch(Exception ex)
				{
					errorMessage = "Unknown exception when loading plugins '" + ex.Message + "'!";
				}
				return null;
			}
            // Methods - Properties
            public Plugin[] HandlerCache_RequestStart
            {
                get
                {
                    return cacheRequestStart;
                }
            }
            public Plugin[] HandlerCache_RequestEnd
            {
                get
                {
                    return cacheRequestEnd;
                }
            }
            public Plugin[] HandlerCache_PageError
            {
                get
                {
                    return cachePageError;
                }
            }
            public Plugin[] HandlerCache_PageNotFound
            {
                get
                {
                    return cachePageNotFound;
                }
            }
            public Dictionary<int, Plugin> Fetch
            {
                get
                {
                    return plugins;
                }
            }
		}
	}
}
