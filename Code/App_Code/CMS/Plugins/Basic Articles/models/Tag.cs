using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public class Tag
    {
        // Fields ******************************************************************************************************
        private bool        modified,       // Indicates if the model has been modified.
                            persisted;      // Indicates if the model has been persisted.
        private int         tagid;          // The unique identifier of the model on the database.
        private string      keyword;        // The tag's keyword.
        // Methods - Constructors **************************************************************************************
        public Tag()
        {
            this.modified = this.persisted = false;
        }
        // Methods - Database Persisetence *****************************************************************************
        /// <summary>
        /// Creates or loads a tag model for a keyword.
        /// 
        /// This may return null if a model cannot be loaded or persisted.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="keyword">The tag keyword.</param>
        /// <returns>Model or null.</returns>
        public static Tag createOrLoad(Connector conn, string keyword)
        {
            Tag t = load(conn, keyword);
            if (t != null)
                return t;
            t = new Tag();
            t.Keyword = keyword;
            return t.save(conn) ? t : null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="tagId">Identifier of the tag.</param>
        /// <returns>Model or null.</returns>
        public static Tag load(Connector conn, int tagId)
        {
            PreparedStatement ps = new PreparedStatement("SELECT tagid, keyword FROM ba_tags WHERE tagid=?tagid;");
            ps["tagid"] = tagId;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="keyword">The keyword of the tag.</param>
        /// <returns>Model or null.</returns>
        public static Tag load(Connector conn, string keyword)
        {
            PreparedStatement ps = new PreparedStatement("SELECT tagid, keyword FROM ba_tags WHERE keyword=?keyword;");
            ps["keyword"] = keyword;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="row">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static Tag load(ResultRow row)
        {
            Tag t = new Tag();
            t.persisted = true;
            t.tagid = row.get2<int>("tagid");
            t.keyword = row.get2<string>("keyword");
            return t;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = not persisted.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return false;
                // Compile SQL
                SQLCompiler sql = new SQLCompiler();
                sql["keyword"] = keyword;
                // Execute
                try
                {
                    if (persisted)
                    {
                        sql.UpdateAttribute = "tagid";
                        sql.UpdateValue = tagid;
                        sql.executeUpdate(conn, "ba_tags");
                    }
                    else
                    {
                        tagid = (int)sql.executeInsert(conn, "ba_tags", "tagid")[0].get2<int>("tagid");
                        persisted = true;
                    }
                    modified = false;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Unpersists the data from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            lock (this)
            {
                PreparedStatement ps = new PreparedStatement("DELETE FROM ba_tags WHERE tagid=?tagid;");
                ps["tagid"] = tagid;
                conn.queryExecute(ps);
                this.persisted = false;
            }
        }
        // Methods - Properties ***************************************************************************************&
        /// <summary>
        /// The identifier of the tag.
        /// </summary>
        public int TagID
        {
            get
            {
                return tagid;
            }
        }
        /// <summary>
        /// The keyword of the tag.
        /// </summary>
        public string Keyword
        {
            get
            {
                return keyword;
            }
            set
            {
                keyword = value;
                modified = true;
            }
        }
    }
}