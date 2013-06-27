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
 *      File:           Data.cs
 *      Path:           /App_Code/CMS/Core/Data.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 * 
 * *****************************************************************************
 * Used for passing data between the controller and plugins.
 * *****************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using CMS.Base;
using UberLib.Connector;

namespace CMS
{
	namespace Base
	{
		public class Data
		{
			// Variables
			private PathInfo 						pathInfo;			// Information about the request path.
			private HttpRequest 					request;			// ASP.NET request object.
			private HttpResponse 					response;			// ASP.NET response object.
			private Stopwatch						stopwatch;			// Used to measure the current request's process speed.
			private Dictionary<string, string>		session;			// Used to store key-value pair's for the current request session.
			private Connector						connector;			// The database connector.
			// Methods - Constructors
			public Data(HttpRequest request, HttpResponse response, string pathData)
			{
				this.pathInfo = new PathInfo(pathData);
				this.request = request;
				this.response = response;
				stopwatch = new Stopwatch();
				this.session = new Dictionary<string, string>();
			}
			// Methods
			public void timingStart()
			{
				stopwatch.Start();
			}
			public void timingEnd()
			{
				stopwatch.Stop();
				session["BENCH_MARK_MS"] = stopwatch.ElapsedMilliseconds.ToString();
				session["BENCH_MARK_S"] = ((float)stopwatch.ElapsedMilliseconds / 1000.0f).ToString();
			}
			// Methods - Properties
			public string this[string key]
			{
				get
				{
					return session[key];
				}
				set
				{
					session[key] = value;
				}
			}
			// Methods - Accessors
			public HttpRequest getRequest()
			{
				return request;
			}
			public HttpResponse getResponse()
			{
				return response;
			}
			public PathInfo getPathInfo()
			{
				return pathInfo;
			}
		}
	}
}