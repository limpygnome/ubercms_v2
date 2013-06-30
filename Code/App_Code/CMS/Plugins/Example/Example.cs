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
 *      Path:           /App_Code/CMS/Plugins/Example.cs
 * 
 *      Change-Log:
 *                      2013-06-28      Created initial class.
 *                      2013-06-29      Updated name to Example.
 * 
 * *********************************************************************************************************************
 * Example/debugging plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;

namespace CMS
{
    namespace Plugins
    {
        public class Example : Plugin
        {
            public Example(int pluginid, string title, PluginState state, PluginHandlerInfo handlerInfo) : base(pluginid, title, state, handlerInfo)
            {
            }
            public override bool handler_handleRequest(Base.Data data)
            {
                switch (data.PathInfo.ModuleHandler)
                {
                    case "error_example":
                        throw new Exception("An example of error catching!");
                    default:
                        data["Content"] = "Example handler works! Click <a href=\"/error_example\">here</a> for error-catching example.";
                        break;
                }
                return true;
            }
            public override bool handler_handlePageNotFound(Base.Data data)
            {
                data["Content"] = "Path '" + data.PathInfo.FullPath + "' not found - caught by test handler!";
                return true;
            }
            public override bool handler_handlePageError(Base.Data data, Exception ex)
            {
                data["Content"] = "Error '" + ex.Message + "' caught by example plugin!";
                return true;
            }
        }
    }
}
