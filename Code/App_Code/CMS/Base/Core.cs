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
using UberLib.Connector;
using UberLib.Connector.Connectors;

namespace CMS
{
	namespace Base
	{
		public static class Core
		{
			// Enums
			enum State
			{
				Failed,
				Running,
				Stopped,
				NotInstalled
			}
			enum DatabaseType
			{
				MySQL
			}
			// Fields
			private static string				basePath;							// The base path to the CMS on disk.
			private static State				currentState = State.Stopped;		// The current state of the core.
			private static DatabaseType			dbType;								// The type of database connector to create (faster than checking config value each time).
			private static Connector			connector;							// The core connector.
			private static Plugins				plugins;							// Plugin management.
			private static EmailQueue			emailQueue;							// E-mail queue sending service.
			private static Templates			templates;							// Template storage and rendering.
			private static Settings				settingsDisk;						// Disk, read-only, settings.
			private static string 				errorMessage;						// Used to store the exception message when loading the core (if one occurs).
			// Methods - starting/stopping
			public static void start()
			{
				lock(typeof(Core))
				{
					if(currentState == State.Running || currentState == State.NotInstalled)
						return;
					try
					{
						// Setup the current base-path
						basePath = AppDomain.CurrentDomain.BaseDirectory;
						if (basePath[basePath.Length - 1] == '\\' || basePath[basePath.Length - 1] == '/')
							basePath = basePath.Remove(basePath.Length - 1, 1);
						basePath.Replace("\\", "/");
						// Load the configuration file
						if((settingsDisk = Settings.loadFromDisk(ref errorMessage, basePath + "/CMS.config")) == null)
							currentState = State.Failed;
						else
						{
							// Setup connector
							switch(settingsDisk["database/provider"])
							{
							case "mysql":
								dbType = DatabaseType.MySQL;
								break;
							default:
								errorMessage = "Invalid provider specified in configuration file!";
								currentState = State.Failed;
								break;
							}
							connector = createConnector(true);
							if(connector == null)
							{
								errorMessage = "Failed to create connector to database server (connection issue)!";
								currentState = State.Failed;
							}
							else
							{
								// Setup services
							}
						}
					}
					catch(Exception ex)
					{
						currentState = State.Failed;
					}
				}
			}
			public static void stop()
			{
				lock(typeof(Core))
				{
					// Dispose
					plugins = null;
					emailQueue = null;
					templates = null;
					settingsDisk = null;
				}
			}
			public static void fail(string reason)
			{
				errorMessage = reason;
				stop();
				currentState = State.Failed;
			}
			// Methods - database
			public static Connector createConnector(bool persist)
			{
				switch(dbType)
				{
				case DatabaseType.MySQL:
					MySQL m = new MySQL();
					m.Settings_Host = settingsDisk["database/host"];
					m.Settings_Port = settingsDisk.getInteger("database/port");
					m.Settings_User = settingsDisk["database/user"];
					m.Settings_Pass = settingsDisk["database/pass"];
					m.Settings_Database = settingsDisk["database/db"];
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
			// Methods - properties
			public static string BasePath
			{
				get
				{
					return basePath;
				}
			}
		}
	}
}

