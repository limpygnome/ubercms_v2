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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/models/ArticleThreadTags.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A class for handling the Tag-models/tags belonging to a thread.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Text;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A class for handling the Tag-models/tags belonging to a thread.
    /// </summary>
    public class ArticleThreadTags
    {
        // Enums *******************************************************************************************************
        /// <summary>
        /// The status of attempting to persist this model.
        /// </summary>
        public enum PersistStatus
        {
            TooManyTags,
            Error,
            Success
        }
        // Fields ******************************************************************************************************
        private bool            modified,           // Indicates if the model has been modified.
                                persisted;          // Indicates if the model has been persisted.
        private UUID            uuidThread;         // The UUID of the article thread.
        private List<Tag>       tags;               // The tags belonging to the article thread.
        // Methods - Constructors **************************************************************************************
        public ArticleThreadTags()
        {
            tags = new List<Tag>();
        }
        // Methods - Enumerators ***************************************************************************************
        public List<Tag>.Enumerator GetEnumerator()
        {
            return tags.GetEnumerator();
        }
        // Methods - Database Persistence ******************************************************************************
        public static ArticleThreadTags load(Connector conn, UUID uuidThread)
        {
            PreparedStatement ps = new PreparedStatement("SELECT tagid, keyword FROM ba_view_tags WHERE uuid_thread_raw=?uuid_thread;");
            ps["uuid_thread"] = uuidThread.Bytes;
            ArticleThreadTags ts = new ArticleThreadTags();
            ts.uuidThread = uuidThread;
            Result data = conn.queryRead(ps);
            Tag t;
            foreach (ResultRow row in data)
            {
                t = Tag.load(row);
                if (t != null)
                    ts.tags.Add(t);
            }
            return ts;
        }
        public PersistStatus save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return PersistStatus.Error;
                else if (tags.Count > Core.Settings[Settings.SETTINGS__THREAD_TAGS_MAX].get<int>())
                    return PersistStatus.TooManyTags;
                // Compile SQL
                StringBuilder t = new StringBuilder();
                t.Append("BEGIN;");
                // -- Check all the tags are persisted, else persist them
                SQLCompiler sql;
                foreach (Tag tag in tags)
                {
                    if (!tag.IsPersisted)
                    {
                        sql = new SQLCompiler();
                        sql["keyword"] = tag.Keyword;
                        tag.TagID = int.Parse(sql.executeInsert(conn, "ba_tags", "tagid")[0]["tagid"]);
                    }
                }
                // -- Delete all the tags for the current thread
                t.Append("DELETE FROM ba_tags_thread WHERE uuid_thread=").Append(uuidThread.NumericHexString).Append(";");
                // -- Add all the current tags
                if(tags.Count > 0)
                {
                    t.Append("INSERT INTO ba_tags_thread (tagid, uuid_thread) VALUES");
                    foreach (Tag tag in tags)
                        t.Append("(").Append(tag.TagID).Append(", ").Append(uuidThread.NumericHexString).Append("),");
                    t.Remove(t.Length - 1, 1).Append(";");
                }
                // -- Clean unused tags
                t.Append("DELETE btt FROM ba_tags AS btt WHERE (SELECT COUNT('') FROM ba_tags_thread WHERE tagid=btt.tagid) = 0;");
                t.Append("COMMIT;");
                // Execute SQL
                try
                {
                    conn.queryExecute(t.ToString());
                    persisted = true;
                    modified = false;
                    return PersistStatus.Success;
                }
                catch
                {
                    return PersistStatus.Error;
                }
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Tests if a keyword of a tag exists within the collection.
        /// </summary>
        /// <param name="keyword">The keyword to test.</param>
        /// <returns>True = exists, false = not exists.</returns>
        public bool contains(string keyword)
        {
            lock (tags)
            {
                foreach (Tag t in tags)
                    if (t.Keyword == keyword)
                        return true;
                return false;
            }
        }
        /// <summary>
        /// Adds a keyword.
        /// 
        /// This will automatically attempt to load the model for the tag or create a new unpersisted model.
        /// </summary>
        /// <param name="keyword">The keyword of the tag.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = added, false = invalid characters/length.</returns>
        public bool add(string keyword, Connector conn)
        {
            lock (this)
            {
                keyword = keyword.Trim();
                if (!contains(keyword))
                {
                    Tag t = Tag.load(conn, keyword);
                    if (t == null)
                        t = Tag.create(keyword);
                    if (t != null)
                    {
                        tags.Add(t);
                        return true;
                    }
                    return false;
                }
                else
                    return true;
            }
        }
        /// <summary>
        /// Adds a new tag to the thread; this also checks a tag with the same keyword does not exist.
        /// </summary>
        /// <param name="tag"></param>
        public void add(Tag tag)
        {
            lock (this)
            {
                if (tag != null && !contains(tag.Keyword))
                    tags.Add(tag);
            }
        }
        /// <summary>
        /// Removes a tag from the collection.
        /// </summary>
        /// <param name="tag">The tag model to be removed.</param>
        public void remove(Tag tag)
        {
            lock (this)
            {
                tags.Remove(tag);
            }
        }
        /// <summary>
        /// Removes any tags with the specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword to be removed from the collection.</param>
        public void remove(string keyword)
        {
            lock (this)
            {
                List<Tag> buffer = new List<Tag>();
                foreach (Tag tag in tags)
                    if (tag.Keyword == keyword)
                        buffer.Add(tag);
                foreach (Tag tag in buffer)
                    tags.Remove(tag);
            }
        }
        /// <summary>
        /// Removes all the tags from the collection.
        /// </summary>
        public void clear()
        {
            lock (this)
            {
                tags.Clear();
                modified = true;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The tags belonging to the thread.
        /// </summary>
        public Tag[] Tags
        {
            get
            {
                return tags.ToArray();
            }
        }
    }
}