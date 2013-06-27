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
 *      File:           Default.aspx.cs
 *      Path:           /Default.aspx.cs
 * 
 *      Change-Log:
 *                      2013-06-25     Created initial class.
 * 
 * *****************************************************************************
 * The fundamental core of the CMS, used for loading any data etc when the
 * application starts.
 * *****************************************************************************
 */
using System;
using System.IO;
using UberLib.Connector;
using UberLib.Connector.Connectors;

namespace CMS
{
	namespace Base
	{
		public static class Core
		{
			// Enums ***************************************************************************************************
			public enum CoreState
			{
				Failed,
				Running,
				Stopped,
				NotInstalled
			}
			public enum DatabaseType
			{
				MySQL
			}
			// Fields - Runtime ****************************************************************************************
			private static string				basePath;							// The base path to the CMS on disk.
			private static CoreState			currentState = CoreState.Stopped;	// The current state of the core.
			private static DatabaseType			dbType;								// The type of database connector to create (faster than checking config value each time).
			private static string 				errorMessage;						// Used to store the exception message when loading the core (if one occurs).
			// Fields - Services/Connections/Data **********************************************************************
			private static Connector			connector;							// The core connector.
			private static Plugins				plugins;							// Plugin management.
			private static EmailQueue			emailQueue;							// E-mail queue sending service.
			private static Templates			templates;							// Template storage and rendering.
			private static Settings				settingsDisk;						// Disk, read-only, settings.
			private static Settings				settings;							// The main settings for the CMS, stored in the database.
			// Methods - starting/stopping *****************************************************************************
			public static void start()
			{
				lock(typeof(Core))
				{
					if(currentState == CoreState.Running || currentState == CoreState.NotInstalled)
						return;
					try
					{
						// Setup the current base-path
						basePath = AppDomain.CurrentDomain.BaseDirectory;
						if (basePath[basePath.Length - 1] == '\\' || basePath[basePath.Length - 1] == '/')
							basePath = basePath.Remove(basePath.Length - 1, 1);
						basePath.Replace("\\", "/");
						// Load the configuration file
						if(!File.Exists(basePath + "/CMS.config"))
							currentState = CoreState.NotInstalled;
						if((settingsDisk = Settings.loadFromDisk(ref errorMessage, basePath + "/CMS.config")) == null)
							currentState = CoreState.Failed;
						else
						{
							// Setup connector
							switch(settingsDisk["settings/database/provider"])
							{
							case "mysql":
								dbType = DatabaseType.MySQL;
								break;
							default:
								fail("Invalid provider specified in configuration file!");
								break;
							}
							connector = createConnector(true);
							if(connector == null)
								fail("Failed to create connector to database server (connection issue)!");
							else
							{
								// Setup services
								if((emailQueue = EmailQueue.create()) == null)
									fail("Failed to start e-mail queue service!");
								else if((templates = Templates.create()) == null)
									fail("Failed to load templates!");
								else
									currentState = CoreState.Running;
							}
						}
					}
					catch(Exception ex)
					{
						fail("Exceptiom thrown whilst loading core '" + ex.Message + "'!");
					}
				}
			}
			public static void stop()
			{
				lock(typeof(Core))
				{
					// Dispose
					connector.Disconnect();
					connector = null;
					plugins = null;
					if(emailQueue != null)
						emailQueue.stop();
					emailQueue = null;
					templates = null;
					settingsDisk = null;
					settings = null;
					// Update state
					currentState = CoreState.Stopped;
				}
			}
			public static void fail(string reason)
			{
				stop();
				errorMessage = reason;
				currentState = CoreState.Failed;
			}
			// Methods - Database **************************************************************************************
			public static Connector createConnector(bool persist)
			{
				switch(dbType)
				{
				case DatabaseType.MySQL:
					MySQL m = new MySQL();
					m.Settings_Host = settingsDisk["settings/database/host"];
					m.Settings_Port = settingsDisk.getInteger("settings/database/port");
					m.Settings_User = settingsDisk["settings/database/user"];
					m.Settings_Pass = settingsDisk["settings/database/pass"];
					m.Settings_Database = settingsDisk["settings/database/db"];
					m.Settings_Connection_String += "Charset=utf8;";
					m.Settings_Timeout_Connection = 864000; // 10 days
					m.Settings_Timeout_Command = 3600; // 1 hour
					m.Connect();
					return m;
				default:
					fail("Failed to create a connector - unknown type!");
					throw new Exception("Could not create connector, core failure!");
				}
			}
			// Methods - Properties ************************************************************************************
			public static string BasePath
			{
				get
				{
					return basePath;
				}
			}
			public static CoreState State
			{
				get
				{
					return currentState;
				}
			}
			public static string ErrorMessage
			{
				get
				{
					return errorMessage;
				}
			}
			public static Connector Connector
			{
				get
				{
					return connector;
				}
			}
			public static Settings SettingsDisk
			{
				get
				{
					return settingsDisk;
				}
			}
			public static Settings Settings
			{
				get
				{
					return settings;
				}
			}
			public static Plugins Plugins
			{
				get
				{
					return plugins;
				}
			}
			public static EmailQueue EmailQueue
			{
				get
				{
					return emailQueue;
				}
			}
			public static Templates Templates
			{
				get
				{
					return templates;
				}
			}
		}
	}
}

