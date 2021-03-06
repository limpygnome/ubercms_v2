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
 *      Path:           /App_Code/CMS/Plugins/Basic 404 Page/Basic404Page.cs
 * 
 *      Change-Log:
 *                      2013-07-01      Created initial class.
 * 
 * *********************************************************************************************************************
 * A very basic page-not-found plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Plugins
{
    /// <summary>
    /// A very basic page-not-found plugin.
    /// </summary>
    public class Basic404Page : Plugin
    {
        public Basic404Page(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        { }
        public override bool install(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Register handlers
            HandlerInfo.PageNotFound = true;
            HandlerInfo.save(conn);
            return true;
        }
        public override bool uninstall(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return true;
        }
        public override bool enable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Install content
            BaseUtils.contentInstall(PathContent, Core.PathContent, false, ref messageOutput);
            // Install templates
            Core.Templates.install(conn, this, PathTemplates, ref messageOutput);
            return true;
        }
        public override bool disable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Remove content
            BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput);
            // Remove templates
            Core.Templates.uninstall(conn, this, ref messageOutput);
            return true;
        }
        public override bool handler_handlePageNotFound(Data data)
        {
            data["Title"] = "404 - Page Not Found";
            data["Content"] = Core.Templates.get(data.Connector, "basic404page/page_not_found");
            data["404_PATH"] = data.PathInfo.FullPath;
            data.Response.StatusCode = 404;
            return true;
        }
    }
}