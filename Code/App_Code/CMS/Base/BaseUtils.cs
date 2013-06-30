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
 *      File:           BaseUtils.cs
 *      Path:           /CMS/Base/BaseUtils.cs
 * 
 *      Change-Log:
 *                      2013-06-25      Created initial class.
 *                      2013-06-29      Updated header and namespace.
 * 
 * *********************************************************************************************************************
 * A utility class of commonly used code.
 * *********************************************************************************************************************
 */
using System;
using System.IO;
using System.Text;
using UberLib.Connector;

namespace CMS
{
    namespace Base
    {
        public static class BaseUtils
        {
            /// <summary>
            /// Executes a multi-line SQL file on the provided connector.
            /// </summary>
            /// <param name="path">The path of the file to execute.</param>
            /// <param name="conn">The connector on-which to execute the file's data/SQL.</param>
            /// <returns></returns>
            public static string executeSQL(string path, Connector conn)
            {
                try
                {
                    if (!File.Exists(path))
                        throw new Exception("SQL script '" + path + "' could not be found!");
                    else
                    {
                        StringBuilder statements = new StringBuilder();
                        // Build the new list of statements to be executed by stripping out any comments
                        string data = File.ReadAllText(path).Replace("\r", string.Empty);
                        int commentIndex;
                        foreach (string line in data.Split('\n'))
                        {
                            commentIndex = line.IndexOf("--");
                            if (commentIndex == -1)
                                statements.Append(line).Append("\r\n");
                            else if (commentIndex < line.Length)
                                statements.Append(line.Substring(0, commentIndex)).Append("\r\n");
                        }
                        // Execute the statements
                        conn.Query_Execute(statements.ToString());
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return "Failed to execute SQL file '" + path + "' - " + ex.Message + " - " + ex.GetBaseException().Message + "!";
                }
            }
        }
    }
}

