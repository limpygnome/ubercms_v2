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
 *      Path:           /App_Code/CMS/Base/Templates.cs
 * 
 *      Change-Log:
 *                      2013-06-25     Created initial class.
 * 
 * *****************************************************************************
 * Used to load, and possibly cache, HTML templates from the database; this class
 * also transforms the custom markup syntax.
 * *****************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UberLib.Connector;

namespace CMS
{
	namespace Base
	{
		public class Templates
		{
			// Fields **************************************************************************************************
			private bool loadFromCache;							// Indicates if to load templates from cache, else templates will be loaded straight from the database.
			private Dictionary<string,string> primaryCache;		// Used to cache copies of templates from the database for reduced I/O.
			// Methods - Constructors **********************************************************************************
			private Templates()
			{
				primaryCache = new Dictionary<string, string>();
			}
			// Methods
			public void render(ref StringBuilder text, ref Data data)
			{
				render(ref text, 0, 8, ref data);
			}
			private void render(ref StringBuilder text, int currTree, int treeMax, ref Data data)
			{
				// Find if-statements
				bool expressionValue;
				bool expressionValueNegated;
				string expression;
				MatchCollection elseMC;
				bool foundFlag;
				bool containsFlag;
				MatchCollection matches = Regex.Matches(text.ToString(), @"<!--IF:([a-zA-Z0-9!_\|\&]*)-->(.*?)<!--ENDIF(:\1)?-->", RegexOptions.Singleline);
				foreach (Match m in matches)
				{
					expression = m.Groups[1].Value;
					expressionValueNegated = expression.StartsWith("!");
					if (expression.Contains("|"))
					{
						foundFlag = false;
						// Iterate each flag inside of an expression like e.g. flag1|flag2|flag3 until we find it
						foreach (string s in expression.Split('|'))
						{
							expressionValueNegated = s.StartsWith("!");
							if ((expressionValueNegated && s.Length > 1) || (!expressionValueNegated && s.Length > 0))
							{
								containsFlag = data.isKeySet(expressionValueNegated ? s.Substring(1) : s);
								if ((expressionValueNegated ? !containsFlag : containsFlag))
								{
									foundFlag = true;
									break;
								}
							}
						}
						expressionValue = foundFlag;
					}
					else if (expression.Contains("&"))
					{
						foundFlag = true; // We leave this as true until a flag is not found, then we break the iteration since the expression is no longer valid
						foreach (string s in expression.Split('&'))
						{
							expressionValueNegated = s.StartsWith("!");
							if ((expressionValueNegated && s.Length > 1) || (!expressionValueNegated && s.Length > 0))
							{
								containsFlag = data.isKeySet(expressionValueNegated ? s.Substring(1) : s);
								if (!(expressionValueNegated ? !containsFlag : containsFlag))
								{
									foundFlag = false;
									break;
								}
							}
						}
						expressionValue = foundFlag;
					}
					else if((!expression.StartsWith("!") && expression.Length > 0) || expression.Length > 1)
						// Expression contains no other operators
						expressionValue = expressionValueNegated ? !data.isKeySet(expression.Substring(1)) : data.isKeySet(expression);
					else
						expressionValue = false;

					elseMC = Regex.Matches(m.Groups[2].Value, @"(.*?)<!--ELSE-->(.*$?)", RegexOptions.Singleline);
					if (elseMC.Count == 1)
					{
						if (expressionValue)
							text.Replace(m.Value, elseMC[0].Groups[1].Value);
						else
							text.Replace(m.Value, elseMC[0].Groups[2].Value);
					}
					else
					{
						if (expressionValue)
							text.Replace(m.Value, m.Groups[2].Value);
						else
							text.Replace(m.Value, string.Empty);
					}
				}
				// Find replacement tags
				foreach (Match m in Regex.Matches(text.ToString(), @"<!--([a-zA-Z0-9_]*)-->"))
					text.Replace(m.Value, data[m.Groups[1].Value]);
				// Check if to iterate again - check we haven't surpassed max tree-level and we found a match i.e. data changed
				currTree++;
				if (matches.Count > 0 && currTree < treeMax)
					render(ref text, currTree, treeMax, ref data);
			}
			// Methods - Accessors *************************************************************************************
			/// <summary>
			/// Fetches a template.
			/// </summary>
			/// <param name="path">Path.</param>
			public string get(Connector conn, string path)
			{
				return get(conn, path, "");
			}
			/// <summary>
			/// Fetches a template.
			/// </summary>
			/// <param name="path">Path.</param>
			/// <param name="alternative">Alternative string to be returned if the template does not exist.</param>
			public string get(Connector conn, string path, string alternative)
			{
				if(loadFromCache)
					return primaryCache.ContainsKey(path) ? primaryCache[path] : alternative;
				else
				{
					Result data = conn.Query_Read("SELECT html FROM cms_templates WHERE path='" + Utils.Escape(path) + "'");
					return data.Rows.Count == 1 ? data[0]["html"] : alternative;
				}
			}
			// Methods - Static ****************************************************************************************
			/// <summary>
			/// Creates a new instance of this class, configured and cache loaded (if loading from cache).
			/// </summary>
			public static Templates create()
			{
				Templates templates = new Templates();
				// Check if to cache templates
				if(templates.loadFromCache = Core.SettingsDisk["settings/templates/cache"].Equals("1"))
				{
					// Copy the templates from the database
					foreach(ResultRow template in Core.Connector.Query_Read("SELECT path, html FROM cms_templates"))
						templates.primaryCache.Add(template["path"], template["html"]);
				}
				return templates;
			}
		}
	}
}
