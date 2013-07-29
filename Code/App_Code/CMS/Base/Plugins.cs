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
 *      Path:           /App_Code/CMS/Base/Plugins.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-30      Finished initial class.
 *                                      Added install/uninstall/enable/disable exception catching for safety.
 *                                      Added remove method.
 *                                      Improved general safety of install/uninstall/enable/disable and cycling.
 *                      2013-07-01      Invoking uninstall will now invoke disable if the plugin is enabled.
 *                                      Added getPlugin methods.
 *                                      Fixed incorrect enum value being set in install method (enabled instead of
 *                                      disabled).
 *                      2013-07-08      Added ability to fetch plugins by unique identifiers.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Added changes to support pluginid to UUID changes.
 *                      2013-07-22      Added and modified plugin loading/unloading; loading plugins can now be done
 *                                      during runtime.
 * 
 * *********************************************************************************************************************
 * A data-collection for managing and interacting with plugin models.
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

namespace CMS.Base
{
    /// <summary>
    /// A data-collection for managing and interacting with plugin models.
    /// </summary>
	public class Plugins
	{
		// Fields **************************************************************************************************
		private Dictionary<UUID,Plugin> plugins;			// Map of UUID to plugin.
        private Thread cycler;                              // The thread used for running cycles of plugins.
        // Fields - Handler List Caching ***************************************************************************
        private Plugin[] cacheRequestStart;                 // Cache of plugins capable of handling the start of a request.
        private Plugin[] cacheRequestEnd;                   // Cache of plugins capable of handling the end of a request.
        private Plugin[] cachePageError;				    // Cache of plugins capable of handling a page error.
        private Plugin[] cachePageNotFound;			        // Cache of plugins capable of handling a page not found.
		// Methods - Constructors **********************************************************************************
		private Plugins()
		{
			this.plugins = new Dictionary<UUID, Plugin>();
            cycler = null;
            cacheRequestStart = cacheRequestEnd = cachePageError = cachePageNotFound = new Plugin[0];
		}
		// Methods *************************************************************************************************
		/// <summary>
		/// Finds the available plugins to serve a request. Returns an empty array if a consistency issue has occurred
		/// (a plugin cannot be found).
		/// </summary>
		/// <returns>The request handlers.</returns>
		/// <param name="pathInfo">Path info.</param>
		/// <param name="conn">Database connector.</param>
		public Plugin[] findRequestHandlers(PathInfo pathInfo, Connector conn)
		{
			Result r = conn.queryRead("SELECT DISTINCT HEX(ur.uuid) AS uuid FROM cms_urlrewriting AS ur LEFT OUTER JOIN cms_plugins AS p ON p.uuid=ur.uuid WHERE p.state='" + (int)Plugin.PluginState.Enabled + "' AND (ur.full_path='" + SQLUtils.escape(pathInfo.FullPath) + "' OR ur.full_path ='" + SQLUtils.escape(pathInfo.ModuleHandler) + "') ORDER BY ur.priority DESC");
            List<Plugin> result = new List<Plugin>(r.Count);
			Plugin p;
			foreach(ResultRow plugin in r)
			{
                p = this[UUID.createFromHex(plugin["uuid"])];
                if(p != null)
                    result.Add(p);
			}
			return result.ToArray();
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
            {
#if DEBUG
                data["Content"] = "<h2>Server Error</h2><p>An error occurred whilst handling your request and the error could not be handled!</p><h3>Debug Information</h3><h4>Primary Error</h4><p>" + ex.Message + "</p><p>" + ex.StackTrace + "</p><h4>Base Error</h4><p>" + ex.GetBaseException().Message + "</p><p>" + ex.GetBaseException().StackTrace + "</p>";
#else
                data["Content"] = "<h2>Server Error</h2><p>An error occurred whilst handling your request and the error could not be handled!</p>";
#endif
                data.Response.StatusCode = 500;
            }
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
        /// Dynamically loads a plugin into the CMS's runtime. This does not rebuild the plugin handler cache.
        /// </summary>
        /// <param name="uuid">The identifier of the plugin; can be null without core failure occurring.</param>
        /// <param name="coreError">Indicates if the core should be stopped if the plugin fails to load.</param>
        /// <param name="messageOutput">A place for outputting errors and warnings; can be null.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool pluginLoad(UUID uuid, bool coreError, ref StringBuilder messageOutput)
        {
            if (uuid == null)
                return false;
            try
            {
                // Retrieve the plugin's data from the database
                Result result = Core.Connector.queryRead("SELECT * FROM cms_view_plugins_loadinfo WHERE uuid_raw=" + uuid.SQLValue);
                if (result.Count != 1)
                {
                    if(coreError)
                        Core.fail("Data for plugin with UUID '" + uuid.HexHyphens + "' could not be retrieved! Critical consistency failure!");
                    if(messageOutput != null)
                        messageOutput.AppendLine("Could not retrieve the data for plugin with UUID '" + uuid.HexHyphens + "'!");
                    return false;
                }
                // Invoke base method for this operation
                return pluginLoad(Assembly.GetExecutingAssembly(), result[0], false, coreError, ref messageOutput);
            }
            catch
            {
                return false;
            }
        }
        private bool pluginLoad(Assembly ass, ResultRow data, bool callByFactoryCreator, bool coreError, ref StringBuilder messageOutput)
        {
            lock (this)
            {   
                UUID uuid = UUID.createFromHex(data["uuid"]);
                if (uuid == null)
                {
                    if(coreError)
                        Core.fail("Failed to parse UUID '" + data["uuid"] + "' for plugin '" + data["title"] + "'!");
                    if (messageOutput != null)
                        messageOutput.Append("Failed to parse UUID '").Append(data["uuid"]).AppendLine("'!");
                    return false;
                }
                // Check the plugin is not already loaded
                if (plugins.ContainsKey(uuid))
                {
                    if(coreError)
                        Core.fail("Plugin '" + uuid.HexHyphens + "' is already loaded in the runtime! Critical consistency failure!");
                    if (messageOutput != null)
                        messageOutput.Append("Plugin with UUID '").Append(uuid.HexHyphens).AppendLine("' is already loaded in the runtime!");
                    return false;
                }
                // Parse plugin params
                Plugin.PluginState state = (Plugin.PluginState)Enum.Parse(typeof(Plugin.PluginState), data["state"]);
                PluginHandlerInfo phi;
                try
                {
                    phi = new PluginHandlerInfo(uuid, data["request_start"] == "1", data["request_end"] == "1", data["page_error"] == "1", data["page_not_found"] == "1", data["cms_start"] == "1", data["cms_end"] == "1", data["cms_plugin_action"] == "1", int.Parse(data["cycle_interval"]));
                }
                catch (Exception ex)
                {
                    if (coreError)
                    {
                        if (callByFactoryCreator)
                            Core.ErrorMessage = "Failed to load plugin handler information for plugin UUID: '" + uuid.HexHyphens + "' (" + data["title"] + ") - '" + ex.Message + "'!";
                        else
                            Core.fail("Failed to load plugin handler information for plugin UUID: '" + uuid.HexHyphens + "' (" + data["title"] + ") - '" + ex.Message + "'!");
                    }
                    if (messageOutput != null)
                        messageOutput.Append("Failed to load plugin handler information for plugin UUID '" ).Append(uuid.HexHyphens).AppendLine("'!");
                    return false;
                }
                // Create an instance of the class and add it
                try
                {
                    Plugin plugin = (Plugin)ass.CreateInstance(data["classpath"], false, BindingFlags.CreateInstance, null, new object[] { uuid, data["title"], data["directory"], state, phi }, null, null);
                    if (plugin != null)
                        plugins.Add(uuid, plugin);
                    else
                    {
                        if (coreError)
                        {
                            if (callByFactoryCreator)
                                Core.ErrorMessage = "Failed to load plugin UUID: '" + uuid.HexHyphens + "' (" + data["title"] + ") - could not find class-path or an issue occurred creating an instance!";
                            else
                                Core.fail("Failed to load plugin UUID: '" + uuid.HexHyphens + "' (" + data["title"] + ") - could not find class-path or an issue occurred creating an instance!");
                        }
                        if (messageOutput != null)
                            messageOutput.Append("Failed to load plugin with UUID '").Append(uuid.HexHyphens).Append("'; could not find class-path '").Append(data["classpath"]).AppendLine("'!");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (coreError)
                    {
                        if (callByFactoryCreator)
                            Core.ErrorMessage = "Could not load plugin UUID: '" + uuid.HexHyphens + "' (" + data["title"] + ") - '" + ex.Message + "'!";
                        else
                            Core.fail("Could not load plugin UUID: '" + uuid.HexHyphens + "' (" + data["title"] + ") - '" + ex.Message + "'!");
                    }
                    if (messageOutput != null)
                        messageOutput.Append("Could not load plugin with UUID '").Append(uuid.HexHyphens).Append("'; unknown exception: '").Append(ex.Message).AppendLine("'!");
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// Unloads a plugin from the runtime; this does not affect the plugin's state.
        /// </summary>
        /// <param name="plugin"></param>
        public void pluginUnload(Plugin plugin)
        {
            lock (this)
            {
                plugins.Remove(plugin.UUID);
            }
        }
        /// <summary>
        /// Creates a plugin from a zip-file.
        /// </summary>
        /// <param name="pathToZip">The path to the zip file.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool createFromZip(string pathToZip, ref StringBuilder messageOutput)
        {
            lock(this)
            {
                if (pathToZip == null)
                {
                    messageOutput.AppendLine("Plugins.createFromZip - invalid zip path specified!");
                    return false;
                }
                // Find a suitable output directory for the zip extraction
                Random rand = new Random((int)DateTime.Now.ToBinary());
                string outputDir;
                int attempts = 0;
                while(Directory.Exists(outputDir = Core.TempPath + "/pu_" + rand.Next(0, int.MaxValue) + "_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + DateTime.Now.Millisecond) && attempts++ < 5)
                    ;
                if(attempts == 5)
                {
                    messageOutput.AppendLine("Failed to create a temporary directory for the zip upload '" + pathToZip + "'!");
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
                    UUID uuid = UUID.createFromHex(doc["plugin"]["uuid"].InnerText);
                    if (plugins.ContainsKey(uuid) || Core.Connector.queryCount("SELECT COUNT('') FROM cms_plugins WHERE uuid=" + uuid.SQLValue + ";") > 0)
                    {
                        success = false;
                        messageOutput.Append("UUID (univerisally unique identifier) '").Append(uuid.HexHyphens).Append("' already exists! It's likely the plugin has already been installed.").AppendLine();
                    }
                    else if (Directory.Exists(directory))
                    {
                        success = false;
                        messageOutput.AppendLine("Destination directory '" + directory + "' for plugin already exists, aborted!");
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
                            messageOutput.AppendLine("Failed to move plugin to destination directory '" + directory + "' for plugin; exception: '" + ex.Message + "'!");
                            success = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    messageOutput.AppendLine("Failed to handle temporary directory: '" + ex.Message + "'!");
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
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool createFromDirectory(string directoryPath, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (directoryPath == null)
                {
                    messageOutput.AppendLine("Plugins.createFromDirectory - invalid directory path specified!");
                    return false;
                }
                int priority;
                string title, directory, classPath;
                UUID uuid;
                // Read base configuration
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(directoryPath + "/Plugin.config");
                    priority = int.Parse(doc["plugin"]["priority"].InnerText);
                    title = doc["plugin"]["title"].InnerText;
                    directory = doc["plugin"]["directory"].InnerText;
                    classPath = doc["plugin"]["class_path"].InnerText;
                    uuid = UUID.createFromHexHyphens(doc["plugin"]["uuid"].InnerText);
                    if (uuid == null)
                    {
                        messageOutput.Append("Invalid UUID '").Append(doc["plugin"]["uuid"].InnerText).AppendLine("'!");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    messageOutput.AppendLine("Failed to read configuration file; exception: '" + ex.Message + "'!");
                    return false;
                }
                // Check the UUID does not exist
                if (plugins.ContainsKey(uuid) || Core.Connector.queryCount("SELECT COUNT('') FROM cms_plugins WHERE uuid=" + uuid.SQLValue + ";") > 0)
                {
                    messageOutput.Append("UUID (univerisally unique identifier) '").Append(uuid.HexHyphens).Append("' already exists! It's likely the plugin has already been installed.").AppendLine();
                    return false;
                }
                // Check the path is correct
                else if ((Core.BasePath + "/" + directory) != directoryPath)
                {
                    messageOutput.AppendLine("Incorrect directory location! Should be at '" + Core.BasePath + "/" + directory + "', however directory is at '" + directoryPath + "'! If you need to change the install location of a plugin, modify its 'Plugin.config' file, however the new path MUST be relative.");
                    return false;
                }
                // Update the database
                try
                {
                    Core.Connector.queryExecute("INSERT INTO cms_plugins (uuid, title, directory, classpath, priority) VALUES(" + uuid.SQLValue + ", '" + SQLUtils.escape(title) + "', '" + SQLUtils.escape(directory) + "', '" + SQLUtils.escape(classPath) + "', '" + SQLUtils.escape(priority.ToString()) + "'); INSERT INTO cms_plugin_handlers (uuid) VALUES(" + uuid.SQLValue + ");");
                }
                catch (Exception ex)
                {
                    messageOutput.AppendLine("Failed to insert the plugin into the database: '" + ex.Message + "'!");
                    return false;
                }
                // Load the plugin into the runtime, if the class is available (non-critical operation)
                if (!pluginLoad(uuid, false, ref messageOutput))
                    messageOutput.AppendLine("Warning: could not load the new plugin into the virtual runtime of the CMS. If the plugin files have been added during this operation, ignore this message; else restart the application pool!");
                else
                    try
                    {
                        // Success - rebuild the handler cache!
                        rebuildHandlerCaches();
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Warning: failed to reload plugin handler cache for pluginUUID '").Append(uuid.HexHyphens).Append("'; exception: '").Append(ex.Message).AppendLine("'!");
                    }
                return true;
            }
        }
        /// <summary>
        /// Installs a plugin.
        /// </summary>
        /// <param name="plugin">The plugin to be installed.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool install(Plugin plugin, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (plugin == null)
                {
                    messageOutput.AppendLine("Plugins.install - invalid plugin specified!");
                    return false;
                }
                lock (plugin)
                {
                    if (plugin.State != Plugin.PluginState.NotInstalled)
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - already installed!");
                        return false;
                    }
                    // Invoke pre-action handlers
                    foreach (Plugin p in Fetch)
                    {
                        if (plugin != p && p.HandlerInfo.CmsPluginAction && !p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PreInstall, plugin))
                        {
                            messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.install(Core.Connector, ref messageOutput))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occured invoking install method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.Disabled;
                    plugin.save(Core.Connector);
                    // Invoke post-action handlers
                    foreach (Plugin p in Fetch)
                        if (plugin != p && p.HandlerInfo.CmsPluginAction)
                            p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PostInstall, plugin);
                }
            }
            return true;
        }
        /// <summary>
        /// Uninstalls a plugin.
        /// </summary>
        /// <param name="plugin">The plugin to be uninstalled.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool uninstall(Plugin plugin, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (plugin == null)
                {
                    messageOutput.AppendLine("Plugins.uninstall - invalid plugin specified!");
                    return false;
                }
                lock (plugin)
                {
                    if (plugin.State == Plugin.PluginState.NotInstalled)
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - already uninstalled!");
                        return false;
                    }
                    else if (plugin.State == Plugin.PluginState.Enabled && !disable(plugin, ref messageOutput))
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborting uninstall, failed to disable plugin!");
                        return false;
                    }
                    // Invoke pre-action handlers
                    foreach (Plugin p in Fetch)
                    {
                        if (plugin != p && p.HandlerInfo.CmsPluginAction && !p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PreUninstall, plugin))
                        {
                            messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.uninstall(Core.Connector, ref messageOutput))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred invoking uninstall method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.NotInstalled;
                    plugin.save(Core.Connector);
                    // Invoke post-action handlers
                    foreach (Plugin p in Fetch)
                        if (plugin != p && p.HandlerInfo.CmsPluginAction)
                            p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PostUninstall, plugin);
                }
            }
            return true;
        }
        /// <summary>
        /// Enables a plugin.
        /// </summary>
        /// <param name="plugin">The plugin to be enabled.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool enable(Plugin plugin, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (plugin == null)
                {
                    messageOutput.AppendLine("Plugins.enable - invalid plugin specified!");
                    return false;
                }
                lock (plugin)
                {
                    if (plugin.State == Plugin.PluginState.NotInstalled)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - not installed!");
                        return false;
                    }
                    else if (plugin.State == Plugin.PluginState.Enabled)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - already enabled!");
                        return false;
                    }
                    // Invoke pre-action handlers
                    foreach (Plugin p in Fetch)
                    {
                        if (plugin != p && p.HandlerInfo.CmsPluginAction && !p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PreEnable, plugin))
                        {
                            messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.enable(Core.Connector, ref messageOutput))
                        return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred invoking enable method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.Enabled;
                    plugin.save(Core.Connector);
                    // Invoke post-action handlers
                    foreach (Plugin p in Fetch)
                        if (plugin != p && p.HandlerInfo.CmsPluginAction)
                            p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PostEnable, plugin);
                }
            }
            return true;
        }
        /// <summary>
        /// Disable a plugin.
        /// </summary>
        /// <param name="plugin">The plugin to be disabled.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool disable(Plugin plugin, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (plugin == null)
                {
                    messageOutput.Append("Plugins.disable - invalid plugin specified!");
                    return false;
                }
                lock (plugin)
                {
                    if (plugin.State == Plugin.PluginState.NotInstalled)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - not installed!");
                        return false;
                    }
                    else if (plugin.State == Plugin.PluginState.Disabled)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - already disabled!");
                        return false;
                    }
                    // Invoke pre-action handlers
                    foreach (Plugin p in Fetch)
                    {
                        if (plugin != p && p.HandlerInfo.CmsPluginAction && !p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PreDisable, plugin))
                        {
                            messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.disable(Core.Connector, ref messageOutput))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred invoking disable method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.Disabled;
                    plugin.save(Core.Connector);
                    // Invoke post-action handlers
                    foreach (Plugin p in Fetch)
                        if (plugin != p && p.HandlerInfo.CmsPluginAction)
                            p.handler_cmsPluginAction(Core.Connector, Plugin.PluginAction.PostDisable, plugin);
                }
            }
            return true;
        }
        /// <summary>
        /// Permanently removes a plugin from the CMS; warning: this may delete all database data!
        /// </summary>
        /// <param name="plugin">The plugin to be removed/deleted.</param>
        /// <param name="removeDirectory">Indicates if to delete the physical directory of the plugin.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool remove(Plugin plugin, bool removeDirectory, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (plugin == null)
                {
                    messageOutput.Append("Plugins.remove - invalid plugin specified!");
                    return false;
                }
                lock (plugin)
                {
                    // Remove from runtime
                    pluginUnload(plugin);
                    // Remove from the database
                    try
                    {
                        Core.Connector.queryExecute("DELETE FROM cms_plugins WHERE uuid=" + plugin.UUID.SQLValue + ";");
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - failed to execute SQL to remove the plugin from the database: '" + SQLUtils.escape(ex.Message) + "'!");
                        return false;
                    }
                    // Attempt to delete the files; if it fails, we can continue since the plugin has been removed (just inform the user)
                    if (removeDirectory)
                    {
                        try
                        {
                            Directory.Delete(plugin.FullPath, true);
                        }
                        catch (Exception ex)
                        {
                            messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - warning - exception occurred removing the directory ('" + plugin.FullPath + "'): '" + ex.Message + "'; you will need to remove the directory manually!");
                        }
                    }
                }
            }
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
            int cyclingInterval = Core.SettingsDisk["settings/core/cycling_interval"].get<int>();
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
                lock (this)
                {
                    foreach (Plugin p in pluginsCycling)
                        if ((DateTime.Now - p.LastCycled).Milliseconds > p.HandlerInfo.CycleInterval)
                            p.handler_cmsCycle();
                }
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
                    StringBuilder sb = null;
                    foreach (ResultRow t in Core.Connector.queryRead("SELECT * FROM cms_view_plugins_loadinfo"))
                        pluginLoad(ass, t, true, true, ref sb);
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
        // Methods - Accessors *************************************************************************************
        /// <summary>
        /// Gets a plugin by its identifier.
        /// </summary>
        /// <param name="uuid">The UUID of the plugin.</param>
        /// <returns>A plugin with the specified identifier or null.</returns>
        public Plugin getPlugin(UUID uuid)
        {
            lock(this)
                return uuid != null && plugins.ContainsKey(uuid) ? plugins[uuid] : null;
        }
        // Methods - Properties ************************************************************************************
        /// <summary>
        /// Returns a plugin by its UUID, else null if a plugin with the specified ID cannot be found.
        /// </summary>
        /// <param name="uuid">The plugin's identifier.</param>
        /// <returns>The plugin associated with the identifier, else null if not found.</returns>
        public Plugin this[UUID uuid]
        {
            get
            {
                lock(this)
                    return uuid != null && plugins.ContainsKey(uuid) ? plugins[uuid] : null;
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
