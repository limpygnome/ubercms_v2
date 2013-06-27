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
 *                      2013-06-27     Created initial class.
 * 
 * *****************************************************************************
 * Handles settings which can be stored on disk or in a database.
 * *****************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;
using UberLib.Connector;

namespace CMS
{
	namespace Base
	{
		public class Settings
		{
			// Sub-Class ***********************************************************************************************
			public class SettingsNode
			{
				// Enums ***********************************************************************************************
				public enum SettingsNodeState
				{
					None,
					Modified,
					Added
				}
				// Fields **********************************************************************************************
				private int pluginid;
				private string value, description;
				private SettingsNodeState state;
				// Methods - Constructors ******************************************************************************
				public SettingsNode(string value)
				{
					this.value = value;
					this.state = SettingsNodeState.None;
				}
				public SettingsNode(string value, SettingsNodeState state)
				{
					this.state = state;
				}
				public SettingsNode(string value, string description, int pluginid)
				{
					this.value = value;
					this.description = description;
					this.pluginid = pluginid;
				}
				public SettingsNode(string value, string description, int pluginid, SettingsNodeState state)
				{
					this.value = value;
					this.description = description;
					this.pluginid = pluginid;
					this.state = state;
				}
				// Methods - Properties ********************************************************************************
				public string this[string key]
				{
					get
					{
						return value;
					}
					set
					{
						Value = value;
					}
				}
				public SettingsNodeState State
				{
					get
					{
						return state;
					}
				}
				public int PluginID
				{
					get
					{
						return pluginid;
					}
				}
				public string Value
				{
					get
					{
						return value;
					}
					set
					{
						this.value = value;
						state = SettingsNodeState.Modified;
					}
				}
				public string Description
				{
					get
					{
						return description;
					}
					set
					{
						description = value;
						state = SettingsNodeState.Modified;
					}
				}
			}
			// Fields **************************************************************************************************
			private Dictionary<string, SettingsNode> config; // path of key, value
			// Methods - Constructors **********************************************************************************
			private Settings()
			{
				config = new Dictionary<string, SettingsNode>();
			}
			public Dictionary<string, SettingsNode> getAllKeys()
			{
				return config;
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
					StringBuilder sqlInserted = new StringBuilder("INSERT INTO cms_settings (path, pluginid, value, description) VALUES");
					int sqlInsertedDefaultLength = sqlInserted.Length;
					// Iterate keys
					foreach(KeyValuePair<string, SettingsNode> k in config)
						if(k.Value.State == SettingsNode.SettingsNodeState.Modified)
							sqlUpdated.Append("UPDATE cms_settings SET value='" + Utils.Escape(k.Value.Value) + "', description='" + Utils.Escape(k.Value.Description) + "' WHERE path='" + Utils.Escape(k.Key) + "'");
					// Push to the database
					if(sqlUpdated.Length > 0)
						conn.Query_Execute(sqlUpdated.ToString());
					if(sqlInserted.Length > sqlInsertedDefaultLength)
					{
						sqlInserted.Remove(sqlInserted.Length - 1, 1); // Remove tailing comma
						conn.Query_Execute(sqlInserted.ToString());
					}
				}
			}
			// Methods - Static ****************************************************************************************
			private static void loadProcessNode(ref Settings settings, string path, XmlNode node)
			{
				// Process child nodes
				bool nodes = false;
				foreach(XmlNode n in node.ChildNodes)
					if(n.Name != "#text")
					{
						nodes = true;
						loadProcessNode(ref settings, path + "\\" + n.Name, n);
					}
				if(!nodes)
					settings.config.Add(path, new SettingsNode(node.InnerText));
			}
			/// <summary>
			/// Loads settings from an xml file on disk.
			/// </summary>
			/// <returns>Settings object with loaded configuration.</returns>
			/// <param name="path">Path to XML configuration file.</param>
			public static Settings loadFromDisk(ref string errorMessage, string path)
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
					errorMessage = "Unable to open configuration file at '" + path + "' due to security/permission issues!";
				}
				catch(FileNotFoundException)
				{
					errorMessage = "Configuration file at path '" + path + "' not found!";
				}
				catch(ArgumentException)
				{
					errorMessage = "Empty XML file for path '" + path + "'!";
				}
				catch(XmlException)
				{
					errorMessage = "Unable to parse XML (possibly invalid) for path '" + path + "'!";
				}
				catch(Exception ex)
				{
					errorMessage = "Unknown exception loading settings for path '" + path + "' - '" + ex.Message + "'!";
				}
				return null;
			}
			/// <summary>
			/// Loads settings from database.
			/// </summary>
			/// <returns>Settings object with loaded configuration.</returns>
			/// <param name="conn">A connector connected to the desired database of which the settings are to be loaded from.</param>
			public static Settings loadFromDatabase(ref string errorMessage, Connector conn)
			{
				try
				{
					Settings settings = new Settings();
					Result result = conn.Query_Read("SELECT path, pluginid, value, description FROM cms_settings");
					foreach(ResultRow row in result)
						settings.config.Add(row["path"], new SettingsNode(row["path"], row["description"], int.Parse(row["pluginid"])));
				}
				catch(Exception ex)
				{
					errorMessage = "Unknown exception loading settings from database ;" + ex.Message + "'!";
				}
				return null;
			}
			// Methods - Properties ************************************************************************************
			public string this[string key]
			{
				get
				{
					return config[key].Value;
				}
				set
				{
					lock(this)
					{
						if(config.ContainsKey(key))
							config[key].Value = value;
						else
							config.Add(key, new SettingsNode(value, SettingsNode.SettingsNodeState.Added));
					}
				}
			}
			// Methods - Mutators **************************************************************************************
			public void updateOrAdd(int pluginid, string path, string description, string value)
			{
				if(config.ContainsKey(path))
				{
					SettingsNode n = config[path];
					n.Description = description;
					n.Value = value;
				}
				else
					config.Add(path, new SettingsNode(value, description, pluginid, SettingsNode.SettingsNodeState.Added));
			}
			// Methods - Accessors *************************************************************************************
			public bool contains(string key)
			{
				return config.ContainsKey(key);
			}
			public int getInteger(string key)
			{
				return int.Parse(config[key].Value);
			}
			public string getDefault(int pluginid, string path, string description, string defaultValue)
			{
				if(config.ContainsKey(path))
					return config[path].Value;
				else
				{
					updateOrAdd(pluginid, path, description, defaultValue);
					return defaultValue;
				}
			}
			public int getDefaultInteger(int pluginid, string path, string description, int defaultValue)
			{
				if(config.ContainsKey(path))
					return int.Parse(config[path].Value);
				else
				{
					updateOrAdd(pluginid, path, description, defaultValue.ToString());
					return defaultValue;
				}
			}
			public int getDefaultInteger(int pluginid, string path, string description, int defaultValue, int min, int max)
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
					updateOrAdd(pluginid, path, description, defaultValue.ToString());
					return defaultValue;
				}
			}
		}
	}
}

