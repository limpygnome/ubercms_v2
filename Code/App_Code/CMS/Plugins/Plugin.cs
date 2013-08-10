using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS
{
	namespace Plugins
	{
		public abstract class Plugin
		{
			// Enums ***************************************************************************************************
			public enum PluginState
			{
				NotInstalled = 0,
				Disabled = 1,
				Enabled = 2
			}
			// Fields **************************************************************************************************
			private int pluginid;
			private PluginState state;
			// Methods - Abstract - State ******************************************************************************
			/// <summary>
			/// Invoked when the plugin is to be enabled; no checking of the plugin state is required. Return
			/// true if successful or false if the plugin cannot be enabled.
			/// </summary>
			/// <param name="pluginid">Pluginid.</param>
			/// <param name="conn">Database connector.</param>
			/// <param name="errorMessage">Error message output.</param>
			public bool enable(Connector conn, ref string errorMessage)
			{
				errorMessage = "Not implemented.";
				return false;
			}
			/// <summary>
			/// Invoked when the plugin is to be disabled; no checking of the plugin state is required. Return true
			/// if successful or false if the plugin cannot be disabled.
			/// </summary>
			/// <param name="pluginid">Pluginid.</param>
			/// <param name="conn">Database connector.</param>
			/// <param name="errorMessage">Error message output.</param>
			public bool disable(Connector conn, ref string errorMessage)
			{
				errorMessage = "Not implemented.";
				return false;
			}
			public bool uninstall(Connector conn, ref string errorMessage)
			{
				errorMessage = "Not implemented.";
				return false;
			}
			public bool install(Connector conn, ref string errorMessage)
			{
				errorMessage = "Not implemented.";
				return false;
			}
			// Methods - Abstract - Handlers - CMS *********************************************************************
			public bool handler_cmsStart(Connector conn)
			{
				return true;
			}
			public void handler_cmsEnd(Connector conn)
			{
			}
			public void handler_cmsCycle(Data data)
			{
			}
			// Methods - Abstract - Handlers - Requests ****************************************************************
			public void handler_requestStart(Data data)
			{
			}
			public void handler_requestEnd(Data data)
			{
			}
			public bool handler_handleRequest(Data data)
			{
				return false;
			}
			public bool handler_handlePageError(Data data)
			{
				return false;
			}
			public bool handler_handlePageNotFound(Data data)
			{
				return false;
			}
			// Methods - Properties
			public int PluginID
			{
				get
				{
					return pluginid;
				}
			}
			public PluginState State 
			{
				get
				{
					return state;
				}
			}
		}
	}
}