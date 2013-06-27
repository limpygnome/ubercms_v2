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
 * Used to parse url-rewriting/request data 
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
			public string moduleHandler;
			public string[] subDirs;
			// Constructors
			public PathInfo(string pathData)
			{
				parse(pathData);
			}
			// Methods
			public void parse(string pathData)
			{
				// Remove starting /
				if(pathData.Length > 0 && pathData[0] == '/')
					pathData = pathData.Substring(1);
				// Process tokens
				string[] exp = pathData.Split('/');
				if(exp.Length > 0)
				{
					moduleHandler = exp[0];
					subDirs = new string[exp.Length - 1];
					for(int i = 1; i < exp.Length; i++)
						subDirs[i-1] = exp[i];
				}
				else
					moduleHandler = "default_here";
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
		}
	}
}
