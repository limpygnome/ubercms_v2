using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public class ArticleThread
    {
        // Enums *******************************************************************************************************

        // Fields ******************************************************************************************************
        private bool            modified,                   // Indicates if the model has been modified.
                                persisted;                  // Indicates if the model has been persisted.
        private UUID            uuidThread;                 // The UUID/identifier of this article thread.
        private int             urlid;                      // The URL rewriting ID belonging to this thread.
        private string          fullPath;                   // The full URL path of the article thread.
        private UUID            uuidArticleCurrent;         // The UUID of the article currently displayed for the thread.
        private bool            thumbnail;                  // Indicates if the article has a thumbnail uploaded.
        // Methods - Constructors **************************************************************************************
        public ArticleThread()
        {
            this.modified = this.persisted = false;
            this.urlid = -1;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Either creates or fetches an article thread based on a full-path.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="fullPath">The full path of the desired thread.</param>
        /// <param name="at">The article thread model output parameter; set to null if unsuccessful.</param>
        /// <returns>True = successfully fetched article thread model, false = failed to fetch article thread model.</returns>
        public bool createFetch(Connector conn, string fullPath, out ArticleThread at)
        {
            // Ensure the URL is formatted correctly
            fullPath = UrlRewriting.stripFullPath(fullPath);
            // Lookup for an existing article at the URL
            PreparedStatement ps = new PreparedStatement("SELECT uuid_thread FROM ba_article_thread_createfetch WHERE full_path=?full_path");
            ps["full_path"] = fullPath;
            Result r = conn.queryRead(ps);
            // Load, else create, the model for the article
            ArticleThread temp;
            if (r.Count == 1)
                temp = ArticleThread.load(conn, UUID.parse(r[0].get2<string>("uuid_thread")));
            else if (r.Count > 1)
                throw new Exception("Multiple article threads exist for the full-path - critical exception!");
            else
            {
                // A thread does not exist at the specified full-path - create it!
                temp = new ArticleThread();
                temp.UUIDThread = UUID.generateVersion4();
                temp.FullPath = fullPath;
                if (!temp.save(conn))
                    temp = null;
            }
            at = temp;
            return temp == null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">Identifier of thread.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(Connector conn, UUID uuidThread)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_thread WHERE uuid_thread=?uuid_thread;");
            ps["uuid_thread"] = uuidThread.Bytes;
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
            th.fullPath = data["full_path"];
            th.uuidArticleCurrent = UUID.parse(data["uuid_article_current"]);
            th.thumbnail = data["thumbnail"].Equals("1");
            return th;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connecotor.</param>
        /// <returns>True = persisted, false = no change.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return false;
                SQLCompiler sql = new SQLCompiler();
                if(urlid < 0)
                    sql["urlid"] = null;
                else
                    sql["urlid"] = urlid;
                sql["uuid_article_current"] = uuidArticleCurrent.Bytes;
                sql["thumbnail"] = thumbnail ? "1" : "0";
                sql["uuid_thumbnail"] = thumbnail;
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
                        uuidThread = UUID.generateVersion4();
                        sql["uuid_thread"] = uuidThread.Bytes;
                        sql.executeInsert(conn, "ba_article_thread");
                        persisted = true;
                    }
                    modified = false;
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// Unpersists the model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM ba_article_thread WHERE uuid_thread=?uuid_thread;");
            ps["uuid_thread"] = uuidThread.Bytes;
            conn.queryExecute(ps);
            persisted = false;
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Changes the URL rewriting path of the thread.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ba">BA plugin.</param>
        /// <param name="fullPath">The new URL for the article thread.</param>
        /// <returnsTrue = success, false = path invalid/in-use.></returns>
        public UrlRewriting.PersistStatus setPath(Connector conn, BasicArticles ba, string fullPath)
        {
            if (urlid < -1)
            {
                // Create new URL path
                UrlRewriting.PersistStatus ps = UrlRewriting.PersistStatus.Error;
                UrlRewriting rw = UrlRewriting.create(conn, ba, fullPath, out ps);
                if (rw != null)
                    urlid = rw.UrlID;
                return ps;
            }
            else
            {
                // Load existing URL path and switch it
                UrlRewriting rw = UrlRewriting.load(conn, urlid);
                if (rw == null)
                {
                    urlid = -1;
                    return UrlRewriting.PersistStatus.Error;
                }
                else
                {
                    rw.FullPath = fullPath;
                    return rw.save(conn);
                }
            }
        }
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
        /// The identifier of the url-rewriting model used by this thread.
        /// </summary>
        public int UrlID
        {
            get
            {
                return urlid;
            }
        }
        /// <summary>
        /// The full-path (URL) of this article thread. Refer to UrlRewriting model for format.
        /// 
        /// This can only be set interally if the model has yet to be persisted!
        /// </summary>
        public string FullPath
        {
            get
            {
                return fullPath;
            }
            internal set
            {
                if (!persisted)
                    fullPath = value;
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
                modified = true;
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
                modified = true;
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