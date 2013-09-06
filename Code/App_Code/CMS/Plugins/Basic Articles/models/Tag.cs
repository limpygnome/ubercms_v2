using System;
using System.Text.RegularExpressions;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A read-only model; persisted by ArticleThreadTags model. Represents a tag shared by many articles.
    /// </summary>
    public class Tag
    {
        // Fields ******************************************************************************************************
        private bool        persisted;      // Indicates if the model has been persisted.
        private int         tagid;          // The unique identifier of the model on the database.
        private string      keyword;        // The tag's keyword.
        // Methods - Constructors **************************************************************************************
        private Tag()
        {
            this.persisted = false;
        }
        private Tag(string keyword)
        {
            this.persisted = false;
            this.keyword = keyword;
        }
        // Methods - Database Persisetence *****************************************************************************
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="tagId">Identifier of the tag.</param>
        /// <returns>Model or null.</returns>
        public static Tag load(Connector conn, int tagId)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_tags WHERE tagid=?tagid;");
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
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_tags WHERE keyword=?keyword;");
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
        // Methods *****************************************************************************************************
        /// <summary>
        /// Attempts to create a tag or returns null if invalid.
        /// </summary>
        /// <returns>Model or null.</returns>
        public Tag create(string keyword)
        {
            return Regex.IsMatch(keyword, @"^([a-zA-Z0-9_-]*?)$") ? new Tag(keyword) : null;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of the tag.
        /// </summary>
        public int TagID
        {
            get
            {
                return tagid;
            }
            set
            {
                tagid = value;
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
        }
        /// <summary>
        /// Indicates if the model is persisted.
        /// </summary>
        public bool IsPersisted
        {
            get
            {
                return persisted;
            }
            set
            {
                persisted = value;
            }
        }
    }
}