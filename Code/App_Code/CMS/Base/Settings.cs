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
using System.Xml;
using UberLib.Connector;

namespace CMS
{
	namespace Base
	{
		public class Settings
		{
			// Fields
			private Dictionary<string, string> config; // path of key, value
			// Methods - Constructors
			private Settings()
			{
				config = new Dictionary<string, string>();
			}
			public Dictionary<string, string> getAllKeys()
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
			}
			// Methods - Static
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
					settings.config.Add(path, node.InnerText);
			}
			/// <summary>
			/// Loads settings from an xml file on disk.
			/// </summary>
			/// <returns>Settings object with loaded configuration.</returns>
			/// <param name="path">Path to XML configuration file.</param>
			public static Settings loadFromDisk(ref string errorMessage, string path)
			{
				Settings settings = new Settings();
				XmlDocument xml = new XmlDocument();
				try
				{
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
				return null;
			}
			// Methods - Properties
			public string this[string key]
			{
				get
				{
					return config[key];
				}
				set
				{
					config[key] = value;
				}
			}
			public int getInteger(string key)
			{
				return int.Parse(config[key]);
			}
		}
	}
}

