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
 *      Path:           /App_Code/CMS/Base/Core.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Finished initial class.
 *                      2013-06-30      Added temporary directory creation.
 *                      2013-07-01      Added web.config path property.
 *                                      Added PathContent property.
 *                                      Modified core-start to use CmsConfigPath property.
 *                      2013-07-05      Modified content path property to be lower-case.
 *                      2013-07-06      Updated temp-path to also be lower-case.
 *                                      Added install paths properties.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                      2013-07-23      Updated the way settings are handled.
 *                      2013-08-01      Added DefaultHandler property.
 *                      2013-08-02      Improved error handling.
 *                      2013-08-05      Core connector no longer contnued; likely to cause issues.
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

namespace CMS.Base
{
	public static class Core
	{
		// Enums *******************************************************************************************************
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
		// Fields - Runtime ********************************************************************************************
		private static string				basePath        = null;					// The base path to the CMS on disk.
        private static string               tempPath        = null;                 // The path of the temporary folder, created when the core starts and deleted when the core stops.
		private static CoreState			currentState    = CoreState.Stopped;	// The current state of the core.
		private static DatabaseType			dbType			= DatabaseType.None;	// The type of database connector to create (faster than checking config value each time).
		private static string 				errorMessage    = null;				    // Used to store the exception message when loading the core (if one occurs).
        private static Version              version         = null;                 // The version of the CMS/base.
		// Fields - Services/Connections/Data **************************************************************************
		private static Plugins				plugins         = null;					// Plugin management.
		private static EmailQueue			emailQueue      = null;					// E-mail queue sending service.
		private static Templates			templates       = null;					// Template storage and rendering.
		private static Settings				settingsDisk    = null;					// Disk, read-only, settings.
		private static Settings				settings        = null;					// The main settings for the CMS, stored in the database.
		// Methods - starting/stopping *********************************************************************************
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
                    tempPath = basePath + "/temp";
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
                        // Load the version information
                        version = new Version(settingsDisk["settings/version/major"].get<int>(), settingsDisk["settings/version/minor"].get<int>(), settingsDisk["settings/version/build"].get<int>());
                        // Setup connector
                        switch (settingsDisk["settings/database/provider"].get<string>())
                        {
                            case "mysql":
                                dbType = DatabaseType.MySQL;
                                break;
                            default:
                                fail("Invalid provider specified in configuration file!");
                                break;
                        }
                        Connector conn = createConnector(false);
                        if (conn == null)
                        {
                            fail("Failed to create connector to database server (connection issue)!");
                            return;
                        }
                        else
                        {
                            // Setup services/data
                            if ((settings = Settings.loadFromDatabase(conn)) == null)
                                fail(errorMessage ?? "Failed to load the settings stored in the database!");
                            else if ((emailQueue = EmailQueue.create()) == null)
                                fail("Failed to start e-mail queue service!");
                            else if ((templates = Templates.create(conn)) == null)
                                fail("Failed to load templates!");
                            else if ((plugins = Plugins.load(conn)) == null)
                                fail(errorMessage ?? "Failed to load plugins!");
                            else
                            {
                                currentState = CoreState.Running;
                                // Start any services
                                emailQueue.start();
                                // Invoke plugin handlers
                                foreach (Plugin p in plugins.Fetch)
                                    if (p.State == Plugin.PluginState.Enabled && p.HandlerInfo.PluginStart && !p.handler_pluginStart(conn))
                                        plugins.pluginUnload(p);
                            }
                            // Dispose connector
                            conn.disconnect();
                        }
                    }
				}
				catch(Exception ex)
				{
					fail("Exceptiom thrown whilst loading core '" + ex.Message + "' - stack-trace '" + ex.StackTrace + "'! Base exception: '" + ex.GetBaseException().Message + "'; base stack-trace: '" + ex.GetBaseException().StackTrace + "'.");
				}
			}
		}
        /// <summary>
        /// Stops the core.
        /// </summary>
        public static void stop()
        {
            stop(false);
        }
		private static void stop(bool failed)
		{
			lock(typeof(Core))
			{
                // Setup connector
                Connector conn = createConnector(false);
                // Invoke handlers
                if (plugins != null)
                {
                    foreach (Plugin p in plugins.Fetch)
                        if (p.State == Plugin.PluginState.Enabled && p.HandlerInfo.PluginStop)
                            p.handler_pluginStop(conn);
                    plugins = null;
                }
                // Dispose connector
                conn.disconnect();
                conn = null;
				// Dispose services
				if(emailQueue != null)
					emailQueue.stop();
                // Dispose core
                basePath = null;
                dbType = DatabaseType.None;
				emailQueue = null;
				templates = null;
				settingsDisk = null;
				settings = null;
                version = null;
                // Dispose temporary directory
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch { }
				// Update state
                if (!failed)
                {
                    currentState = CoreState.Stopped;
                    errorMessage = null;
                }
			}
		}
        /// <summary>
        /// Safely stops the core with an error-message; this should be used to stop the core when a critical error
        /// has occurred.
        /// </summary>
        /// <param name="reason">The error-message/reason for failing/stopping the core.</param>
		public static void fail(string reason)
		{
            lock (typeof(Core))
            {
                errorMessage = errorMessage != null ? errorMessage + "\n" + reason : reason;
                currentState = CoreState.Failed;
                stop(true);
            }
		}
		// Methods - Database ******************************************************************************************
        /// <summary>
        /// Creates a database connection.
        /// </summary>
        /// <param name="persist">Indicates if the connection should be persistent.</param>
        /// <returns>Database connector.</returns>
		public static Connector createConnector(bool persist)
		{
			switch(dbType)
			{
			case DatabaseType.MySQL:
                MySQL m = new MySQL();
                m.SettingsHost = DatabaseHost;
                m.SettingsPort = DatabasePort;
                m.SettingsUser = DatabaseUser;
                m.SettingsPass = DatabasePass;
                m.SettingsDatabase = DatabaseSchema;
                m.SettingsConnectionString += DatabaseConnectionString;
                if (persist)
                    m.SettingsTimeoutConnection = 864000; // 10 days
                m.connect();
                return m;
			default:
				fail("Failed to create a connector - unknown type!");
				throw new Exception("Could not create connector, core failure!");
			}
		}
        // Methods - Properties - Database *****************************************************************************
        /// <summary>
        /// The host of the DBMS.
        /// </summary>
        public static string DatabaseHost
        {
            get
            {
                return settingsDisk["settings/database/host"].get<string>();
            }
        }
        /// <summary>
        /// The port of the DBMS.
        /// </summary>
        public static int DatabasePort
        {
            get
            {
                return settingsDisk["settings/database/port"].get<int>();
            }
        }
        /// <summary>
        /// The username for authentication of the DBMS.
        /// </summary>
        public static string DatabaseUser
        {
            get
            {
                return settingsDisk["settings/database/user"].get<string>();
            }
        }
        /// <summary>
        /// The password for authentication of the DBMS.
        /// </summary>
        public static string DatabasePass
        {
            get
            {
                return settingsDisk["settings/database/pass"].get<string>();
            }
        }
        /// <summary>
        /// The databse/schema on the DBMS to use.
        /// </summary>
        public static string DatabaseSchema
        {
            get
            {
                return settingsDisk["settings/database/db"].get<string>();
            }
        }
        /// <summary>
        /// The database connection string, for any additional parameters. This property is used if the DBMS is actually
        /// a file; in such a situation, this property should contain the file-path.
        /// </summary>
        public static string DatabaseConnectionString
        {
            get
            {
                return settingsDisk["settings/database/connection_string"].get<string>();
            }
        }
		// Methods - Properties ****************************************************************************************
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
                return basePath + "/content";
            }
        }
        public static string PathInstaller
        {
            get
            {
                return basePath + "/installer";
            }
        }
        public static string PathInstaller_Content
        {
            get
            {
                return basePath + "/installer/content";
            }
        }
        public static string PathInstaller_Templates
        {
            get
            {
                return basePath + "/installer/templates";
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
        /// <summary>
        /// The default handler when no URL has been provided.
        /// </summary>
        public static string DefaultHandler
        {
            get
            {
                return Core.SettingsDisk["settings/core/default_handler"].get<string>();
            }
        }
        /// <summary>
        /// The title of the CMS/website/community.
        /// </summary>
        public static string Title
        {
            get
            {
                return Core.Settings["core/title"].get<string>();
            }
        }
        /// <summary>
        /// The version of the CMS/base.
        /// </summary>
        public static Version Version
        {
            get
            {
                return version;
            }
        }
	}
}