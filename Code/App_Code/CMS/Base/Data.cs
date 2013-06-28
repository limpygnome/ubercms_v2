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
            private bool                            outputContent;      // Indicates if the content should be written to the client.
			private PathInfo 						pathInfo;			// Information about the request path.
			private HttpRequest 					request;			// ASP.NET request object.
			private HttpResponse 					response;			// ASP.NET response object.
			private Stopwatch						stopwatch;			// Used to measure the current request's process speed.
			private Dictionary<string, string>		variables;			// Used to store request variables.
			private Connector						connector;			// The database connector.
			// Methods - Constructors
			public Data(HttpRequest request, HttpResponse response, string pathData)
			{
				this.pathInfo = new PathInfo(pathData);
				this.request = request;
				this.response = response;
				stopwatch = new Stopwatch();
				this.variables = new Dictionary<string, string>();
                this.outputContent = true;
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
                variables["BENCH_MARK_MS"] = stopwatch.ElapsedMilliseconds.ToString();
                variables["BENCH_MARK_S"] = ((float)stopwatch.ElapsedMilliseconds / 1000.0f).ToString();
			}
			public void dispose()
			{
				connector.Disconnect();
			}
			// Methods - Accessors
			/// <summary>
			/// Indicates if a variable has been defined.
			/// </summary>
			/// <returns>True if defined, false if not defined.</returns>
			/// <param name="key">Key.</param>
			public bool isKeySet(string key)
			{
				return variables.ContainsKey(key);
			}
			// Methods - Properties
			public string this[string key]
			{
				get
				{
                    return variables.ContainsKey(key) ? variables[key] : "";
				}
				set
				{
                    variables[key] = value;
				}
			}
            public Dictionary<string, string> Variables
            {
                get
                {
                    return variables;
                }
            }
            public bool OutputContent
            {
                get
                {
                    return outputContent;
                }
                set
                {
                    outputContent = value;
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