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
 * *****************************************************************************
 * Stores information about a plugin's handler's, indicating if they should
 * invoked and any parameters.
 * *****************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;

namespace CMS
{
    namespace Plugins
    {
        public class PluginHandlerInfo
        {
            // Fields
            private bool    requestStart,
                            requestEnd,
                            pageError,
                            pageNotFound;
            int             cycleInterval;
            // Methods - Constructors
            public PluginHandlerInfo() { }
            public PluginHandlerInfo(bool requestStart, bool requestEnd, bool pageError, bool pageNotFound, int cycleInterval)
            {
                this.requestStart = requestStart;
                this.requestEnd = requestEnd;
                this.pageError = pageError;
                this.pageNotFound = pageNotFound;
                this.cycleInterval = cycleInterval;
            }
            // Methods - Properties
            public bool RequestStart
            {
                get
                {
                    return requestStart;
                }
            }
            public bool RequestEnd
            {
                get
                {
                    return requestEnd;
                }
            }
            public bool PageError
            {
                get
                {
                    return pageError;
                }
            }
            public bool PageNotFound
            {
                get
                {
                    return pageNotFound;
                }
            }
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
