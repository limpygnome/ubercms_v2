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
 *      File:           Plugins.cs
 *      Path:           /App_Code/CMS/Core/Plugins.cs
 * 
 *      Change-Log:
 *                      2013-06-25     Created initial class.
 * 
 * *****************************************************************************
 * Used to store and interact with plugins.
 * *****************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using CMS.Plugins;
using UberLib.Connector;

namespace CMS
{
	namespace Base
	{
		public class Plugins
		{
			// Fields
			private Dictionary<int,Plugin> plugins;			// Map of pluginid to plugin.
			private Plugin[] pluginsPageNotFound;			// Cache of plugins capable of handling a page not found
			private Plugin[] pluginsPageError;				// Cache of plugins capable of handling a page error.
			// Methods - Constructors
			private Plugins()
			{
				this.plugins = new Dictionary<int, Plugin>();
			}
			// Methods
			/// <summary>
			/// Finds the available plugins to serve a request. Returns null if a consistency issue has occurred
			/// (a plugin cannot be found).
			/// </summary>
			/// <returns>The request handlers.</returns>
			/// <param name="pathInfo">Path info.</param>
			/// <param name="conn">Conn.</param>
			public Plugin[] findRequestHandlers(PathInfo pathInfo, Connector conn)
			{
				Result r = conn.Query_Read("SELECT DISTINCT pluginid FROM cms_urlrewriting WHERE path='" + Utils.Escape(pathInfo.FullPath) + "' OR path ='" + Utils.Escape(pathInfo.ModuleHandler) + "' ORDER BY priority DESC");
				Plugin[] result = new Plugin[r.Rows.Count];
				int c = 0;
				Plugin p;
				foreach(ResultRow plugin in r)
				{
					p = this[int.Parse(plugin["pluginid"])];
					if(p != null)
						result[c++] = p;
					else
						return null;
				}
				return result;
			}
			public Plugin[] getPageNotFoundHandlers()
			{
				return pluginsPageNotFound;
			}
			public Plugin[] getPageErrorHandlers()
			{
				return pluginsPageError;
			}
			// Methods - Accessors
			public Plugin this[int pluginid]
			{
				get
				{
					return plugins[pluginid];
				}
			}
			// Methods - Static
			public static Plugins load(Connector conn, ref string errorMessage)
			{
				try
				{
					Plugins plugins = new Plugins();

					return plugins;
				}
				catch(Exception ex)
				{
					errorMessage = "Unknown exception when loading plugins '" + ex.Message + "'!";
				}
				return null;
			}
		}
	}
}
