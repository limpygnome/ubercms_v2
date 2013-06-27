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
		// Setup request data (parse path, etc)
		Data data = new Data(Request, Response, Request.QueryString["path"]);
		// Start recording the time taken to process the request
		data.timingStart();
		// Create a connection to the database
		MySQL m = new MySQL();
		m.Settings_Host = "10.0.0.1";
		m.Settings_User = "arch";
		m.Settings_Database = "arch";
		m.Connect();
		m.Disconnect();
		// Lookup handler

		// Invoke handler

		// Stop timing the request
		data.timingEnd();
		// Format content

		// Dispose the request
		Response.Write("base path: '" + Core.BasePath + "'<br />");
		Response.Write("<br /><br />" + data["BENCH_MARK_MS"] + " m/s");
    }
}
