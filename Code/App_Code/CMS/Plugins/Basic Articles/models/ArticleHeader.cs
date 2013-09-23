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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/models/ArticleHeader.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model to represent the header-data of an article's cached rendered text.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A model to represent the header-data of an article's cached rendered text.
    /// </summary>
    public class ArticleHeader
    {
        // Fields ******************************************************************************************************
        private bool            modified;   // Indicates if the model has been modified.
        private List<string>    lines;      // The lines which make-up the article's header-data.
        private string          hash;       // The hash of the compiled lines, used as an identifier for linking the same header data to multiple articles.
                                            // This is also null if not persisted.
        // Methods - Constructors **************************************************************************************
        public ArticleHeader()
        {
            this.hash = null;
            this.lines = new List<string>();
            this.modified = false;
        }
        /// <summary>
        /// Creates a new unpersisted article-header model.
        /// </summary>
        /// <param name="data">Header-data.</param>
        public ArticleHeader(string data)
        {
            this.hash = null;
            this.lines = new List<string>();
            this.modified = true;
            addMultiple(data);
        }
        /// <summary>
        /// Creates a article-header model for already-persisted data.
        /// </summary>
        /// <param name="data">Header-data.</param>
        /// <param name="hash">Hash of data.</param>
        public ArticleHeader(string data, string hash)
        {
            this.hash = hash;
            this.lines = new List<string>();
            this.modified = false;
            addMultiple(data);
        }
        // Methods - Persistence ***************************************************************************************
        /// <summary>
        /// Persists the article header-data.
        /// 
        /// Note: this should be invoked after an article has been persisted.
        /// Note 2: this uses a transaction; thus this should not be invoked within a transaction.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidArticle">The identifier of the article.</param>
        /// <returns>True = persisted, false = no changes/not persisted.</returns>
        public bool persist(Connector conn, UUID uuidArticle)
        {
            lock (this)
            {
                if (!modified)
                    return false;
                // Generate new hash
                string data = compile();
                string hashNew = generateHash(data);
                // Check the hash's are not the same - else we have no work to do!
                if (hashNew == hash)
                    return true;
                conn.queryExecute("BEGIN;");
                // Check if to attempt to insert new header-data
                PreparedStatement ps;
                if (hashNew != null)
                {
                    // Check it doesn't already exist
                    ps = new PreparedStatement("SELECT COUNT('') AS count FROM ba_article_headerdata WHERE hash=?hash;");
                    ps["hash"] = hashNew;
                    if (int.Parse(conn.queryRead(ps)[0]["count"]) == 0)
                    {
                        ps = new PreparedStatement("INSERT INTO ba_article_headerdata (hash, headerdata) VALUES(?hash, ?headerdata);");
                        ps["hash"] = hashNew;
                        ps["headerdata"] = compile();
                        conn.queryExecute(ps);
                    }
                }
                // Update the article
                ps = new PreparedStatement("UPDATE ba_article SET headerdata_hash=?hash WHERE uuid_article=?uuid_article;");
                ps["hash"] = hashNew;
                ps["uuid_article"] = uuidArticle.Bytes;
                conn.queryExecute(ps);
                // Delete old data
                if (hash != null && hash.Length > 0)
                {
                    ps = new PreparedStatement("DELETE hd FROM ba_article_headerdata AS hd WHERE hash=?hash AND (SELECT COUNT('') FROM ba_article WHERE headerdata_hash=hd.hash) = 0;");
                    ps["hash"] = hash;
                    conn.queryExecute(ps);
                }
                // Commit - done!
                conn.queryExecute("COMMIT;");
                hash = hashNew;
                return true;
            }
        }
        /// <summary>
        /// Unpersists the header-data if no articles are using it.
        /// 
        /// Note: this model will appear unpersisted, regardless of being persisted on the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            lock (this)
            {
                PreparedStatement ps = new PreparedStatement("DELETE FROM ba_article_headerdata WHERE hash=?hash AND (SELECT COUNT('') FROM ba_article WHERE headerdata_hash=?hash) = 0;");
                ps["hash"] = hash;
                conn.queryExecute(ps);
                hash = null;
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Add a single line to the header.
        /// 
        /// If the line already exists, it will not be re-added.
        /// </summary>
        /// <param name="line">The line to be added.</param>
        public void add(string line)
        {
            lock (this)
            {
                if (line != null)
                {
                    line = line.Trim();
                    if (!lines.Contains(line))
                    {
                        lines.Add(line);
                        modified = true;
                    }
                }
            }
        }
        /// <summary>
        /// Adds all of the lines from another article header to this collection, if they do not already exist.
        /// </summary>
        /// <param name="header">The lines from the header to add.</param>
        public void add(ArticleHeader header)
        {
            lock (this)
            {
                if (header != null)
                {
                    foreach (string line in header.lines)
                    {
                        if (!lines.Contains(line))
                        {
                            lines.Add(line);
                            modified = true;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Adds data with multiple lines to this model.
        /// </summary>
        /// <param name="data">The data to be parsed.</param>
        public void addMultiple(string data)
        {
            lock (this)
            {
                if (data != null)
                {
                    string t;
                    foreach (string s in data.Split('\n'))
                    {
                        t = s.Trim();
                        if (!lines.Contains(t))
                        {
                            lines.Add(t);
                            modified = true;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Removes a line from the collection.
        /// </summary>
        /// <param name="line">The line to be removed.</param>
        public void remove(string line)
        {
            lock (this)
            {
                if (line != null)
                {
                    lines.Remove(line);
                    modified = true;
                }
            }
        }
        /// <summary>
        /// Indicates if a line is contained within the header.
        /// </summary>
        /// <param name="line">The line to be tested.</param>
        /// <returns>True = exists in collection, false = not exists.</returns>
        public bool contains(string line)
        {
            return lines.Contains(line);
        }
        /// <summary>
        /// Removes all lines.
        /// </summary>
        public void clear()
        {
            lock (this)
            {
                lines.Clear();
                modified = true;
            }
        }
        /// <summary>
        /// Compiles the header-data lines into a string.
        /// </summary>
        /// <returns>Compiled header-data string.</returns>
        public string compile()
        {
            lock (this)
            {
                StringBuilder temp = new StringBuilder();
                foreach (string line in lines)
                    temp.AppendLine(line);
                return temp.ToString();
            }
        }
        /// <summary>
        /// Generates a hash for string data.
        /// </summary>
        /// <param name="data">The data to be hashed.</param>
        /// <returns>If there is no header-data, nullg is returned.</returns>
        public static string generateHash(string data)
        {
            if (data.Length == 0)
                return null;
            HashAlgorithm ha = MD5.Create();
            return System.Text.Encoding.UTF8.GetString(ha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data)));
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The hash for the header-data.
        /// 
        /// Note: if this model has been modified and changes have not been persisted, this model's hash may be out of
        /// date!
        /// </summary>
        public string Hash
        {
            get
            {
                return hash;
            }
        }
        /// <summary>
        /// Indicates if the header-data has been modified.
        /// </summary>
        public bool IsModified
        {
            get
            {
                return modified;
            }
        }
        /// <summary>
        /// Indicates if the header-data has been persisted.
        /// </summary>
        public bool IsPersisted
        {
            get
            {
                return hash != null;
            }
        }
    }
}