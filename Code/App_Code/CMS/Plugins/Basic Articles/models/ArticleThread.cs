using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public class ArticleThread
    {
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
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">Identifier of thread.</param>
        /// <returns>Model or null.</returns>
        public ArticleThread load(Connector conn, UUID uuidThread)
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
        public ArticleThread load(ResultRow data)
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
                sql["uuid_thumbnail"] = thumbnail;
                if (persisted)
                {
                    sql.UpdateAttribute = "uuid_thread";
                    sql.UpdateValue = uuidThread.Bytes;
                    sql.executeUpdate(conn, "ba_article_thread");
                }
                else
                {
                    try
                    {
                        uuidThread = UUID.generateVersion4();
                        sql["uuid_thread"] = uuidThread.Bytes;
                        sql.executeInsert(conn, "ba_article_thread");
                    }
                    catch (Exception)
                    {
                        return false;
                    }
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
        /// </summary>
        public UUID UUIDThread
        {
            get
            {
                return uuidThread;
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
        /// </summary>
        public string FullPath
        {
            get
            {
                return fullPath;
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