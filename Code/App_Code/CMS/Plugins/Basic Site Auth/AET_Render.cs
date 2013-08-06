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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/AET_Render.cs
 * 
 *      Change-Log:
 *                      2013-08-05      Created initial class.
 * 
 * *********************************************************************************************************************
 * A class for rendering account events based on their types.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using CMS.Base;
using CMS.BasicSiteAuth.Models;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A class for rendering account events based on their types.
    /// </summary>
    public class AET_Render
    {
        /// <summary>
        /// Renders an incorrect authentication event.
        /// </summary>
        /// <param name="data"The data for the current request.</param>
        /// <param name="ae">The account event data.</param>
        /// <returns>The HTML of the rendered event.</returns>
        public static string incorrectAuth(Data data, AccountEvent ae)
        {
            StringBuilder t = new StringBuilder(Core.Templates.get(data.Connector, "bsa/my_account/account_log_item"));
            t.Replace("<ID>", HttpUtility.HtmlEncode(ae.EventID.ToString()));
            t.Replace("<TYPE>", "Incorrect Authentication");
            t.Replace("<INFORMATION>", HttpUtility.HtmlEncode("IP: " + (string)ae.Param1 + ", User-Agent: " + (string)ae.Param2));
            t.Replace("<DATE_TIME>", BaseUtils.dateTimeToHumanReadable(ae.DateTime));
            t.Replace("<DATE_TIME_FULL>", ae.DateTime.ToString("yyyy-mm-dd HH:mm:ss"));
            return t.ToString();
        }
        /// <summary>
        /// Renders an authenticated event.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="ae">The account event data.</param>
        /// <returns>The HTML of the rendered event.</returns>
        public static string authed(Data data, AccountEvent ae)
        {
            StringBuilder t = new StringBuilder(Core.Templates.get(data.Connector, "bsa/my_account/account_log_item"));
            t.Replace("<ID>", HttpUtility.HtmlEncode(ae.EventID.ToString()));
            t.Replace("<TYPE>", "Authenticated");
            t.Replace("<INFORMATION>", HttpUtility.HtmlEncode("IP: " + (string)ae.Param1 + ", User-Agent: " + (string)ae.Param2));
            t.Replace("<DATE_TIME>", BaseUtils.dateTimeToHumanReadable(ae.DateTime));
            t.Replace("<DATE_TIME_FULL>", ae.DateTime.ToString("yyyy-mm-dd HH:mm:ss"));
            return t.ToString();
        }
        /// <summary>
        /// Renders an account changed settings event.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="ae">The account event data.</param>
        /// <returns>The HTML of the rendered event.</returns>
        public static string changedSettings(Data data, AccountEvent ae)
        {
            StringBuilder t = new StringBuilder(Core.Templates.get(data.Connector, "bsa/my_account/account_log_item"));
            t.Replace("<ID>", HttpUtility.HtmlEncode(ae.EventID.ToString()));
            t.Replace("<TYPE>", "Changed Account Settings");
            t.Replace("<INFORMATION>", HttpUtility.HtmlEncode("IP: " + (string)ae.Param1 + ", User-Agent: " + (string)ae.Param2));
            t.Replace("<DATE_TIME>", BaseUtils.dateTimeToHumanReadable(ae.DateTime));
            t.Replace("<DATE_TIME_FULL>", ae.DateTime.ToString("yyyy-mm-dd HH:mm:ss"));
            return t.ToString();
        }
        /// <summary>
        /// Renders a logged-out event.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="ae">The account event data.</param>
        /// <returns>The HTML of the rendered event.</returns>
        public static string loggedOut(Data data, AccountEvent ae)
        {
            StringBuilder t = new StringBuilder(Core.Templates.get(data.Connector, "bsa/my_account/account_log_item"));
            t.Replace("<ID>", HttpUtility.HtmlEncode(ae.EventID.ToString()));
            t.Replace("<TYPE>", "Logged Out");
            t.Replace("<INFORMATION>", HttpUtility.HtmlEncode("IP: " + (string)ae.Param1 + ", User-Agent: " + (string)ae.Param2));
            t.Replace("<DATE_TIME>", BaseUtils.dateTimeToHumanReadable(ae.DateTime));
            t.Replace("<DATE_TIME_FULL>", ae.DateTime.ToString("yyyy-mm-dd HH:mm:ss"));
            return t.ToString();
        }
        /// <summary>
        /// Renders a secret question/answer attempt event.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="ae">The account event data.</param>
        /// <returns>The HTML of the rendered event.</returns>
        public static string secretQaAttempt(Data data, AccountEvent ae)
        {
            StringBuilder t = new StringBuilder(Core.Templates.get(data.Connector, "bsa/my_account/account_log_item"));
            t.Replace("<ID>", HttpUtility.HtmlEncode(ae.EventID.ToString()));
            t.Replace("<TYPE>", "Secret Question/Answer Recovery Attempted");
            t.Replace("<INFORMATION>", HttpUtility.HtmlEncode("IP: " + (string)ae.Param1 + ", User-Agent: " + (string)ae.Param2));
            t.Replace("<DATE_TIME>", BaseUtils.dateTimeToHumanReadable(ae.DateTime));
            t.Replace("<DATE_TIME_FULL>", ae.DateTime.ToString("yyyy-mm-dd HH:mm:ss"));
            return t.ToString();
        }
        /// <summary>
        /// Renders a recovery code deployed event.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="ae">The account event data.</param>
        /// <returns>The HTML of the rendered event.</returns>
        public static string recoveryCodeSent(Data data, AccountEvent ae)
        {
            StringBuilder t = new StringBuilder(Core.Templates.get(data.Connector, "bsa/my_account/account_log_item"));
            t.Replace("<ID>", HttpUtility.HtmlEncode(ae.EventID.ToString()));
            t.Replace("<TYPE>", "Recovery Code Deployed");
            t.Replace("<INFORMATION>", HttpUtility.HtmlEncode("IP: " + (string)ae.Param1 + ", User-Agent: " + (string)ae.Param2));
            t.Replace("<DATE_TIME>", BaseUtils.dateTimeToHumanReadable(ae.DateTime));
            t.Replace("<DATE_TIME_FULL>", ae.DateTime.ToString("yyyy-mm-dd HH:mm:ss"));
            return t.ToString();
        }
    }
}