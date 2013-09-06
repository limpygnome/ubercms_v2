using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public class ArticleThread
    {
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            Url = 1,
            Thumbnail = 2,
            ArticleCurrent = 4
        };
        public enum CreateThread
        {
            Error,
            UrlInvalid,
            UrlUsed,
            Success
        };
        // Fields ******************************************************************************************************
        private Fields          modified;                   // Indicates if the model has been modified.
        private bool            persisted;                  // Indicates if the model has been persisted.
        private UUID            uuidThread;                 // The UUID/identifier of this article thread.
        private UrlRewriting    url;                        // The URL rewriting model.
        private UUID            uuidArticleCurrent;         // The UUID of the article currently displayed for the thread.
        private bool            thumbnail;                  // Indicates if the article has a thumbnail uploaded.
        // Methods - Constructors **************************************************************************************
        public ArticleThread()
        {
            this.modified = Fields.None;
            this.persisted = false;
            this.uuidThread = null;
            this.url = null;
            this.uuidArticleCurrent = null;
            this.thumbnail = false;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Either creates or fetches an article thread based on a full-path.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ba">Basic Articles.</param>
        /// <param name="fullPath">The full path of the desired thread.</param>
        /// <param name="at">The article thread model output parameter; set to null if unsuccessful.</param>
        /// <returns>True = successfully fetched article thread model, false = failed to fetch article thread model.</returns>
        public static CreateThread createFetch(Connector conn, BasicArticles ba, string fullPath, out ArticleThread at)
        {
            // Ensure the URL is formatted correctly
            fullPath = UrlRewriting.stripFullPath(fullPath);
            // Lookup for an existing article at the URL
            PreparedStatement ps = new PreparedStatement("SELECT uuid_thread FROM ba_article_thread_createfetch WHERE full_path=?full_path;");
            ps["full_path"] = fullPath;
            Result r = conn.queryRead(ps);
            // Load, else create, the model for the article
            ArticleThread temp;
            if (r.Count == 1)
            {
                if ((temp = ArticleThread.load(conn, UUID.parse(r[0].get2<string>("uuid_thread")))) == null)
                {
                    at = null;
                    return CreateThread.Error;
                }
            }
            else if (r.Count > 1)
                throw new Exception("Multiple article threads exist for the full-path - critical exception!");
            else
            {
                // A thread does not exist at the specified full-path - create it!
                // -- Create URL
                UrlRewriting rw = new UrlRewriting();
                rw.FullPath = fullPath;
                rw.PluginOwner = ba.UUID;
                UrlRewriting.PersistStatus s = rw.save(conn);
                if (s != UrlRewriting.PersistStatus.Success)
                {
                    at = null;
                    switch (s)
                    {
                        case UrlRewriting.PersistStatus.Error:
                            return CreateThread.Error;
                        case UrlRewriting.PersistStatus.InvalidPath:
                            return CreateThread.UrlInvalid;
                        case UrlRewriting.PersistStatus.InUse:
                            return CreateThread.UrlUsed;
                    }
                }
                // -- Create thread
                temp = new ArticleThread();
                temp.Url = rw;
                if (!temp.save(conn))
                {
                    at = null;
                    return CreateThread.Error;
                }
            }
            at = temp;
            return CreateThread.Success;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">Identifier of thread.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(Connector conn, UUID uuidThread)
        {
            if (uuidThread == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_thread WHERE uuid_thread_raw=?uuid_thread_raw;");
            ps["uuid_thread_raw"] = uuidThread.Bytes;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads the article at the specified url.
        /// </summary>
        /// <param name="url">The URL of the article.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(Connector conn, string url)
        {
            if (url == null || url.Length == 0)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_thread WHERE full_path=?full_path;");
            ps["full_path"] = UrlRewriting.stripFullPath(url);
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a model from a database tuple/row.
        /// </summary>
        /// <param name="data">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(ResultRow data)
        {
            ArticleThread th = new ArticleThread();
            th.persisted = true;
            th.uuidThread = UUID.parse(data["uuid_thread"]);
            th.url = UrlRewriting.load(data);
            th.uuidArticleCurrent = UUID.parse(data["uuid_article_current"]);
            th.thumbnail = data["thumbnail"].Equals("1");
            return th;
        }
        /// <summary>
        /// Persists the model to the database.
        /// 
        /// Note: if a UUID for the thread has not been specified and the model has not been persisted, a version 4 UUID
        /// will be generated.
        /// </summary>
        /// <param name="conn">Database connecotor.</param>
        /// <returns>True = persisted, false = no change.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (modified == Fields.None)
                    return false;
                // Compile SQL
                SQLCompiler sql = new SQLCompiler();
                if ((modified & Fields.Url) == Fields.Url)
                {
                    if (url == null)
                        sql["urlid"] = null;
                    else
                        sql["urlid"] = url.UrlID;
                }
                if((modified & Fields.ArticleCurrent) == Fields.ArticleCurrent)
                    sql["uuid_article_current"] = uuidArticleCurrent != null ? uuidArticleCurrent.Bytes : null;
                if((modified & Fields.Thumbnail) == Fields.Thumbnail)
                    sql["thumbnail"] = thumbnail ? "1" : "0";
                // Execute SQL
                try
                {
                    if (persisted)
                    {
                        sql.UpdateAttribute = "uuid_thread";
                        sql.UpdateValue = uuidThread.Bytes;
                        sql.executeUpdate(conn, "ba_article_thread");
                    }
                    else
                    {
                        if(uuidThread == null)
                            uuidThread = UUID.generateVersion4();
                        sql["uuid_thread"] = uuidThread.Bytes;
                        sql.executeInsert(conn, "ba_article_thread");
                        persisted = true;
                    }
                    modified = Fields.None;
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// Unpersists the model from the database, but only if the article has no articles.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            conn.queryExecute("BEGIN;");
            bool delete = conn.queryCount("SELECT COUNT('') FROM ba_article WHERE uuid_thread=" + uuidThread.NumericHexString) == 0;
            if (delete)
            {
                try
                {
                    // Delete the article
                    conn.queryExecute("DELETE FROM ba_article_thread WHERE uuid_thread=" + uuidThread.NumericHexString);
                    // Delete the URL
                    if(url != null)
                        url.remove(conn);
                    persisted = false;
                }
                catch { }
            }
            conn.queryExecute("COMMIT;");
        }
        /// <summary>
        /// Unpersists the model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void removeForce(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM ba_article_thread WHERE uuid_thread=?uuid_thread;");
            ps["uuid_thread"] = uuidThread.Bytes;
            try
            {
                conn.queryExecute(ps);
                url.remove(conn);
            }
            catch { }
            persisted = false;
        }
        // Methods *****************************************************************************************************
        ///// <summary>
        ///// Changes the URL rewriting path of the thread.
        ///// </summary>
        ///// <param name="conn">Database connector.</param>
        ///// <param name="ba">BA plugin.</param>
        ///// <param name="fullPath">The new URL for the article thread.</param>
        ///// <returnsTrue = success, false = path invalid/in-use.></returns>
        //public UrlRewriting.PersistStatus setPath(Connector conn, BasicArticles ba, string fullPath)
        //{
        //    if (urlid < -1)
        //    {
        //        // Create new URL path
        //        UrlRewriting.PersistStatus ps = UrlRewriting.PersistStatus.Error;
        //        UrlRewriting rw = UrlRewriting.create(conn, ba, fullPath, out ps);
        //        if (rw != null)
        //            urlid = rw.UrlID;
        //        return ps;
        //    }
        //    else
        //    {
        //        // Load existing URL path and switch it
        //        UrlRewriting rw = UrlRewriting.load(conn, urlid);
        //        if (rw == null)
        //        {
        //            urlid = -1;
        //            return UrlRewriting.PersistStatus.Error;
        //        }
        //        else
        //        {
        //            rw.FullPath = fullPath;
        //            return rw.save(conn);
        //        }
        //    }
        //}
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The UUID of this article thread.
        /// 
        /// This can only be set interally if the model has yet to be persisted!
        /// </summary>
        public UUID UUIDThread
        {
            get
            {
                return uuidThread;
            }
            internal set
            {
                if(!persisted)
                    uuidThread = value;
            }
        }
        /// <summary>
        /// The URL rewriting model associated with this thread; can be null.
        /// </summary>
        public UrlRewriting Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
                modified |= Fields.Url;
            }
        }
        /// <summary>
        /// The UUID of the current article; can be null.
        /// </summary>
        public UUID UUIDArticleCurrent
        {
            get
            {
                return uuidArticleCurrent;
            }
            set
            {
                uuidArticleCurrent = value;
                modified |= Fields.ArticleCurrent;
            }
        }
        /// <summary>
        /// Indicates if the thread has a thumbnail.
        /// </summary>
        public bool Thumbnail
        {
            get
            {
                return thumbnail;
            }
            set
            {
                thumbnail = value;
                modified |= Fields.Thumbnail;
            }
        }
        /// <summary>
        /// The URL of the thumbnail for this article thread.
        /// </summary>
        public string UrlThumbnail
        {
            get
            {
                return "/content/basic_articles/thumbnails/" + uuidThread.Hex + ".jpg";
            }
        }
        /// <summary>
        /// The physical path for the thumbnail of this article.
        /// </summary>
        public string PathThumbnail
        {
            get
            {
                return PathThumbnails + "/" + uuidThread.Hex + ".jpg";
            }
        }
        /// <summary>
        /// The physical path of thumbnails
        /// </summary>
        public static string PathThumbnails
        {
            get
            {
                return Core.PathContent + "/basic_articles/thumbnails";
            }
        }
    }
}