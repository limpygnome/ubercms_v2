using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CMS.Base;
using CMS.BasicSiteAuth.Models;
using CMS.Plugins;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public class Article
    {
        // Enums *******************************************************************************************************
        public enum Sorting
        {
            Latest,
            Oldest,
            TitleAZ,
            TitleZA
        }
        private enum Fields
        {
            None = 0,
            ThreadUUID = 1,
            Title = 2,
            TextRaw = 4,
            TextCache = 8,
            DateTimeCreated = 16,
            DateTimeModified = 32,
            DateTimePublished = 64,
            Published = 128,
            Comments = 256,
            HTML = 512,
            HidePanel = 1024,
            UserIDAuthor = 2048,
            UserIDPublisher = 4096,
            HeaderData = 8192,
            All = 16383 // This should always be: [the next power of 2] - 1
        }
        /// <summary>
        /// The status of the persistence of the model.
        /// </summary>
        public enum PersistStatus
        {
            Error,
            Invalid_thread,
            Invalid_title_length,
            Invalid_text_length,
            Invalid_uuid_article,
            Success
        }
        // Fields ******************************************************************************************************
        private bool        persisted;              // Indicates if the model has been persisted.
        private Fields      modified;               // Indicates modified fields.
        private UUID        uuidArticle,            // The UUID of this article.
                            uuidThread;             // The UUID of the article thread.
        private string      title,                  // The title of the article.
                            textRaw,                // The raw markup of the article.
                            textCache,              // The parsed and formatted markup, cached for speed.
                            headerData,             // The article's header data.
                            headerDataHash;         // The hash to the existing header data record (may be shared by multiple articles); used for deletion of old header data.
        private DateTime    datetimeCreated,        // The date and time of when the article was created.
                            datetimeModified,       // The date and time of when the article was modified.
                            datetimePublished;      // The date and time of when the article was published.
        private bool        published,              // Indicates if the article has been published.
                            comments,               // Indicates if the article should display comments.
                            html,                   // Indicates if the article should allow HTML.
                            hidePanel;              // Indicates if the article should display an options panel.
        private int         useridAuthor,           // The identifier of the user (BSA) which authored the article.
                            useridPublisher;        // The identifier of the user (BSA) which published the article.
        // Methods - Constructors **************************************************************************************
        public Article()
        {
            this.persisted = false;
            this.modified = Fields.None;
            this.uuidArticle = null;
            this.uuidThread = null;
            this.title = this.textRaw = this.textCache = this.headerData = this.headerDataHash = null;
            this.useridAuthor = this.useridPublisher = -1;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads a persisted model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidArticle">Identifier of the article.</param>
        /// <returns>Model or null.</returns>
        public static Article load(Connector conn, UUID uuidArticle)
        {
            if (uuidArticle == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article WHERE uuid_article_raw=?uuid;");
            ps["uuid"] = uuidArticle.Bytes;
            return load(conn, ps);
        }
        /// <summary>
        /// Loads a persisted model from the database; the raw text is not loaded.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidArticle">Identifier of the article.</param>
        /// <returns>Model or null.</returns>
        public static Article loadRendered(Connector conn, UUID uuidArticle)
        {
            if (uuidArticle == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_rendered WHERE uuid_article_raw=?uuid;");
            ps["uuid"] = uuidArticle.Bytes;
            return load(conn, ps);
        }
        /// <summary>
        /// Loads multiple rendered articles.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="sorting">The sorting to apply</param>
        /// <param name="tagKeyword">The keyword for filtering; can be null.</param>
        /// <param name="articles">The number of articles to fetch.</param>
        /// <param name="page">The page offset starting at 1.</param>
        /// <returns>An array of articles.</returns>
        public static Article[] loadRendered(Connector conn, Sorting sorting, string tagKeyword, int articles, int page)
        {
            // Parse results into models and return the array of models
            List<Article> buffer = new List<Article>(articles);
            Result result = conn.queryRead("SELECT * FROM ba_view_load_article_rendered ORDER BY " + getOrderBy(sorting) + " LIMIT " + articles + " OFFSET " + (page * articles) + ";");
            Article a;
            foreach (ResultRow row in result)
            {
                a = load(row);
                if (a != null)
                    buffer.Add(a);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// Loads a persisted model from the database; the rendered text is not loaded.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidArticle">Identifier of the article.</param>
        /// <returns>Model or null.</returns>
        public static Article loadRaw(Connector conn, UUID uuidArticle)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_raw WHERE uuid_article_raw=?uuid;");
            ps["uuid"] = uuidArticle.Bytes;
            return load(conn, ps);
        }
        /// <summary>
        /// Loads a persisted model from the database; no content (raw and rendered text) is loaded.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidArticle">Identifier of the article.</param>
        /// <returns>Model or null.</returns>
        public static Article loadNoContent(Connector conn, UUID uuidArticle)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_nocontent WHERE uuid_article_raw=?uuid;");
            ps["uuid"] = uuidArticle.Bytes;
            return load(conn, ps);
        }
        /// <summary>
        /// Loads articles with no content for a thread.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">The thread of the articles.</param>
        /// <param name="sorting">The sorting of the articles.</param>
        /// <param name="articlesPerPage">The number of articles to load.</param>
        /// <param name="page">The offset/page, starting at 1.</param>
        /// <returns>Array of articles; possibly empty.</returns>
        public static Article[] loadNoContent(Connector conn, UUID uuidThread, Sorting sorting, int articlesPerPage, int page)
        {
            // Validate input
            if (page < 1)
                page = 1;
            else if (uuidThread == null || articlesPerPage < 0)
                return new Article[] { };
            // Fetch and parse articles
            List<Article> buffer = new List<Article>();
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_nocontent WHERE uuid_thread_raw=?uuid_thread ORDER BY " + getOrderBy(sorting) + " LIMIT ?app OFFSET ?apage;");
            ps["uuid_thread"] = uuidThread.Bytes;
            ps["app"] = articlesPerPage;
            ps["apage"] = ((page - 1) * articlesPerPage);
            Article a;
            foreach (ResultRow row in conn.queryRead(ps))
            {
                if ((a = Article.load(row)) != null)
                    buffer.Add(a);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// Loads a persisted model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ps">The prepared statement to be executed and read.</param>
        /// <returns>Model or null.</returns>
        public static Article load(Connector conn, PreparedStatement ps)
        {
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a persisted model from the database.
        /// </summary>
        /// <param name="row">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static Article load(ResultRow row)
        {
            Article a = new Article();
            a.persisted = true;
            a.uuidArticle = UUID.parse(row.get2<string>("uuid_article"));
            a.uuidThread = UUID.parse(row.get2<string>("uuid_thread"));
            a.title = row.get2<string>("title");
            a.textRaw = row.contains("text_raw") ? row.get2<string>("text_raw") : null;
            a.textCache = row.contains("text_cache") ? row.get2<string>("text_cache") : null;
            a.headerData = row.get2<string>("headerdata");
            a.headerDataHash = row.get2<string>("headerdata_hash");
            a.datetimeCreated = row.get2<DateTime>("datetime_created");
            a.datetimeModified = row.isNull("datetime_modified") ? DateTime.MinValue : row.get2<DateTime>("datetime_modified");
            a.datetimePublished = row.isNull("datetime_published") ? DateTime.MinValue : row.get2<DateTime>("datetime_published");
            a.published = row.get2<string>("published").Equals("1");
            a.comments = row.get2<string>("comments").Equals("1");
            a.html = row.get2<string>("html").Equals("1");
            a.hidePanel = row.get2<string>("hide_panel").Equals("1");
            a.useridAuthor = row.isNull("userid_author") ? -1 : row.get2<int>("userid_author");
            a.useridPublisher = row.isNull("userid_publisher") ? -1 : row.get2<int>("userid_publisher");
            return a;
        }
        /// <summary>
        /// Persists the model.
        /// 
        /// Note: if the article's UUID has not been set and the model has not been persisted, a version 4 UUID will
        /// be generated.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>The status of persisting the model.</returns>
        public PersistStatus save(Connector conn)
        {
            lock (this)
            {
                // Check some fields have been modified
                if (modified == Fields.None)
                    return PersistStatus.Error;
                // Validate model data
                if (persisted && uuidThread == null)
                    return PersistStatus.Invalid_thread;
                else if (persisted && uuidArticle == null)
                    return PersistStatus.Invalid_uuid_article;
                else if ((modified & Fields.Title) == Fields.Title && (title == null || title.Length < Core.Settings[Settings.SETTINGS__TITLE_LENGTH_MIN].get<int>() || title.Length > Core.Settings[Settings.SETTINGS__TITLE_LENGTH_MAX].get<int>()))
                    return PersistStatus.Invalid_title_length;
                else if ((modified & Fields.TextRaw) == Fields.TextRaw && (textRaw == null || textRaw.Length < Core.Settings[Settings.SETTINGS__TEXT_LENGTH_MIN].get<int>() || textRaw.Length > Core.Settings[Settings.SETTINGS__TEXT_LENGTH_MAX].get<int>()))
                    return PersistStatus.Invalid_text_length;
                // Check if to create new header data record
                string hash = null;
                if ((modified & Fields.HeaderData) == Fields.HeaderData && headerData != null && headerData.Length > 0)
                {
                    // Generate new hash
                    HashAlgorithm ha = MD5.Create();
                    hash = System.Text.Encoding.UTF8.GetString(ha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(headerData)));
                    // Check the data has actually changed
                    if (hash == headerDataHash)
                        modified &= ~Fields.HeaderData;
                    else
                    {
                        // Begin a transaction - we want the article and header data to persist or fail together
                        conn.queryExecute("BEGIN;");
                        // Decide if to insert new record for the hash data, for many articles to share (more efficient)
                        PreparedStatement ps = new PreparedStatement("SELECT COUNT('') AS count FROM ba_article_headerdata WHERE hash=?hash;");
                        ps["hash"] = hash;
                        if (int.Parse(conn.queryRead(ps)[0]["count"]) == 0)
                        {
                            ps = new PreparedStatement("INSERT INTO ba_article_headerdata (hash, headerdata) VALUES(?hash, ?headerdata);");
                            ps["hash"] = hash;
                            ps["headerdata"] = headerData;
                            conn.queryExecute(ps);
                        }
                    }
                }
                // Compile article SQL
                SQLCompiler sql = new SQLCompiler();
                if ((modified & Fields.ThreadUUID) == Fields.ThreadUUID)
                    sql["uuid_thread"] = uuidThread != null ? uuidThread.Bytes : null;
                if ((modified & Fields.Title) == Fields.Title)
                    sql["title"] = title;
                if ((modified & Fields.TextRaw) == Fields.TextRaw)
                    sql["text_raw"] = textRaw;
                if ((modified & Fields.TextCache) == Fields.TextCache)
                    sql["text_cache"] = textCache;
                if ((modified & Fields.DateTimeCreated) == Fields.DateTimeCreated)
                {
                    if (datetimeCreated == DateTime.MinValue)
                        sql["datetime_created"] = null;
                    else
                        sql["datetime_created"] = datetimeCreated;
                }
                if ((modified & Fields.DateTimeModified) == Fields.DateTimeModified)
                {
                    if (datetimeModified == DateTime.MinValue)
                        sql["datetime_modified"] = null;
                    else
                        sql["datetime_modified"] = datetimeModified;
                }
                if ((modified & Fields.DateTimePublished) == Fields.DateTimePublished)
                {
                    if (datetimePublished == DateTime.MinValue)
                        sql["datetime_published"] = null;
                    else
                        sql["datetime_published"] = datetimePublished;
                }
                if ((modified & Fields.Published) == Fields.Published)
                    sql["published"] = published ? "1" : "0";
                if ((modified & Fields.Comments) == Fields.Comments)
                    sql["comments"] = comments ? "1" : "0";
                if ((modified & Fields.HTML) == Fields.HTML)
                    sql["html"] = html ? "1" : "0";
                if ((modified & Fields.HidePanel) == Fields.HidePanel)
                    sql["hide_panel"] = hidePanel ? "1" : "0";
                if ((modified & Fields.UserIDAuthor) == Fields.UserIDAuthor)
                    sql["userid_author"] = useridAuthor;
                if ((modified & Fields.UserIDPublisher) == Fields.UserIDPublisher)
                    sql["userid_publisher"] = useridPublisher;
                if ((modified & Fields.HeaderData) == Fields.HeaderData)
                    sql["headerdata_hash"] = hash;
                // Execute article SQL
                try
                {
                    if (persisted)
                    {
                        sql.UpdateAttribute = "uuid_article";
                        sql.UpdateValue = uuidArticle.Bytes;
                        sql.executeUpdate(conn, "ba_article");
                    }
                    else
                    {
                        if(uuidArticle == null)
                            uuidArticle = UUID.generateVersion4();
                        sql["uuid_article"] = uuidArticle.Bytes;
                        sql.executeInsert(conn, "ba_article");
                        persisted = true;
                    }
                    // Check if we're inside a transaction, if so commit it
                    if ((modified & Fields.HeaderData) == Fields.HeaderData)
                        conn.queryExecute("COMMIT;");
                    // Check if to attempt to delete old hash data
                    if ((modified & Fields.HeaderData) == Fields.HeaderData && headerDataHash != null)
                        removeOldHeaderData(conn, headerDataHash);
                    if((modified & Fields.HeaderData) == Fields.HeaderData)
                        headerData = hash;
                    modified = Fields.None;
                    return PersistStatus.Success;
                }
                catch (DuplicateEntryException ex)
                {
                    if ((modified & Fields.HeaderData) == Fields.HeaderData)
                        conn.queryExecute("ROLLBACK;");
                    switch (ex.Attribute)
                    {
                        case "uuid_article":
                            return PersistStatus.Invalid_uuid_article;
                        default:
                            return PersistStatus.Error;
                    }
                }
                catch (Exception)
                {
                    if ((modified & Fields.HeaderData) == Fields.HeaderData)
                        conn.queryExecute("ROLLBACK;");
                    return PersistStatus.Error;
                }
            }
        }
        /// <summary>
        /// Unpersists this model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="thread">The thread model of the article; can be null. This thread may be unpersisted if this is the only article belonging to the thread.</param>
        public void remove(Connector conn, ArticleThread thread)
        {
            lock (this)
            {
                if (uuidArticle == null)
                    return;
                // Remove the article
                PreparedStatement ps = new PreparedStatement("DELETE FROM ba_article WHERE uuid_article=?uuid_article;");
                ps["uuid_article"] = uuidArticle.Bytes;
                conn.queryExecute(ps);
                // Remove header data
                if(headerDataHash != null)
                    removeOldHeaderData(conn, headerDataHash);
                // Remove thread; this will only work if it has no articles
                if(thread != null)
                    thread.remove(conn);
            }
        }
        private void removeOldHeaderData(Connector conn, string hash)
        {
            PreparedStatement p = new PreparedStatement("DELETE FROM ba_article_headerdata WHERE hash=?hash AND (SELECT COUNT('') FROM ba_article WHERE headerdata_hash=?hash) = 0;");
            p["hash"] = headerDataHash;
            conn.queryExecute(p);
        }
        private static string getOrderBy(Sorting sorting)
        {
            string strOrder;
            switch (sorting)
            {
                case Sorting.Latest:
                    strOrder = "datetime_created DESC"; break;
                case Sorting.Oldest:
                    strOrder = "datetime_created ASC"; break;
                case Sorting.TitleAZ:
                    strOrder = "title ASC"; break;
                case Sorting.TitleZA:
                    strOrder = "title DESC"; break;
                default:
                    throw new Exception("Unknown sorting specified!");
            }
            return strOrder;
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Indicates if the user is allowed to edit the article.
        /// 
        /// Note: all users within a group with the flag(s) for administrator or/and moderator set to true will be able
        /// to edit the article.
        /// </summary>
        /// <param name="u">The user to be tested.</param>
        /// <returns>True = allowed, false = not allowed.</returns>
        public bool isAuthorisedEdit(User u)
        {
            return u.UserGroup.Administrator || u.UserGroup.Moderator || u.UserID == useridAuthor;
        }
        /// <summary>
        /// Rebuilds the article's raw text; output available from TextCache and HeaderData.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        public void rebuild(Data data)
        {
            StringBuilder text = new StringBuilder(html ? textRaw : System.Web.HttpUtility.HtmlEncode(textRaw));
            StringBuilder header = new StringBuilder();
            // Render text
#if TextRenderer
            TextRenderer tr = (TextRenderer)Core.Plugins[UUID.parse(TextRenderer.TR_UUID)];
            if (tr != null)
                tr.render(data, ref header, ref text, RenderProvider.RenderType.Objects | RenderProvider.RenderType.TextFormatting);
#endif
            textCache = text.Length > 0 ? text.ToString() : null;
            headerData = header.Length > 0 ? header.ToString() : null;
            modified |= Fields.HeaderData | Fields.TextCache;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The UUID of this article.
        /// 
        /// Note: setting this property on a persisted model will have no effect.
        /// </summary>
        public UUID UUIDArticle
        {
            get
            {
                return uuidArticle;
            }
            set
            {
                lock (this)
                {
                    if (!persisted)
                        uuidArticle = value;
                }
            }
        }
        /// <summary>
        /// The UUID of the thread this article belongs to.
        /// </summary>
        public UUID UUIDThread
        {
            get
            {
                return uuidThread;
            }
            set
            {
                lock (this)
                {
                    uuidThread = value;
                    modified |= Fields.ThreadUUID;
                }
            }
        }
        /// <summary>
        /// The title of the article.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                lock (this)
                {
                    title = value;
                    modified |= Fields.Title;
                }
            }
        }
        /// <summary>
        /// The raw markup of the article.
        /// </summary>
        public string TextRaw
        {
            get
            {
                return textRaw;
            }
            set
            {
                lock (this)
                {
                    textRaw = value;
                    modified |= Fields.TextRaw;
                }
            }
        }
        /// <summary>
        /// The parsed and rendered HTML of the article.
        /// </summary>
        public string TextCache
        {
            get
            {
                return textCache;
            }
            set
            {
                lock (this)
                {
                    textCache = value;
                    modified |= Fields.TextCache;
                }
            }
        }
        /// <summary>
        /// The date and time of when the article was created.
        /// 
        /// The value DateTime.MinValue is the equivalent of null.
        /// </summary>
        public DateTime DateTimeCreated
        {
            get
            {
                return datetimeCreated;
            }
            set
            {
                lock (this)
                {
                    datetimeCreated = value;
                    modified |= Fields.DateTimeCreated;
                }
            }
        }
        /// <summary>
        /// The date and time of when the article was modified.
        /// 
        /// The value DateTime.MinValue is the equivalent of null.
        /// </summary>
        public DateTime DateTimeModified
        {
            get
            {
                return datetimeModified;
            }
            set
            {
                lock (this)
                {
                    datetimeModified = value;
                    modified |= Fields.DateTimeModified;
                }
            }
        }
        /// <summary>
        /// The date and time of when the article was published.
        /// 
        /// The value DateTime.MinValue is the equivalent of null.
        /// </summary>
        public DateTime DateTimePublished
        {
            get
            {
                return datetimePublished;
            }
            set
            {
                lock (this)
                {
                    datetimePublished = value;
                    modified |= Fields.DateTimeModified;
                }
            }
        }
        /// <summary>
        /// Indicates if the article has been published.
        /// </summary>
        public bool Published
        {
            get
            {
                return published;
            }
            set
            {
                lock (this)
                {
                    published = value;
                    modified |= Fields.Published;
                }
            }
        }
        /// <summary>
        /// Indicates if to display comments.
        /// </summary>
        public bool Comments
        {
            get
            {
                return comments;
            }
            set
            {
                lock (this)
                {
                    comments = value;
                    modified |= Fields.Comments;
                }
            }
        }
        /// <summary>
        /// Indicates if the article should allow HTML, else it will be escaped when rendered.
        /// </summary>
        public bool HTML
        {
            get
            {
                return html;
            }
            set
            {
                lock (this)
                {
                    html = value;
                    modified |= Fields.HTML;
                }
            }
        }
        /// <summary>
        /// Indicates if to hide the edit panel.
        /// </summary>
        public bool HidePanel
        {
            get
            {
                return hidePanel;
            }
            set
            {
                lock (this)
                {
                    hidePanel = value;
                    modified |= Fields.HidePanel;
                }
            }
        }
        /// <summary>
        /// The user identifier (BSA) of the author.
        /// </summary>
        public int UserIdAuthor
        {
            get
            {
                return useridAuthor;
            }
            set
            {
                lock (this)
                {
                    useridAuthor = value;
                    modified |= Fields.UserIDAuthor;
                }
            }
        }
        /// <summary>
        /// The user identifier (BSA) of the publisher.
        /// </summary>
        public int UserIdPublisher
        {
            get
            {
                return useridPublisher;
            }
            set
            {
                lock (this)
                {
                    useridPublisher = value;
                    modified |= Fields.UserIDPublisher;
                }
            }
        }
        /// <summary>
        /// The header data to support the rendered article content.
        /// </summary>
        public string HeaderData
        {
            get
            {
                return headerData;
            }
            set
            {
                lock (this)
                {
                    headerData = value;
                    modified |= Fields.HeaderData;
                }
            }
        }
    }
}