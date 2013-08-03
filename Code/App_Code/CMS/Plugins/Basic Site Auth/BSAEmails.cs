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
 *      Path:           /App_Code/CMS/Base/Plugins.cs
 * 
 *      Change-Log:
 *                      2013-08-02      Created initial class.
 * 
 * *********************************************************************************************************************
 * A class used for creating and deploying e-mails.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using System.Web;
using UberLib.Connector;
using CMS.Base;

namespace CMS.BasicSiteAuth
{
    public static class BSAEmails
    {
        /// <summary>
        /// Sends an account verification e-mail.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="user"></param>
        public static void sendVerify(Data data, User user, string verifyCode)
        {
            // Render the template
            StringBuilder buffer = new StringBuilder(Core.Templates.get(data.Connector, "bsa/emails/verify"));
            Data render = new Data(null, null, null);
            setRenderBaseParams(user, ref render, data);
            render["verify_code"] = HttpUtility.HtmlEncode(verifyCode);
            Core.Templates.render(ref buffer, ref render);
            // Add to queue
            Core.EmailQueue.addMessage(data.Connector, user.Email, Core.Title + " - Verify Account", buffer.ToString(), true);
        }
        /// <summary>
        /// Sends a welcome e-mail.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="user"></param>
        public static void sendWelcome(Data data, User user)
        {
            // Render the template
            StringBuilder buffer = new StringBuilder(Core.Templates.get(data.Connector, "bsa/emails/welcome"));
            Data render = new Data(null, null, null);
            setRenderBaseParams(user, ref render, data);
            Core.Templates.render(ref buffer, ref render);
            // Add to queue
            Core.EmailQueue.addMessage(data.Connector, user.Email, Core.Title + " - Welcome!", buffer.ToString(), true);
        }
        /// <summary>
        /// Sends a recovery code for account recovery of a user.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="user"></param>
        public static void sendRecoveryCode(Data data, User user, string recoveryCode)
        {
            // Render the template
            StringBuilder buffer = new StringBuilder(Core.Templates.get(data.Connector, "bsa/emails/recovery_code"));
            Data render = new Data(null, null, null);
            setRenderBaseParams(user, ref render, data);
            render["recovery_code"] = HttpUtility.HtmlEncode(recoveryCode);
            Core.Templates.render(ref buffer, ref render);
            // Add to queue
            Core.EmailQueue.addMessage(data.Connector, user.Email, Core.Title + " - Account Recovery - Code", buffer.ToString(), true);
        }
        private static void setRenderBaseParams(User user, ref Data render, Data request)
        {
            // Site related
            render["site_title"] = HttpUtility.HtmlEncode(Core.Title);
            render["site_url"] = BaseUtils.getWebsiteUrl(request);
            // Host related
            render["host_ip"] = HttpUtility.HtmlEncode(request.Request.UserHostAddress);
            render["host_useragent"] = HttpUtility.HtmlEncode(request.Request.UserAgent);
            // User related
            render["userid"] = user.UserID.ToString();
            render["username"] = HttpUtility.HtmlEncode(user.Username);
            render["email"] = HttpUtility.HtmlEncode(user.Email);
            render["secret_question"] = HttpUtility.HtmlEncode(user.SecretQuestion);
            render["secret_answer"] = HttpUtility.HtmlEncode(user.SecretAnswer);
            render["groupid"] = user.UserGroup.GroupID.ToString();
            render["group_title"] = HttpUtility.HtmlEncode(user.UserGroup.Title);
            render["group_desc"] = HttpUtility.HtmlEncode(user.UserGroup.Description);
            // Date/time related
            DateTime now = DateTime.Now;
            render["datetime"] = now.ToString();
            render["date"] = now.ToLongDateString();
            render["time"] = now.ToLongTimeString();
        }
    }
}