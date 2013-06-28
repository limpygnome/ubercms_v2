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
 *      File:           PathInfo.cs
 *      Path:           /App_Code/CMS/Core/PathInfo.cs
 * 
 *      Change-Log:
 *                      2013-06-25     Created initial class.
 * 
 * *****************************************************************************
 * Used to parse url-rewriting/request data. Plugins are invoked based on either
 * the first directory in the URL, the module-handler variable, being matched or
 * the full-path. Thus a plugin A could own /exmaple and plugin B could own
 * /exmaple/test by having a higher priority.
 * *****************************************************************************
 */
using System;
using System.Text;

namespace CMS
{
	namespace Base
	{
		public class PathInfo
		{
			// Variables
			private string 		moduleHandler;
			private string[] 	subDirs;
			private string 		fullPath;
			// Constructors
			public PathInfo(string pathData)
			{
				parse(pathData);
			}
			// Methods
			public void parse(string pathData)
			{
                // Check against null reference
                if (pathData == null)
                {
                    moduleHandler = Core.SettingsDisk["settings/core/default_handler"];
                    subDirs = new string[0];
                }
                else
                {
                    // Remove starting /
                    if (pathData.Length > 0 && pathData[0] == '/')
                        pathData = pathData.Substring(1);
                    // Process tokens
                    string[] exp = pathData.Split('/');
                    if (exp.Length > 0)
                    {
                        moduleHandler = exp[0];
                        subDirs = new string[exp.Length - 1];
                        for (int i = 1; i < exp.Length; i++)
                            subDirs[i - 1] = exp[i];
                        // Check against empty paths
                        if (moduleHandler.Length == 0)
                        {
                            moduleHandler = Core.SettingsDisk["settings/core/default_handler"];
                            if(subDirs.Length != 0) // Protection against invalid paths
                                subDirs = new string[0];
                        }
                    }
                    else
                        moduleHandler = Core.SettingsDisk["settings/core/default_handler"];
                }
				// Build full-path
				StringBuilder sb = new StringBuilder();
				sb.Append(moduleHandler).Append("/");
				foreach(string s in subDirs)
					sb.Append(s).Append("/");
				sb.Remove(sb.Length - 1, 1);
				fullPath = sb.ToString();
			}
			// Methods - Accessors
			public string getPathInfo()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Handler: '" + moduleHandler + "' : ");
				for(int i = 0; i < subDirs.Length; i++)
				{
					sb.Append(" sub-dir [" + i + "] '" + subDirs[i] + "'");
					if(i < subDirs.Length -1)
						sb.Append(",");
				}
				sb.Append(" : sub-dir count: '" + subDirs.Length + "'");
				return sb.ToString();
			}
			// Methods - Properties
			public string ModuleHandler
			{
				get
				{
					return this.moduleHandler;
				}
			}
			public string[] SubDirectories
			{
				get
				{
					return this.subDirs;
				}
			}
			public string FullPath
			{
				get
				{
					return this.fullPath;
				}
			}
		}
	}
}
