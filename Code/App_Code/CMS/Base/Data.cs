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
				connector = Core.createConnector(false);
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
			public void dispose()
			{
				connector.Disconnect();
			}
			// Methods - Accessors
			/// <summary>
			/// Indicates if a session (session in terms of processing this request) key has been defined.
			/// </summary>
			/// <returns>True if defined, false if not defined.</returns>
			/// <param name="key">Key.</param>
			public bool isKeySet(string key)
			{
				return session.ContainsKey(key);
			}
			// Methods - Properties
			public string this[string key]
			{
				get
				{
					return session.ContainsKey(key) ? session[key] : "";
				}
				set
				{
					session[key] = value;
				}
			}
			public HttpRequest Request
			{
				get
				{
					return request;
				}
			}
			public HttpResponse Response
			{
				get
				{
					return response;
				}
			}
			public PathInfo PathInfo
			{
				get
				{
					return pathInfo;
				}
			}
			public Connector Connector
			{
				get
				{
					return connector;
				}
			}
		}
	}
}