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
 *      Path:           /App_Code/CMS/Base/Settings.cs
 * 
 *      Change-Log:
 *                      2013-06-27      Created initial class.
 *                      2013-06-29      Finished initial class.
 *                      2013-06-06      Added ability to just add keys, without update.
 *                                      Added ability to remove settings by plugin, pluginid and path.
 *                      2013-06-07      Fixed critical bug in save method, where new nodes could not be saved.
 *                                      Fixed critical bug in load from database method.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Added changes to support pluginid to UUID for plugins.
 *                      2013-07-23      add and updateOrAdd methods take a plugin model instead of a UUID, in-case of
 *                                      future changes to identifying plugins.
 *                                      Major change: settings now have types for efficiency and easier editing;
 *                                      add and updateOrAdd have been replaced by set methods. Removal of get<...>
 *                                      methods.
 *                      2013-07-31      Bug-fix (upadting nodes which exist and not parsing the value to the internal
 *                                      type) and improvements.
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

namespace CMS.Base
{
    /// <summary>
    /// Handles settings which can be stored on disk or in a database. Thread-safe.
    /// </summary>
	public class Settings
	{
        // Enums *******************************************************************************************************
        public enum SetAction
        {
            Add,
            Update,
            AddOrUpdate
        }
		// Fields ******************************************************************************************************
		private Dictionary<string, SettingsNode> config;    // Path of setting, setting node
		// Methods - Constructors **************************************************************************************
		private Settings()
		{
			config = new Dictionary<string, SettingsNode>();
		}
		// Methods *****************************************************************************************************
		/// <summary>
		/// Saves the settings to the database of the connector.
		/// </summary>
		/// <param name="conn">Conn.</param>
		public void save(Connector conn)
		{
			lock(this)
			{
				StringBuilder sqlUpdated = new StringBuilder();
				StringBuilder sqlInserted = new StringBuilder("INSERT INTO cms_settings (path, uuid, description, type, value) VALUES");
				int sqlInsertedDefaultLength = sqlInserted.Length;
				// Iterate keys
                foreach (KeyValuePair<string, SettingsNode> k in config)
                {
                    switch (k.Value.State)
                    {
                        case SettingsNode.SettingsNodeState.ModifiedAll:
                            sqlUpdated.Append("UPDATE cms_settings SET value='").Append(SQLUtils.escape(k.Value.ToString())).Append("', type='").Append(SQLUtils.escape(k.Value.ToString())).Append("', description='").Append(SQLUtils.escape(k.Value.Description)).Append("' WHERE path='").Append(SQLUtils.escape(k.Key)).Append("'; ");
                            break;
                        case SettingsNode.SettingsNodeState.ModifiedDescription:
                            sqlUpdated.Append("UPDATE cms_settings SET description='").Append(SQLUtils.escape(k.Value.Description)).Append("' WHERE path='").Append(SQLUtils.escape(k.Key)).Append("'; ");
                            break;
                        case SettingsNode.SettingsNodeState.ModifiedValue:
                            sqlUpdated.Append("UPDATE cms_settings SET value='").Append(SQLUtils.escape(k.Value.ToString())).Append("', type='").Append(SQLUtils.escape(((int)k.Value.ValueDataType).ToString())).Append("' WHERE path='").Append(SQLUtils.escape(k.Key)).Append("'; ");
                            break;
                        case SettingsNode.SettingsNodeState.Added:
                            sqlInserted.Append("('").Append(SQLUtils.escape(k.Key)).Append("', ").Append(k.Value.OwnerUUID.NumericHexString).Append(", ").Append((k.Value.Description == null ? "NULL" : "'" + k.Value.Description + "'")).Append(", '").Append(SQLUtils.escape(((int)k.Value.ValueDataType).ToString())).Append("', ").Append((k.Value.Value == null ? "NULL" : "'" + k.Value.Value + "'")).Append("),");
                            break;
                    }
                }
				// Push to the database
				if(sqlUpdated.Length > 0)
					conn.queryExecute(sqlUpdated.ToString());
				if(sqlInserted.Length > sqlInsertedDefaultLength)
				{
					sqlInserted.Remove(sqlInserted.Length - 1, 1).Append(';'); // Remove tailing comma, append seperator
					conn.queryExecute(sqlInserted.ToString());
				}
			}
		}
		// Methods - Static ********************************************************************************************
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
            if (!nodes)
                settings.config.Add(path, new SettingsNode(SettingsNode.parseType(node.Attributes["type"].Value), node.InnerText));
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
                Result result = conn.queryRead("SELECT * FROM cms_view_settings_load;");
				foreach(ResultRow row in result)
					settings.config.Add(row.get2<string>("path"), new SettingsNode(SettingsNode.parseType(row.get2<string>("type")), row.get2<string>("value"), row.get2<string>("description"), UUID.createFromHex(row.get2<string>("uuid"))));
                return settings;
			}
			catch(Exception ex)
			{
                Core.ErrorMessage = "Unknown exception loading settings from database: " + ex.Message + "' ~ '" + ex.GetBaseException().Message + "' - '" + ex.GetBaseException().StackTrace + "'!";
			}
			return null;
		}
		// Methods - Mutators ******************************************************************************************
        /// <summary>
        /// Sets a string setting.
        /// 
        /// Note: the method save must be called to persist the data.
        /// </summary>
        /// <param name="plugin">The owner of the setting; can be null.</param>
        /// <param name="action">The action of the setting being set.</param>
        /// <param name="path">The path of the setting.</param>
        /// <param name="description">A description of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        /// <returns>Success of operation.</returns>
        public bool set(Plugin plugin, SetAction action, string path, string description, string value)
        {
            return set(plugin, action, path, description, value, SettingsNode.DataType.String);
        }
        /// <summary>
        /// Sets an integer setting.
        /// 
        /// Note: the method save must be called to persist the data.
        /// </summary>
        /// <param name="plugin">The owner of the setting; can be null.</param>
        /// <param name="action">The action of the setting being set.</param>
        /// <param name="path">The path of the setting.</param>
        /// <param name="description">A description of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        /// <returns>Success of operation.</returns>
        public bool setInt(Plugin plugin, SetAction action, string path, string description, int value)
        {
            return set(plugin, action, path, description, value.ToString(), SettingsNode.DataType.Integer);
        }
        /// <summary>
        /// Sets a float setting.
        /// 
        /// Note: the method save must be called to persist the data.
        /// </summary>
        /// <param name="plugin">The owner of the setting; can be null.</param>
        /// <param name="action">The action of the setting being set.</param>
        /// <param name="path">The path of the setting.</param>
        /// <param name="description">A description of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        /// <returns>Success of operation.</returns>
        public bool setFloat(Plugin plugin, SetAction action, string path, string description, float value)
        {
            return set(plugin, action, path, description, value.ToString(), SettingsNode.DataType.Float);
        }
        /// <summary>
        /// Sets a double setting.
        /// 
        /// Note: the method save must be called to persist the data.
        /// </summary>
        /// <param name="plugin">The owner of the setting; can be null.</param>
        /// <param name="action">The action of the setting being set.</param>
        /// <param name="path">The path of the setting.</param>
        /// <param name="description">A description of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        /// <returns>Success of operation.</returns>
        public bool setDouble(Plugin plugin, SetAction action, string path, string description, double value)
        {
            return set(plugin, action, path, description, value.ToString(), SettingsNode.DataType.Double);
        }
        /// <summary>
        /// Sets a boolean setting.
        /// 
        /// Note: the method save must be called to persist the data.
        /// </summary>
        /// <param name="plugin">The owner of the setting; can be null.</param>
        /// <param name="action">The action of the setting being set.</param>
        /// <param name="path">The path of the setting.</param>
        /// <param name="description">A description of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        /// <returns>Success of operation.</returns>
        public bool setBool(Plugin plugin, SetAction action, string path, string description, bool value)
        {
            return set(plugin, action, path, description, value ? "1" : "0", SettingsNode.DataType.Bool);
        }
        private bool set(Plugin plugin, SetAction action, string path, string description, string value, SettingsNode.DataType type)
        {
            lock(this)
            {
                bool exists = config.ContainsKey(path);
                // Check if the key is to be added and exists or doesn't exist and to be updated
                if((action == SetAction.Add && exists) || (action == SetAction.Update && !exists))
                    return false;
                // Either add or update
                if(exists)
                {
                    if(description != null)
                        this[path].Description = description;
                    if(value != null)
                    {
                        this[path].ValueDataType = type;
                        this[path].setValue(value);
                    }
                }
                else
                    config.Add(path, new SettingsNode(type, value, description, plugin != null ? plugin.UUID : null, SettingsNode.SettingsNodeState.Added));
                return true;
            }
        }
        /// <summary>
        /// Removes all of the settings owned by a plugin.
        /// </summary>
        /// <param name="plugin">The owner of the settings to be removed.</param>
        public void remove(Plugin plugin)
        {
            remove(plugin.UUID);
        }
        /// <summary>
        /// Removes all of the settings owned by a plugin.
        /// </summary>
        /// <param name="uuid">The identifier of the plugin which owns the setting(s) to be removed.</param>
        public void remove(UUID uuid)
        {
            lock (this)
            {
                // Delete settings from the database
                Core.Connector.queryExecute("DELETE FROM cms_settings WHERE uuid=" + uuid.NumericHexString + ";");
                // Find keys to remove
                List<string> temp = new List<string>();
                foreach (KeyValuePair<string, SettingsNode> kv in config)
                {
                    if (kv.Value.OwnerUUID == uuid)
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
                    Core.Connector.queryExecute("DELETE FROM cms_settings WHERE path='" + SQLUtils.escape(path) + "';");
                    config.Remove(path);
                }
            }
        }
		// Methods - Accessors *****************************************************************************************
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
        /// A debug function for outputting the settings. Lines are broken by HTML tag 'br'.
        /// </summary>
        /// <returns></returns>
        public string getDebug()
        {
            StringBuilder debug = new StringBuilder();
            foreach (KeyValuePair<string, SettingsNode> node in config)
                debug.Append("'" + node.Key + "'='" + node.Value.ToString() + "' (plugin UUID: '" + node.Value.OwnerUUID.HexHyphens + "')<br />");
            return debug.ToString();
        }
        // Methods - Properties ****************************************************************************************
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

