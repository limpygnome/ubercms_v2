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
 *      File:           Core.cs
 *      Path:           /CMS/Base/Core.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Finished initial class.
 *                      2013-06-30      Added temporary directory creation.
 *                      2013-07-01      Added web.config path property.
 *                                      Added PathContent property.
 *                                      Modified core-start to use CmsConfigPath property.
 * 
 * *********************************************************************************************************************
 * The fundamental core of the CMS, used for loading any data etc when the application starts.
 * *********************************************************************************************************************
 */
using System;
using System.IO;
using UberLib.Connector;
using UberLib.Connector.Connectors;
using CMS.Plugins;

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
                None,
				MySQL
			}
			// Fields - Runtime ****************************************************************************************
			private static string				basePath;							// The base path to the CMS on disk.
            private static string               tempPath;                           // The path of the temporary folder, created when the core starts and deleted when the core stops.
			private static CoreState			currentState = CoreState.Stopped;	// The current state of the core.
			private static DatabaseType			dbType;								// The type of database connector to create (faster than checking config value each time).
			private static string 				errorMessage;				        // Used to store the exception message when loading the core (if one occurs).
			// Fields - Services/Connections/Data **********************************************************************
			private static Connector			connector;							// The core connector.
			private static Plugins				plugins;							// Plugin management.
			private static EmailQueue			emailQueue;							// E-mail queue sending service.
			private static Templates			templates;							// Template storage and rendering.
			private static Settings				settingsDisk;						// Disk, read-only, settings.
			private static Settings				settings;							// The main settings for the CMS, stored in the database.
			// Methods - starting/stopping *****************************************************************************
            /// <summary>
            /// Starts the core; this loads objects shared over requests.
            /// </summary>
			public static void start()
			{
				lock(typeof(Core))
				{
					if(currentState == CoreState.Running || currentState == CoreState.NotInstalled)
						return;
                    errorMessage = null;
					try
					{
						// Setup the current base-path
						basePath = AppDomain.CurrentDomain.BaseDirectory;
						if (basePath[basePath.Length - 1] == '\\' || basePath[basePath.Length - 1] == '/')
							basePath = basePath.Remove(basePath.Length - 1, 1);
                        basePath = basePath.Replace("\\", "/");
                        // Setup the temporary directory
                        tempPath = basePath + "/Temp";
                        if (!Directory.Exists(tempPath))
                        {
                            try
                            {
                                Directory.CreateDirectory(tempPath);
                            }
                            catch (Exception ex)
                            {
                                fail("Failed to create temporary folder at '" + tempPath + "'; exception occurred: '" + ex.Message + "'!");
                                return;
                            }
                        }
						// Load the configuration file
						if(!File.Exists(CmsConfigPath))
							currentState = CoreState.NotInstalled;
                        if ((settingsDisk = Settings.loadFromDisk(CmsConfigPath)) == null)
                            fail(errorMessage ?? "Failed to load disk settings!");
                        else
                        {
                            // Setup connector
                            switch (settingsDisk["settings/database/provider"].Value)
                            {
                                case "mysql":
                                    dbType = DatabaseType.MySQL;
                                    break;
                                default:
                                    fail("Invalid provider specified in configuration file!");
                                    break;
                            }
                            connector = createConnector(true);
                            if (connector == null)
                                fail("Failed to create connector to database server (connection issue)!");
                            else
                            {
                                // Setup services/data
                                if ((settings = Settings.loadFromDatabase(connector)) == null)
                                    fail(errorMessage ?? "Failed to load the settings stored in the database!");
                                else if ((emailQueue = EmailQueue.create()) == null)
                                    fail("Failed to start e-mail queue service!");
                                else if ((templates = Templates.create()) == null)
                                    fail("Failed to load templates!");
                                else if ((plugins = Plugins.load()) == null)
                                    fail(errorMessage ?? "Failed to load plugins!");
                                else
                                {
                                    currentState = CoreState.Running;
                                    // Invoke plugin handlers
                                    foreach (Plugin p in plugins.Fetch)
                                        if (p.State == Plugin.PluginState.Enabled && p.HandlerInfo.CmsStart && !p.handler_cmsStart(connector))
                                            plugins.unload(p);
                                }
                            }
                        }
					}
					catch(Exception ex)
					{
						fail("Exceptiom thrown whilst loading core '" + ex.Message + "' - stack-trace '" + ex.StackTrace + "'!");
					}
				}
			}
            /// <summary>
            /// Stops the core.
            /// </summary>
			public static void stop()
			{
				lock(typeof(Core))
				{
                    // Invoke handlers
                    if(plugins != null)
                        foreach (Plugin p in plugins.Fetch)
                            if (p.State == Plugin.PluginState.Enabled && p.HandlerInfo.CmsEnd)
                                p.handler_cmsEnd(connector);
					// Dispose
                    basePath = null;
                    dbType = DatabaseType.None;
                    errorMessage = null;
                    if(connector != null)
					    connector.Disconnect();
					connector = null;
					plugins = null;
					if(emailQueue != null)
						emailQueue.stop();
					emailQueue = null;
					templates = null;
					settingsDisk = null;
					settings = null;
                    // Dispose temporary directory
                    try
                    {
                        Directory.Delete(tempPath, true);
                    }
                    catch { }
					// Update state
					currentState = CoreState.Stopped;
				}
			}
            /// <summary>
            /// Safely stops the core with an error-message; this should be used to stop the core when a critical error
            /// has occurred.
            /// </summary>
            /// <param name="reason">The error-message/reason for failing/stopping the core.</param>
			public static void fail(string reason)
			{
				stop();
				errorMessage = reason;
				currentState = CoreState.Failed;
			}
			// Methods - Database **************************************************************************************
            /// <summary>
            /// Creates a database connection.
            /// </summary>
            /// <param name="persist">Indicates if the connection should be persistent.</param>
            /// <returns></returns>
			public static Connector createConnector(bool persist)
			{
				switch(dbType)
				{
				case DatabaseType.MySQL:
					MySQL m = new MySQL();
					m.Settings_Host = settingsDisk["settings/database/host"].Value;
					m.Settings_Port = settingsDisk.getInteger("settings/database/port");
					m.Settings_User = settingsDisk["settings/database/user"].Value;
					m.Settings_Pass = settingsDisk["settings/database/pass"].Value;
					m.Settings_Database = settingsDisk["settings/database/db"].Value;
					m.Settings_Connection_String += "Charset=utf8;";
                    if (persist)
                    {
                        m.Settings_Timeout_Connection = 864000; // 10 days
                        m.Settings_Timeout_Command = 3600; // 1 hour
                    }
					m.Connect();
					return m;
				default:
					fail("Failed to create a connector - unknown type!");
					throw new Exception("Could not create connector, core failure!");
				}
			}
			// Methods - Properties ************************************************************************************
            /// <summary>
            /// The base-base of the application. The path directories are separated with '/', with the end of the path
            /// NOT ending/tailing with '/'.
            /// </summary>
			public static string BasePath
			{
				get
				{
					return basePath;
				}
			}
            /// <summary>
            /// The path to the temporary directory, used for temporarily storing files; this directory is created when
            /// the core starts and deleted when the core stops.
            /// </summary>
            public static string TempPath
            {
                get
                {
                    return tempPath;
                }
            }
            /// <summary>
            /// The full path of the web.config file.
            /// </summary>
            public static string WebConfigPath
            {
                get
                {
                    return basePath + "/web.config";
                }
            }
            /// <summary>
            /// The physical path to the CMS's configuration file.
            /// </summary>
            public static string CmsConfigPath
            {
                get
                {
                    return basePath + "/CMS.config";
                }
            }
            /// <summary>
            /// The physical path to the content directory, used for serving media content (such as CSS files and
            /// imagery) to end-users of the CMS.
            /// </summary>
            public static string PathContent
            {
                get
                {
                    return basePath + "/Content";
                }
            }
            /// <summary>
            /// The state of the core.
            /// </summary>
			public static CoreState State
			{
				get
				{
					return currentState;
				}
			}
            /// <summary>
            /// Get/set the error message for core failure.
            /// </summary>
			public static string ErrorMessage
			{
				get
				{
					return errorMessage;
				}
                set
                {
                    errorMessage = value;
                }
			}
            /// <summary>
            /// The persistent database connection used by the core and its objects; thus no client-dependent properties
            /// should be used with this connector, since other multi-threaded objects/services may be using the
            /// connector at the same time. If you require client-dependent properties, you can create a new seperate
            /// connector using the createConnector method.
            /// </summary>
			public static Connector Connector
			{
				get
				{
					return connector;
				}
			}
            /// <summary>
            /// Core settings loaded from disk; this collection is read-only (for safety).
            /// </summary>
			public static Settings SettingsDisk
			{
				get
				{
					return settingsDisk;
				}
			}
            /// <summary>
            /// Settings loaded and stored in the database; any modifications should be followed by an invocation to
            /// the method save to persist the data to the database.
            /// </summary>
			public static Settings Settings
			{
				get
				{
					return settings;
				}
			}
            /// <summary>
            /// A model-representation of the plugins; any changes are persisted to the database.
            /// </summary>
			public static Plugins Plugins
			{
				get
				{
					return plugins;
				}
			}
            /// <summary>
            /// The e-mail queue service, used for sending e-mails. This simplifies e-mail sending and allows
            /// mass messages to be sent without the current thread haivng to wait.
            /// </summary>
			public static EmailQueue EmailQueue
			{
				get
				{
					return emailQueue;
				}
			}
            /// <summary>
            /// Templates service, used for fetching templates for rendering content.
            /// </summary>
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

