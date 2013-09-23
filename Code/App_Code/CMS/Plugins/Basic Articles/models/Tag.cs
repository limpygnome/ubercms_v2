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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/models/Tag.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model to represent a tag, which belongs to one or more thread(s).
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A model to represent a tag, which belongs to one or more thread(s).
    /// </summary>
    public class Tag
    {
        // Enums *******************************************************************************************************
        /// <summary>
        /// The sorting for loading multiple tags.
        /// </summary>
        public enum Sorting
        {
            Keyword,
            Population
        }
        // Fields ******************************************************************************************************
        private bool        persisted;      // Indicates if the model has been persisted.
        private int         tagid;          // The unique identifier of the model on the database.
        private string      keyword;        // The tag's keyword.
        private int         threads;        // The total number of threads referenced by this tag.
        // Methods - Constructors **************************************************************************************
        private Tag()
        {
            this.persisted = false;
        }
        private Tag(string keyword)
        {
            this.persisted = false;
            this.keyword = keyword;
            this.threads = 0;
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
            if (row.contains("count"))
                t.threads = int.Parse(row["count"]);
            return t;
        }
        /// <summary>
        /// Loads multiple tags.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="page">The current page of tags.</param>
        /// <param name="tagsPerPage">The maximum number of tags to load.</param>
        /// <param name="sorting">The sorting when loading tags from the database.</param>
        /// <param name="loadExtra">Indicates if to load an extra model; useful for page systems.</param>
        /// <returns>Array of tags; this is never null but possibly empty.</returns>
        public static Tag[] load(Connector conn, int page, int tagsPerPage, Sorting sorting, bool loadExtra)
        {
            // Validate input
            if (page < 1 || tagsPerPage < 0)
                return new Tag[] { };
            // Build query
            PreparedStatement ps = new PreparedStatement("SELECT * FROM (SELECT (SELECT COUNT('') FROM ba_tags_thread AS tt, ba_article_thread AS t, ba_article AS a WHERE tt.tagid=bt.tagid AND t.uuid_thread=tt.uuid_thread AND a.uuid_article=t.uuid_article_current AND a.published='1') AS count, bt.* FROM ba_tags AS bt) AS t WHERE t.count > 0 ORDER BY " + (sorting == Sorting.Population ? "count DESC, t.keyword ASC" : "t.keyword ASC") + " LIMIT ?limit OFFSET ?offset");
            ps["limit"] = tagsPerPage + (loadExtra ? 1 : 0);
            ps["offset"] = (page * tagsPerPage) - tagsPerPage;
            // Execute query and parse data
            Result r = conn.queryRead(ps);
            Tag tag;
            List<Tag> buffer = new List<Tag>();
            foreach (ResultRow row in r)
            {
                if ((tag = load(row)) != null)
                    buffer.Add(tag);
            }
            return buffer.ToArray();
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Attempts to create a tag or returns null if invalid.
        /// </summary>
        /// <returns>Model or null.</returns>
        public static Tag create(string keyword)
        {
            keyword = keyword.Trim();
            return keyword.Length < Core.Settings[Settings.SETTINGS__THREAD_TAG_LENGTH_MIN].get<int>() || keyword.Length > Core.Settings[Settings.SETTINGS__THREAD_TAG_LENGTH_MAX].get<int>() || Regex.IsMatch(keyword, @"^([a-zA-Z0-9_ -]*?)$") ? new Tag(keyword) : null;
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
        /// <summary>
        /// The number of threads referencing this tag.
        /// 
        /// Note: this is only loaded when loading multiple tags.
        /// </summary>
        public int ThreadReferences
        {
            get
            {
                return threads;
            }
        }
    }
}