﻿/*                       ____               ____________
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
 *      Path:           /App_Code/CMS/Plugins/CSRF Protection/CSRFProtection.cs
 * 
 *      Change-Log:
 *                      2013-07-31      Created and finished initial class.
 * 
 * *********************************************************************************************************************
 * A plugin for offering cross-site request forgery (CSRF) protection. This mechanism works by placing a token as a
 * hidden field in forms and setting the same token as a cookie (with the token remaining the same throughout the
 * session). For more information on CSRF exploits:
 * https://www.owasp.org/index.php/Cross-Site_Request_Forgery_(CSRF)
 * 
 * Templates can embed a hidden field by calling/embedding '<!--csrf()-->' (without quotations); this plugin can also be
 * made completely optional using the pre-processor directive 'CSRFP'.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using CMS.Base;

namespace CMS.Plugins
{
    public class CSRFProtection : Plugin
    {
        // Constants ***************************************************************************************************
        public const string CSRF_KEY = "csrfp";         // The key used for storing CSRF tokens for cookies and hidden form data.
        public const int    CSRF_TOKEN_LENGTH = 16;     // The length of the CSRF randomly-generated tokens.
        // Methods - Constructors **************************************************************************************
        public CSRFProtection(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo)
            : base(uuid, title, directory, state, handlerInfo)
        { }
        // Methods - Overrides *****************************************************************************************
        public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return true;
        }
        public override bool uninstall(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return true;
        }
        public override bool enable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Install pre-processor directives
            if (!BaseUtils.preprocessorDirective_Add("CSRFP", ref messageOutput))
                return false;
            // Install template function handlers
            if (!Core.Templates.handlerAdd(conn, this, "csrf", "CMS.Plugins.CSRFProtection", "templateHandler_csrf", ref messageOutput))
                return false;
            return true;
        }
        public override bool disable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Uninstall pre-processor directives
            if (!BaseUtils.preprocessorDirective_Remove("CSRFP", ref messageOutput))
                return false;
            // Uninstall template function handlers
            if (!Core.Templates.handlerRemove(conn, "cssrf", ref messageOutput))
                return false;
            return base.disable(conn, ref messageOutput);
        }
        public override void handler_requestStart(Data data)
        {
            base.handler_requestStart(data);
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Fetches the CSRF token for the current session.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string getCSRFToken(Data data)
        {
#if !CSRFP
            HttpCookie cresp = data.Response.Cookies[CSRF_KEY];
            HttpCookie creq = data.Request.Cookies[CSRF_KEY];
            if (cresp != null && cresp.Value != null && cresp.Value.Length == CSRF_TOKEN_LENGTH)
                return cresp.Value;
            else if (creq != null && creq.Value != null && creq.Value.Length == CSRF_TOKEN_LENGTH)
                return creq.Value;
            else
            {
                string token = BaseUtils.generateRandomString(CSRF_TOKEN_LENGTH);
                data.Response.Cookies.Add(new HttpCookie(CSRF_KEY, token));
                return token;
            }
#else
            return string.Empty;
#endif
        }
        /// <summary>
        /// Indicates if the current request is valid and CSRF protected.
        /// </summary>
        /// <param name="data">The current request's data.</param>
        /// <returns>True = secure, false = could not verify the request/insecure/possibly fake.</returns>
        public static bool authenticated(Data data)
        {
#if CSRFP
            string fdata = data.Request.Form[CSRF_KEY];
            HttpCookie cdata = data.Request.Cookies[CSRF_KEY];
            // Ensure the form data length is correct and then check the cookie and form data are the same
            return cdata != null && fdata != null && fdata.Length == CSRF_TOKEN_LENGTH && fdata == cdata.Value;
#else
            return true;
#endif
        }
        // Methods - Static - Template Handlers ************************************************************************
        /// <summary>
        /// The CSRF template function for including hidden CSRF verification fields in forms, required for
        /// CSRF security.
        /// </summary>
        /// <param name="data">The current request's data.</param>
        /// <param name="args">Not required; can be null.</param>
        /// <returns>HTML for the hidden field.</returns>
        public static string templateHandler_csrf(Data data, string[] args)
        {
#if CSRFP
            return "<input type=\"hidden\" name=\"" + CSRF_KEY + "\" value=\"" + getCSRFToken(data) + "\" />";
#else
            return string.Empty;
#endif
        }
    }
}