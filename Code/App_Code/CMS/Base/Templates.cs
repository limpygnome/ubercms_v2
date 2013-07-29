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
 *      Path:           /App_Code/CMS/Base/Templates.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Finished initial class.
 *                      2013-07-01      Added template install/uninstall/dump methods.
 *                                      Added include template handler.
 *                                      Added uninstall by plugin method.
 *                                      Added dump for plugin method.
 *                      2013-07-06      Fixed dump bug.
 *                                      Fixed major multi-level rendering bug with replacementOccurred variable.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                      2013-07-23      Updated the way settings are handled.
 * 
 * *********************************************************************************************************************
 * Used to load, and possibly cache, HTML templates from the database; this class also transforms the custom markup
 * syntax.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Xml;
using UberLib.Connector;
using CMS.Plugins;

namespace CMS.Base
{
    /// <summary>
    /// Used to load, and possibly cache, HTML templates from the database; this class also transforms the custom
    /// markup syntax.
    /// </summary>
	public class Templates
	{
        // Delegates ***************************************************************************************************
        public delegate string TemplateFunction(Data data, string[] args);
		// Fields ******************************************************************************************************
		private bool                                loadFromCache;  // Indicates if to load templates from cache, else templates will be loaded straight from the database.
		private Dictionary<string,string>           primaryCache;   // Used to cache copies of templates from the database for reduced I/O.
        private Dictionary<string,TemplateFunction> functions;      // Function name,function delegate; used for template function calls.
		// Methods - Constructors **********************************************************************************
		private Templates()
		{
			primaryCache = new Dictionary<string, string>();
            functions = new Dictionary<string, TemplateFunction>();
		}
		// Methods *****************************************************************************************************
        /// <summary>
        /// Renders the specified text according to the rules of the custom markup syntax.
        /// </summary>
        /// <param name="text">The text to be rendered/formatted.</param>
        /// <param name="data">The data for the current request.</param>
		public void render(ref StringBuilder text, ref Data data)
		{
			render(ref text, 0, 8, ref data);
		}
		private void render(ref StringBuilder text, int currTree, int treeMax, ref Data data)
		{
            bool replacementOccurred = false; // Indicates if any changes have occurred to the text variable.
			// Find if-statements
			bool expressionValue;
			bool expressionValueNegated;
			string expression;
			MatchCollection elseMC;
			bool foundFlag;
			bool containsFlag;
			MatchCollection matches = Regex.Matches(text.ToString(), @"<!--IF:([a-zA-Z0-9!_\|\&]*)-->(.*?)<!--ENDIF(:\1)?-->", RegexOptions.Singleline);
			foreach (Match m in matches)
			{
				expression = m.Groups[1].Value;
				expressionValueNegated = expression.StartsWith("!");
				if (expression.Contains("|"))
				{
					foundFlag = false;
					// Iterate each flag inside of an expression like e.g. flag1|flag2|flag3 until we find it
					foreach (string s in expression.Split('|'))
					{
						expressionValueNegated = s.StartsWith("!");
						if ((expressionValueNegated && s.Length > 1) || (!expressionValueNegated && s.Length > 0))
						{
							containsFlag = data.isKeySet(expressionValueNegated ? s.Substring(1) : s);
							if ((expressionValueNegated ? !containsFlag : containsFlag))
							{
								foundFlag = true;
								break;
							}
						}
					}
					expressionValue = foundFlag;
				}
				else if (expression.Contains("&"))
				{
					foundFlag = true; // We leave this as true until a flag is not found, then we break the iteration since the expression is no longer valid
					foreach (string s in expression.Split('&'))
					{
						expressionValueNegated = s.StartsWith("!");
						if ((expressionValueNegated && s.Length > 1) || (!expressionValueNegated && s.Length > 0))
						{
							containsFlag = data.isKeySet(expressionValueNegated ? s.Substring(1) : s);
							if (!(expressionValueNegated ? !containsFlag : containsFlag))
							{
								foundFlag = false;
								break;
							}
						}
					}
					expressionValue = foundFlag;
				}
				else if((!expression.StartsWith("!") && expression.Length > 0) || expression.Length > 1)
					// Expression contains no other operators
					expressionValue = expressionValueNegated ? !data.isKeySet(expression.Substring(1)) : data.isKeySet(expression);
				else
					expressionValue = false;

				elseMC = Regex.Matches(m.Groups[2].Value, @"(.*?)<!--ELSE-->(.*$?)", RegexOptions.Singleline);
				if (elseMC.Count == 1)
				{
                    if (expressionValue)
                    {
                        text.Replace(m.Value, elseMC[0].Groups[1].Value);
                        replacementOccurred = true;
                    }
                    else
                    {
                        text.Replace(m.Value, elseMC[0].Groups[2].Value);
                        replacementOccurred = true;
                    }
				}
				else
				{
                    if (expressionValue)
                    {
                        text.Replace(m.Value, m.Groups[2].Value);
                        replacementOccurred = true;
                    }
                    else
                        text.Replace(m.Value, string.Empty);
				}
			}
            // Find fucntion callbacks
            foreach (Match m in Regex.Matches(text.ToString(), @"<!--([a-zA-Z0-9_]*)\(([a-zA-Z0-9_,]*)\)-->"))
            {
                if (functions.ContainsKey(m.Groups[1].Value))
                {
                    text.Replace(m.Value, functions[m.Groups[1].Value](data, m.Groups[2].Value.Split(','))); // Group 1 = function name, group 2 = params i.e. a,b,c,..,n
                    replacementOccurred = true;
                }
                else
                    text.Replace(m.Value, "Function '" + m.Groups[1].Value + "' undefined!");
            }
			// Find replacement tags (for replacing sections of text with request variables)
            foreach (Match m in Regex.Matches(text.ToString(), @"<!--([a-zA-Z0-9_]*)-->"))
            {
                if (data.isKeySet(m.Groups[1].Value))
                {
                    text.Replace(m.Value, data[m.Groups[1].Value]);
                    replacementOccurred = true;
                }
                else
                    text.Replace(m.Value, "Element '" + m.Groups[1].Value + "' undefined!");
            }
			// Check if to iterate again - check we haven't surpassed max tree-level and we found a match i.e. data changed
			currTree++;
			if (replacementOccurred && currTree < treeMax)
				render(ref text, currTree, treeMax, ref data);
		}
        /// <summary>
        /// Reloads the templates held within the collection, if caching is enabled.
        /// </summary>
        /// <param name="conn"></param>
        public void reload(Connector conn)
        {
            lock (this)
            {
                if (loadFromCache = Core.SettingsDisk["settings/templates/cache"].get<bool>())
                {
                    // Clear any previous templates
                    primaryCache.Clear();
                    // Copy the templates from the database
                    foreach (ResultRow template in Core.Connector.queryRead("SELECT path, html FROM cms_templates"))
                        primaryCache.Add(template["path"], template["html"]);
                }
            }
        }
        /// <summary>
        /// Dumps templates from the database to disk; useful for development.
        /// 
        /// Note: if the destination already exists, it will be deleted! If you want to avoid this, check the
        /// destination before invoking this method.
        /// </summary>
        /// <param name="pathDestination">The physical path to dump the templates.</param>
        /// <param name="path">The template parent path; anything matching the path from the left-side will be dumped e.g. specifying \example will dump everything starting with \example e.g. \example, \example\a, etc.</param>
        /// <param name="messageOutput">Message output.</param>
        public void dump(string pathDestination, string path, ref StringBuilder messageOutput)
        {
            dump(pathDestination, path, null, ref messageOutput);
        }
        /// <summary>
        /// Dumps templates from the database to disk; useful for development.
        /// 
        /// Note: if the destination already exists, it will be deleted! If you want to avoid this, check the
        /// destination before invoking this method.
        /// </summary>
        /// <param name="pathDestination">The physical path to dump the templates.</param>
        /// <param name="plugin">The owner of the templates to dump; can be null to dump core CMS templates</param>
        /// <param name="messageOutput">Message output.</param>
        public void dumpForPlugin(string pathDestination, Plugin plugin, ref StringBuilder messageOutput)
        {
            dump(pathDestination, null, plugin, ref messageOutput);
        }
        private void dump(string pathDestination, string path, Plugin plugin, ref StringBuilder messageOutput)
        {
            // Delete destination contents if it already exists
            if (Directory.Exists(pathDestination))
            {
                // Delete all the files
                foreach (string file in Directory.GetFiles(pathDestination, "*.xml", SearchOption.AllDirectories))
                    File.Delete(file);
                // Delete all the empty subdirs (any non-empty dirs will not contain xml files, thus skip them by allowing an exception)
                foreach (string subdir in Directory.GetDirectories(pathDestination, "*", SearchOption.AllDirectories))
                    try
                    { Directory.Delete(subdir, false); }
                    catch { }
            }
            else
                Directory.CreateDirectory(pathDestination);
            // Write the templates to the destination
            XmlWriter w;
            int count = 0;
            foreach (ResultRow template in Core.Connector.queryRead("SELECT path, description, html FROM cms_templates WHERE " + (path != null ? "path LIKE '" + SQLUtils.escape(path) + "%'" : plugin != null ? "uuid=" + plugin.UUID.SQLValue : "uuid=NULL")))
            {
                // Create dir and file info
                string dir = pathDestination + "/" + Path.GetDirectoryName(template["path"]);
                string file = Path.GetFileName(template["path"]) + ".xml";
                string dest = dir + "/" + file;
                // Create sub-directory if it does not exist
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                // Create the xml file
                w = XmlWriter.Create(dest);
                w.WriteStartDocument();
                w.WriteStartElement("template");

                w.WriteStartElement("path");
                w.WriteCData(template["path"]);
                w.WriteEndElement();

                w.WriteStartElement("description");
                w.WriteCData(template["description"]);
                w.WriteEndElement();

                w.WriteStartElement("html");
                w.WriteCData(template["html"]);
                w.WriteEndElement();

                w.WriteEndElement();
                w.WriteEndDocument();
                // Flush and close
                w.Flush();
                w.Close();
                messageOutput.AppendLine("Created template file '" + dest + "'.");
                count++;
            }
            messageOutput.Append("Dumped a total of ").Append(count).AppendLine(" templates.");
        }
        // Methods - Installation Related ******************************************************************************
        /// <summary>
        /// Installs templates from disk to the database and reloads the cache (if caching is enabled).
        /// </summary>
        /// <param name="conn">The database connector; must be unique, cannot be a shared connector!</param>
        /// <param name="plugin">The plugin which owns the template; can be null (not recommended).</param>
        /// <param name="pathSource">The physical source of the templates to be installed.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation fails.</returns>
        public bool install(Connector conn, Plugin plugin, string pathSource, ref StringBuilder messageOutput)
        {
            try
            {
                XmlDocument doc;
                conn.queryExecute("BEGIN;");
                foreach (string file in Directory.GetFiles(pathSource, "*.xml", SearchOption.AllDirectories))
                {
                    try
                    {
                        doc = new XmlDocument();
                        doc.LoadXml(File.ReadAllText(file));
                        conn.queryExecute("INSERT IGNORE INTO cms_templates (path, uuid, description, html) VALUES('" + SQLUtils.escape(doc["template"]["path"].InnerText) + "', " + plugin.UUID.SQLValue + ", '" + SQLUtils.escape(doc["template"]["description"].InnerText) + "', '" + SQLUtils.escape(doc["template"]["html"].InnerText) + "');");
                    }
                    catch (Exception ex)
                    {
                        messageOutput.AppendLine("Failed to install templates from '" + pathSource + "', exception occurred processing file '" + file + "': '" + ex.Message + "'!");
                        conn.queryExecute("ROLLBACK;");
                        return false;
                    }
                }
                conn.queryExecute("COMMIT;");
                reload(Core.Connector);
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to install templates from '" + pathSource + "', exception occurred: '" + ex.Message + "'!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Uninstalls templates from the database and reloads the cache (if caching is enabled).
        /// </summary>
        /// <param name="path">The template parent path; anything matching the path from the left-side will be dumped e.g. specifying \example will dump everything starting with \example e.g. \example, \example\a, etc.</param>
        /// <returns>True if successful, false if the operation fails.</returns>
        public bool uninstall(string path, ref StringBuilder messageOutput)
        {
            try
            {
                Core.Connector.queryExecute("DELETE FROM cms_templates WHERE path LIKE '" + SQLUtils.escape(path) + "%'");
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to delete templates at path '" + path + "'; exception: '" + ex.Message + "'!");
                return false;
            }
            try
            {
                reload(Core.Connector);
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to reload template cache from deleting templates at path '" + path + "'; exception: '" + ex.Message + "'!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Uninstalls templates from the database based on the ownership by a plugin.
        /// </summary>
        /// <param name="plugin">The owner of the templates to be removed.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>True if successful, false if the operation fails.</returns>
        public bool uninstall(Plugin plugin, ref StringBuilder messageOutput)
        {
            if (plugin == null)
            {
                messageOutput.AppendLine("Templates uninstall - plugin cannot be null!");
                return false;
            }
            try
            {
                Core.Connector.queryExecute("DELETE FROM cms_templates WHERE uuid=" + plugin.UUID.SQLValue + ";");
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to delete templates for plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "'); exception: '" + ex.Message + "'!");
                return false;
            }
            try
            {
                reload(Core.Connector);
            }
            catch (Exception ex)
            {
                messageOutput.AppendLine("Failed to reload template cache from deleting templates for plugin '" + plugin.Title + "' (UUID: '" + plugin.UUID.HexHyphens + "'); exception: '" + ex.Message + "'!");
                return false;
            }
            return true;
        }
		// Methods - Accessors *****************************************************************************************
		/// <summary>
		/// Fetches a template; returns an empty-string if the template is not found.
		/// </summary>
        /// <param name="conn">Database connector; used if caching is not enabled.</param>
		/// <param name="path">Path of template.</param>
		public string get(Connector conn, string path)
		{
			return get(conn, path, "");
		}
		/// <summary>
		/// Fetches a template.
		/// </summary>
        /// <param name="conn">Database connector; used if caching is not enabled.</param>
		/// <param name="path">Path of template.</param>
		/// <param name="alternative">Alternative string to be returned if the template does not exist.</param>
		public string get(Connector conn, string path, string alternative)
		{
			if(loadFromCache)
				return primaryCache.ContainsKey(path) ? primaryCache[path] : alternative;
			else
			{
				Result data = conn.queryRead("SELECT html FROM cms_templates WHERE path='" + SQLUtils.escape(path) + "'");
				return data.Count == 1 ? data[0]["html"] : alternative;
			}
		}
		// Methods - Static ********************************************************************************************
		/// <summary>
		/// Creates a new instance of this class, configured and cache loaded (if loading from cache).
		/// </summary>
		public static Templates create()
		{
			Templates templates = new Templates();
			// Cache templates
            templates.reload(Core.Connector);
            // Load function mappings
            Assembly ass = Assembly.GetExecutingAssembly();
            foreach (ResultRow function in Core.Connector.queryRead("SELECT path, classpath, function_name FROM cms_template_handlers"))
            {
                Type t = ass.GetType(function["classpath"], false);
                // Check we found the type - ignore if we haven't, function calls will state it's not defined (informing the developer)
                if (t != null)
                {
                    MethodInfo m = t.GetMethod(function["function_name"]);
                    if (m != null)
                    {
                        // Convert to delegate and add to function mappings
                        TemplateFunction f = new TemplateFunction(Delegate.CreateDelegate(typeof(TemplateFunction), m) as TemplateFunction);
                        templates.functions.Add(function["path"], f);
                    }
                }
            }
			return templates;
		}
        // Methods - Static - Default Handers **************************************************************************
        public static string handler_include(Data data, string[] args)
        {
            if (args.Length != 1)
                return "Template include error: invalid number of arguments!";
            else
                return Core.Templates.get(data.Connector, args[0], "Template include error: '" + args[0] + "' not found!");
        }
	}
}
