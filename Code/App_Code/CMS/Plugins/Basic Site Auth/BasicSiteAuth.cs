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
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A basic authentication plugin, featuring user-groups and banning.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Xml;
using System.Web.Security;
using CMS.Base;
using CMS.Plugins;
using CMS.BasicSiteAuth.Models;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A basic authentication plugin, featuring user-groups and banning.
    /// </summary>
    public class BasicSiteAuth : Plugin
    {
        // Constants ***************************************************************************************************
        /// <summary>
        /// UUID of this plugin with no hypthens in upper-case.
        /// </summary>
        public const string     BSA_UUID_NHUC = "943C3F9DDFCB483DBF3448AD5231A15F";
        public const int        BSA_UNIQUE_USER_HASH_MIN = 10;
        public const int        BSA_UNIQUE_USER_HASH_MAX = 16;
        private const string    HTTPCONTEXTITEMS_BSA_CURRENT_USER = "bsa_current_user";
        private const int       ACCOUNT_EVENTS_PER_PAGE = 10;
        // Constants - Settings ****************************************************************************************
        // -- User Restrictions ****************************************************************************************
        public const string     SETTINGS_USERNAME_MIN = "bsa/account/username_min";
        public const string     SETTINGS_USERNAME_MIN__DESCRIPTION = "The minimum number of characters a username can be.";
        public const int        SETTINGS_USERNAME_MIN__DEFAULT = 2;

        public const string     SETTINGS_USERNAME_MAX = "bsa/account/username_max";
        public const string     SETTINGS_USERNAME_MAX__DESCRIPTION = "The maximum number of characters a username can be.";
        public const int        SETTINGS_USERNAME_MAX__DEFAULT = 32;

        public const string     SETTINGS_PASSWORD_MIN = "bsa/account/password_min";
        public const string     SETTINGS_PASSWORD_MIN__DESCRIPTION = "The minimum number of characters a password can be.";
        public const int        SETTINGS_PASSWORD_MIN__DEFAULT = 6;

        public const string     SETTINGS_PASSWORD_MAX = "bsa/account/password_max";
        public const string     SETTINGS_PASSWORD_MAX__DESCRIPTION = "The maximum number of characters a password can be.";
        public const int        SETTINGS_PASSWORD_MAX__DEFAULT = 128;

        public const string     SETTINGS_EMAIL_MIN = "bsa/account/email_min";
        public const string     SETTINGS_EMAIL_MIN__DESCRIPTION = "The minimum number of characters an e-mail can be.";
        public const int        SETTINGS_EMAIL_MIN__DEFAULT = 6;

        public const string     SETTINGS_EMAIL_MAX = "bsa/account/email_max";
        public const string     SETTINGS_EMAIL_MAX_DESCRIPTION = "The maximum number of characters an e-mail can be.";
        public const int        SETTINGS_EMAIL_MAX__DEFAULT = 64;

        public const string     SETTINGS_SECRETQUESTION_MIN = "bsa/account/secretquestion_min";
        public const string     SETTINGS_SECRETQUESTION_MIN__DESCRIPTION = "The minimum characters a secret question can be.";
        public const int        SETTINGS_SECRETQUESTION_MIN__DEFAULT = 0;

        public const string     SETTINGS_SECRETQUESTION_MAX = "bsa/account/secretquestion_max";
        public const string     SETTINGS_SECRETQUESTION_MAX__DESCRIPTION = "The maximum characters a secret question can be.";
        public const int        SETTINGS_SECRETQUESTION_MAX__DEFAULT = 64;

        public const string     SETTINGS_SECRETANSWER_MIN = "bsa/account/secretanswer_min";
        public const string     SETTINGS_SECRETANSWER_MIN__DESCRIPTION = "The minimum characters a secret answer can be.";
        public const int        SETTINGS_SECRETANSWER_MIN__DEFAULT = 0;

        public const string     SETTINGS_SECRETANSWER_MAX = "bsa/account/secretanswer_max";
        public const string     SETTINGS_SECRETANSWER_MAX__DESCRIPTION = "The maximum characters a secret answer can be.";
        public const int        SETTINGS_SECRETANSWER_MAX__DEFAULT = 64;

        public const string     SETTINGS_EMAIL_VERIFICATION = "bsa/account/email_verification";
        public const string     SETTINGS_EMAIL_VERIFICATION__DESCRIPTION = "Specifies if users need to verify their accounts via e-mail.";
        public const bool       SETTINGS_EMAIL_VERIFICATION__DEFAULT = true;
        // -- User Groups **********************************************************************************************
        public const string     SETTINGS_GROUP_ANONYMOUS_GROUPID = "bsa/groups/anonymous_groupid";
        public const string     SETTINGS_GROUP_ANONYMOUS_GROUPID__DESCRIPTION = "The identifier of the anonymous/non-authenticated group.";

        public const string     SETTINGS_GROUP_UNVERIFIED_GROUPID = "bsa/groups/unverified_groupid";
        public const string     SETTINGS_GROUP_UNVERIFIED_GROUPID__DESCRIPTION = "The identifier of the unverified group.";

        public const string     SETTINGS_GROUP_USER_GROUPID = "bsa/groups/user_groupid";
        public const string     SETTINGS_GROUP_USER_GROUPID__DESCRIPTION = "The identifier of the user group.";

        public const string     SETTINGS_GROUP_MODERATOR_GROUPID = "bsa/groups/moderator_groupid";
        public const string     SETTINGS_GROUP_MODERATOR_GROUPID__DESCRIPTION = "The identifier of the moderator group.";

        public const string     SETTINGS_GROUP_ADMINISTRATOR_GROUPID = "bsa/groups/administrator_groupid";
        public const string     SETTINGS_GROUP_ADMINISTRATOR_GROUPID__DESCRIPTION = "The identifier of the administrator group.";
        // -- Authentication Failed Attempts ***************************************************************************
        public const string     SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD = "bsa/authfailedattempts/ban_period";
        public const string     SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD__DESCRIPTION = "The duration an IP is banned from being able to login after reaching the failed attempts threshold.";
        public const int        SETTINGS_AUTHFAILEDATTEMPTS_BAN_PERIOD__DEFAULT = 3600000;

        public const string     SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD = "bsa/authfailedattempts/threshold";
        public const string     SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD__DESCRIPTION = "The threshold/maximum number of failed attempts allowed; once exceeded, an IP is banned for a period.";
        public const int        SETTINGS_AUTHFAILEDATTEMPTS_THRESHOLD__DEFAULT = 5;
        // -- Recovery Codes *******************************************************************************************
        public const string     SETTINGS_RECOVERYCODES_EXPIRE = "bsa/recoverycodes/expire";
        public const string     SETTINGS_RECOVERYCODES_EXPIRE__DESCRIPTION = "The life-span of a recovery code in milliseconds.";
        public const int        SETTINGS_RECOVERYCODES_EXPIRE__DEFAULT = 3600000;
        // Constants - Account Event Types *****************************************************************************
        // -- Incorrect authentication
        public const string     ACCOUNT_EVENT__INCORRECT_AUTH__UUID = "B98F1FC842D04A3D914F5653D098DB50";
        public const string     ACCOUNT_EVENT__INCORRECT_AUTH__TITLE = "Incorrect Authentication";
        public const string     ACCOUNT_EVENT__INCORRECT_AUTH__DESC = "An incorrect authentication attempt was made on the account.";
        public const string     ACCOUNT_EVENT__INCORRECT_AUTH__RENDER_CLASSPATH = "CMS.BasicSiteAuth.AET_Render";
        public const string     ACCOUNT_EVENT__INCORRECT_AUTH__RENDER_FUNCTION = "incorrectAuth";
        // -- Authentication
        public const string     ACCOUNT_EVENT__AUTH__UUID = "0FF9AA9A77A64499BB6C12FC7BF04594";
        public const string     ACCOUNT_EVENT__AUTH__TITLE = "Authenticated";
        public const string     ACCOUNT_EVENT__AUTH__DESC = "Account was successfully authenticated.";
        public const string     ACCOUNT_EVENT__AUTH__RENDER_CLASSPATH = "CMS.BasicSiteAuth.AET_Render";
        public const string     ACCOUNT_EVENT__AUTH__RENDER_FUNCTION = "authed";
        // -- Changed account settings
        public const string     ACCOUNT_EVENT__CHANGEDSETTINGS__UUID = "6D1B51E00B4D4459A67F8C6B67E2A37B";
        public const string     ACCOUNT_EVENT__CHANGEDSETTINGS__TITLE = "Account Settings Changed";
        public const string     ACCOUNT_EVENT__CHANGEDSETTINGS__DESC = "The settings of the account were changed.";
        public const string     ACCOUNT_EVENT__CHANGEDSETTINGS__RENDER_CLASSPATH = "CMS.BasicSiteAuth.AET_Render";
        public const string     ACCOUNT_EVENT__CHANGEDSETTINGS__RENDER_FUNCTION = "changedSettings";
        // -- Logged-out
        public const string     ACCOUNT_EVENT__LOGGEDOUT__UUID = "C21622AD82624E57843900DDB9A27CAF";
        public const string     ACCOUNT_EVENT__LOGGEDOUT__TITLE = "Logged Out";
        public const string     ACCOUNT_EVENT__LOGGEDOUT__DESC = "The account was logged-out.";
        public const string     ACCOUNT_EVENT__LOGGEDOUT__RENDER_CLASSPATH = "CMS.BasicSiteAuth.AET_Render";
        public const string     ACCOUNT_EVENT__LOGGEDOUT__RENDER_FUNCTION = "loggedOut";
        // -- Recovery secret question/answer attempted
        public const string     ACCOUNT_EVENT__SECRETQA_ATTEMPT__UUID = "D63560D8DC8F491BAE1693D5EFA9839A";
        public const string     ACCOUNT_EVENT__SECRETQA_ATTEMPT__TITLE = "Secret Question/Answer Recovery Attempt";
        public const string     ACCOUNT_EVENT__SECRETQA_ATTEMPT__DESC = "An attempt to recover the account using a secret question/answer was made.";
        public const string     ACCOUNT_EVENT__SECRETQA_ATTEMPT__RENDER_CLASSPATH = "CMS.BasicSiteAuth.AET_Render";
        public const string     ACCOUNT_EVENT__SECRETQA_ATTEMPT__RENDER_FUNCTION = "secretQaAttempt";
        // -- Recovery code sent
        public const string     ACCOUNT_EVENT__RECOVERYCODE_SENT__UUID = "02D9335BAA6A4741BB29E6E7F00D901C";
        public const string     ACCOUNT_EVENT__RECOVERYCODE_SENT__TITLE = "Recovery Code Sent";
        public const string     ACCOUNT_EVENT__RECOVERYCODE_SENT__DESC = "A recovery code was sent to the account's e-mail.";
        public const string     ACCOUNT_EVENT__RECOVERYCODE_SENT__RENDER_CLASSPATH = "CMS.BasicSiteAuth.AET_Render";
        public const string     ACCOUNT_EVENT__RECOVERYCODE_SENT__RENDER_FUNCTION = "recoveryCodeSent";
        // Fields ******************************************************************************************************
        private string                          salt1,                  // The first salt, used for generating a secure SHA-512 hash.
                                                salt2;                  // The second salt, used for generating a secure SHA-512 hash.
        private UserGroups                      groups;                 // A collection of all the user-groups.
        private AccountEventTypes               accountEventTypes;      // A collection of all the account event types.
        // Methods - Constructors **************************************************************************************
        public BasicSiteAuth(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        {
            groups = null;
            accountEventTypes = null;
        }
        // Methods - Handlers ******************************************************************************************
        public override bool install(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Setup handlers
            HandlerInfo.RequestEnd = true;
            HandlerInfo.PluginStart = true;
            HandlerInfo.CycleInterval = 900000; // 15 minutes
            HandlerInfo.save(conn);
            // Install SQL
            if (!BaseUtils.executeSQL(PathSQL + "/install.sql", conn, ref messageOutput))
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
            // -- Anonymous
            UserGroup ugAnonymous = new UserGroup();
            ugAnonymous.Title = "Anonymous";
            ugAnonymous.Description = "Non-authenticated/anonymous users.";
            ugAnonymous.save(this, conn);
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
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_ANONYMOUS_GROUPID, SETTINGS_GROUP_ANONYMOUS_GROUPID__DESCRIPTION, ugAnonymous.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_UNVERIFIED_GROUPID, SETTINGS_GROUP_UNVERIFIED_GROUPID__DESCRIPTION, ugUnverified.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_USER_GROUPID, SETTINGS_GROUP_USER_GROUPID__DESCRIPTION, ugUser.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_MODERATOR_GROUPID, SETTINGS_GROUP_MODERATOR_GROUPID__DESCRIPTION, ugModerator.GroupID);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_GROUP_ADMINISTRATOR_GROUPID, SETTINGS_GROUP_ADMINISTRATOR_GROUPID__DESCRIPTION, ugAdministrator.GroupID);
            // Save settings
            Core.Settings.save(conn);
            // Create default account event types
            // -- Incorrect authentication
            if (AccountEventType.create(conn, this, UUID.parse(ACCOUNT_EVENT__INCORRECT_AUTH__UUID), ACCOUNT_EVENT__INCORRECT_AUTH__TITLE, ACCOUNT_EVENT__INCORRECT_AUTH__DESC, ACCOUNT_EVENT__INCORRECT_AUTH__RENDER_CLASSPATH, ACCOUNT_EVENT__INCORRECT_AUTH__RENDER_FUNCTION, ref messageOutput) == null)
                return false;
            // -- Authenticated
            if (AccountEventType.create(conn, this, UUID.parse(ACCOUNT_EVENT__AUTH__UUID), ACCOUNT_EVENT__AUTH__TITLE, ACCOUNT_EVENT__AUTH__DESC, ACCOUNT_EVENT__AUTH__RENDER_CLASSPATH, ACCOUNT_EVENT__AUTH__RENDER_FUNCTION, ref messageOutput) == null)
                return false;
            // -- Changed account settings
            if (AccountEventType.create(conn, this, UUID.parse(ACCOUNT_EVENT__CHANGEDSETTINGS__UUID), ACCOUNT_EVENT__CHANGEDSETTINGS__TITLE, ACCOUNT_EVENT__CHANGEDSETTINGS__DESC, ACCOUNT_EVENT__CHANGEDSETTINGS__RENDER_CLASSPATH, ACCOUNT_EVENT__CHANGEDSETTINGS__RENDER_FUNCTION, ref messageOutput) == null)
                return false;
            // -- Logged-out
            if (AccountEventType.create(conn, this, UUID.parse(ACCOUNT_EVENT__LOGGEDOUT__UUID), ACCOUNT_EVENT__LOGGEDOUT__TITLE, ACCOUNT_EVENT__LOGGEDOUT__DESC, ACCOUNT_EVENT__LOGGEDOUT__RENDER_CLASSPATH, ACCOUNT_EVENT__LOGGEDOUT__RENDER_FUNCTION, ref messageOutput) == null)
                return false;
            // -- Recovery secret question/answer attempted
            if (AccountEventType.create(conn, this, UUID.parse(ACCOUNT_EVENT__SECRETQA_ATTEMPT__UUID), ACCOUNT_EVENT__SECRETQA_ATTEMPT__TITLE, ACCOUNT_EVENT__SECRETQA_ATTEMPT__DESC, ACCOUNT_EVENT__SECRETQA_ATTEMPT__RENDER_CLASSPATH, ACCOUNT_EVENT__SECRETQA_ATTEMPT__RENDER_FUNCTION, ref messageOutput) == null)
                return false;
            // -- Recovery code sent
            if (AccountEventType.create(conn, this, UUID.parse(ACCOUNT_EVENT__RECOVERYCODE_SENT__UUID), ACCOUNT_EVENT__RECOVERYCODE_SENT__TITLE, ACCOUNT_EVENT__RECOVERYCODE_SENT__DESC, ACCOUNT_EVENT__RECOVERYCODE_SENT__RENDER_CLASSPATH, ACCOUNT_EVENT__RECOVERYCODE_SENT__RENDER_FUNCTION, ref messageOutput) == null)
                return false;
            // Load salts for the installation process
            loadSalts();
            // Create default root account (user = root, pass = password)
            User userRoot = new User();
            userRoot.Username = "root";
            userRoot.setPassword(this, "helloworld");
            userRoot.Email = "admin@localhost.com";
            userRoot.SecretQuestion = null;
            userRoot.SecretAnswer = null;
            userRoot.UserGroup = ugAdministrator;
            userRoot.Registered = DateTime.Now;
            User.UserCreateSaveStatus urStatus = userRoot.save(this, conn, true); // Skip since user-groups cache is not active
            if (urStatus != User.UserCreateSaveStatus.Success)
                messageOutput.AppendLine("Warning: failed to create root user - '").Append(urStatus.ToString()).Append("'!");
            return true;
        }
        public override bool uninstall(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Remove SQL
            if (!BaseUtils.executeSQL(PathSQL + "/uninstall.sql", conn, ref messageOutput))
                return false;
            // Remove settings
            Core.Settings.remove(conn, this);
            return true;
        }
        public override bool enable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Add directives
            if (!BaseUtils.preprocessorDirective_Add("BSA", ref messageOutput))
                return false;
            // Install templates
            if (!Core.Templates.install(conn, this, PathTemplates, ref messageOutput))
                return false;
            // Install content
            if (!BaseUtils.contentInstall(PathContent, Core.PathContent, true, ref messageOutput))
                return false;
            // Reserve URLs
            if (!BaseUtils.urlRewritingInstall(conn, this, new string[]
            {
                "login",
                "logout",
                "register",
                "account_recovery",
                "my_account",
                "account_log"
            }, ref messageOutput))
                return false;
            return true;
        }
        public override bool disable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Unreserve URLs
            if (!BaseUtils.urlRewritingUninstall(conn, this, ref messageOutput))
                return false;
            // Remove content
            if (!BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput))
                return false;
            // Remove templates
            if (!Core.Templates.uninstall(conn, this, ref messageOutput))
                return false;
            // Remove directives
            if (!BaseUtils.preprocessorDirective_Remove("BSA", ref messageOutput))
                return false;
            return true;
        }
        public override bool handler_pluginStart(Connector conn)
        {
            // Load password hashing salts
            loadSalts();
            // Load user-groups
            if ((this.groups = UserGroups.load(conn)) == null)
                return false;
            // Load account event types
            if ((this.accountEventTypes = AccountEventTypes.load(conn)) == null)
                return false;
            return true;
        }
        public override void handler_pluginCycle()
        {
            // Setup connector
            Connector conn = Core.connectorCreate(false);
            // Clean old recovery codes
            AccountCode.removeExpired(conn);
            // Delete old failed authentication attempts
            AuthFailedAttempt.remove(conn);
            // Dispose connector
            conn.disconnect();
        }
        public override void  handler_requestEnd(Data data)
        {
#if !BSA
            return; // Fail-safe
#endif
            // Fetch the user for the current request
            User user = getCurrentUser(data);
            // Check if the user is banned/unable to login - invalidate the model
            if (user != null && UserBan.getLatestBan(data.Connector, user) != null)
                user = null;
            // Set the user's information
            else if (user != null)
            {
                // Set elements - note: disposed by method invalidCurrentUserSession
                data["Username"] = user.Username;
                if (user.UserGroup.Administrator)
                    data["Administrator"] = null;
                if (user.UserGroup.Moderator)
                    data["Moderator"] = null;
            }
        }
        public override bool handler_handleRequest(Data data)
        {
#if !BSA
            return false; // Fail-safe
#endif
            if(HttpContext.Current.User.Identity.IsAuthenticated)
                switch (data.PathInfo.ModuleHandler)
                {
                    case "my_account":
                        switch (data.PathInfo[1])
                        {
                            case null:
                                return pageMyAccount(data);
                            case "account_log":
                                return pageMyAccount_AccountLog(data);
                            case "close":
                                return pageMyAccount_CloseAccount(data);
                            default:
                                return false;
                        }
                    case "logout":
                        return pageLogout(data);
                    default:
                        return false;
                }
            else
                switch (data.PathInfo.ModuleHandler)
                {
                    case "login":
                        return pageLogin(data);
                    case "register":
                        switch (data.PathInfo[1])
                        {
                            case null:
                                return pageRegister(data);
                            case "verify":
                                return pageRegisterVerify(data);
                            case "success":
                                return pageRegisterSuccess(data);
                            default:
                                return false;
                        }
                    case "account_recovery":
                        switch (data.PathInfo[1])
                        {
                            case null:
                                return pageAccountRecovery(data);
                            case "email":
                                switch (data.PathInfo[2])
                                {
                                    case null:
                                        return pageAccountRecovery_Email(data);
                                    default:
                                        return pageAccountRecovery_EmailNewPassword(data);
                                }
                            case "sqa":
                                return pageAccountRecovery_SQA(data);
                            default:
                                return false;
                        }
                    default:
                        return false;
                }
        }
        // Methods - Pages *********************************************************************************************
        private bool pageLogin(Data data)
        {
            // Setup the page
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Check for postback
            string error = null;
            string username = data.Request.Form["username"];
            string password = data.Request.Form["password"];
            bool keepLoggedIn = data.Request.Form["session_persist"] != null;
            if (username != null && password != null)
            {
                // Validate security
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Invalid captcha verification code!";
#endif
                if (error == null)
                {
                    // Fetch the user
                    User u = User.load(this, data.Connector, username);
                    if (u == null)
                        error = "Invalid username/password!";
                    else
                    {
                        // Attempt to authenticate
                        UserBan ban = null;
                        User.AuthenticationStatus s = u.authenticate(this, password, data, ref ban);
                        switch (s)
                        {
                            case User.AuthenticationStatus.FailedIncorrect:
                                error = "Incorrect username or password specified!"; break;
                            case User.AuthenticationStatus.Failed:
                                error = "An unknown error occurred, please try again!"; break;
                            case User.AuthenticationStatus.FailedBanned:
                                error = "Your account has been banned: '" + (ban.Reason == null || ban.Reason.Length == 0 ? "unspecified reason" : ban.Reason) + "'; the ban will expire at: " + (ban.DateTimeEnd == DateTime.MaxValue ? "never" : ban.DateTimeEnd.ToString("YYYY-MM-dd HH:mm:ss")) + "."; break;
                            case User.AuthenticationStatus.FailedDisabled:
                                error = "Your account is disabled from logging-in!"; break;
                            case User.AuthenticationStatus.FailedTempBanned:
                                error = "Your IP has been temporarily banned for too many incorrect attempts, try again later!"; break;
                            case User.AuthenticationStatus.Success:
                                FormsAuthentication.SetAuthCookie(username, keepLoggedIn);
                                BaseUtils.redirect(data, data.Request.UrlReferrer != null && data.Request.UrlReferrer.AbsolutePath != "/login" ? data.Request.UrlReferrer.AbsoluteUri : BaseUtils.getAbsoluteURL(data, "/" + Core.DefaultURL));
                                break;
                        }
                    }
                }
            }
            // Set form data
            data["bsa_login_username"] = HttpUtility.HtmlEncode(username);
            if (keepLoggedIn)
                data.setFlag("bsa_login_persist");
            if (error != null)
                data["bsa_login_error"] = HttpUtility.HtmlEncode(error);
            // Set content
            data["Title"] = "Login";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/login");
            return true;
        }
        private bool pageRegisterVerify(Data data)
        {
            // Fetch data
            string code = data.PathInfo[3];
            string email = data.PathInfo[4];
            if (code == null || email == null)
                return false;
            // Attempt to verify
            if (!AccountActions.accountVerify(data, this, code, email))
                return false;
            // Set content
            data["Content"] = Core.Templates.get(data.Connector, "bsa/register/verify_success");
            data["Title"] = "Register - Verify Account";
            return true;
        }
        private bool pageRegisterSuccess(Data data)
        {
            // Fetch data
            string rawEmail = data.PathInfo[2];
            int ind;
            if (rawEmail == null || rawEmail.Length == 0 || (ind = rawEmail.IndexOf('@')) == -1 || ind >= rawEmail.Length || !Utils.validEmail(rawEmail))
                return false;
            // Set content
            string url = rawEmail[ind + 1].ToString().ToUpper() + rawEmail.Substring(ind + 2);
            data["bsa_register_success_email"] = url;
            data["bsa_register_success_email_url"] = "http://www." + url;
            data["Content"] = Core.Templates.get(data.Connector, "bsa/register/success_verify");
            data["Title"] = "Register - Success!";
            return true;
        }
        private bool pageRegister(Data data)
        {
            // Setup the page
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Check for postback
            string error = null;
            string username = data.Request.Form["username"];
            string password = data.Request.Form["password"];
            string passwordConfirm = data.Request.Form["password_confirm"];
            string email = data.Request.Form["email"];
            string secretQuestion = data.Request.Form["secret_question"];
            string secretAnswer = data.Request.Form["secret_answer"];
            string secretAnswerConfirm = data.Request.Form["secret_answer_confirm"];
            if (username != null && password != null && passwordConfirm != null && email != null && secretQuestion != null && secretAnswer != null && secretAnswerConfirm != null)
            {
                // Validate security
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request, please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Incorrect captcha verification code!";
#endif
                if (error == null)
                {
                    if (password != passwordConfirm)
                        error = "Passwords do not match!";
                    else if (secretAnswer.Length > 0 && secretAnswer != secretAnswerConfirm)
                        error = "Secret answers do not match!";
                    else
                    {
                        // Create the user model
                        User u = null;
                        User.UserCreateSaveStatus s = User.create(this, data, username, password, email, secretQuestion, secretAnswer, ref u);
                        // Check the status of the creation attempt
                        switch (s)
                        {
                            case User.UserCreateSaveStatus.Success:
                                // Attempt to authenticate the user
                                UserBan ub = null;
                                if (u.authenticate(this, password, data, ref ub) == User.AuthenticationStatus.Success)
                                    // Redirect to the main page
                                    BaseUtils.redirectAbs(data, "/" + Core.DefaultURL);
                                else
                                    // Redirect to login page
                                    BaseUtils.redirectAbs(data, "/login");
                                break;
                            case User.UserCreateSaveStatus.SuccessVerify:
                                BaseUtils.redirectAbs(data, "/register/success/" + u.Email);
                                break;
                            case User.UserCreateSaveStatus.InvalidEmail_AlreadyExists:
                                error = "E-mail already in-use!";
                                break;
                            case User.UserCreateSaveStatus.InvalidEmail_Format:
                            case User.UserCreateSaveStatus.InvalidEmail_Length:
                                error = "Invalid e-mail address!";
                                break;
                            case User.UserCreateSaveStatus.InvalidPassword_Length:
                                error = "Your password must be " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MIN].get<int>() + " to " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MAX].get<int>() + " characters in length!";
                                break;
                            case User.UserCreateSaveStatus.InvalidPassword_Security:
                                error = "Your password is too simple, pick another!";
                                break;
                            case User.UserCreateSaveStatus.InvalidSecretAnswer_Length:
                                error = "Your secret answer must be " + Core.Settings[BasicSiteAuth.SETTINGS_SECRETANSWER_MIN].get<int>() + " to " + Core.Settings[BasicSiteAuth.SETTINGS_SECRETANSWER_MAX].get<int>() + " characters in length!";
                                break;
                            case User.UserCreateSaveStatus.InvalidSecretQuestion_Length:
                                error = "Your secret question must be " + Core.Settings[BasicSiteAuth.SETTINGS_SECRETQUESTION_MIN].get<int>() + " to " + Core.Settings[BasicSiteAuth.SETTINGS_SECRETQUESTION_MAX].get<int>() + " characters in length!";
                                break;
                            case User.UserCreateSaveStatus.InvalidUsername_AlreadyExists:
                                error = "The desired username is already in-use!";
                                break;
                            case User.UserCreateSaveStatus.InvalidUsername_Length:
                                error = "Your username must be " + Core.Settings[BasicSiteAuth.SETTINGS_USERNAME_MIN].get<int>() + " to " + Core.Settings[BasicSiteAuth.SETTINGS_USERNAME_MAX].get<int>() + " characters in length!";
                                break;
                            case User.UserCreateSaveStatus.InvalidUserGroup:
                            case User.UserCreateSaveStatus.Error_Regisration:
                            default:
                                error = "An unknown error occurred; please try again!";
                                break;
                        }
                    }
                }
            }
            // Set form data
            data["bsa_register_username"] = HttpUtility.HtmlEncode(username);
            data["bsa_register_email"] = HttpUtility.HtmlEncode(email);
            data["bsa_register_secret_question"] = HttpUtility.HtmlEncode(secretQuestion);
            data["bsa_register_secret_answer"] = HttpUtility.HtmlEncode(secretAnswer);
            if (error != null)
                data["bsa_register_error"] = HttpUtility.HtmlEncode(error);
            // Set content
            data["Title"] = "Register";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/register");
            return true;
        }
        private bool pageAccountRecovery(Data data)
        {
            // Set content
            data["Title"] = "Account Recovery";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/recovery/home");
            return true;
        }
        private bool pageAccountRecovery_SQA(Data data)
        {
            // Setup the page
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            string error = null;
            bool displayget = true;
            // Fetch possible form data
            string username = data.Request.Form["username"];
            string secretAnswer = data.Request.Form["secret_answer"];
            string secretQuestion = null;
            string newPassword = data.Request.Form["password"];
            string newPasswordConfirm = data.Request.Form["password_confirm"];
            User.UserCreateSaveStatus s = User.UserCreateSaveStatus.Error_Regisration;
            if (username != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Invalid captcha verification code!";
#endif
                if (error == null)
                {
                    if (newPassword != null && (newPasswordConfirm == null || newPassword != newPasswordConfirm))
                    {
                        error = "Passwords do not match!";
                        displayget = false;
                    }
                    AccountActions.RecoverySQA sqa = AccountActions.recoverySQA(data, this, username, secretAnswer, error != null ? null : newPassword, ref secretQuestion, ref s);
                    switch (sqa)
                    {
                        case AccountActions.RecoverySQA.FailedBanned:
                            error = "Too many incorrect attempts have been made, try again later!"; break;
                        case AccountActions.RecoverySQA.FailedDisabled:
                            error = "Secret question/answer recovery is disabled for this account!"; break;
                        case AccountActions.RecoverySQA.FailedPersist:
                            displayget = false;
                            switch (s)
                            {
                                case User.UserCreateSaveStatus.InvalidPassword_Length:
                                    error = "Your password must be between " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MIN] + " to " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MAX] + " characters in length!"; break;
                                case User.UserCreateSaveStatus.InvalidPassword_Security:
                                    error = "Your password is too insecure, try another!"; break;
                            }
                            break;
                        case AccountActions.RecoverySQA.FailedAnswer:
                            displayget = false;
                            error = "Incorrect answer!"; break;
                        case AccountActions.RecoverySQA.Exists:
                            displayget = false; break;
                        case AccountActions.RecoverySQA.Failed:
                            error = "An unknown error occurred, please try again later!"; break;
                        case AccountActions.RecoverySQA.Success:
                            // The user's password has been successfully changed!
                            data["Content"] = Core.Templates.get(data.Connector, "bsa/recovery/sqa_success");
                            data["Title"] = "Account Recovery - Secret Question - Success";
                            return true;
                    }
                }
            }
            // Set content
            data["Content"] = Core.Templates.get(data.Connector, displayget ? "bsa/recovery/sqa_get" : "bsa/recovery/sqa_change");
            data["Title"] = "Account Recovery - Secret Question";
            data["bsa_sqa_username"] = HttpUtility.HtmlEncode(username);
            if (error != null)
                data["bsa_sqa_error"] = HttpUtility.HtmlEncode(error);
            if (secretQuestion != null)
                data["bsa_sqa_question"] = HttpUtility.HtmlEncode(secretQuestion);
            if (secretAnswer != null)
                data["bsa_sqa_answer"] = HttpUtility.HtmlEncode(secretAnswer);
            return true;
        }
        private bool pageAccountRecovery_Email(Data data)
        {
            // Setup the page
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            bool sent = false;
            string error = null;
            // Attempt to deploy a recovery code
            string email = data.Request.Form["email"];
            if (email != null && email.Length > 0)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Invalid captcha verification code!";
#endif
                if (error == null)
                {
                    if (!Utils.validEmail(email))
                        error = "Invalid e-mail address specified!";
                    else
                    {
                        AccountActions.RecoveryCodeEmail t = AccountActions.recoveryEmailDeploy(data, this, email);
                        switch (t)
                        {
                            case AccountActions.RecoveryCodeEmail.Failed:
                                error = "The specified e-mail is not associated with any accounts!"; break;
                            case AccountActions.RecoveryCodeEmail.FailedBanned:
                                error = "You've tried too many different e-mails! Please try again later..."; break;
                            case AccountActions.RecoveryCodeEmail.Success:
                                sent = true; break;
                            default:
                                error = "Unknown error occurred!"; break;
                        }
                    }
                }
            }
            // Set content
            data["Title"] = "Account Recovery - E-mail";
            data["Content"] = Core.Templates.get(data.Connector, sent ? "bsa/recovery/email_sent" : "bsa/recovery/email_get");
            if(error != null)
                data["bsa_recovery_email_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageAccountRecovery_EmailNewPassword(Data data)
        {
            string error = null;
            string code = data.PathInfo[2];
            string password = data.Request.Form["password"];
            string passwordConfirm = data.Request.Form["password_confirm"];
            // Change the password or check it exists
            if (password != null && (passwordConfirm == null || password != passwordConfirm))
                error = "Passwords do not match!";
            else
            {
                User.UserCreateSaveStatus uS = User.UserCreateSaveStatus.Error_Regisration;
                AccountActions.RecoveryCodeEmail s = AccountActions.recoveryEmail(data, this, code, password, ref uS);
                switch (s)
                {
                    case AccountActions.RecoveryCodeEmail.FailedBanned:
                    case AccountActions.RecoveryCodeEmail.Failed:
                        // Set content
                        data["Title"] = "Account Recovery - E-mail";
                        data["Content"] = Core.Templates.get(data.Connector, "bsa/recovery/email_fail");
                        return true;
                    case AccountActions.RecoveryCodeEmail.FailedUserPersist:
                        switch (uS)
                        {
                            case User.UserCreateSaveStatus.InvalidPassword_Length:
                                error = "Your password must be between " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MIN] + " to " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MAX] + " characters in length!"; break;
                            case User.UserCreateSaveStatus.InvalidPassword_Security:
                                error = "Your password is too insecure, try another!"; break;
                            default:
                                error = "An unknown error occurred, please try again later or contact us!" + uS.ToString(); break;
                        }
                        break;
                    case AccountActions.RecoveryCodeEmail.Success:
                        // Success - set content!
                        data["Title"] = "Account Recovery - Success";
                        data["Content"] = Core.Templates.get(data.Connector, "bsa/recovery/email_success");
                        return true;
                }
            }
            // Set content
            data["Title"] = "Account Recovery - New Password";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/recovery/email_change");
            if(error != null)
                data["bsa_recovery_email_error"] = HttpUtility.HtmlEncode(error);
            data["bsa_recovery_email_code"] = HttpUtility.HtmlEncode(code);
            return true;
        }
        private bool pageMyAccount(Data data)
        {
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            string error = null;
            bool success = false;
            // Load the current user
            User u = getCurrentUser(data);
            // Fetch form data and check for postback
            string  currentPassword = data.Request.Form["password"],
                    newPassword = data.Request.Form["new_password"],
                    newPasswordConfirm = data.Request.Form["new_password_confirm"],
                    email = data.Request.Form["email"],
                    secretQuestion = data.Request.Form["secret_question"],
                    secretAnswer = data.Request.Form["secret_answer"],
                    secretAnswerConfirm = data.Request.Form["secret_answer_confirm"];
            if (currentPassword != null && newPassword != null && newPasswordConfirm != null && email != null &&
                secretQuestion != null && secretAnswer != null && secretAnswerConfirm != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request, please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Incorrect captcha verification code!";
#endif
                if (error == null)
                {
                    // Check the new password's match
                    if (newPassword != null && newPassword.Length > 0 && (newPasswordConfirm == null || newPasswordConfirm != newPassword))
                        error = "New password's do not match!";
                    // Check the secret answer's match
                    else if (secretAnswer != null && secretAnswer.Length > 0 && (secretAnswer == null || secretAnswer != secretAnswerConfirm))
                        error = "Secret answer's do not match!";
                    else
                    {
                        // Attempt to update the account
                        User.UserCreateSaveStatus ps = User.UserCreateSaveStatus.Error_Regisration;
                        AccountActions.AccountUpdate t = AccountActions.updateAccount(data, this, u, currentPassword, newPassword, email, secretQuestion, secretAnswer, ref ps);
                        switch (t)
                        {
                            case AccountActions.AccountUpdate.Failed:
                                error = "An unknown error occurred, please try again later!"; break;
                            case AccountActions.AccountUpdate.FailedCurrentPassword:
                                error = "Current password is incorrect or you've made too many incorrect attempts!"; break;
                            case AccountActions.AccountUpdate.FailedPassword:
                                switch (ps)
                                {
                                    case User.UserCreateSaveStatus.InvalidPassword_Length:
                                        error = "Your new password must be between " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MIN] + " to " + Core.Settings[BasicSiteAuth.SETTINGS_PASSWORD_MAX] + " characters in length!"; break;
                                    case User.UserCreateSaveStatus.InvalidPassword_Security:
                                        error = "The desired new password is too basic, pick another!"; break;
                                    default:
                                        error = "An unknown issue occurred updating your password, please try again later!"; break;
                                }
                                break;
                            case AccountActions.AccountUpdate.FailedUserPersist:
                                switch (ps)
                                {
                                    case User.UserCreateSaveStatus.InvalidEmail_Format:
                                        error = "Invalid e-mail address!"; break;
                                    case User.UserCreateSaveStatus.InvalidEmail_Length:
                                        error = "E-mail must be between " + Core.Settings[SETTINGS_EMAIL_MIN].get<int>() + " and " + Core.Settings[SETTINGS_EMAIL_MAX].get<int>() + " characters in length!"; break;
                                    case User.UserCreateSaveStatus.InvalidSecretQuestion_Length:
                                        error = "Your secret question must be between " + Core.Settings[SETTINGS_SECRETQUESTION_MIN].get<int>() + " to " + Core.Settings[SETTINGS_SECRETQUESTION_MAX].get<int>() + " characters in length!"; break;
                                    case User.UserCreateSaveStatus.InvalidSecretAnswer_Length:
                                        error = "Your secret answer must be between " + Core.Settings[SETTINGS_SECRETANSWER_MIN].get<int>() + " to " + Core.Settings[SETTINGS_SECRETANSWER_MAX].get<int>() + " characters in length!"; break;
                                    default:
                                        error = "An unknown issue occurred updating your account (" + ps.ToString() + "), please try again later!"; break;
                                }
                                break;
                            case AccountActions.AccountUpdate.Success:
                                success = true; break;
                        }
                    }
                }
            }
            // Set content
            data["Content"] = Core.Templates.get(data.Connector, "bsa/my_account");
            data["Title"] = "My Account";
            data["bsa_ma_username"] = HttpUtility.HtmlEncode(u.Username);
            data["bsa_ma_email"] = HttpUtility.HtmlEncode(email ?? u.Email);
            data["bsa_ma_secret_question"] = HttpUtility.HtmlEncode(secretQuestion ?? u.SecretQuestion);
            data["bsa_ma_secret_answer"] = HttpUtility.HtmlEncode(secretAnswer ?? u.SecretAnswer);
            data["bsa_ma_secret_answer_confirm"] = HttpUtility.HtmlEncode(secretAnswerConfirm ?? u.SecretAnswer);
            if (error != null)
                data["bsa_ma_error"] = HttpUtility.HtmlEncode(error);
            else if(success)
                data.setFlag("bsa_ma_success");
            return true;
        }
        private bool pageMyAccount_AccountLog(Data data)
        {
            // Setup page
            BaseUtils.headerAppendCss("/content/css/bsa.css", ref data);
            // Fetch the current user
            User u = getCurrentUser(data);
            if(u == null)
                return false;
            // Check for postback actions
            string error = null;
            string action = data.Request.Form["action"] ?? data.Request.QueryString["action"];
            if (action != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request, please try again!";
#endif
                if (error == null)
                {
                    // Handle the action
                    switch (action)
                    {
                        case "Clear All":
                            AccountEvent.removeAll(data.Connector, u);
                            break;
                        case "remove":
                            AccountEvent ae;
                            if ((ae = AccountEvent.load(this, data.Connector, int.Parse(data.Request.QueryString["id"]))) != null)
                            {
                                // Attempt to load the model
                                ae.remove(data.Connector);
                                BaseUtils.redirectAbs(data, "/my_account/account_log");
                            }
                            else
                                error = "Event with ID '" + data.Request.QueryString["id"] + "' could not be loaded!";
                            break;
                        default:
                            error = "Unknown action '" + action + "'!"; break;
                    }
                }
            }
            // Fetch display parameters
            int page;
            if (!int.TryParse(data.PathInfo[2], out page) || page < 0)
                page = 1;
            // Fetch account events
            StringBuilder items = new StringBuilder();
            AccountEvent[] events = AccountEvent.loadByUser(this, data.Connector, u, ACCOUNT_EVENTS_PER_PAGE, page, AccountEvent.Sorting.DateTimeDescending);
            if(events.Length > 0)
            {
                foreach (AccountEvent ae in events)
                {
                    // Render the event and append it
                    items.Append((string)ae.Type.RenderMethod.Invoke(null, new object[] { data, ae }));
                }
                data["bsa_account_log_items"] = items.ToString();
            }
            // Set content
            data["Title"] = "My Account - Account Log";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/my_account/account_log");
            if (error != null)
                data["bsa_account_log_error"] = HttpUtility.HtmlEncode(error);
            data["bsa_account_log_page"] = page.ToString();
            if(page > 1)
                data["bsa_account_log_prev"] = (page - 1).ToString();
            if (page < int.MaxValue && events.Length == ACCOUNT_EVENTS_PER_PAGE)
                data["bsa_account_log_next"] = (page + 1).ToString();
            return true;
        }
        private bool pageMyAccount_CloseAccount(Data data)
        {
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            string error = null;
            // Check for postback
            if (data.Request["close"] != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Invalid captcha verification code!";
#endif
                if (error == null)
                {
                    // Fetch the current user
                    User u = getCurrentUser(data);
                    // Update the user and persist
                    u.PendingDeletion = true;
                    if (u.save(this, data.Connector) != User.UserCreateSaveStatus.Success)
                        error = "An unknown error occurred, please try again later or contact us!";
                    else
                    {
                        // Destroy the session and redirect to the default handler
                        invalidateCurrentUserSession();
                        BaseUtils.redirectAbs(data, "/" + Core.DefaultURL);
                    }
                }
            }
            // Set content
            data["Title"] = "My Account - Close Account";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/my_account/close");
            if (error != null)
                data["bsa_close_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageLogout(Data data)
        {
            User usr = getCurrentUser(data);
            if (usr == null)
                return false;
            // Dispose the session
            invalidateCurrentUserSession();
            // Log the event
            AccountEvent.create(data.Connector, this, BasicSiteAuth.ACCOUNT_EVENT__LOGGEDOUT__UUID, DateTime.Now, usr.UserID, data.Request.UserHostAddress, SettingsNode.DataType.String, data.Request.UserAgent, SettingsNode.DataType.String);
            // Set content
            data["Title"] = "Logout";
            data["Content"] = Core.Templates.get(data.Connector, "bsa/logout");
            return true;
        }
        // Methods - Static - User Related *****************************************************************************
        /// <summary>
        /// Gets the user object for the current request; returns null if the user is anonymous. This can be invoked
        /// at any stage; if the user for the current request has not been loaded, it will be loaded.
        /// </summary>
        /// <returns>The user model which represents the user of the current request.</returns>
        public static User getCurrentUser(Data data)
        {
#if BSA
            if (!HttpContext.Current.Items.Contains(HTTPCONTEXTITEMS_BSA_CURRENT_USER))
                loadCurrentUser(data);
            return (User)HttpContext.Current.Items[HTTPCONTEXTITEMS_BSA_CURRENT_USER];
#else
            return null;
#endif
        }
        public static void invalidateCurrentUserSession()
        {
#if !BSA
            return;
#endif
            // Destroy model
            HttpContext.Current.Items[HTTPCONTEXTITEMS_BSA_CURRENT_USER] = null;
            // Destroy authentication
            FormsAuthentication.SignOut();
            // Destroy entire session
            HttpContext.Current.Session.Abandon();
        }
        private static void loadCurrentUser(Data data)
        {
#if !BSA
            return; // Fail-safe
#endif
            // Check the user object has not already been loaded
            if (HttpContext.Current.Items[HTTPCONTEXTITEMS_BSA_CURRENT_USER] != null)
                return;
            // Check if the user is authenticated
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                HttpContext.Current.Items[HTTPCONTEXTITEMS_BSA_CURRENT_USER] = null;
            else
            {
                // Fetch the BSA plugin
                BasicSiteAuth bsa = getCurrentInstance();
                if (bsa != null)
                {
                    // Load and set user object
                    User usr = User.load(bsa, data.Connector, HttpContext.Current.User.Identity.Name);
                    if (usr != null && usr.UserGroup.Login)
                        HttpContext.Current.Items[HTTPCONTEXTITEMS_BSA_CURRENT_USER] = usr;
                    else
                        HttpContext.Current.Items[HTTPCONTEXTITEMS_BSA_CURRENT_USER] = null;
                }
            }
        }
        /// <summary>
        /// Gets the current instance of the BasicSiteAuth in the runtime.
        /// </summary>
        /// <returns>Model or null.</returns>
        public static BasicSiteAuth getCurrentInstance()
        {
            return (BasicSiteAuth)Core.Plugins[BSA_UUID_NHUC];
        }
        // Methods *****************************************************************************************************
        private void loadSalts()
        {
            string salts = Path + "/salts.xml";
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
        
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// A collection of all the user-groups.
        /// </summary>
        public UserGroups UserGroups
        {
            get
            {
                return this.groups;
            }
        }
        /// <summary>
        /// A collection of all the account event types.
        /// </summary>
        public AccountEventTypes AccountEventTypes
        {
            get
            {
                return this.accountEventTypes;
            }
        }
        /// <summary>
        /// The first salt used for generating password hash's.
        /// </summary>
        public string Salt1
        {
            get
            {
                return salt1;
            }
        }
        /// <summary>
        /// The second salt used for generating password hash's.
        /// </summary>
        public string Salt2
        {
            get
            {
                return salt2;
            }
        }
    }
}