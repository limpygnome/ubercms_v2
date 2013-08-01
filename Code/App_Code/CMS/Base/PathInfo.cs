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
 *      Path:           /App_Code/CMS/Base/PathInfo.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Finished initial class.
 *                      2013-07-01      Modified this property to something more sensible (module handler at index zero).
 *                      2013-07-05      Fixed this property bug.
 *                      2013-07-06      Fixed tailing slash bug where it would create an empty token.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 * 
 * *********************************************************************************************************************
 * Used to parse url-rewriting/request data. Plugins are invoked based on either the first directory in the URL, the
 * module-handler variable, being matched or the full-path. Thus a plugin A could own /exmaple and plugin B could own
 * /exmaple/test by having a higher priority.
 * *********************************************************************************************************************
 */
using System;
using System.Text;

namespace CMS.Base
{
	public class PathInfo
	{
		// Fields ******************************************************************************************************
		private string 		moduleHandler;          // The top-directory of the request.
		private string[] 	subDirs;                // Subsequent directories of the request, in order from 0 to n.
		private string 		fullPath;               // The full rebuilt path of the request (excludes query-string data).
		// Methods - Constructors **************************************************************************************
		public PathInfo(string pathData)
		{
			parse(pathData);
		}
		// Methods *****************************************************************************************************
        /// <summary>
        /// Parses the current request for its request path.
        /// </summary>
        /// <param name="pathData">The data for the current request path.</param>
		public void parse(string pathData)
		{
            // Check against null reference
            if (pathData == null)
            {
                moduleHandler = Core.DefaultHandler;
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
                    int totaltokens = exp.Length == 0 ? 0 : exp[exp.Length - 1].Length > 0 ? exp.Length : exp.Length - 1; // Tailing slash empty token protection
                    moduleHandler = totaltokens == 0 ? string.Empty : exp[0];
                    subDirs = new string[totaltokens > 0 ? totaltokens - 1 : 0];
                    for (int i = 1; i < totaltokens; i++)
                        subDirs[i - 1] = exp[i];
                    // Check against empty paths
                    if (moduleHandler.Length == 0)
                    {
                        moduleHandler = Core.SettingsDisk["settings/core/default_handler"].get<string>();
                        if(subDirs.Length != 0) // Protection against invalid paths
                            subDirs = new string[0];
                    }
                }
                else
                    moduleHandler = Core.SettingsDisk["settings/core/default_handler"].get<string>();
            }
			// Build full-path
			StringBuilder sb = new StringBuilder();
			sb.Append(moduleHandler).Append("/");
			foreach(string s in subDirs)
				sb.Append(s).Append("/");
			sb.Remove(sb.Length - 1, 1);
			fullPath = sb.ToString();
		}
		// Methods - Accessors *****************************************************************************************
        /// <summary>
        /// Returns debug information about the current path.
        /// </summary>
        /// <returns></returns>
		public string getPathInfo()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Handler: '" + moduleHandler + "' : ");
			for(int i = 0; i < subDirs.Length; i++)
			{
				sb.Append(" sub-dir [" + i + "] '" + subDirs[i] + "'");
				if(i < subDirs.Length -1)
					sb.Append(" : ");
			}
			sb.Append(" : sub-dir count: '" + subDirs.Length + "'");
			return sb.ToString();
		}
		// Methods - Properties ****************************************************************************************
        /// <summary>
        /// Gets the directory at the specified index; returns null if the directory cannot be found.
        /// 0 = module handler.
        /// 1...n = sub-dir.
        /// </summary>
        /// <param name="index">The directory index of the request.</param>
        /// <returns>The directory's name/alias.</returns>
        public string this[int index]
        {
            get
            {
                if (index > this.subDirs.Length)
                    return null;
                else if (index == 0)
                    return moduleHandler;
                else
                    return this.subDirs[index-1];
            }
        }
        /// <summary>
        /// The top-directory of the request. This is called the module-handler because some plugins can handle
        /// all requests for a top-directory, regardless of sub-directories.
        /// </summary>
		public string ModuleHandler
		{
			get
			{
				return this.moduleHandler;
			}
		}
        /// <summary>
        /// The sub-directories of the request.
        /// </summary>
		public string[] SubDirectories
		{
			get
			{
				return this.subDirs;
			}
		}
        /// <summary>
        /// The full-path of the request, excludes query-string data. This is rebuilt from the parsed data for
        /// safety.
        /// </summary>
		public string FullPath
		{
			get
			{
				return this.fullPath;
			}
		}
	}
}
