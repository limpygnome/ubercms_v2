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
 *                      2013-08-01      ThreadAbortException ignored due to being thrown by Response.Redirect.
 *                      2013-08-22      Added support for new core state (starting and stopping).
 *                                      Added quick-install for debug-mode (for development purposes).
 *                      2013-09-19      Fixed case-sensitive bug.
 * 
 * *********************************************************************************************************************
 * The entry-point for clients to be served by the main CMS.
 * *********************************************************************************************************************
 */
using CMS.Base;
using CMS.Plugins;
using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Diagnostics;
using System.Threading;
using UberLib.Connector;
using UberLib.Connector.Connectors;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
#if DEBUG
        // Check if we want to fast-install the CMS (using pre-existing settings)
        PathInfo pi = new PathInfo(Request);
        if (pi.ModuleHandler == "debug_install")
        {
            // Start only the absolute base of the CMS
            Core.start(true);
            // Check the base has started
            if (Core.State == Core.CoreState.Failed)
            {
                Response.Write("Debug install cannot continue; base core failure occurred: " + Core.ErrorMessage);
                return;
            }
            // Prepare message output and base path
            StringBuilder messageOutput = new StringBuilder();
            // Load settings from disk
            Settings settings = Settings.loadFromDisk(Core.CmsConfigPath);
            // Create connector
            Connector conn = Core.connectorCreate(true, ref settings);
            // Install CMS database
            if (!BaseUtils.executeSQL(Core.BasePath + "/installer/sql/mysql/install.sql", conn, ref messageOutput))
            {
                Response.Write("Debug install cannot continue; failed to execute base SQL!");
                return;
            }
            // Restart core to load as usual
            Core.stop();
            Core.start();
            // Check the core has started
            if (Core.State != Core.CoreState.Started)
            {
                Response.Write("Debug install cannot continue; core has not started (state: " + Core.State.ToString() + "): " + (Core.ErrorMessage ?? "(no error message)"));
                return;
            }
            // Install core templates
            if (!Core.Templates.install(conn, null, Core.BasePath + "/installer/templates", ref messageOutput))
            {
                Response.Write("Debug install cannot continue; failed to install templates!");
                return;
            }
            // Install package developer
            Plugin p = null;
            if (Core.Plugins.createFromDirectory(conn, Core.BasePath + "/App_Code/CMS/Plugins/Package Developer", ref p, ref messageOutput) && p != null)
            {
                Core.Plugins.install(conn, p, ref messageOutput);
                Core.Plugins.enable(conn, p, ref messageOutput);
            }
            // Output status
            Response.Write("CMS fast-install called successful! Output:<br />");
            Response.Write(messageOutput.ToString());
            return;
        }
#endif
		// Check the status of the core
		if(Core.State != Core.CoreState.Started)
		{
			switch(Core.State)
			{
			case Core.CoreState.Failed:
				Response.Write("The website failed to start; error-message: '" + HttpUtility.HtmlEncode(Core.ErrorMessage) + "'.");
				break;
			case Core.CoreState.NotInstalled:
				Response.Redirect("/install");
				break;
            case Core.CoreState.Stopping:
			case Core.CoreState.Stopped:
				Response.Write("The website is currently offline.");
				break;
            case Core.CoreState.Starting:
                Response.Write("The website is still starting, please try your request again shortly...");
                break;
			default:
				Response.Write("Unknown website/core state; website is not running!");
				break;
			}
		}
		else
		{
			// Setup request data model (used for carrying data between plugins)
			Data data = new Data(Request, Response);
            // Debug-mode options should go here (before timing)
#if DEBUG
            Core.Templates.reload(data.Connector);  // Reloads all of the cached templates
#endif
			// Start recording the time taken to process the request
			data.timingStart();
            // Begin request
            try
            {
                // Invoke request-start handlers
                foreach (Plugin p in Core.Plugins.HandlerCache_RequestStart)
                    if (p.State == Plugin.PluginState.Enabled)
                        p.handler_requestStart(data);
                // Lookup and invoke possible request handler's
                bool handled = false;
                foreach (Plugin p in UrlRewriting.findRequestHandlers(data.PathInfo, data.Connector))
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
                    {
#if DEBUG
                        data["Content"] = "<h2>Page Not Found</h2><p>Page could not be found; no handler could serve the request!</p><h3>Debug Information</h3><p>Full-path: '" + data.PathInfo.FullPath + "'<br />Module Handler: '" + data.PathInfo.ModuleHandler + "'</p>";
#else
                        data["Content"] = "<h2>Page Not Found</h2><p>Page could not be found; no handler could serve the request!</p>";
#endif
                        Response.StatusCode = 404;
                    }
                }
                // Invoke request-end handlers
                foreach (Plugin p in Core.Plugins.HandlerCache_RequestEnd)
                    if (p.State == Plugin.PluginState.Enabled)
                        p.handler_requestEnd(data);
            }
            catch (ThreadAbortException)
            { return; }
            catch (Exception ex)
            {
                if (Core.Plugins != null)
                    Core.Plugins.handlePageException(data, ex);
                else
                {
                    Response.Write("Core failure occurred during request; refresh for message.");
                    Response.End();
                }
            }
            // Check if to specify page
            if(!data.isKeySet("Page"))
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
