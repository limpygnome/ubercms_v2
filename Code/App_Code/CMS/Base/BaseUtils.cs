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
 *      Path:           /CMS/Base/BaseUtils.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Updated header and namespace.
 *                      2013-07-01      Added many functions from the old CMS plugins library.
 *                                      Added URL reservation methods.
 *                                      Fixed critical URL reservation uninstall bug.
 *                                      Added generateRandomString method.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 * 
 * *********************************************************************************************************************
 * A utility class of commonly used code.
 * *********************************************************************************************************************
 */
using System;
using System.IO;
using System.Text;
using UberLib.Connector;
using Ionic.Zip;
using System.IO;
using System.Xml;
using CMS.Plugins;

namespace CMS.Base
{
    /// <summary>
    /// A utility class of commonly used code.
    /// </summary>
    public static class BaseUtils
    {
        /// <summary>
        /// Executes a multi-line SQL file on the provided connector.
        /// </summary>
        /// <param name="path">The path of the file to execute.</param>
        /// <param name="conn">The connector on-which to execute the file's data/SQL.</param>
        /// <returns></returns>
        public static string executeSQL(string path, Connector conn)
        {
            try
            {
                if (!File.Exists(path))
                    throw new Exception("SQL script '" + path + "' could not be found!");
                else
                {
                    StringBuilder statements = new StringBuilder();
                    // Build the new list of statements to be executed by stripping out any comments
                    string data = File.ReadAllText(path).Replace("\r", string.Empty);
                    int commentIndex;
                    foreach (string line in data.Split('\n'))
                    {
                        commentIndex = line.IndexOf("--");
                        if (commentIndex == -1)
                            statements.Append(line).Append("\r\n");
                        else if (commentIndex < line.Length)
                            statements.Append(line.Substring(0, commentIndex)).Append("\r\n");
                    }
                    // Execute the statements
                    conn.queryExecute(statements.ToString());
                    return null;
                }
            }
            catch (Exception ex)
            {
                return "Failed to execute SQL file '" + path + "' - " + ex.Message + " - " + ex.GetBaseException().Message + "!";
            }
        }
        /// <summary>
        /// Extracts a zip file to the destination path. This will overwrite pre-existing files!
        /// </summary>
        /// <param name="sourcePath">Path to the source-file/zip-file to extract.</param>
        /// <param name="destinationPath">The destination directory for the contents of the zip.</param>
        /// <returns></returns>
        public static bool extractZip(string sourcePath, string destinationPath)
        {
            try
            {
                using (ZipFile file = new ZipFile(sourcePath))
                {
                    foreach (ZipEntry entry in file)
                        entry.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);
                }
                return true;
            }
            catch { }
            return false;
        }
        /// <summary>
        /// Installs content from the source path and clones/copies it to the destination folder.
        /// 
        /// Note: source files can end with .file to avoid the ASP.NET runtime loading the files into the assembly;
        /// this is useful for any .cs and .js files; this file extension will be automatically removed. Thus you
        /// can rename a file to example.cs.file and the output will be example.cs.
        /// </summary>
        /// <param name="pathSource">The source path of the files to copy.</param>
        /// <param name="pathDestination">The destination of the files to be copied.</param>
        /// <param name="overwrite">Indicates if you want to overwrite a file if it exists at the specified location.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public static bool contentInstall(string pathSource, string pathDestination, bool overwrite, ref StringBuilder messageOutput)
        {
            try
            {
                string destPath;
                string destDirectory;
                foreach (string file in Directory.GetFiles(pathSource, "*", SearchOption.AllDirectories))
                {
                    destPath = pathDestination + file.Substring(pathSource.Length).Replace('\\', '/');
                    if (destPath.EndsWith(".file"))
                        destPath = destPath.Remove(destPath.Length - 5, 5);
                    destDirectory = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destDirectory))
                        Directory.CreateDirectory(destDirectory);
                    File.Copy(file, destPath, false);
                }
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to install content from source '" + pathSource + "' to destination '" + pathDestination + "'; exception: " + ex.Message + "!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Uninstalls content from the destination path using the source path as a model of the files to remove.
        /// </summary>
        /// <param name="pathSource">The source directory used as a model of files to be removed from the destination directory.</param>
        /// <param name="pathDestination">The directory of which to remove files from.</param>
        /// <param name="messageOutput">Message output/</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public static bool contentUninstall(string pathSource, string pathDestination, ref StringBuilder messageOutput)
        {
            try
            {
                string destPath;
                string destDirectory;
                foreach (string file in Directory.GetFiles(pathSource, "*", SearchOption.AllDirectories))
                {
                    destPath = pathDestination + file.Substring(pathSource.Length).Replace('\\', '/');
                    if (destPath.EndsWith(".file"))
                        destPath = destPath.Remove(destPath.Length - 5, 5);
                    try
                    {
                        File.Delete(destPath);
                    }
                    catch (Exception ex)
                    {
                        messageOutput.AppendLine("Warning - could not delete file '" + destPath + "' - '" + ex.Message + "'!");
                    }
                    try
                    {
                        destDirectory = Path.GetDirectoryName(destPath);
                        // Delete the directory if it's empty
                        if (Directory.Exists(destDirectory) && Directory.GetFiles(destDirectory).Length == 0)
                            // Attempt to delete the directory - we could get no files back due to permissions not allowing us to access certain files,
                            // it's not critical the directory is deleted so we can ignore it...
                            Directory.Delete(destDirectory);
                    }
                    catch(Exception ex)
                    {
                        messageOutput.AppendLine("Warning - could not delete empty directory '" + destPath + "' -  '" + ex.Message + "'!");
                    }
                }
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to uninstall content using source '" + pathSource + "' from destination '" + pathDestination + "'; exception: " + ex.Message + "!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Adds a new preprocessor directive symbol to the web.config file.
        /// </summary>
        /// <param name="symbol">The symbol to be added.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public static bool preprocessorDirective_Add(string symbol, ref StringBuilder messageOutput)
        {
            return preprocessorDirective_Modify(symbol, true, ref messageOutput);
        }
        /// <summary>
        /// Removes a preprocessor directive symbol from the web.config file.
        /// </summary>
        /// <param name="symbol">The symbol to be removed.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation failed.</returns>
        public static bool preprocessorDirective_Remove(string symbol, ref StringBuilder messageOutput)
        {
            return preprocessorDirective_Modify(symbol, false, ref messageOutput);
        }
        private static bool preprocessorDirective_Modify(string symbol, bool addingSymbol, ref StringBuilder messageOutput)
        {
            try
            {
                string configPath = Core.WebConfigPath;
                XmlDocument webConfig = new XmlDocument();
                webConfig.Load(configPath);
                XmlNode compiler;
                if ((compiler = webConfig.SelectSingleNode("configuration/system.codedom/compilers/compiler")) == null)
                {
                    messageOutput.AppendLine("The web.config is missing the compiler section and hence directives cannot be added! Please modify your web.config...");
                    return false;
                }
                else
                {
                    if (addingSymbol)
                    {
                        string symbols = compiler.Attributes["compilerOptions"].Value;
                        if (symbols.Length == 0)
                            symbols = "/d:" + symbol;
                        else if (symbols.Contains("/d:" + symbol + ",") || symbols.Contains("," + symbol + ",") || symbols.EndsWith("," + symbol))
                            return true; // Contains pre-processor already
                        else
                            symbols += "," + symbol;
                        compiler.Attributes["compilerOptions"].Value = symbols;
                    }
                    else
                    {
                        string symbols = compiler.Attributes["compilerOptions"].Value;
                        if (symbols.Length == 0)
                            return true; // No values to remove, just return
                        else if (symbols.Length == 3 + symbol.Length)
                            symbols = string.Empty; // The symbol string must be /d:<symbol> - hence we'll leave it empty
                        else if (symbols.EndsWith("," + symbol))
                            symbols = symbols.Remove(symbols.Length - (symbol.Length + 1), symbol.Length + 1);
                        else
                        {
                            // Remove the symbol, which could be like /d:<symbol>, *or* ,<symbol>,
                            symbols = symbols.Replace("/d:" + symbol + ",", "/d:").Replace("," + symbol + ",", ",");
                            // Remove ending ,<symbol>
                            if (symbols.EndsWith("," + symbol)) symbols = symbols.Remove(symbols.Length - (symbol.Length + 1), symbol.Length + 1);
                        }
                        // -- Update the modified flags
                        compiler.Attributes["compilerOptions"].Value = symbols;
                    }
                    webConfig.Save(configPath);
                }
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to " + (addingSymbol ? "add" : "remove") + " pre-processor directive symbol '" + symbol + "' - " + ex.Message + "!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Appends a CSS link to the header.
        /// </summary>
        /// <param name="webPath">Path to CSS file.</param>
        /// <param name="data">Data object for the current request.</param>
        public static void headerAppendCss(string webPath, ref Data data)
        {
            string t = "<link href=\"" + webPath + "\" type=\"text/css\" rel=\"Stylesheet\" />";
            headerAppend(ref t, ref data);
        }
        /// <summary>
        /// Appends a CSS link to the header.
        /// </summary>
        /// <param name="webPath">Path to CSS file.</param>
        /// <param name="data">Data object for the current request.</param>
        /// <param name="media">Media attribute options, refer to: http://www.w3schools.com/tags/att_link_media.asp</param>
        public static void headerAppendCss(string webPath, ref Data data, string media)
        {
            string t = "<link href=\"" + webPath + "\" type=\"text/css\" rel=\"Stylesheet\" media=\"" + media + "\" />";
            headerAppend(ref t, ref data);
        }
        /// <summary>
        /// Appends a JavaScript file include to the header.
        /// </summary>
        /// <param name="webPath">Path to the JavaScript file.</param>
        /// <param name="data">Data object for the current request.</param>
        public static void headerAppendJs(string webPath, ref Data data)
        {
            string t = "<script src=\"" + webPath + "\"></script>";
            headerAppend(ref t, ref data);
        }
        private static void headerAppend(ref string entity, ref Data data)
        {
            if (!data.isKeySet("Header"))
                data["Header"] = entity;
            else if (!data["Header"].Contains(entity))
                data["Header"] += entity;
        }
        /// <summary>
        /// Returns the human-readable format of a date-time string, which converts a date into the nearest time
        /// unit e.g. 20 seconds ago, 20 minutes ago, 20 days ago or 20 years ago. Any date past greater or equal to
        /// 365 days ago is returned as the actual date and time.
        /// </summary>
        /// <param name="dt">The date-time of the event.</param>
        /// <returns>Human-readable format of the date.</returns>
        public static string dateTimeToHumanReadable(DateTime dt)
        {
            TimeSpan t = DateTime.Now.Subtract(dt);
            if (t.TotalSeconds < 60)
                return t.TotalSeconds < 2 ? "1 second ago" : Math.Round(t.TotalSeconds, 0) + " seconds ago";
            else if (t.TotalMinutes < 60)
                return t.TotalMinutes < 2 ? "1 minute ago" : Math.Round(t.TotalMinutes, 0) + " minutes ago";
            else if (t.TotalHours < 24)
                return t.TotalHours < 2 ? "1 hour ago" : Math.Round(t.TotalHours, 0) + " hours ago";
            else if (t.TotalDays < 365)
                return t.TotalDays < 2 ? "1 day ago" : Math.Round(t.TotalDays, 0) + " days ago";
            else
                return dt.ToString("dd/MM/yyyy HH:mm:ss");
        }
        /// <summary>
        /// Converts bytes to the largest possible unit of bytes available to make it more human-readable.
        /// </summary>
        /// <param name="bytes">Total number of bytes.</param>
        /// <returns>Bytes parameter formatted into a larger unit of bytes.</returns>
        public static string getBytesString(float bytes)
        {
            const float kiolobyte = 1024.0f;
            const float megabyte = 1048576.0f;
            const float gigabyte = 1073741824.0f;
            const float terrabyte = 1099511627776.0f;
            const float petabyte = 1125899906842624.0f;

            if (bytes < kiolobyte)
                return bytes + " B";
            else if (bytes < megabyte)
                return (bytes / kiolobyte).ToString("0.##") + " KB";
            else if (bytes < gigabyte)
                return (bytes / megabyte).ToString("0.##") + " MB";
            else if (bytes < terrabyte)
                return (bytes / gigabyte).ToString("0.##") + " GB";
            else if (bytes < petabyte)
                return (bytes / terrabyte).ToString("0.##") + "TB";
            else
                return (bytes / petabyte).ToString("0.##") + " PB";
        }
        /// <summary>
        /// Indicates if the provided string is a valid integer.
        /// </summary>
        /// <param name="text">Text to be tested.</param>
        /// <returns>True if integer, false if not an integer.</returns>
        public static bool isNumeric(string text)
        {
            int output;
            return int.TryParse(text, out output);
        }
        /// <summary>
        /// Indicates if the provided string is a valid integer.
        /// </summary>
        /// <param name="text">Text to be tested.</param>
        /// <param name="min">The minimum value of the number allowed.</param>
        /// <returns>True if integer, false if not an integer.</returns>
        public static bool isNumeric(string text, int min)
        {
            int output;
            return int.TryParse(text, out output) && output >= min;
        }
        /// <summary>
        /// Indicates if the provided string is a valid integer.
        /// </summary>
        /// <param name="text">Text to be tested.</param>
        /// <param name="min">The minimum value of the number allowed.</param>
        /// <param name="max">The maximum value of the number allowed.</param>
        /// <returns>True if integer, false if not an integer.</returns>
        public static bool isNumeric(string text, int min, int max)
        {
            int output;
            return int.TryParse(text, out output) && output >= min && output <= max;
        }
        /// <summary>
        /// Reserves multiple URL rewriting paths.
        /// </summary>
        /// <param name="plugin">The owner of the paths; cannot be null.</param>
        /// <param name="paths">String array consisting of the paths to reserve.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation fails.</returns>
        public static bool urlRewritingInstall(Plugin plugin, string[] paths, ref StringBuilder messageOutput)
        {
            if (paths.Length == 0) // Check we have work to do, else we'll just skip straight out.
                return true;
            else if (plugin == null)
            {
                messageOutput.AppendLine("URL rewriting install - plugin cannot be null!");
                return false;
            }
            try
            {
                StringBuilder query = new StringBuilder("INSERT INTO cms_urlrewriting (uuid, full_path) VALUES");
                foreach (string s in paths)
                    query.Append("(").Append(plugin.UUID.SQLValue).Append(", '").Append(SQLUtils.escape(s)).Append("'),");
                query.Remove(query.Length - 1, 1).Append(";");
                Core.Connector.queryExecute(query.ToString());
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("URL rewriting - failed to install items; exception: '" + ex.Message + "'!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Uninstalls all of the URL rewriting reservations associated with the specified plugin.
        /// </summary>
        /// <param name="plugin">The owner of the paths; cannot be null.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation fails.</returns>
        public static bool urlRewritingUninstall(Plugin plugin, ref StringBuilder messageOutput)
        {
            return urlRewritingUninstall(plugin, null, ref messageOutput);
        }
        /// <summary>
        /// Uninstalls all of the URL rewriting reservations associated with the specified plugin.
        /// </summary>
        /// <param name="plugin">The owner of the paths; cannot be null.</param>
        /// <param name="path">The starting path of elements to be deleted. If you specify e.g. \example, the paths \example, \example\a, \example\b would be deleted.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation fails.</returns>
        public static bool urlRewritingUninstall(Plugin plugin, string path, ref StringBuilder messageOutput)
        {
            if (plugin == null)
            {
                messageOutput.AppendLine("URL rewriting install - plugin cannot be null!");
                return false;
            }
            try
            {
                if (path == null)
                    Core.Connector.queryExecute("DELETE FROM cms_urlrewriting WHERE uuid=" + plugin.UUID.SQLValue + ";");
                else
                    Core.Connector.queryExecute("DELETE FROM cms_urlrewriting WHERE full_path LIKE '" + SQLUtils.escape(path) + "%';");
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("URL rewriting - failed to uninstall items at path '" + path + "'; exception: '" + ex.Message + "'!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Generates a string of random characters.
        /// </summary>
        /// <param name="length">The length of the string to generate.</param>
        /// <returns>The random string generated.</returns>
        public static string generateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz01234567890";
            Random rand = new Random((int)DateTime.Now.ToBinary());
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < length; i++)
                buffer.Append(chars[rand.Next(chars.Length - 1)]);
            return buffer.ToString();
        }
    }
}