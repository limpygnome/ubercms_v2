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
 *                      2013-07-08      Added rcability to fetch plugins by unique identifiers.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Added changes to support pluginid to UUID changes.
 *                      2013-07-22      Added and modified plugin loading/unloading; loading plugins can now be done
 *                                      during runtime.
 *                      2013-08-01      UUIDs are now stored as non-hyphen strings for better performance.
 *                      2013-08-23      Changed the way loading/unloading works with start/stop handlers of plugins, as
 *                                      well as general improvements.
 *                                      Plugins are now stored based on UUID hex, rather than hex-with-hyphens, for less
 *                                      space complexity.
 *                      2013-08-27      Bug-fixes, general improvements.
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
		// Fields ******************************************************************************************************
		private Dictionary<string,Plugin>   plugins;			    // Map of UUID (string, no hyphen's, upper-case) to plugin.
        private Thread                      cycler;                 // The thread used for running periodic cycles of plugins.
        // Fields - Handler List Caching *******************************************************************************
        private Plugin[]                    cacheRequestStart,      // Cache of plugins capable of handling the start of a request.
                                            cacheRequestEnd,        // Cache of plugins capable of handling the end of a request.
                                            cachePageError,			// Cache of plugins capable of handling a page error.
                                            cachePageNotFound;		// Cache of plugins capable of handling a page not found.
		// Methods - Constructors **************************************************************************************
		private Plugins()
		{
			this.plugins = new Dictionary<string, Plugin>();
            this.cycler = null;
            this.cacheRequestStart = this.cacheRequestEnd = this.cachePageError = this.cachePageNotFound = new Plugin[0];
		}
		// Methods *****************************************************************************************************
        /// <summary>
        /// Used to determine and possibly rebuild the handler cache.
        /// </summary>
        /// <param name="plugin">The plugin which has been changed.</param>
        private void pluginActionRebuildCache(Plugin plugin)
        {
            if (plugin.HandlerInfo.RequestStart || plugin.HandlerInfo.RequestEnd || plugin.HandlerInfo.PageError || plugin.HandlerInfo.PageNotFound)
                rebuildHandlerCaches();
        }
        /// <summary>
        /// Invoked when a page exception occurs; this method will find a plugin which can handle the exception.
        /// </summary>
        /// <param name="data">The data of the request.</param>
        public void handlePageException(Data data, Exception ex)
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
        /// every plugin (constant as opposed to N complexity) or querying the database (since this data will not change
        /// often).
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
        /// Loads a plugin into the CMS's runtime.
        /// 
        /// This does not rebuild the plugin handler cache.
        /// This will invoke any handlers, such as pluginStart.
        /// </summary>
        /// <param name="plugin">The plugin to be loaded into the runtime.</param>
        /// <param name="coreError">Indicates if to hault the CMS/throw a core exception if the plugin cannot be loaded into the runtime.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True = plugin loaded into runtime, false = plugin not loaded into the runtime.</returns>
        public bool load(Plugin plugin, bool coreError, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                lock (plugin)
                {
                    // Check the model is valid
                    if (plugin == null)
                    {
                        messageOutput.Append("Failed to load plugin into runtime: model is null/failed to load!");
                        return false;
                    }
                    // Check the plugin, based on the UUID, is not already loaded into the runtime
                    else if (plugins.ContainsKey(plugin.UUID.Hex))
                    {
                        messageOutput.Append("Failed to load plugin (UUID: " + (plugin.UUID != null ? plugin.UUID.HexHyphens : "invalid/null UUID") + ") into runtime: already loaded in runtime!");
                        return false;
                    }
                    // Add the plugin
                    plugins.Add(plugin.UUID.Hex, plugin);
                    return true;
                }
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
                plugins.Remove(plugin.UUID.NumericHexString);
            }
        }
        /// <summary>
        /// Creates a plugin from a zip-file.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="pathToZip">The path to the zip file.</param>
        /// <param name="plugin">If a plugin is created successfully, it's outputted to this parameter.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool createFromZip(Connector conn, string pathToZip, ref Plugin plugin, ref StringBuilder messageOutput)
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
                    UUID uuid = UUID.parse(doc["plugin"]["uuid"].InnerText);
                    if (plugins.ContainsKey(uuid.HexHyphens) || conn.queryCount("SELECT COUNT('') FROM cms_plugins WHERE uuid=" + uuid.NumericHexString + ";") > 0)
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
                    success = createFromDirectory(conn, directory, ref plugin, ref messageOutput);
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
        /// 
        /// Note: this will rebuild the handler cache if the plugin is successfully created and loaded into the runtime.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="directoryPath">The directory of the new plugin.</param>
        /// <param name="plugin">If a plugin is created successfully, it's outputted to this parameter.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool createFromDirectory(Connector conn, string directoryPath, ref Plugin plugin, ref StringBuilder messageOutput)
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
                int versionMajor, versionMinor, versionBuild;
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
                    uuid = UUID.parse(doc["plugin"]["uuid"].InnerText);
                    if (uuid == null)
                    {
                        messageOutput.Append("Invalid UUID '").Append(doc["plugin"]["uuid"].InnerText).AppendLine("'!");
                        return false;
                    }
                    versionMajor = int.Parse(doc["plugin"]["version_major"].InnerText);
                    versionMinor = int.Parse(doc["plugin"]["version_minor"].InnerText);
                    versionBuild = int.Parse(doc["plugin"]["version_build"].InnerText);
                }
                catch (Exception ex)
                {
                    messageOutput.AppendLine("Failed to read configuration file; exception: '" + ex.Message + "'!");
                    return false;
                }
                // Check the UUID does not exist
                if (plugins.ContainsKey(uuid.HexHyphens) || conn.queryCount("SELECT COUNT('') FROM cms_plugins WHERE uuid=" + uuid.NumericHexString + ";") > 0)
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
                // Create and persist model
                try
                {
                    Plugin p = new PackageDeveloper();
                    p.UUID = uuid;
                    p.Title = title;
                    p.RelativeDirectory = directory;
                    p.ClassPath = classPath;
                    p.Priority = priority;
                    p.Version = new Version(versionMajor, versionMinor, versionBuild);
                    p.HandlerInfo = new PluginHandlerInfo(uuid);
                    if (!p.save(conn) || !p.HandlerInfo.save(conn))
                    {
                        p.remove(conn);
                        messageOutput.Append("Failed to persist plugin (UUID: '" + uuid.HexHyphens + "') or plugin handler model!");
                        return false;
                    }
                    // Successfully persisted - set the plugin parameter
                    plugin = p;
                    // Load the plugin into the runtime (non-critical operation - may require app-pool restart)
                    if (!load(p, false, ref messageOutput))
                        messageOutput.AppendLine("Warning: could not load a new plugin (UUID: " + (uuid != null ? uuid.HexHyphens : "invalid/null UUID") + ") into the virtual runtime of the CMS. If the plugin files have been added during this operation, ignore this message; else restart the application pool!");
                    else
                    {
                        // Success - rebuild the handler cache!
                        try
                        {
                            rebuildHandlerCaches();
                        }
                        catch (Exception ex)
                        {
                            messageOutput.Append("Warning: failed to reload plugin handler cache for pluginUUID '").Append(uuid.HexHyphens).Append("'; exception: '").Append(ex.Message).Append("; stack-trace: '").Append(ex.StackTrace).Append("'").AppendLine("'!");
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    messageOutput.Append("Failed to persist the plugin model due to an exception: '" + ex.Message + "'");
                }
                return false;
            }
        }
        /// <summary>
        /// Installs a plugin.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="plugin">The plugin to be installed.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool install(Connector conn, Plugin plugin, ref StringBuilder messageOutput)
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
                        if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction && !p.handler_pluginAction(conn, Plugin.PluginAction.PreInstall, plugin))
                        {
                            messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.install(conn, ref messageOutput))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occured invoking install method: '" + ex.Message + "'!");
                        messageOutput.AppendLine(ex.StackTrace + ex.GetBaseException().Message);
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.Disabled;
                    if (plugin.save(conn))
                    {
                        // Invoke post-action handlers
                        foreach (Plugin p in Fetch)
                            if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction)
                                p.handler_pluginAction(conn, Plugin.PluginAction.PostInstall, plugin);
                    }
                    else
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - install - failed to persist new state!");
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Uninstalls a plugin.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="plugin">The plugin to be uninstalled.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool uninstall(Connector conn, Plugin plugin, ref StringBuilder messageOutput)
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
                    else if (plugin.State == Plugin.PluginState.Enabled && !disable(conn, plugin, ref messageOutput))
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborting uninstall, failed to disable plugin!");
                        return false;
                    }
                    // Invoke pre-action handlers
                    foreach (Plugin p in Fetch)
                    {
                        if (plugin != p && p.HandlerInfo.PluginAction && !p.handler_pluginAction(conn, Plugin.PluginAction.PreUninstall, plugin))
                        {
                            messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.uninstall(conn, ref messageOutput))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred invoking uninstall method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.NotInstalled;
                    if (plugin.save(conn))
                    {
                        // Invoke post-action handlers
                        foreach (Plugin p in Fetch)
                            if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction)
                                p.handler_pluginAction(conn, Plugin.PluginAction.PostUninstall, plugin);
                    }
                    else
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - uninstall - failed to persist new state!");
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Enables a plugin.
        /// 
        /// Note: this may rebuild the handler cache.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="plugin">The plugin to be enabled.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool enable(Connector conn, Plugin plugin, ref StringBuilder messageOutput)
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
                        if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction && !p.handler_pluginAction(conn, Plugin.PluginAction.PreEnable, plugin))
                        {
                            messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.enable(conn, ref messageOutput))
                        return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred invoking enable method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.Enabled;
                    if (plugin.save(conn))
                    {
                        // Invoke post-action handlers
                        foreach (Plugin p in Fetch)
                            if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction)
                                p.handler_pluginAction(conn, Plugin.PluginAction.PostEnable, plugin);
                    }
                    else
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - enable - failed to persist new state!");
                        return false;
                    }
                }
                // Rebuild handler cache (if needed)
                pluginActionRebuildCache(plugin);
            }
            return true;
        }
        /// <summary>
        /// Disable a plugin.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="plugin">The plugin to be disabled.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool disable(Connector conn, Plugin plugin, ref StringBuilder messageOutput)
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
                        if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction && !p.handler_pluginAction(conn, Plugin.PluginAction.PreDisable, plugin))
                        {
                            messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - aborted by plugin '" + p.Title + "' (UUID: '" + p.UUID.HexHyphens + "')!");
                            return false;
                        }
                    }
                    // Invoke install handler of plugin
                    try
                    {
                        if (!plugin.disable(conn, ref messageOutput))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred invoking disable method: '" + ex.Message + "'!");
                        return false;
                    }
                    // Update the database
                    plugin.State = Plugin.PluginState.Disabled;
                    if (plugin.save(conn))
                    {
                        // Invoke post-action handlers
                        foreach (Plugin p in Fetch)
                            if (plugin.UUID != p.UUID && p.HandlerInfo.PluginAction)
                                p.handler_pluginAction(conn, Plugin.PluginAction.PostDisable, plugin);
                    }
                    else
                    {
                        messageOutput.AppendLine("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - failed to persist new state!");
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Permanently removes a plugin from the CMS; warning: this may delete all database data!
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="plugin">The plugin to be removed/deleted.</param>
        /// <param name="removeDirectory">Indicates if to delete the physical directory of the plugin.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public bool remove(Connector conn, Plugin plugin, bool removeDirectory, ref StringBuilder messageOutput)
        {
            lock (this)
            {
                if (plugin == null)
                {
                    messageOutput.Append("Plugins removal- invalid plugin specified!");
                    return false;
                }
                lock (plugin)
                {
                    // Remove from runtime
                    unload(plugin);
                    // Unpersist
                    try
                    {
                        plugin.remove(conn);
                    }
                    catch (Exception ex)
                    {
                        messageOutput.Append("Plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - failed to unpersist from database - exception: '" + ex.Message + "'!");
                    }
                    // Attempt to delete the files; if it fails, we can continue since the plugin has been removed (just inform the user)
                    if (removeDirectory)
                    {
                        try
                        {
                            Directory.Delete(plugin.Path, true);
                        }
                        catch (Exception ex)
                        {
                            messageOutput.AppendLine("Warning: plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "') - exception occurred removing the directory ('" + plugin.Path + "'): '" + ex.Message + "'; you will need to remove the directory manually!");
                        }
                    }
                }
            }
            return true;
        }
        // Methods - Cycles ********************************************************************************************
        /// <summary>
        /// Starts the internal thread for invoking the cycle handler of plugins periodically.
        /// </summary>
        /// <returns>True = cycler started, false = cycler not started.</returns>
        public bool cyclerStart()
        {
            lock (this)
            {
                if (cycler != null)
                    return false;
                cycler = new Thread(delegate()
                    {
                        cyclePlugins();
                    });
                cycler.Start();
                return true;
            }
        }
        /// <summary>
        /// Stops the internal thread for invoking the cycle handler of plugins.
        /// </summary>
        /// <returns>True = cycler stopped, false = cycler not stopped.</returns>
        public bool cyclerStop()
        {
            lock (this)
            {
                if (this.cycler == null)
                    return false;
                this.cycler.Abort();
                this.cycler = null;
                return true;
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
                            p.handler_pluginCycle();
                }
                Thread.Sleep(cyclingInterval);
            }
        }
        // Methods - Reloading *****************************************************************************************
        /// <summary>
        /// Reloads the collection of plugins from the database; dispose will be invoked on the plugin.
        /// 
        /// Invokes pluginStop and pluginStart handler.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void reload(Connector conn)
        {
            reload(conn, false);
        }
        private bool reload(Connector conn, bool callByCreator)
        {
            lock (this)
            {
                try
                {
                    // Stop cycler
                    cyclerStop();
                    // Inform each plugin they're being stopped
                    foreach (Plugin p in Fetch)
                        if (p.State == Plugin.PluginState.Enabled && p.HandlerInfo.PluginStart)
                            p.handler_pluginStop(conn);
                    // Clear old plugins
                    plugins.Clear();
                    // Load each plugin and inform them they've been started
                    Assembly ass = Assembly.GetExecutingAssembly();
                    {
                        StringBuilder sb = new StringBuilder(); // Not actually needed.
                        foreach (ResultRow t in conn.queryRead("SELECT * FROM cms_view_plugins_loadinfo;"))
                        {
                            // Load plugin model
                            Plugin p = Plugin.load(t);
                            if (p == null)
                                throw new Exception("Failed to load model for plugin (title: '" + t["title"] + "', UUID: '" + t["uuid"] + "')!");
                            // Load into runtime
                            if (!load(p, true, ref sb))
                                throw new Exception("Failed to load plugin into runtime (title: '" + t["title"] + "', UUID: '" + t["uuid"] + "')!");
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
		// Methods - Static ********************************************************************************************
        /// <summary>
        /// Creates a new instance of the Plugins manager, with all the plugins loaded and configured.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>An instance of this model, with plugins loaded.</returns>
		public static Plugins load(Connector conn)
		{
            Plugins plugins = new Plugins();
            return plugins.reload(conn, true) ? plugins : null;
		}
        // Methods - Accessors *****************************************************************************************
        /// <summary>
        /// Gets a plugin by its identifier.
        /// </summary>
        /// <param name="uuid">The UUID of the plugin.</param>
        /// <returns>A plugin with the specified identifier or null.</returns>
        public Plugin get(UUID uuid)
        {
            lock(this)
                return uuid != null && contains(uuid) ? plugins[uuid.Hex] : null;
        }
        /// <summary>
        /// Gets a plugin by its identifier.
        /// </summary>
        /// <param name="uuid">The UUID of the plugin as a string without hyphens.</param>
        /// <returns>A plugin with the specified identifier or null.</returns>
        public Plugin get(string uuid)
        {
            lock (this)
                return uuid != null && contains(uuid) ? plugins[uuid] : null;
        }
        // Methods - Accessors *****************************************************************************************
        /// <summary>
        /// Indicates if a plugin is currently loaded into the runtime.
        /// </summary>
        /// <param name="plugin">The plugin to check.</param>
        /// <returns>True = loaded in runtime, false = not loaded in runtime.</returns>
        public bool contains(Plugin plugin)
        {
            return contains(plugin.UUID);
        }
        /// <summary>
        /// Indicates if a plugin is currently loaded into the runtime.
        /// </summary>
        /// <param name="uuid">The identifier of the plugin to check.</param>
        /// <returns>True = loaded in runtime, false = not loaded in runtime.</returns>
        public bool contains(UUID uuid)
        {
            return plugins.ContainsKey(uuid.Hex);
        }
        /// <summary>
        /// Indicates if a plugin is currently loaded into the runtime.
        /// </summary>
        /// <param name="uuid">The identifier of the plugin, as a string without hyphens, to check.</param>
        /// <returns>True = loaded in runtime, false = not loaded in runtime.</returns>
        public bool contains(string uuid)
        {
            return plugins.ContainsKey(uuid);
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Returns a plugin by its UUID, else null if a plugin with the specified identifier cannot be found.
        /// </summary>
        /// <param name="uuid">The plugin's universally unique identifier.</param>
        /// <returns>Model or null.</returns>
        public Plugin this[UUID uuid]
        {
            get
            {
                lock (this)
                    return get(uuid);
            }
        }
        /// <summary>
        /// Returns a plugin by its UUID, else null if a plugin with the specified identifier cannot be found.
        /// </summary>
        /// <param name="uuid">The plugin's universally unique identifier as a string without hyphens.</param>
        /// <returns>Model or null.</returns>
        public Plugin this[string uuid]
        {
            get
            {
                lock (this)
                    return get(uuid);
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
                        return new Plugin[]{};
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
