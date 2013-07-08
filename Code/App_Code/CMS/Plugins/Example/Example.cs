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
 *      Path:           /App_Code/CMS/Plugins/Example/Example.cs
 * 
 *      Change-Log:
 *                      2013-06-28      Created initial class.
 *                      2013-06-29      Updated name to Example.
 *                      2013-07-05      Updated HTML output to use paragraphs and set titles.
 *                      2013-07-06      Added output for install/uninstall/enable/disable.
 *                      2013-07-07      Error handler now outputs a stack-trace, useful for development.
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
            public Example(int pluginid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo) : base(pluginid, title, directory, state, handlerInfo)
            {
            }
            public override bool handler_handleRequest(Base.Data data)
            {
                switch (data.PathInfo.ModuleHandler)
                {
                    case "error_example":
                        throw new Exception("An example of error catching!");
                    default:
                        data["Title"] = "Welcome!";
                        data["Content"] = "<p>Example handler works! Click <a href=\"/error_example\">here</a> for error-catching example.</p>";
                        break;
                }
                return true;
            }
            public override bool handler_handlePageNotFound(Base.Data data)
            {
                data["Title"] = "Page Not Found";
                data["Content"] = "<p>Path '" + data.PathInfo.FullPath + "' not found - caught by test handler!</p>";
                return true;
            }
            public override bool handler_handlePageError(Base.Data data, Exception ex)
            {
                data["Title"] = "Error Serving Request";
                data["Content"] = "<p>Error '" + ex.Message + "' caught by example plugin!</p><h3>Stack-trace:</h3><p>" + ex.StackTrace + "</p>";
                return true;
            }
            public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                messageOutput.AppendLine("Invoked example-plugin's install method!");
                return true;
            }
            public override bool uninstall(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                messageOutput.AppendLine("Invoked example-plugin's uninstall method!");
                return true;
            }
            public override bool enable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                messageOutput.AppendLine("Invoked example-plugin's enable method!");
                return true;
            }
            public override bool disable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
            {
                messageOutput.AppendLine("Invoked example-plugin's disable method!");
                return true;
            }
        }
    }
}
