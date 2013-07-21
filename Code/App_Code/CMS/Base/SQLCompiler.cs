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
 *      Path:           /App_Code/CMS/Base/SQLCompiler.cs
 * 
 *      Change-Log:
 *                      2013-07-07      Created initial class.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 * 
 * *********************************************************************************************************************
 * A class for compiling large SQL statements.
 * *********************************************************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UberLib.Connector;

namespace CMS.Base
{
    /// <summary>
    /// A class for compiling large SQL statements.
    /// </summary>
    public class SQLCompiler
    {
        // Fields ******************************************************************************************************
        private Dictionary<string, string> attributes;
        // Methods - Constructors **************************************************************************************
        public SQLCompiler()
        {
            attributes = new Dictionary<string, string>();
        }
        // Methods - Compilation
        /// <summary>
        /// Compiles the attributes into an SQL insert statement, terminated with a semi-colon.
        /// </summary>
        /// <param name="table">The table the values are being inserted into.</param>
        /// <param name="identifierColumn">The identifier column to be returned; this can be null or empty to not be returned.</param>
        /// <returns>The compiled insert statement.</returns>
        public string compileInsert(string table, string identifierColumn)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("INSERT INTO ").Append(table).Append(" (");
            // Add attributes
            foreach (KeyValuePair<string, string> kv in attributes)
                buffer.Append(kv.Key).Append(",");
            buffer.Remove(buffer.Length - 1, 1).Append(") VALUES(");
            // Add values
            foreach (KeyValuePair<string, string> kv in attributes)
                buffer.Append("'").Append(SQLUtils.escape(kv.Value)).Append("',");
            buffer.Remove(buffer.Length - 1, 1).Append(");");
            if (identifierColumn != null && identifierColumn.Length > 0)
                buffer.Append(" SEELECT MAX(").Append(identifierColumn.ToString()).Append(") FROM ").Append(table).Append(";");
            return buffer.ToString();
        }
        /// <summary>
        /// Compiles the attributes into an SQL update statement, terminated with a semi-colon.
        /// </summary>
        /// <param name="table">The table being updated.</param>
        /// <param name="whereClauses">The where-clause part of the query; this is appended between 'WHERE ' and ';'. This can be left null or empty to be ignored.</param>
        /// <returns>The compiled update statement.</returns>
        public string compileUpdate(string table, string whereClauses)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("UPDATE ").Append(table).Append(" SET");
            // Add attributes with values
            foreach (KeyValuePair<string, string> kv in attributes)
                buffer.Append(kv.Key).Append("='").Append(SQLUtils.escape(kv.Value)).Append("',");
            buffer.Remove(buffer.Length - 1, 1);
            // Add where clause
            if (whereClauses != null && whereClauses.Length > 0)
                buffer.Append(" WHERE ").Append(whereClauses).Append(";");
            else
                buffer.Append(";");
            return buffer.ToString();
        }
        // Methods - Properties
        /// <summary>
        /// Gets/sets an attributes value.
        /// </summary>
        /// <param name="key">The column/key-name of the attribute.</param>
        /// <returns>The value of the attribute.</returns>
        public string this[string key]
        {
            get
            {
                return attributes.ContainsKey(key) ? attributes[key] : null;
            }
            set
            {
                attributes[key] = value;
            }
        }
    }
}