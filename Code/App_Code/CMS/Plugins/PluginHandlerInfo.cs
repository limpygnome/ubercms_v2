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
 *      File:           Plugins.cs
 *      Path:           /App_Code/CMS/Plugins/PluginHandlerInfo.cs
 * 
 *      Change-Log:
 *                      2013-06-28      Created initial class.
 * 
 * *********************************************************************************************************************
 * Stores information about a plugin's handler's, indicating if they should invoked and any parameters.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;

namespace CMS
{
    namespace Plugins
    {
        /// <summary>
        /// Stores information about a plugin's handler's, indicating if they should invoked and any parameters.
        /// </summary>
        public class PluginHandlerInfo
        {
            // Fields **************************************************************************************************
            private bool    requestStart,
                            requestEnd,
                            pageError,
                            pageNotFound,
                            cmsStart,
                            cmsEnd,
                            cmsPluginAction;
            int             cycleInterval;
            // Methods - Constructors
            public PluginHandlerInfo() { }
            public PluginHandlerInfo(bool requestStart, bool requestEnd, bool pageError, bool pageNotFound, bool cmsStart, bool cmsEnd, bool cmsPluginAction, int cycleInterval)
            {
                this.requestStart = requestStart;
                this.requestEnd = requestEnd;
                this.pageError = pageError;
                this.pageNotFound = pageNotFound;
                this.cmsStart = cmsStart;
                this.cmsEnd = cmsEnd;
                this.cmsPluginAction = cmsPluginAction;
                this.cycleInterval = cycleInterval;
            }
            // Methods - Properties
            /// <summary>
            /// Indicates if the plugin's request-start handler should be invoked for every request.
            /// </summary>
            public bool RequestStart
            {
                get
                {
                    return requestStart;
                }
            }
            /// <summary>
            /// Indicates if the plugins' request-end handler should be invoked for every request.
            /// </summary>
            public bool RequestEnd
            {
                get
                {
                    return requestEnd;
                }
            }
            /// <summary>
            /// Indicates if the plugin's request-error handler should be considered for invocation when a page error
            /// occurs.
            /// </summary>
            public bool PageError
            {
                get
                {
                    return pageError;
                }
            }
            /// <summary>
            /// Indicates if the plugin's page-not-found handler should be considered for invocation when a page not found
            /// error occurs.
            /// </summary>
            public bool PageNotFound
            {
                get
                {
                    return pageNotFound;
                }
            }
            /// <summary>
            /// Indicates if the plugin's cms-start handler should be invoked after the cms's core has started.
            /// </summary>
            public bool CmsStart
            {
                get
                {
                    return cmsStart;
                }
            }
            /// <summary>
            /// Indicates if the plugin's cms-end handler should be invoked before the cms's core is stopped.
            /// </summary>
            public bool CmsEnd
            {
                get
                {
                    return cmsEnd;
                }
            }
            /// <summary>
            /// Indicates if the plugin's cms-action handler should be invoked when any plugin action occurs
            /// (enable/disable/uninstall/install).
            /// </summary>
            public bool CmsPluginAction
            {
                get
                {
                    return cmsPluginAction;
                }
            }
            /// <summary>
            /// Indicates how often the plugin's cycler handler should be invoked; if this is greater than zero, it will
            /// not be invoked.
            /// </summary>
            public int CycleInterval
            {
                get
                {
                    return cycleInterval;
                }
            }
        }
    }
}
