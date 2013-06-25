using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using CMS.Core;

namespace CMS
{
	namespace Core
	{
		public class Data
		{
			// Variables
			private PathInfo 						pathInfo;
			private HttpRequest 					request;
			private HttpResponse 					response;
			private Stopwatch						stopwatch;
			private Dictionary<string, string>		session;
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