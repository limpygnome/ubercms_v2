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
using CMS.Base;

namespace CMS
{
    namespace Plugins
    {
        public class BasicSiteAuth : Plugin
        {
            public BasicSiteAuth(int pluginid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo)
                : base(pluginid, title, directory, state, handlerInfo)
            { }

            public override bool handler_handleRequest(Data data)
            {
                switch (data.PathInfo.ModuleHandler)
                {
                    case "login":

                        break;
                    case "register":

                        break;
                    case "account_recovery":

                        break;
                    case "my_account":

                        break;
                    case "account_log":

                        break;
                    case "members":

                        break;
                }
                return true;
            }
        }
    }
}