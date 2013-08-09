using System;
using System.Collections.Generic;
using System.Text;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public class ArticleThreadTags
    {
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
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return false;
                // Compile SQL
                StringBuilder t = new StringBuilder();
                t.Append("BEGIN;");
                // -- Delete all the tags for the current thread
                t.Append("DELETE FROM bsa_tags_thread WHERE uuid_thread=").Append(uuidThread.NumericHexString).Append(";");
                // -- Add all the current tags
                if(tags.Count > 0)
                {
                    t.Append("INSERT INTO bsa_tags_thread (tagid, uuid_thread) VALUES");
                    foreach (Tag tag in tags)
                        t.Append("(").Append(tag.TagID).Append(", ").Append(uuidThread.NumericHexString).Append("),");
                    t.Remove(t.Length - 1, 1).Append(";");
                }
                t.Append("END;");
                // Execute SQL
                try
                {
                    conn.queryExecute(t.ToString());
                    persisted = true;
                    modified = false;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        // Methods - Properties ****************************************************************************************
    }
}