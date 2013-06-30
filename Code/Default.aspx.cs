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
 *      File:           Default.aspx.cs
 *      Path:           /Default.aspx.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-30      Finished initial class.
 * 
 * *****************************************************************************
 * The entry-point for clients to be served by the main CMS.
 * *****************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Diagnostics;

using UberLib.Connector;
using UberLib.Connector.Connectors;

using CMS.Base;
using CMS.Plugins;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
		// Check the status of the core
		if(Core.State != Core.CoreState.Running)
		{
			switch(Core.State)
			{
			case Core.CoreState.Failed:
				Response.Write("Core failure: '" + HttpUtility.HtmlEncode(Core.ErrorMessage) + "'.");
				break;
			case Core.CoreState.NotInstalled:
				Response.Redirect("/install");
				break;
			case Core.CoreState.Stopped:
				Response.Write("Core is not running.");
				break;
			default:
				Response.Write("Unknown core state; core is not running!");
				break;
			}
		}
		else
		{
			// Setup request data (parse path, database connection, etc)
			Data data = new Data(Request, Response, Request.QueryString["path"]);
            // Debug-mode options should go here (before timing)
#if DEBUG
            Core.Templates.reload(data.Connector);  // Reloads all of the cached templates
#endif
			// Start recording the time taken to process the request
			data.timingStart();
            try
            {
                // Invoke request-start handlers
                foreach (Plugin p in Core.Plugins.HandlerCache_RequestStart)
                    if (p.State == Plugin.PluginState.Enabled)
                        p.handler_requestStart(data);
                // Lookup and invoke possible request handler's
                bool handled = false;
                foreach (Plugin p in Core.Plugins.findRequestHandlers(data.PathInfo, data.Connector))
                {
                    if (p.State == Plugin.PluginState.Enabled && (handled = p.handler_handleRequest(data)))
                        break;
                }
                // Check the request was handled - else page not found!
                if (!handled)
                {
                    foreach (Plugin p in Core.Plugins.HandlerCache_PageNotFound)
                    {
                        if (p.State == Plugin.PluginState.Enabled && (handled = p.handler_handlePageNotFound(data)))
                            break;
                    }
                    // Set an error message if the page has still not been handled
                    if (!handled)
                        data["Content"] = "Page could not be found; no handler could serve the request!";
                }
                // Invoke request-end handlers
                foreach (Plugin p in Core.Plugins.HandlerCache_RequestEnd)
                    if (p.State == Plugin.PluginState.Enabled)
                        p.handler_requestEnd(data);
            }
            catch (Exception ex)
            {
                Core.Plugins.handlePageError(data, ex);
            }
            // Check if to specify page
            if(!data.isKeySet("page"))
                data["Page"] = Core.Templates.get(data.Connector, "core/page");
			// Stop timing the request
			data.timingEnd();
			// Render content and output to client
            if (data.OutputContent)
            {
                StringBuilder output = new StringBuilder(data["Page"]);
                Core.Templates.render(ref output, ref data);
                Response.Write(output.ToString());
            }
			// Dispose the request
			data.dispose();
		}
    }
}
