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
 *      File:           PackageDeveloper.cs
 *      Path:           /App_Code/CMS/Plugins/Basic 404 Page/Basic404Page.cs
 * 
 *      Change-Log:
 *                      2013-07-01      Created initial class.
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

namespace CMS
{
    namespace Plugins
    {
        /// <summary>
        /// A plugin to help developers produce plugins and deployable packages.
        /// </summary>
        public class PackageDeveloper : Plugin
        {
            public PackageDeveloper(int pluginid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo)
                : base(pluginid, title, directory, state, handlerInfo)
            { }
            public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                return true;
            }
            public override bool uninstall(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                return true;
            }
            public override bool enable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                // Install content
                BaseUtils.contentInstall(PathContent, Core.PathContent, false, ref messageOutput);
                // Install templates
                Core.Templates.install(conn, this, PathTemplates, ref messageOutput);
                // Install URL rewriting paths
                BaseUtils.urlRewritingInstall(this, new string[] { "package_developer" }, ref messageOutput);
                return true;
            }
            public override bool disable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                // Remove content
                BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput);
                // Remove templates
                Core.Templates.uninstall(this, ref messageOutput);
                // Remove URL rewriting paths
                BaseUtils.urlRewritingUninstall(this, ref messageOutput);
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
                    default:
                        return false;
                }
            }
            public bool pageHome(Data data)
            {
                data["Title"] = "Package Developer - Home";
                // Generate list of plugins and options
                string item = Core.Templates.get(data.Connector, "package_developer/home_item");
                StringBuilder plugins = new StringBuilder();
                foreach (Plugin plugin in Core.Plugins.Fetch)
                {
                    plugins.Append(item
                        .Replace("%PLUGINID%", plugin.PluginID.ToString())
                        .Replace("%TITLE%", HttpUtility.HtmlEncode(plugin.Title))
                        );
                }
                data["Content"] = Core.Templates.get(data.Connector, "package_developer/home").Replace("%PLUGINS%", plugins.ToString());
                return true;
            }
            public bool pageSync(Data data)
            {
                Plugin plugin = Core.Plugins.getPlugin(data.PathInfo[2]);
                if (plugin == null)
                    return false;
                data["Title"] = "Package Developer - Sync Global Files";
                // Check files.list exists
                if (!File.Exists(plugin.FullPath + "/files.list"))
                    data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", "Cannot sync plugin '<i>" + HttpUtility.HtmlEncode(plugin.Title) + "</i>' (ID: " + plugin.PluginID + ") at '<i>" + HttpUtility.HtmlEncode(plugin.FullPath) + "</i>' - files.list does not exist!");
                else
                {
                    StringBuilder filesIncluded = new StringBuilder("Files synchronised:");
                    StringBuilder filesExcluded = new StringBuilder("<br /><br />Files excluded:");
                    // Begin syncing files
                    string line;
                    string[] file;
                    string file1, file2;
                    foreach (string rawline in File.ReadAllText(plugin.FullPath + "/files.list").Replace("\r", "").Split('\n'))
                    {
                        line = rawline.Trim();
                        if (line.Length != 0 && !line.StartsWith("//") && (file = line.Split(',')).Length == 2) // Check against empty-line or comment-line
                        {
                            // Format the file path's
                            file1 = file[0].Trim().Replace("%GLOBAL%", Core.BasePath).Replace("%LOCAL%", plugin.FullPath);
                            file2 = file[1].Trim().Replace("%GLOBAL%", Core.BasePath).Replace("%LOCAL%", plugin.FullPath);
                            // Check if file1 is multiple files i.e. a directory
                            bool multipleFiles = file1.EndsWith("/*");
                            // Sync the files
                            if(multipleFiles)
                            {
                                file1 = file1.Remove(file1.Length - 2, 2); // Remove /*
                                string ft;
                                foreach (string f in Directory.GetFiles(file1, "*", SearchOption.AllDirectories))
                                {
                                    ft = f.Replace('\\', '/');
                                    pageSync_file(ft, file1 + ft.Substring(file1.Length), ref filesIncluded, ref filesExcluded);
                                }
                            }
                            else
                                pageSync_file(file1, file2 + "/" + Path.GetFileName(file1), ref filesIncluded, ref filesExcluded);

                        }
                    }
                    data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", filesExcluded.ToString() + filesExcluded.ToString());
                }
                return true;
            }
            private void pageSync_file(string source, string destination, ref StringBuilder filesIncluded, ref StringBuilder filesExcluded)
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
                string dir = Path.GetDirectoryName(destination);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                // Copy the file
                File.Copy(source, destination);
                filesIncluded.Append("<i>").Append(HttpUtility.HtmlEncode(source)).Append("</i> to <i>").Append(HttpUtility.HtmlEncode(destination)).Append("</i><br />");
            }
            public bool pagePackage(Data data)
            {
                Plugin plugin = Core.Plugins.getPlugin(data.PathInfo[2]);
                if (plugin == null)
                    return false;
                data["Title"] = "Package Developer - Package Plugin";
                // Create a new archive in the base directory, add every file in the base of the target plugin to the archive
                StringBuilder output = new StringBuilder();
                string archivePath = Core.BasePath + "/" + Path.GetDirectoryName(plugin.RelativeDirectory) + ".zip";
                using(ZipFile archive = new ZipFile(archivePath))
                {
                    int pluginPathLength = plugin.FullPath.Length;
                    foreach(string file in Directory.GetFiles(plugin.FullPath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            archive.AddFile(file, file.Substring(pluginPathLength));
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
            public bool pageTemplatesDump(Data data)
            {
                data["Title"] = "Package Developer - Dump Templates";
                string dumpDest;
                Plugin plugin;
                // Decide on what and where to dump templates
                switch (data.PathInfo[2])
                {
                    case "core":
                        plugin = null;
                        dumpDest = Core.BasePath + "/Installer/Templates";
                        break;
                    default:
                        plugin = Core.Plugins.getPlugin(data.PathInfo[2]);
                        if (plugin == null)
                            return false;
                        dumpDest = plugin.PathTemplates;
                        break;
                }
                // Dump the templates
                StringBuilder messageOutput = new StringBuilder();
                messageOutput.Append("Dumping templates to '" + HttpUtility.HtmlEncode(dumpDest) + "'...<br />");
                Core.Templates.dumpForPlugin(dumpDest, plugin, ref messageOutput);
                messageOutput.Replace("\r", "").Replace("\n", "<br />");
                // Output page
                data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", messageOutput.ToString());
                return true;
            }
            public bool pageTemplatesUpload(Data data)
            {
                data["Title"] = "Package Developer - Upload Templates";
                Plugin plugin = Core.Plugins.getPlugin(data.PathInfo[2]);
                if (plugin == null)
                    return false;
                StringBuilder messageOutput = new StringBuilder();
                Core.Templates.install(data.Connector, plugin, plugin.PathTemplates, ref messageOutput);
                messageOutput.Replace("\r", "").Replace("\n", "<br />").Append("Finished uploading templates from '" + plugin.PathTemplates + "'.");
                data["Content"] = Core.Templates.get(data.Connector, "package_developer/output").Replace("%OUTPUT%", HttpUtility.HtmlEncode(messageOutput.ToString()));
                return true;
            }
        }
    }
}
