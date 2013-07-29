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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/BasicSiteAuth.cs
 * 
 *      Change-Log:
 *                      2013-07-06      Created initial class.
 * 
 * *********************************************************************************************************************
 * A basic authentication plugin, featuring user-groups and banning.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using CMS.Base;
using CMS.Plugins;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A basic authentication plugin, featuring user-groups and banning.
    /// </summary>
    public class BasicSiteAuth : Plugin
    {
        // Constants ***************************************************************************************************
        public const string BSA_UUID = "943c3f9d-dfcb-483d-bf34-48ad5231a15f";
        public const int BSA_UNIQUE_USER_HASH_MIN = 10;
        public const int BSA_UNIQUE_USER_HASH_MAX = 16;
        // Constants - Settings ****************************************************************************************
        // -- User Restrictions ****************************************************************************************
        public const string SETTINGS_USERNAME_MIN = "bsa/account/username_min";
        public const string SETTINGS_USERNAME_MIN__DESCRIPTION = "The minimum number of characters a username can be.";
        public const int SETTINGS_USERNAME_MIN__DEFAULT = 2;

        public const string SETTINGS_USERNAME_MAX = "bsa/account/username_max";
        public const string SETTINGS_USERNAME_MAX__DESCRIPTION = "The maximum number of characters a username can be.";
        public const int SETTINGS_USERNAME_MAX__DEFAULT = 32;

        public const string SETTINGS_PASSWORD_MIN = "bsa/account/password_min";
        public const string SETTINGS_PASSWORD_MIN__DESCRIPTION = "The minimum number of characters a password can be.";
        public const int SETTINGS_PASSWORD_MIN__DEFAULT = 6;

        public const string SETTINGS_PASSWORD_MAX = "bsa/account/password_max";
        public const string SETTINGS_PASSWORD_MAX__DESCRIPTION = "The maximum number of characters a password can be.";
        public const int SETTINGS_PASSWORD_MAX__DEFAULT = 128;

        public const string SETTINGS_EMAIL_MIN = "bsa/account/email_min";
        public const string SETTINGS_EMAIL_MIN__DESCRIPTION = "The minimum number of characters an e-mail can be.";
        public const int SETTINGS_EMAIL_MIN__DEFAULT = 6;

        public const string SETTINGS_EMAIL_MAX = "bsa/account/email_max";
        public const string SETTINGS_EMAIL_MAX_DESCRIPTION = "The maximum number of characters an e-mail can be.";
        public const int SETTINGS_EMAIL_MAX__DEFAULT = 64;

        public const string SETTINGS_SECRETQUESTION_MIN = "bsa/account/secretquestion_min";
        public const string SETTINGS_SECRETQUESTION_MIN__DESCRIPTION = "The minimum characters a secret question can be.";
        public const int SETTINGS_SECRETQUESTION_MIN__DEFAULT = 0;

        public const string SETTINGS_SECRETQUESTION_MAX = "bsa/account/secretquestion_max";
        public const string SETTINGS_SECRETQUESTION_MAX__DESCRIPTION = "The maximum characters a secret question can be.";
        public const int SETTINGS_SECRETQUESTION_MAX__DEFAULT = 64;

        public const string SETTINGS_SECRETANSWER_MIN = "bsa/account/secretanswer_min";
        public const string SETTINGS_SECRETANSWER_MIN__DESCRIPTION = "The minimum characters a secret answer can be.";
        public const int SETTINGS_SECRETANSWER_MIN__DEFAULT = 64;

        public const string SETTINGS_SECRETANSWER_MAX = "bsa/account/secretanswer_max";
        public const string SETTINGS_SECRETANSWER_MAX__DESCRIPTION = "The maximum characters a secret answer can be.";
        public const int SETTINGS_SECRETANSWER_MAX__DEFAULT = 64;

        public const string SETTINGS_EMAIL_VERIFICATION = "bsa/account/email_verification";
        public const string SETTINGS_EMAIL_VERIFICATION__DESCRIPTION = "Specifies if users need to verify their accounts via e-mail.";
        public const bool SETTINGS_EMAIL_VERIFICATION__DEFAULT = true;
        // -- User Groups **********************************************************************************************
        public const string SETTINGS_GROUP_UNVERIFIED_GROUPID = "bsa/groups/unverified_groupid";
        public const string SETTINGS_GROUP_UNVERIFIED_GROUPID__DESCRIPTION = "The identifier of the unverified group.";

        public const string SETTINGS_GROUP_USER_GROUPID = "bsa/groups/user_groupid";
        public const string SETTINGS_GROUP_USER_GROUPID__DESCRIPTION = "The identifier of the user group.";

        public const string SETTINGS_GROUP_MODERATOR_GROUPID = "bsa/groups/moderator_groupid";
        public const string SETTINGS_GROUP_MODERATOR_GROUPID__DESCRIPTION = "The identifier of the moderator group.";

        public const string SETTINGS_GROUP_ADMINISTRATOR_GROUPID = "bsa/groups/administrator_groupid";
        public const string SETTINGS_GROUP_ADMINISTRATOR_GROUPID__DESCRIPTION = "The identifier of the administrator group.";
        // -- Authentication Failed Attempts ***************************************************************************
        public const string SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD = "bsa/authfailedattempts/ban_period";
        public const string SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD__DESCRIPTION = "The duration an IP is banned from being able to login after reaching the failed attempts threshold.";
        public const int SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD__DEFAULT = 3600000;

        public const string SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD = "bsa/authfailedattempts/threshold";
        public const string SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD__DESCRIPTION = "The threshold/maximum number of failed attempts allowed; once exceeded, an IP is banned for a period.";
        public const int SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD__DEFAULT = 5;
        // -- Recovery Codes *******************************************************************************************
        public const string SETTINGS_RECOVERYCODES_EXPIRE = "bsa/recoverycodes/expire";
        public const string SETTINGS_RECOVERYCODES_EXPIRE__DESCRIPTION = "The life-span of a recovery code in milliseconds.";
        public const int SETTINGS_RECOVERYCODES_EXPIRE__DEFAULT = 3600000;
        // Fields ******************************************************************************************************
        private string                          salt1,                  // The first salt, used for generating a secure SHA-512 hash.
                                                salt2;                  // The second salt, used for generating a secure SHA-512 hash.
        private UserGroups                      groups;                 // A collection of all the user-groups.
        private AccountEventTypes               accountEventTypes;      // A collection of all the account event types.
        // Methods - Constructors **************************************************************************************
        public BasicSiteAuth(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo)
            : base(uuid, title, directory, state, handlerInfo)
        {
            groups = null;
        }
        // Methods - Handlers ******************************************************************************************
        public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Setup handlers
            HandlerInfo.RequestStart = true;
            HandlerInfo.CmsStart = true;
            HandlerInfo.CycleInterval = 3600000; // Every hour
            HandlerInfo.save(conn);
            // Install SQL
            if (!BaseUtils.executeSQL(FullPath + "/sql/install.sql", conn, ref messageOutput))
                return false;
            // Create settings
            // -- User restrictions
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_USERNAME_MIN, SETTINGS_USERNAME_MIN__DESCRIPTION, SETTINGS_USERNAME_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_USERNAME_MAX, SETTINGS_USERNAME_MAX__DESCRIPTION, SETTINGS_USERNAME_MAX__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_PASSWORD_MIN, SETTINGS_PASSWORD_MIN__DESCRIPTION, SETTINGS_PASSWORD_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_PASSWORD_MAX, SETTINGS_PASSWORD_MAX__DESCRIPTION, SETTINGS_PASSWORD_MAX__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_EMAIL_MIN, SETTINGS_EMAIL_MIN__DESCRIPTION, SETTINGS_EMAIL_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_EMAIL_MAX, SETTINGS_EMAIL_MAX_DESCRIPTION, SETTINGS_EMAIL_MAX__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_SECRETQUESTION_MIN, SETTINGS_SECRETQUESTION_MIN__DESCRIPTION, SETTINGS_SECRETQUESTION_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_SECRETQUESTION_MAX, SETTINGS_SECRETQUESTION_MAX__DESCRIPTION, SETTINGS_SECRETQUESTION_MAX__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_SECRETANSWER_MIN, SETTINGS_SECRETANSWER_MIN__DESCRIPTION, SETTINGS_SECRETANSWER_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_SECRETANSWER_MAX, SETTINGS_SECRETANSWER_MAX__DESCRIPTION, SETTINGS_SECRETANSWER_MAX__DEFAULT);
            Core.Settings.setBool(this, Settings.SetAction.AddOrUpdate, SETTINGS_EMAIL_VERIFICATION, SETTINGS_EMAIL_VERIFICATION__DESCRIPTION, SETTINGS_EMAIL_VERIFICATION__DEFAULT);
            // -- -- Authentication failed attempts
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD, SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD__DESCRIPTION, SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD, SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD__DESCRIPTION, SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD__DEFAULT);
            // -- -- Recovery codes
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_RECOVERYCODES_EXPIRE, SETTINGS_RECOVERYCODES_EXPIRE__DESCRIPTION, SETTINGS_RECOVERYCODES_EXPIRE__DEFAULT);
            // Create default user-groups
            // -- Unverified
            UserGroup ugUnverified = new UserGroup();
            ugUnverified.Title = "Unverified";
            ugUnverified.Description = "An unverified user unable to perform any actions.";
            ugUnverified.save(this, conn);
            // -- User (verified)
            UserGroup ugUser = new UserGroup();
            ugUser.Title = "User";
            ugUser.Description = "A verified user able to create, modify and delete their own content.";
            ugUser.Comments_Create = true;
            ugUser.Comments_DeleteOwn = true;
            ugUser.Comments_ModifyOwn = true;
            ugUser.Comments_Publish = true;
            ugUser.Login = true;
            ugUser.Media_Create = true;
            ugUser.Media_DeleteOwn = true;
            ugUser.Media_ModifyOwn = true;
            ugUser.Pages_Create = true;
            ugUser.Pages_ModifyOwn = true;
            ugUser.Pages_Modify = true;
            ugUser.save(this, conn);
            // -- Moderator
            UserGroup ugModerator = new UserGroup();
            ugModerator.Title = "Moderator";
            ugModerator.Description = "A user able to create, modify and delete all content.";
            ugModerator.Administrator = true;
            ugModerator.Comments_Create = true;
            ugModerator.Comments_Delete = true;
            ugModerator.Comments_DeleteOwn = true;
            ugModerator.Comments_ModifyOwn = true;
            ugModerator.Comments_Publish = true;
            ugModerator.Login = true;
            ugModerator.Media_Create = true;
            ugModerator.Media_Delete = true;
            ugModerator.Media_DeleteOwn = true;
            ugModerator.Media_Modify = true;
            ugModerator.Media_ModifyOwn = true;
            ugModerator.Moderator = true;
            ugModerator.Pages_Create = true;
            ugModerator.Pages_Delete = true;
            ugModerator.Pages_DeleteOwn = true;
            ugModerator.Pages_Modify = true;
            ugModerator.Pages_ModifyOwn = true;
            ugModerator.Pages_Publish = true;
            // -- Administrator
            UserGroup ugAdministrator = new UserGroup();
            ugAdministrator.Title = "Administrator";
            ugAdministrator.Description = "A user with complete control of the system and content.";
            ugAdministrator.Administrator = true;
            ugAdministrator.Comments_Create = true;
            ugAdministrator.Comments_Delete = true;
            ugAdministrator.Comments_DeleteOwn = true;
            ugAdministrator.Comments_ModifyOwn = true;
            ugAdministrator.Comments_Publish = true;
            ugAdministrator.Login = true;
            ugAdministrator.Media_Create = true;
            ugAdministrator.Media_Delete = true;
            ugAdministrator.Media_DeleteOwn = true;
            ugAdministrator.Media_Modify = true;
            ugAdministrator.Media_ModifyOwn = true;
            ugAdministrator.Moderator = true;
            ugAdministrator.Pages_Create = true;
            ugAdministrator.Pages_Delete = true;
            ugAdministrator.Pages_DeleteOwn = true;
            ugAdministrator.Pages_Modify = true;
            ugAdministrator.Pages_ModifyOwn = true;
            ugAdministrator.Pages_Publish = true;
            ugAdministrator.save(this, conn);
            // Save group ID's
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_UNVERIFIED_GROUPID, SETTINGS_GROUP_UNVERIFIED_GROUPID__DESCRIPTION, ugUnverified.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_USER_GROUPID, SETTINGS_GROUP_USER_GROUPID__DESCRIPTION, ugUser.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_MODERATOR_GROUPID, SETTINGS_GROUP_MODERATOR_GROUPID__DESCRIPTION, ugModerator.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_ADMINISTRATOR_GROUPID, SETTINGS_GROUP_ADMINISTRATOR_GROUPID__DESCRIPTION, ugAdministrator.GroupID);
            // Create default root account (user = root, pass = password)
            User userRoot = new User();
            userRoot.Username = "root";
            userRoot.setPassword(this, "password");
            userRoot.Email = "admin@localhost";
            userRoot.SecretQuestion = string.Empty;
            userRoot.SecretAnswer = string.Empty;
            userRoot.UserGroup = ugAdministrator;
            if(userRoot.save(this, conn) != User.UserCreateSaveStatus.Success)
                messageOutput.AppendLine("Warning: failed to create root user!");
            return true;
        }
        public override bool uninstall(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Check if the user table exists; if so, abort and inform the user to manually remove it
            if (conn.queryCount("SELECT COUNT('') FROM information_schema.tables WHERE table_schema='" + SQLUtils.escape(Core.DatabaseSchema) + "' AND table_name='bsa_users';") > 0)
            {
                messageOutput.AppendLine("Basic site authentication cannot be uninstalled until you remove the users table (bsa_users) from the database! This is protection against accidental uninstallation of the user data.");
                return false;
            }
            // Remove SQL
            if (!BaseUtils.executeSQL(FullPath + "/sql/uninstall.sql", conn, ref messageOutput))
                return false;
            // Remove settings
            Core.Settings.remove(this);
            return true;
        }
        public override bool enable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Reserve URLs
            if (!BaseUtils.urlRewritingInstall(this, new string[]
            {
                "login",
                "register",
                "account_recovery",
                "my_account",
                "account_log"
            }, ref messageOutput))
                return false;
            // Add directives
            if (!BaseUtils.preprocessorDirective_Add("bsa", ref messageOutput))
                return false;
            // Install templates
            if (!Core.Templates.install(conn, this, PathTemplates, ref messageOutput))
                return false;
            // Install content
            if (!BaseUtils.contentInstall(PathContent, Core.PathContent, true, ref messageOutput))
                return false;
            return true;
        }
        public override bool disable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Unreserve URLs
            if (!BaseUtils.urlRewritingUninstall(this, ref messageOutput))
                return false;
            // Remove directives
            if (!BaseUtils.preprocessorDirective_Remove("bsa", ref messageOutput))
                return false;
            // Remove templates
            if (!Core.Templates.uninstall(this, ref messageOutput))
                return false;
            // Remove content
            if (!BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput))
                return false;
            return true;
        }
        public override bool handler_cmsStart(UberLib.Connector.Connector conn)
        {
            loadSalts();
            groups = UserGroups.load(conn);
            accountEventTypes = AccountEventTypes.load(conn);
            return true;
        }
        public override void handler_cmsCycle()
        {
            // Clean old recovery codes

            // Delete old failed authentication attempts

        }
        public override void handler_requestStart(Data data)
        {
            // Fetch the user for the current request
            User user = getCurrentUser(data);
            // Check if the user is banned/unable to login

            // Set the user's information
            if (user != null)
            {
                // Render elements
                data["Username"] = user.Username;
            }
        }
        public override bool handler_handleRequest(Data data)
        {
            if(HttpContext.Current.User.Identity.IsAuthenticated)
                switch (data.PathInfo.ModuleHandler)
                {
                    case "my_account":
                        return pageMyAccount(data);
                    case "account_log":
                        return pageAccountLog(data);
                    case "members":
                        return pageMembers(data);
                    default:
                        return false;
                }
            else
                switch (data.PathInfo.ModuleHandler)
                {
                    case "login":
                        return pageLogin(data);
                    case "register":
                        return pageRegister(data);
                    case "account_recovery":
                        return pageAccountRecovery(data);
                    case "members":
                        return pageMembers(data);
                    default:
                        return false;
                }
        }
        // Methods - Pages *********************************************************************************************
        private bool pageLogin(Data data)
        {
            // Check for postback
            string username = data.Request.Form["username"];
            string password = data.Request.Form["password"];
            string captcha = data.Request.Form["captcha"];
            bool keepLoggedIn = data.Request.Form["session_persist"] != null;

            // Set form data
            data["bsa_form_username"] = username;
            // Set content
            data["Title"] = "Login";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/login");
            return true;
        }
        private bool pageRegister(Data data)
        {
            // Check for postback
            string username = data.Request.Form["username"];
            string password = data.Request.Form["password"];
            // Set form data

            // Set content
            return true;
        }
        private bool pageAccountRecovery(Data data)
        {
            switch(data.PathInfo[2])
            {
                case null:
                    return pageAccountRecovery_Home(data);
                case "sqa":
                    return pageAccountRecovery_SQA(data);
                case "email":
                    return pageAccountRecovery_Email(data);
                default:
                    return false;
            }
        }
        private bool pageAccountRecovery_Home(Data data)
        {
            return true;
        }
        private bool pageAccountRecovery_SQA(Data data)
        {
            return true;
        }
        private bool pageAccountRecovery_Email(Data data)
        {
            return true;
        }
        private bool pageMyAccount(Data data)
        {
            return true;
        }
        private bool pageAccountLog(Data data)
        {
            return true;
        }
        private bool pageMembers(Data data)
        {
            return true;
        }
        // Methods - Static ********************************************************************************************
        private const string bsaCurrentUserKey = "bsa_current_user";
        /// <summary>
        /// Gets the user object for the current request; returns null if the user is anonymous. This can be invoked
        /// at any stage; if the user for the current request has not been loaded, it will be loaded.
        /// </summary>
        /// <returns>The user model which represents the user of the current request.</returns>
        public static User getCurrentUser(Data data)
        {
            if (!HttpContext.Current.Items.Contains(bsaCurrentUserKey))
                loadCurrentUser(data);
            return (User)HttpContext.Current.Items[bsaCurrentUserKey];
        }
        private static void loadCurrentUser(Data data)
        {
            // Check the user object has not already been loaded
            if (HttpContext.Current.Items[bsaCurrentUserKey] != null)
                return;
            // Check if the user is authenticated
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                HttpContext.Current.Items[bsaCurrentUserKey] = null;
            else
            {
                // Fetch the BSA plugin
                BasicSiteAuth bsa = (BasicSiteAuth)Core.Plugins.getPlugin(UUID.createFromHexHyphens(BasicSiteAuth.BSA_UUID));
                // Load and set user object
                User usr = User.load(bsa, data.Connector, int.Parse(HttpContext.Current.User.Identity.Name));
                if(usr.UserGroup.Login)
                    HttpContext.Current.Items[bsaCurrentUserKey] = usr;
                else
                    HttpContext.Current.Items[bsaCurrentUserKey] = null;
            }
        }
        // Methods *****************************************************************************************************
        private void setPageError(Data data, string error)
        {
            data["BsaError"] = error;
        }
        private void loadSalts()
        {
            string salts = FullPath + "/salts.xml";
            // Check if existing salts exist, if so...we'll load them
            if (File.Exists(salts))
            {
                // Salts exist - load them
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(salts));
                salt1 = doc["salts"]["salt1"].InnerText;
                salt2 = doc["salts"]["salt2"].InnerText;
            }
            else
            {
                // Salts do not exist - create them - their length is also variable for even more security!
                // -- If the hashes were compromised, the attacker would need the salts to even perform a
                // -- brute-force or dictionary attack. Since these are stored physically, it's even less likely
                // -- they could be compromised. Brute-forcing a known hash would also require permutating between
                // -- two 18 to 26 length salts. Thus this would make hashes very secure.
                Random rand = new Random((int)DateTime.Now.ToBinary());
                salt1 = BaseUtils.generateRandomString(rand.Next(18, 26));
                salt2 = BaseUtils.generateRandomString(rand.Next(18, 26));
                StringBuilder saltsConfig = new StringBuilder();
                XmlWriter writer = XmlWriter.Create(saltsConfig);
                writer.WriteStartDocument();
                writer.WriteStartElement("salts");

                writer.WriteStartElement("salt1");
                writer.WriteCData(salt1);
                writer.WriteEndElement();

                writer.WriteStartElement("salt2");
                writer.WriteCData(salt2);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();

                File.WriteAllText(salts, saltsConfig.ToString());
            }
        }
        /// <summary>
        /// Generates a hash for a piece of string data, primarily intended for usage with user passwords.
        /// </summary>
        /// <param name="data">The string to be hashed.</param>
        /// <param name="uniqueSalt">A salt unique for this hash; this must have a length of at least two!</param>
        /// <returns></returns>
        public string generateHash(string data, string uniqueSalt)
        {
            int usLenHalf = uniqueSalt.Length / 2;
            byte[] rawData = Encoding.UTF8.GetBytes(uniqueSalt.Substring(0, usLenHalf) + data + uniqueSalt.Substring(usLenHalf));
            byte[] rawSalt1 = Encoding.UTF8.GetBytes(salt1);
            byte[] rawSalt2 = Encoding.UTF8.GetBytes(salt2);
            int usLenInd = uniqueSalt.Length - 1;
            // Apply salt
            int s1, s2;
            long buffer;
            for (int i = 0; i < rawData.Length; i++)
            {
                buffer = 0;
                // Change the value of the current byte
                for (s1 = 0; s1 < rawSalt1.Length; s1++)
                    for (s2 = 0; s2 < rawSalt2.Length; s2++)
                        buffer = rawData[i] + ((salt1.Length + rawData[i]) * (rawSalt1[s1] + salt2.Length) * (rawSalt2[s2] + rawData.Length));
                // Apply third (unique user) hash
                buffer |= uniqueSalt[usLenInd % i];
                // Round it down within numeric range of byte
                while (buffer > byte.MaxValue)
                    buffer -= byte.MaxValue;
                // Check the value is not below 0
                if (buffer < 0) buffer = 0;
                // Reset the byte value
                rawData[i] = (byte)buffer;
            }
            // Hash the byte-array
            HashAlgorithm hasher = new SHA512Managed();
            Byte[] computedHash = hasher.ComputeHash(rawData);
            // Convert to base64 and return
            return Convert.ToBase64String(computedHash);
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// A collection of all the user-groups.
        /// </summary>
        public UserGroups UserGroups
        {
            get
            {
                return groups;
            }
        }
        /// <summary>
        /// A collection of all the account event types.
        /// </summary>
        public AccountEventTypes AccountEventTypes
        {
            get
            {
                return accountEventTypes;
            }
        }
    }
}