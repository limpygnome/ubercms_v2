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
 *      File:           App_code/CMS/Base/Settings.cs
 *      Path:           /Settings.cs
 * 
 *      Change-Log:
 *                      2013-06-27      Created initial class.
 *                      2013-06-29      Finished initial class.
 *                      2013-06-06      Added ability to just add keys, without update.
 *                                      Added ability to remove settings by plugin, pluginid and path.
 *                      2013-06-07      Fixed critical bug in save method, where new nodes could not be saved.
 * 
 * *********************************************************************************************************************
 * Handles settings which can be stored on disk or in a database. Thread-safe.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;
using UberLib.Connector;
using CMS.Plugins;

namespace CMS
{
	namespace Base
	{
        /// <summary>
        /// Handles settings which can be stored on disk or in a database. Thread-safe.
        /// </summary>
		public class Settings
		{
			// Fields **************************************************************************************************
			private Dictionary<string, SettingsNode> config; // Path of setting, setting node
			// Methods - Constructors **********************************************************************************
			private Settings()
			{
				config = new Dictionary<string, SettingsNode>();
			}
			// Methods
			/// <summary>
			/// Saves the settings to the database of the connector.
			/// </summary>
			/// <param name="conn">Conn.</param>
			public void save(Connector conn)
			{
				lock(this)
				{
					StringBuilder sqlUpdated = new StringBuilder();
					StringBuilder sqlInserted = new StringBuilder("INSERT INTO cms_settings (path, pluginid, description, value) VALUES");
					int sqlInsertedDefaultLength = sqlInserted.Length;
					// Iterate keys
                    foreach (KeyValuePair<string, SettingsNode> k in config)
                    {
                        switch (k.Value.State)
                        {
                            case SettingsNode.SettingsNodeState.ModifiedAll:
                                sqlUpdated.Append("UPDATE cms_settings SET value='" + Utils.Escape(k.Value.Value) + "', description='" + Utils.Escape(k.Value.Description) + "' WHERE path='" + Utils.Escape(k.Key) + "'; ");
                                break;
                            case SettingsNode.SettingsNodeState.ModifiedDescription:
                                sqlUpdated.Append("UPDATE cms_settings SET description='" + Utils.Escape(k.Value.Description) + "' WHERE path='" + Utils.Escape(k.Key) + "'; ");
                                break;
                            case SettingsNode.SettingsNodeState.ModifiedValue:
                                sqlUpdated.Append("UPDATE cms_settings SET value='" + Utils.Escape(k.Value.Value) + "' WHERE path='" + Utils.Escape(k.Key) + "'; ");
                                break;
                            case SettingsNode.SettingsNodeState.Added:
                                sqlInserted.Append("('" + Utils.Escape(k.Key) + "', '" + Utils.Escape(k.Value.PluginID.ToString()) + "', " + (k.Value.Description == null ? "NULL" : "'" + k.Value.Description + "'") + ", " + (k.Value.Value == null ? "NULL" : "'" + k.Value.Value + "'") + "),");
                                break;
                        }
                    }
					// Push to the database
					if(sqlUpdated.Length > 0)
						conn.Query_Execute(sqlUpdated.ToString());
					if(sqlInserted.Length > sqlInsertedDefaultLength)
					{
						sqlInserted.Remove(sqlInserted.Length - 1, 1).Append(';'); // Remove tailing comma, append seperator
						conn.Query_Execute(sqlInserted.ToString());
					}
				}
			}
			// Methods - Static ****************************************************************************************
            /// <summary>
            /// Processes an XML node from an XML file.
            /// </summary>
            /// <param name="settings">The settings object.</param>
            /// <param name="path">The path of the current XML tree being iterated, the parent of this node.</param>
            /// <param name="node">The current node to process.</param>
			private static void loadProcessNode(ref Settings settings, string path, XmlNode node)
			{
				// Process child nodes
				bool nodes = false;
				foreach(XmlNode n in node.ChildNodes)
					if(n.Name != "#text") // #text is created, in-duplication, as apart of the standard and should be ignored
					{
						nodes = true;
						loadProcessNode(ref settings, path + "/" + n.Name, n);
					}
                // Process the current node
				if(!nodes)
					settings.config.Add(path, new SettingsNode(node.InnerText));
			}
			/// <summary>
			/// Loads settings from an xml file on disk.
			/// </summary>
			/// <returns>Settings object with loaded configuration.</returns>
			/// <param name="path">Path to XML configuration file.</param>
			public static Settings loadFromDisk(string path)
			{
				try
				{
					Settings settings = new Settings();
					XmlDocument xml = new XmlDocument();
					// Load configuration
					xml.Load(path);
					// Process all the nodes
					foreach(XmlNode node in xml.ChildNodes)
						if(node.Name != "#text" && node.Name != "xml")
							loadProcessNode(ref settings, node.Name, node);
					return settings;
				}
				catch(SecurityException)
				{
					Core.ErrorMessage = "Unable to open configuration file at '" + path + "' due to security/permission issues!";
				}
				catch(FileNotFoundException)
				{
                    Core.ErrorMessage = "Configuration file at path '" + path + "' not found!";
				}
				catch(ArgumentException)
				{
                    Core.ErrorMessage = "Empty XML file for path '" + path + "'!";
				}
				catch(XmlException)
				{
                    Core.ErrorMessage = "Unable to parse XML (possibly invalid) for path '" + path + "'!";
				}
				catch(Exception ex)
				{
                    Core.ErrorMessage = "Unknown exception loading settings for path '" + path + "' - '" + ex.Message + "'!";
				}
				return null;
			}
			/// <summary>
			/// Loads settings from database.
			/// </summary>
			/// <returns>Settings object with loaded configuration.</returns>
			/// <param name="conn">A connector connected to the desired database of which the settings are to be loaded from.</param>
			public static Settings loadFromDatabase(Connector conn)
			{
				try
				{
					Settings settings = new Settings();
                    Result result = conn.Query_Read("SELECT * FROM cms_view_settings_load");
					foreach(ResultRow row in result)
						settings.config.Add(row["path"], new SettingsNode(row["path"], row["description"], row["pluginid"].Length == 0 ? 0 : int.Parse(row["pluginid"])));
                    return settings;
				}
				catch(Exception ex)
				{
                    Core.ErrorMessage = "Unknown exception loading settings from database: " + ex.Message + "'!";
				}
				return null;
			}
			// Methods - Mutators **************************************************************************************
            /// <summary>
            /// Updates a configuration node; however if the node does not exist, it's added to the collection. Method
            /// 'save' must be invoked to persist the data to the data-store.
            /// </summary>
            /// <param name="pluginid">The owner of the plugin; this will not be updated if the node exists.</param>
            /// <param name="path">The path of the node.</param>
            /// <param name="description">A description of the node; can be left null to not be updated.</param>
            /// <param name="value">The value of the node; can be left null to not be updated.</param>
            /// <param name="updateOnly">Indicates if to only update the node.</param>
            /// <param name="throwExceptionNonExistant">Indicates if to throw an exception if the node does not exist; this will throw a KeyNotFound exception.</param>
			public void updateOrAdd(int pluginid, string path, string description, string value, bool updateOnly, bool throwExceptionNonExistant)
			{
                lock(this)
                {
				    if(config.ContainsKey(path))
				    {
                        if (description != null)
                            this[path].Description = description;
                        if (value != null)
                            this[path].Value = value;
				    }
				    else if(throwExceptionNonExistant)
                        throw new KeyNotFoundException("Settings node '" + path + "' not found!");
                    else if(!updateOnly)
					    config.Add(path, new SettingsNode(value, description, pluginid, SettingsNode.SettingsNodeState.Added));
                }
			}
            /// <summary>
            /// Adds a new key to the collection. If the key already exists, nothing is changed.
            /// </summary>
            /// <param name="pluginid">The identifier of the plugin which owns the setting.</param>
            /// <param name="path">The path of the node.</param>
            /// <param name="description">A decription of the node; can be null.</param>
            /// <param name="value">The value of a node.</param>
            public void add(int pluginid, string path, string description, string value)
            {
                lock (this)
                {
                    if (!config.ContainsKey(path))
                        config.Add(path, new SettingsNode(value, description, pluginid, SettingsNode.SettingsNodeState.Added));
                }
            }
            /// <summary>
            /// Removes all of the settings owned by a plugin.
            /// </summary>
            /// <param name="plugin">The owner of the settings to be removed.</param>
            public void remove(Plugin plugin)
            {
                remove(plugin.PluginID);
            }
            /// <summary>
            /// Removes all of the settings owned by a plugin.
            /// </summary>
            /// <param name="pluginid">The identifier of the plugin which owns the settings to be removed.</param>
            public void remove(int pluginid)
            {
                lock (this)
                {
                    // Delete settings from the database
                    Core.Connector.Query_Execute("DELETE FROM cms_settings WHERE pluginid='" + Utils.Escape(pluginid.ToString()) + "';");
                    // Find keys to remove
                    List<string> temp = new List<string>();
                    foreach (KeyValuePair<string, SettingsNode> kv in config)
                    {
                        if (kv.Value.PluginID == pluginid)
                            temp.Add(kv.Key);
                    }
                    // Remove the keys
                    foreach (string s in temp)
                        config.Remove(s);
                }
            }
            /// <summary>
            /// Removes a single configuration key.
            /// </summary>
            /// <param name="path">The path of the node to be removed.</param>
            public void remove(string path)
            {
                lock (this)
                {
                    if (config.ContainsKey(path))
                    {
                        Core.Connector.Query_Execute("DELETE FROM cms_settings WHERE path='" + Utils.Escape(path) + "'");
                        config.Remove(path);
                    }
                }
            }
			// Methods - Accessors *************************************************************************************
            /// <summary>
            /// Indicates if a node exists at the specified path.
            /// </summary>
            /// <param name="path">The path of the node to check.</param>
            /// <returns></returns>
			public bool contains(string path)
			{
				return config.ContainsKey(path);
			}
            /// <summary>
            /// Fetches the value of a node at the specified path as an integer; no integer-safety is present for
            /// efficiency!
            /// </summary>
            /// <param name="path">The path of the node.</param>
            /// <returns></returns>
			public int getInteger(string path)
			{
				return int.Parse(config[path].Value);
			}
            /// <summary>
            /// Fetches the value of a node from the collection.
            /// 
            /// If the node does not exist, a node is created and the default value is returned.
            /// </summary>
            /// <param name="pluginid">The identifier of the plugin which owns the setting.</param>
            /// <param name="path">The path of the node.</param>
            /// <param name="description">A description of the node.</param>
            /// <param name="defaultValue">The default value of the node.</param>
            /// <returns>The value of the node.</returns>
			public string get(int pluginid, string path, string description, string defaultValue)
			{
				if(config.ContainsKey(path))
					return config[path].Value;
				else
				{
					updateOrAdd(pluginid, path, description, defaultValue, false, false);
					return defaultValue;
				}
			}
            /// <summary>
            /// Returns the value of the node at the specified path; if the node does not exist, null is returned.
            /// </summary>
            /// <param name="path">The path of the node.</param>
            /// <returns></returns>
            public string get(string path)
            {
                return config.ContainsKey(path) ? config[path].Value : null;
            }
            /// <summary>
            /// Fetches the value of a node from the collection as an integer.
            /// 
            /// If the node does not exist, a node is created and the default value is returned.
            /// </summary>
            /// <param name="pluginid">The identifier of the plugin which owns the setting.</param>
            /// <param name="path">The path of the node.</param>
            /// <param name="description">A description of the node.</param>
            /// <param name="defaultValue">The default value of the node.</param>
            /// <returns>The integer value of the node.</returns>
			public int getInteger(int pluginid, string path, string description, int defaultValue)
			{
				if(config.ContainsKey(path))
					return int.Parse(config[path].Value);
				else
				{
					updateOrAdd(pluginid, path, description, defaultValue.ToString(), false, false);
					return defaultValue;
				}
			}
            /// <summary>
            /// Fetches the value of a node from the collection as an integer with range checking.
            /// 
            /// If the node does not exist, a node is created and the default value is returned. If the value is not
            /// within the range, only the default value is returned (the node is not updated).
            /// </summary>
            /// <param name="pluginid">The identifier of the plugin which owns the setting.</param>
            /// <param name="path">The path of the node.</param>
            /// <param name="description">A description of the node.</param>
            /// <param name="defaultValue">The default value of the node.</param>
            /// <param name="min">The inclusive minimum allowed for the value.</param>
            /// <param name="max">The inclusive maximum allowed for the value.</param>
            /// <returns></returns>
            public int getInteger(int pluginid, string path, string description, int defaultValue, int min, int max)
			{
				if(config.ContainsKey(path))
				{
					int v = int.Parse(config[path].Value);
					if(v < min || v > max)
						return defaultValue;
					else
						return v;
				}
				else
				{
					updateOrAdd(pluginid, path, description, defaultValue.ToString(), false, false);
					return defaultValue;
				}
			}
            /// <summary>
            /// A debug function for outputting the settings. Lines are broken by HTML tag 'br'.
            /// </summary>
            /// <returns></returns>
            public string getDebug()
            {
                StringBuilder debug = new StringBuilder();
                foreach (KeyValuePair<string, SettingsNode> node in config)
                    debug.Append("'" + node.Key + "'='" + node.Value.Value + "' (plugin ID: '" + node.Value.PluginID + "')<br />");
                return debug.ToString();
            }
            // Methods - Properties ************************************************************************************
            /// <summary>
            /// Returns the node at the specified path.
            /// </summary>
            /// <param name="path">The path of the node.</param>
            /// <returns>The node.</returns>
            public SettingsNode this[string path]
            {
                get
                {
                    lock(this)
                        return config.ContainsKey(path) ? config[path] : null;
                }
            }
            /// <summary>
            /// Returns all of the nodes held by this collection.
            /// </summary>
            public Dictionary<string, SettingsNode> KeyValues
            {
                get
                {
                    return config;
                }
            }
		}
	}
}

