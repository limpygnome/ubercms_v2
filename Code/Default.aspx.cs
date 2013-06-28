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
 *                      2013-06-25     Created initial class.
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
				Response.Write("Core failure: '" + Core.ErrorMessage + "'.");
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
			// Start recording the time taken to process the request
			data.timingStart();
			// Load base template
			data["Page"] = Core.Templates.get(data.Connector, "core/page");
			// Lookup handler
            
			// Invoke handler

			// Stop timing the request
			data.timingEnd();
			// Format content and output to client
			StringBuilder output = new StringBuilder(data["Page"]);
			Core.Templates.render(ref output, ref data);
			Response.Write(output.ToString());
			// Dispose the request
			data.dispose();
		}
    }
}
