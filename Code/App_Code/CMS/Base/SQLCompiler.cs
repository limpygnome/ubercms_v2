﻿/*                       ____               ____________
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
 *                      2013-09-23      Finished initial class.
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
        private Dictionary<string, object> attributes;      // The attributes to form the query.
        private string updateAttribute = null;              // The attribute filter for update statements.
        private object updateValue = null;                  // The attribute value for filtering.
        // Methods - Constructors **************************************************************************************
        public SQLCompiler()
        {
            attributes = new Dictionary<string, object>();
        }
        // Methods - Compilation ***************************************************************************************
        /// <summary>
        /// Compiles the attributes into an SQL insert statement, terminated with a semi-colon.
        /// 
        /// Values are escaped.
        /// </summary>
        /// <param name="table">The table the values are being inserted into.</param>
        /// <returns>The compiled insert statement.</returns>
        public string compileInsert(string table)
        {
            return compileInsert(table, null, Connector.ConnectorType.Unknown);
        }
        /// <summary>
        /// Compiles the attributes into an SQL insert statement, terminated with a semi-colon.
        /// 
        /// Values are escaped.
        /// </summary>
        /// <param name="table">The table the values are being inserted into.</param>
        /// <param name="uniqueAttribute">The unique attribute from the operation to be returned; can be left null and ignored.</param>
        /// <param name="ctype">The type of connector to be used to execute the operation.</param>
        /// <returns>The compiled insert statement.</returns>
        public string compileInsert(string table, string uniqueAttribute, Connector.ConnectorType ctype)
        {
            if (attributes.Count == 0)
                return string.Empty;
            StringBuilder buffer = new StringBuilder();
            buffer.Append("INSERT INTO ").Append(table).Append(" (");
            // Add attributes
            foreach (KeyValuePair<string, object> kv in attributes)
                buffer.Append(kv.Key).Append(",");
            buffer.Remove(buffer.Length - 1, 1).Append(") VALUES(");
            // Add values
            foreach (KeyValuePair<string, object> kv in attributes)
                if (kv.Value == null)
                    buffer.Append("NULL,");
                else
                    buffer.Append("'").Append(SQLUtils.escape(kv.Value.ToString())).Append("',");
            buffer.Remove(buffer.Length - 1, 1).Append(");");
            if (uniqueAttribute != null)
            {
                switch (ctype)
                {
                    case Connector.ConnectorType.MySQL:
                        buffer.Append("SELECT LAST_INSERT_ID() AS ").Append(uniqueAttribute).Append(";");
                        break;
                    default:
                        throw new Exception("Unsupported Connector type!");
                }
            }
            return buffer.ToString();
        }
        /// <summary>
        /// Compiles the attributes into an SQL update statement, terminated with a semi-colon.
        /// 
        /// Values are escaped.
        /// </summary>
        /// <param name="table">The table being updated.</param>
        /// <returns>The compiled update statement.</returns>
        public string compileUpdate(string table)
        {
            return compileUpdate(table, null);
        }
        /// <summary>
        /// Compiles the attributes into an SQL update statement, terminated with a semi-colon.
        /// 
        /// Values are escaped.
        /// </summary>
        /// <param name="table">The table being updated.</param>
        /// <param name="whereClauses">The where-clause part of the query; this is appended between 'WHERE ' and ';'. This can be left null or empty to be ignored.</param>
        /// <returns>The compiled update statement.</returns>
        public string compileUpdate(string table, string whereClauses)
        {
            if (attributes.Count == 0)
                return string.Empty;
            StringBuilder buffer = new StringBuilder();
            buffer.Append("UPDATE ").Append(table).Append(" SET");
            // Add attributes with values
            foreach (KeyValuePair<string, object> kv in attributes)
                if (kv.Value == null)
                    buffer.Append(kv.Key).Append("=NULL,");
                else
                    buffer.Append(kv.Key).Append("='").Append(SQLUtils.escape(kv.Value.ToString())).Append("',");
            buffer.Remove(buffer.Length - 1, 1);
            // Add where clause
            if (whereClauses != null)
                buffer.Append(" WHERE ").Append(whereClauses);
            else if (updateAttribute != null)
                buffer.Append("WHERE ").Append(updateAttribute).Append("=").Append(updateValue == null ? "NULL" : "'" + SQLUtils.escape(updateValue.ToString()) + "'");
            buffer.Append(";");
            return buffer.ToString();
        }
        /// <summary>
        /// Creates a prepared statement for an insert using the attributes and executes it.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="table">The table being inserted to.</param>
        /// <returns>The result returned by the query.</returns>
        public Result executeInsert(Connector conn, string table)
        {
            return executeInsert(conn, table, null);
        }
        /// <summary>
        /// Creates a prepared statement for an insert using the attributes and executes it.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="table">The table being inserted to.</param>
        /// <param name="uniqueAttribute">The unique attribute from the operation to be returned; can be left null and ignored.</param>
        /// <returns>The result returned by the query; may return null.</returns>
        public Result executeInsert(Connector conn, string table, string uniqueAttribute)
        {
            if (attributes.Count == 0)
                return null;
            // Switch for connector's both capable and not capable of executing a prepared statement for this type of operation
            switch(conn.Type)
            {
                case Connector.ConnectorType.MySQL:
                    // Create prepared statement
                    PreparedStatement ps = new PreparedStatement();
                    // Build query
                    {
                        StringBuilder k = new StringBuilder();
                        StringBuilder v = new StringBuilder();
                        k.Append("INSERT INTO ").Append(table).Append(" (");
                        v.Append(") VALUES(");
                        foreach (KeyValuePair<string, object> kv in this.attributes)
                        {
                            k.Append(kv.Key).Append(",");
                            v.Append("?").Append(kv.Key).Append(",");
                        }
                        k.Remove(k.Length - 1, 1).Append(v.Remove(v.Length - 1, 1).ToString()).Append(");");
                        if (uniqueAttribute != null)
                        {
                            switch (conn.Type)
                            {
                                case Connector.ConnectorType.MySQL:
                                    k.Append("SELECT LAST_INSERT_ID() AS ").Append(uniqueAttribute).Append(";");
                                    break;
                                default:
                                    throw new Exception("Unsupported Connector type!");
                            }
                        }
                        ps.Query = k.ToString();
                    }
                    ps.Parameters = this.attributes;
                    // Execute prepared statement
                    return conn.queryRead(ps);
                default:
                    return conn.queryRead(compileInsert(table, uniqueAttribute, conn.Type));
            }
        }
        /// <summary>
        /// Creates a prepared statement for an update with the attributes and executes it.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="table">Table being updated.</param>
        public void executeUpdate(Connector conn, string table)
        {
            executeUpdate(conn, table, null);
        }
        /// <summary>
        /// Creates a prepared statement for an update with the attributes and executes it.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="table">Table being updated.</param>
        /// <param name="whereClauses">The where-clause part of the query; this is appended between 'WHERE ' and ';'. This can be left null or empty to be ignored.</param>
        public void executeUpdate(Connector conn, string table, string whereClauses)
        {
            if (attributes.Count == 0)
                return;
            // Switch for connector's both capable and not capable of executing a prepared statement for this type of operation
            switch(conn.Type)
            {
                case Connector.ConnectorType.MySQL:
                    // Create prepared statement
                    PreparedStatement ps = new PreparedStatement();
                    ps.Parameters = this.attributes;
                    // Build query
                    {
                        StringBuilder s = new StringBuilder();
                        s.Append("UPDATE ").Append(table).Append(" SET ");
                        foreach (KeyValuePair<string, object> kv in this.attributes)
                            s.Append(kv.Key).Append("=?").Append(kv.Key).Append(",");
                        s.Remove(s.Length - 1, 1);
                        if (whereClauses != null)
                            s.Append(" WHERE ").Append(whereClauses);
                        else if(updateAttribute != null)
                        {
                            s.Append(" WHERE ").Append(updateAttribute).Append("=?ua_").Append(updateAttribute);
                            ps["ua_" + updateAttribute] = updateValue;
                        }
                        ps.Query = s.Append(";").ToString();
                    }
                    // Execute prepared statement
                    conn.queryExecute(ps);
                    break;
                default:
                    conn.queryExecute(compileUpdate(table, whereClauses));
                    break;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Gets/sets an attributes value.
        /// </summary>
        /// <param name="key">The column/key-name of the attribute.</param>
        /// <returns>The value of the attribute. Can be null.</returns>
        public object this[string key]
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
        /// <summary>
        /// The attribute filter for update-statements. For example `UPDATE ... WHERE key='';`, this property is for
        /// the key/attribute part.
        /// 
        /// If a where-clause is specified, this is ignored.
        /// </summary>
        public string UpdateAttribute
        {
            get
            {
                return updateAttribute;
            }
            set
            {
                updateAttribute = value;
            }
        }
        /// <summary>
        /// The attribute filter value for update-statements. For example `UPDATE ... WHERE key='value';`, this property
        /// is for the 'value' (without quotations) part.
        /// 
        /// If a where-clause is specified, this is ignored.
        /// </summary>
        public object UpdateValue
        {
            get
            {
                return updateValue;
            }
            set
            {
                updateValue = value;
            }
        }
        /// <summary>
        /// The total number of attributes.
        /// </summary>
        public int Count
        {
            get
            {
                return attributes.Count;
            }
        }
    }
}