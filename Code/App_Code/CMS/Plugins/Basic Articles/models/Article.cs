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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/models/Article.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model to represent an article.
 * *********************************************************************************************************************
 */
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
    /// <summary>
    /// A model to represent an article.
    /// </summary>
    public class Article
    {
        // Enums *******************************************************************************************************
        /// <summary>
        /// Indicates which parts of the article's text should be loaded.
        /// </summary>
        public enum Text
        {
            Rendered,
            Raw,
            Both,
            None
        }
        /// <summary>
        /// Indicates the sorting when fetching articles from the database.
        /// </summary>
        public enum Sorting
        {
            Latest,
            Oldest,
            TitleAZ,
            TitleZA
        }
        /// <summary>
        /// The filter for articles based on their publication status.
        /// </summary>
        public enum PublishFilter
        {
            Published,
            NonPublished,
            Both
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
            HeaderData = 8192
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
        private bool            persisted;              // Indicates if the model has been persisted.
        private Fields          modified;               // Indicates modified fields.
        private UUID            uuidArticle,            // The UUID of this article.
                                uuidThread;             // The UUID of the article thread.
        private string          title,                  // The title of the article.
                                textRaw,                // The raw markup of the article.
                                textCache;              // The parsed and formatted markup, cached for speed.
        private ArticleHeader   header;                 // The header-data for this article.
        private DateTime        datetimeCreated,        // The date and time of when the article was created.
                                datetimeModified,       // The date and time of when the article was modified.
                                datetimePublished;      // The date and time of when the article was published.
        private bool            published,              // Indicates if the article has been published.
                                comments,               // Indicates if the article should display comments.
                                html,                   // Indicates if the article should allow HTML.
                                hidePanel;              // Indicates if the article should display an options panel.
        private int             useridAuthor,           // The identifier of the user (BSA) which authored the article.
                                useridPublisher;        // The identifier of the user (BSA) which published the article.
        // Methods - Constructors **************************************************************************************
        public Article()
        {
            this.persisted = false;
            this.modified = Fields.None;
            this.uuidArticle = null;
            this.uuidThread = null;
            this.title = this.textRaw = this.textCache = null;
            this.useridAuthor = this.useridPublisher = -1;
            this.header = new ArticleHeader();
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads a persisted model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidArticle">Identifier of the article.</param>
        /// <param name="text">The text to load.</param>
        /// <returns>Model or null.</returns>
        public static Article load(Connector conn, UUID uuidArticle, Text text)
        {
            if (uuidArticle == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM " + loadTable(text, false) + " WHERE uuid_article_raw=?uuid;");
            ps["uuid"] = uuidArticle.Bytes;
            return load(conn, ps);
        }
        /// <summary>
        /// Loads multiple articles with sorting.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">The thread of the articles; can be null.</param>
        /// <param name="sorting">The sorting of the articles.</param>
        /// <param name="tagKeyword">The tag keyword for filtering; can be null.</param>
        /// <param name="search">The search keyword(s); can be null.</param>
        /// <param name="articlesPerPage">The number of articles to load.</param>
        /// <param name="page">The offset/page, starting at 1.</param>
        /// <param name="text">The text to load.</param>
        /// <param name="singleArticle">Indicates if to only load the current article per thread.</param>
        /// <param name="publishedOnly">Indicates if to display only published articles.</param>
        /// <param name="loadExtra">Indicates if to load an extra model on top of the amount and page; this is useful for page systems.</param>
        /// <returns>Array of articles; possibly empty.</returns>
        public static Article[] load(Connector conn, UUID uuidThread, Sorting sorting, string tagKeyword, string search, int articlesPerPage, int page, Text text, bool singleArticle, PublishFilter published, bool loadExtra)
        {
            // Validate input
            if (page < 1 || articlesPerPage < 0)
                return new Article[] { };
            // Build query
            PreparedStatement ps = new PreparedStatement();
            StringBuilder query = new StringBuilder();
            query.Append("SELECT v.* FROM ").Append(loadTable(text, singleArticle));
            if (uuidThread != null || tagKeyword != null || search != null || singleArticle || published != PublishFilter.Both)
            {
                query.Append(" WHERE ");
                if (singleArticle)
                    query.Append("t.uuid_thread=v.uuid_thread_raw AND v.uuid_article_raw=t.uuid_article_current AND ");
                switch(published)
                {
                    case PublishFilter.NonPublished:
                        query.Append("published='0' AND ");
                        break;
                    case PublishFilter.Published:
                        query.Append("published='1' AND ");
                        break;
                }
                if (uuidThread != null)
                {
                    ps["uuid_thread"] = uuidThread.Bytes;
                    query.Append("uuid_thread_raw=?uuid_thread AND ");
                }
                if (tagKeyword != null)
                {
                    ps["keyword"] = tagKeyword;
                    query.Append("(SELECT COUNT('') FROM ba_tags_thread AS btt, ba_tags AS bt WHERE bt.keyword=?keyword AND btt.uuid_thread=t.uuid_thread AND btt.tagid=bt.tagid) > 0 AND ");
                }
                if (search != null)
                {
                    ps["sch"] = "%" + search + "%";
                    query.Append("(title LIKE ?sch OR text_raw LIKE ?sch) AND ");
                }
                query.Remove(query.Length - 5, 5);
            }
            query.Append(" ORDER BY " + getOrderBy(sorting) + " LIMIT ?app OFFSET ?apage;");
            ps.Query = query.ToString();
            ps["app"] = articlesPerPage + (loadExtra ? 1 : 0);
            ps["apage"] = ((page - 1) * articlesPerPage);
            // Fetch and parse articles
            List<Article> buffer = new List<Article>();
            Article a;
            foreach (ResultRow row in conn.queryRead(ps))
            {
                if ((a = Article.load(row)) != null)
                    buffer.Add(a);
            }
            return buffer.ToArray();
        }
        private static string loadTable(Text text, bool joinThreadTable)
        {
            switch (text)
            {
                case Text.Both:
                    return "ba_view_load_article AS v" + (joinThreadTable ? ", ba_article_thread AS t" : "");
                case Text.Raw:
                    return "ba_view_load_article_raw AS v" + (joinThreadTable ? ", ba_article_thread AS t" : "");
                case Text.Rendered:
                    return "ba_view_load_article_rendered AS v" + (joinThreadTable ? ", ba_article_thread AS t" : "");
                case Text.None:
                default:
                    return "ba_view_load_article_nocontent AS v" + (joinThreadTable ? ", ba_article_thread AS t" : "");
            }
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
            a.header = new ArticleHeader(row.get2<string>("headerdata"), row.get2<string>("headerdata_hash"));
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
                    // Persist header-data
                    if ((modified & Fields.HeaderData) == Fields.HeaderData)
                        header.persist(conn, uuidArticle);
                    // Done!
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
                if (header != null)
                    header.remove(conn);
                // Remove thread; this will only work if it has no articles
                if(thread != null)
                    thread.remove(conn);
            }
        }
        private static string getOrderBy(Sorting sorting)
        {
            string strOrder;
            switch (sorting)
            {
                case Sorting.Latest:
                    strOrder = "datetime_published DESC"; break;
                case Sorting.Oldest:
                    strOrder = "datetime_published ASC"; break;
                case Sorting.TitleAZ:
                    strOrder = "title ASC"; break;
                case Sorting.TitleZA:
                    strOrder = "title DESC"; break;
                default:
                    throw new Exception("Unknown sorting specified!");
            }
            return strOrder;
        }
        /// <summary>
        /// Returns the total number of articles pending publication.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>The number of articles pending publication.</returns>
        public static int getTotalPendingArticles(Connector conn)
        {
            return conn.queryCount("SELECT COUNT('') FROM ba_article WHERE published='0';");
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
            this.header.clear();
            if(header.Length > 0)
                this.header.addMultiple(header.ToString());
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
        public ArticleHeader HeaderData
        {
            get
            {
                return header;
            }
            set
            {
                lock (this)
                {
                    header = value;
                    modified |= Fields.HeaderData;
                }
            }
        }
    }
}