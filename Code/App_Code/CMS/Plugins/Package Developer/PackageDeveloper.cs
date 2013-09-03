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
 *      Path:           /App_Code/CMS/Plugins/Basic 404 Page/Basic404Page.cs
 * 
 *      Change-Log:
 *                      2013-07-01      Created initial class.
 *                      2013-07-05      Added templates and content variables.
 *                      2013-07-06      Added plugin actions (install/uninstall/remove/enable/disable).
 *                                      Minor tweaks due to testing.
 *                                      Added more core functions and core syncing.
 *                                      Syncing now overwrites files.
 *                                      Added plugin versioning.
 * 
 * *********************************************************************************************************************
 * A plugin to help developers produce plugins and deployable packages.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Ionic.Zip;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Plugins
{
    /// <summary>
    /// A plugin to help developers produce plugins and deployable packages.
    /// </summary>
    public class PackageDeveloper : Plugin
    {
        // Constants ***************************************************************************************************
        private const string ERROR_BOX_VARIDENT = "PackageDeveloperError";
        private const string SUCCESS_BOX_VARIDENT = "PackageDeveloperSuccess";
        public PackageDeveloper() { }
        public PackageDeveloper(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        { }
        public override bool install(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return true;
        }
        public override bool uninstall(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return true;
        }
        public override bool enable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Install templates
            Core.Templates.install(conn, this, PathTemplates, ref messageOutput);
            // Install URL rewriting paths
            BaseUtils.urlRewritingInstall(conn, this, new string[] { "package_developer" }, ref messageOutput);
            return true;
        }
        public override bool disable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Remove templates
            Core.Templates.uninstall(conn, this, ref messageOutput);
            // Remove URL rewriting paths
            BaseUtils.urlRewritingUninstall(conn, this, ref messageOutput);
            return true;
        }
        public override bool handler_handleRequest(Data data)
        {
            switch (data.PathInfo[1])
            {
                case null:
                case "home":
                    return pageHome(data);
                case "sync":
                    return pageSync(data);
                case "package":
                    return pagePackage(data);
                case "dump":
                    return pageTemplatesDump(data);
                case "upload":
                    return pageTemplatesUpload(data);
                case "install":
                case "uninstall":
                case "enable":
                case "disable":
                case "remove":
                case "unload":
                    return pagePluginAction(data);
                case "core":
                    return pageCore(data);
                default:
                    return false;
            }
        }
        private bool pageHome(Data data)
        {
            data["Title"] = "Package Developer - Home";
            // Check postback
            string relativePath = data.Request.Form["relative_path"];
            if (relativePath != null && relativePath.Length > 0)
            {
                if(!Directory.Exists(Core.BasePath + "/" + relativePath))
                    data[ERROR_BOX_VARIDENT] = "The path '" + Core.BasePath + "/" + relativePath + "' does not exist!";
                else
                {
                    StringBuilder messageOutput = new StringBuilder();
                    Plugin p = null;
                    data[Core.Plugins.createFromDirectory(data.Connector, Core.BasePath + "/" + relativePath, ref p, ref messageOutput) ? SUCCESS_BOX_VARIDENT : ERROR_BOX_VARIDENT] = messageOutput.Length == 0 ? "No output from the operation." : HttpUtility.HtmlEncode(messageOutput.ToString()).Replace("\r", "").Replace("\n", "<br />");
                }
            }
            else
                relativePath = "App_Code/CMS/Plugins/";
            // Generate list of plugins and options
            string item = Core.Templates.get(data.Connector, "package_developer/home_item");
            StringBuilder plugins = new StringBuilder();
            foreach (Plugin plugin in Core.Plugins.Fetch)
            {
                plugins.Append(item
                    .Replace("%UUID%", plugin.UUID.HexHyphens)
                    .Replace("%STATE%", HttpUtility.HtmlEncode(plugin.State.ToString()))
                    .Replace("%VERSION%", plugin.Version.Major + "." + plugin.Version.Minor + "." + plugin.Version.Build)
                    .Replace("%TITLE%", HttpUtility.HtmlEncode(plugin.Title))
                    );
            }
            data["Content"] = Core.Templates.get(data.Connector, "package_developer/home").Replace("%BASE_PATH%", HttpUtility.HtmlEncode(Core.BasePath)).Replace("%RELATIVE_PATH%", HttpUtility.HtmlEncode(relativePath)).Replace("%PLUGINS%", plugins.ToString());
            return true;
        }
        private bool pageSync(Data data)
        {
            Plugin plugin;
            string filesList;
            if (data.PathInfo[2] == null)
            {
                filesList = Core.PathInstaller + "/files.list";
                plugin = null;
            }
            else
            {
                if ((plugin = Core.Plugins.get(UUID.parse(data.PathInfo[2]))) == null)
                    return false;
                filesList = plugin.Path + "/files.list";
            }
            data["Title"] = "Package Developer - Sync Global Files";
            // Check files.list exists
            if (!File.Exists(filesList))
            {
                if(plugin != null)
                    data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", "Cannot sync plugin '<i>" + HttpUtility.HtmlEncode(plugin.Title) + "</i>' (UUID: " + plugin.UUID.HexHyphens + ") at '<i>" + HttpUtility.HtmlEncode(plugin.Path) + "</i>' - files.list does not exist!");
                else
                    data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", "Cannot sync core files - files.list does not exist in /installer!");
            }
            else
            {
                StringBuilder filesIncluded = new StringBuilder("Files synchronised:<br />");
                StringBuilder filesExcluded = new StringBuilder("<br /><br />Files excluded:<br />");
                // Begin syncing files
                string line;
                string[] file;
                string file1, file2;
                foreach (string rawline in File.ReadAllText(filesList).Replace("\r", "").Split('\n'))
                {
                    line = rawline.Trim();
                    if (line.Length != 0 && !line.StartsWith("//") && (file = line.Split(',')).Length == 2) // Check against empty-line or comment-line
                    {
                        // Format the file path's
                        file1 = file[0].Trim().Replace("%GLOBAL%", Core.BasePath).Replace("%LOCAL%", plugin == null ? Core.PathInstaller : plugin.Path).Replace("%TEMPLATES%", plugin == null ? Core.PathInstaller_Templates : plugin.PathTemplates).Replace("%CONTENT%", plugin == null ? Core.PathInstaller_Content : plugin.PathContent);
                        file2 = file[1].Trim().Replace("%GLOBAL%", Core.BasePath).Replace("%LOCAL%", plugin == null ? Core.PathInstaller : plugin.Path).Replace("%TEMPLATES%", plugin == null ? Core.PathInstaller_Templates : plugin.PathTemplates).Replace("%CONTENT%", plugin == null ? Core.PathInstaller_Content : plugin.PathContent);
                        // Check if file1 is multiple files i.e. a directory
                        bool multipleFiles = file1.EndsWith("/*");
                        // Sync the files
                        if (multipleFiles)
                        {
                            file1 = file1.Remove(file1.Length - 2, 2); // Remove /*
                            string ft;
                            foreach (string f in Directory.GetFiles(file1, "*", SearchOption.AllDirectories))
                            {
                                ft = f.Replace('\\', '/');
                                pageSync_file(ft, file2 + ft.Substring(file1.Length), ref filesIncluded, ref filesExcluded);
                            }
                        }
                        else
                            pageSync_file(file1, file2 + "/" + System.IO.Path.GetFileName(file1), ref filesIncluded, ref filesExcluded);

                    }
                }
                data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", filesIncluded.ToString() + filesExcluded.ToString());
            }
            return true;
        }
        private void pageSync_file(string source, string destination, ref StringBuilder filesIncluded, ref StringBuilder filesExcluded)
        {
            try
            {
                // Check the file is not a disallowed/system file and exists
                if (source.EndsWith("/Thumbs.db") || !File.Exists(source))
                {
                    filesExcluded.Append("<i>").Append(HttpUtility.HtmlEncode(source)).Append("</i> to <i>").Append(HttpUtility.HtmlEncode(destination)).Append("</i><br />");
                    return;
                }
                // Add any protection against files loading into the assembly
                if (destination.EndsWith(".js"))
                    destination += ".file";
                // Check the destination directoy exists
                string dir = System.IO.Path.GetDirectoryName(destination);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                // Copy the file
                File.Copy(source, destination, true);
                filesIncluded.Append("<i>").Append(HttpUtility.HtmlEncode(source)).Append("</i> to <i>").Append(HttpUtility.HtmlEncode(destination)).Append("</i><br />");
            }
            catch (Exception ex)
            {
                filesExcluded.Append("Failed to include <i>").AppendLine(HttpUtility.HtmlEncode(source)).AppendLine("</i> to <i>").Append(HttpUtility.HtmlEncode(destination)).Append("</i> - exception: '").Append(ex.Message).Append("'<br />");
            }
        }
        private bool pagePackage(Data data)
        {
            Plugin plugin = Core.Plugins.get(UUID.parse(data.PathInfo[2]));
            if (plugin == null)
                return false;
            data["Title"] = "Package Developer - Package Plugin";
            // Create a new archive in the base directory, add every file in the base of the target plugin to the archive
            StringBuilder output = new StringBuilder();
            string archivePath = Core.BasePath + "/" + System.IO.Path.GetFileName(plugin.RelativeDirectory) + "_" + plugin.Version.Major + "." + plugin.Version.Minor + "." + plugin.Version.Build + ".zip";
            output.Append("Creating archive at '").Append(HttpUtility.HtmlEncode(archivePath)).Append("'...");
            using(ZipFile archive = new ZipFile(archivePath))
            {
                int pluginPathLength = plugin.Path.Length;
                foreach(string file in Directory.GetFiles(plugin.Path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        archive.AddFile(file, System.IO.Path.GetDirectoryName(file.Substring(pluginPathLength)));
                        output.Append("Added file '" + HttpUtility.HtmlEncode(file) + "'.<br />");
                    }
                    catch (Exception ex)
                    {
                        output.Append("Failed to add file '" + HttpUtility.HtmlEncode(file) + "' to archive: '" + HttpUtility.HtmlEncode(file) + "'!<br />");
                    }
                }
                archive.Save();
            }
            data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", output.ToString());
            return true;
        }
        private bool pageTemplatesDump(Data data)
        {
            string dumpDest;
            Plugin plugin;
            // Decide on what and where to dump templates
            switch (data.PathInfo[2])
            {
                case "core":
                    plugin = null;
                    dumpDest = Core.BasePath + "/installer/templates";
                    break;
                default:
                    plugin = Core.Plugins.get(UUID.parse(data.PathInfo[2]));
                    if (plugin == null)
                        return false;
                    dumpDest = plugin.PathTemplates;
                    break;
            }
            data["Title"] = "Package Developer - Dump Templates";
            // Dump the templates
            StringBuilder messageOutput = new StringBuilder();
            messageOutput.Append("Dumping templates to '" + HttpUtility.HtmlEncode(dumpDest) + "'...<br />");
            Core.Templates.dumpForPlugin(data.Connector, dumpDest, plugin, ref messageOutput);
            messageOutput.Replace("\r", "").Replace("\n", "<br />");
            // Output page
            data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", messageOutput.ToString());
            return true;
        }
        private bool pageTemplatesUpload(Data data)
        {
            Plugin plugin = Core.Plugins.get(UUID.parse(data.PathInfo[2]));
            if (plugin == null)
                return false;
            data["Title"] = "Package Developer - Upload Templates";
            StringBuilder messageOutput = new StringBuilder();
            Core.Templates.install(data.Connector, plugin, plugin.PathTemplates, ref messageOutput);
            messageOutput.AppendLine("Finished uploading templates from '" + plugin.PathTemplates + "'.");
            data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", HttpUtility.HtmlEncode(messageOutput.ToString()).Replace("\r", "").Replace("\n", "<br />"));
            return true;
        }
        private bool pagePluginAction(Data data)
        {
            Plugin plugin = Core.Plugins.get(UUID.parse(data.PathInfo[2]));
            if (plugin == null)
                return false;
            StringBuilder output = new StringBuilder();
            switch (data.PathInfo[1])
            {
                case "install":
                    Core.Plugins.install(data.Connector, plugin, ref output);
                    break;
                case "uninstall":
                    Core.Plugins.uninstall(data.Connector, plugin, ref output);
                    break;
                case "enable":
                    Core.Plugins.enable(data.Connector, plugin, ref output);
                    break;
                case "disable":
                    Core.Plugins.disable(data.Connector, plugin, ref output);
                    break;
                case "remove":
                    Core.Plugins.remove(data.Connector, plugin, false, ref output);
                    break;
                case "unload":
                    Core.Plugins.unload(data.Connector, plugin);
                    output.Append("Unloaded plugin '").Append(plugin.Title).Append("' (UUID: '").Append(plugin.UUID.HexHyphens).AppendLine("') from virtual runtime!");
                    break;
                default:
                    return false;
            }
            data["Title"] = "Package Developer - Plugin Action - <i>" + data.PathInfo[1] + "</i>";
            data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", HttpUtility.HtmlEncode(output.ToString()).Replace("\r", "").Replace("\n", "<br />"));
            return true;
        }
        private bool pageCore(Data data)
        {
            StringBuilder messageOutput = new StringBuilder();
            switch (data.PathInfo[2])
            {
                case "rebuild_handler_cache":
                    Core.Plugins.rebuildHandlerCaches();
                    messageOutput.AppendLine("Rebuilt handler cache!");
                    data["Title"] = "Package Developer - Core - Rebuild Handler Cache";
                    break;
                case "reload_plugins":
                    Core.Plugins.reload(data.Connector);
                    messageOutput.AppendLine("Reloaded plugin runtime!");
                    data["Title"] = "Package Developer - Core - Reload Plugins";
                    break;
                default:
                    return false;
            }
            data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", HttpUtility.HtmlEncode(messageOutput.ToString()).Replace("\r", "").Replace("\n", "<br />"));
            return true;
        }
    }
}