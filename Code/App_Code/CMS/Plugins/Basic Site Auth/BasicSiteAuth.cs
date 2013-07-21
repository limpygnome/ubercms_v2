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
 *      File:           BasicSiteAuth.cs
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
    public class BasicSiteAuth : Plugin
    {
        // Constants
        public const string BSA_UUID = "943c3f9d-dfcb-483d-bf34-48ad5231a15f";
        // Constants - Settings
        public const string SETTINGS_USERNAME_MIN = "bsa/account/username_min";
        public const int SETTINGS_DEFAULT_USERNAME_MIN = 2;
        public const string SETTINGS_USERNAME_MAX = "bsa/account/username_max";
        public const int SETTINGS_DEFAULT_USERNAME_MAX = 32;
        public const string SETTINGS_PASSWORD_MIN = "bsa/account/password_min";
        public const int SETTINGS_DEFAULT_PASSWORD_MIN = 6;
        public const string SETTINGS_PASSWORD_MAX = "bsa/account/password_max";
        public const int SETTINGS_DEFAULT_PASSWORD_MAX = 128;
        public const string SETTINGS_EMAIL_MIN = "bsa/account/email_min";
        public const int SETTINGS_DEFAULT_EMAIL_MIN = 6;
        public const string SETTINGS_EMAIL_MAX = "bsa/account/email_max";
        public const int SETTINGS_DEFAULT_EMAIL_MAX = 64;
        public const string SETTINGS_SECRETQUESTION_MIN = "bsa/account/secretquestion_min";
        public const int SETTINGS_DEFAULT_SECRETQUESTION_MIN = 0;
        public const string SETTINGS_SECRETQUESTION_MAX = "bsa/account/secretquestion_max";
        public const int SETTINGS_DEFAULT_SECRETQUESTION_MAX = 64;
        public const string SETTINGS_SECRETANSWER_MIN = "bsa/account/ecretanswer_min";
        public const int SETTINGS_DEFAULT_SECRETANSWER_MIN = 64;
        public const string SETTINGS_SECRETANSWER_MAX = "bsa/account/secretanswer_max";
        public const int SETTINGS_DEFAULT_SECRETANSWER_MAX = 64;
        public const string SETTINGS_EMAIL_VERIFICATION = "bsa/account/email_verification";
        public const bool SETTINGS_DEFAULT_EMAIL_VERIFICATION = true;
        public const string SETTINGS_GROUP_UNVERIFIED_GROUPID = "bsa/groups/unverified_groupid";
        public const int SETTINGS_DEFAULT_GROUP_UNVERIFIED_GROUPID = 1;
        public const string SETTINGS_GROUP_VERIFIED_GROUPID = "bsa/groups/verified_groupid";
        public const int SETTINGS_DEFAULT_GROUP_VERIFIED_GROUPID = 2;
        public const string SETTINGS_GROUP_BANNED_GROUPID = "bsa/groups/banned_groupid";
        public const int SETTINGS_DEFAULT_GROUP_BANNED_GROUPID = 3;
        // Fields
        private string                          salt1,          // The first salt, used for generating a secure SHA-512 hash.
                                                salt2;          // The second salt, used for generating a secure SHA-512 hash.
        private UserGroups                      groups;         // A collection of all the user-groups.
        // Methods - Constructors
        public BasicSiteAuth(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo)
            : base(uuid, title, directory, state, handlerInfo)
        {
            groups = null;
        }
        // Methods - Handlers
        public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Setup handlers
            HandlerInfo.RequestStart = true;
            HandlerInfo.CmsStart = true;
            HandlerInfo.save(conn);
            // Install SQL

            // Create default user-groups (1 = unverified, 2 = verified, 3 = banned, 4 = administrators)

            // Create default root account (user = root, pass = password)
            return true;
        }
        public override bool uninstall(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return true;
        }
        public override bool enable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Reserve URLs
            BaseUtils.urlRewritingInstall(this, new string[]
            {
                "login",
                "register",
                "account_recovery",
                "my_account",
                "account_log",
                "members"
            }, ref messageOutput);
            // Add directives
            BaseUtils.preprocessorDirective_Add("bsa", ref messageOutput);
            return true;
        }
        public override bool disable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Unreserve URLs
            BaseUtils.urlRewritingUninstall(this, ref messageOutput);
            // Remove directives
            BaseUtils.preprocessorDirective_Remove("bsa", ref messageOutput);
            return true;
        }
        public override bool handler_cmsStart(UberLib.Connector.Connector conn)
        {
            loadSalts();
            groups = UserGroups.load(conn);
            return true;
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
        // Methods - Pages
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
        // Methods - Static
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
        // Methods
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
        private string generateHash(string data)
        {
            byte[] rawData = Encoding.UTF8.GetBytes(data);
            byte[] rawSalt1 = Encoding.UTF8.GetBytes(salt1);
            byte[] rawSalt2 = Encoding.UTF8.GetBytes(salt2);
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
        // Methods - Properties
        public UserGroups UserGroups
        {
            get
            {
                return groups;
            }
        }
    }
}