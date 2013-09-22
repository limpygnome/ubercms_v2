using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;
using CMS.Base;
using CMS.BasicSiteAuth;
using CMS.BasicSiteAuth.Models;
using CMS.Plugins;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public partial class BasicArticles : Plugin
    {
        // Enums *******************************************************************************************************
        public enum ArticleCreatePostback
        {
            None,
            Source,
            Render,
            CreateEdit
        };
        // Constants ***************************************************************************************************
        private const string REBUILD_MESSAGE = "The articles system is currently in read-only mode due to article rebuilding!";
        // Fields ******************************************************************************************************
        private bool rebuilding = false;        // Indicates if articles are being rebuilt; if this is true, the system
                                                // is in read-only mode.
        // Methods - Constructors **************************************************************************************
        public BasicArticles(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        { }
        // Methods - Handlers ******************************************************************************************
        public override bool install(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Install SQL
            if (!BaseUtils.executeSQL(PathSQL + "/install.sql", conn, ref messageOutput))
                return false;
#if TextRenderer
            RenderProvider tr = new ArticleTextRenderer(UUID.parse("da38909c-d348-478f-9add-45be7847695c"), this.UUID, "Basic Articles - General Formatting", "Provides general markup for interacting with the basic articles plugin.", true, 0);
#endif
            // Install settings
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__ARTICLE_THREAD_URL_MIN, Settings.SETTINGS__ARTICLE_THREAD_URL_MIN__DESC, Settings.SETTINGS__ARTICLE_THREAD_URL_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__ARTICLE_THREAD_URL_MAX, Settings.SETTINGS__ARTICLE_THREAD_URL_MAX__DESC, Settings.SETTINGS__ARTICLE_THREAD_URL_MAX__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TITLE_LENGTH_MIN, Settings.SETTINGS__TITLE_LENGTH_MIN__DESC, Settings.SETTINGS__TITLE_LENGTH_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TITLE_LENGTH_MAX, Settings.SETTINGS__TITLE_LENGTH_MAX__DESC, Settings.SETTINGS__TITLE_LENGTH_MAX__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TEXT_LENGTH_MIN, Settings.SETTINGS__TEXT_LENGTH_MIN__DESC, Settings.SETTINGS__TEXT_LENGTH_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TEXT_LENGTH_MAX, Settings.SETTINGS__TEXT_LENGTH_MAX__DESC, Settings.SETTINGS__TEXT_LENGTH_MAX__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE, Settings.SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE__DESC, Settings.SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_IMAGE_LENGTH_MIN, Settings.SETTINGS__THREAD_IMAGE_LENGTH_MIN__DESC, Settings.SETTINGS__THREAD_IMAGE_LENGTH_MIN__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_IMAGE_LENGTH_MAX, Settings.SETTINGS__THREAD_IMAGE_LENGTH_MAX__DESC, Settings.SETTINGS__THREAD_IMAGE_LENGTH_MAX__VALUE);
            Core.Settings.set(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS, Settings.SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS__DESC, Settings.SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_IMAGE_WIDTH, Settings.SETTINGS__THREAD_IMAGE_WIDTH__DESC, Settings.SETTINGS__THREAD_IMAGE_WIDTH__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_IMAGE_HEIGHT, Settings.SETTINGS__THREAD_IMAGE_HEIGHT__DESC, Settings.SETTINGS__THREAD_IMAGE_HEIGHT__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_TAG_LENGTH_MIN, Settings.SETTINGS__THREAD_TAG_LENGTH_MIN__DESC, Settings.SETTINGS__THREAD_TAG_LENGTH_MIN__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_TAG_LENGTH_MAX, Settings.SETTINGS__THREAD_TAG_LENGTH_MAX__DESC, Settings.SETTINGS__THREAD_TAG_LENGTH_MAX__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__THREAD_TAGS_MAX, Settings.SETTINGS__THREAD_TAGS_MAX__DESC, Settings.SETTINGS__THREAD_TAGS_MAX__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__BROWSER_ARTICLES_PER_PAGE, Settings.SETTINGS__BROWSER_ARTICLES_PER_PAGE__DESC, Settings.SETTINGS__BROWSER_ARTICLES_PER_PAGE__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__BROWSER_TAGS_POPULATED_LIMIT, Settings.SETTINGS__BROWSER_TAGS_POPULATED_LIMIT__DESC, Settings.SETTINGS__BROWSER_TAGS_POPULATED_LIMIT__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__BROWSER_TAGS_PER_PAGE, Settings.SETTINGS__BROWSER_TAGS_PER_PAGE__DESC, Settings.SETTINGS__BROWSER_TAGS_PER_PAGE__VALUE);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE, Settings.SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE__DESC, Settings.SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE__VALUE);
            Core.Settings.set(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG, Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG__DESC, Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG__VALUE);
            Core.Settings.set(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE, Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE__DESC, Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE__VALUE);
            Core.Settings.save(conn);
            return true;
        }
        public override bool uninstall(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Uninstall SQL
            if (!BaseUtils.executeSQL(PathSQL + "/uninstall.sql", conn, ref messageOutput))
                return false;
            // Uninstall settings
            Core.Settings.remove(conn, this);
            return true;
        }
        public override bool enable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Install templates
            if (!Core.Templates.install(conn, this, PathTemplates, ref messageOutput))
                return false;
            // Install content
            if (!BaseUtils.contentInstall(PathContent, Core.PathContent, true, ref messageOutput))
                return false;
            // Install URL rewriting
            if (!BaseUtils.urlRewritingInstall(conn, this, new string[] { "articles_home", "articles", "article", "thread" }, ref messageOutput))
                return false;
            return true;
        }
        public override bool disable(Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Uninstall URL rewriting
            if (!BaseUtils.urlRewritingUninstall(conn, this, ref messageOutput))
                return false;
            // Uninstall content
            if (!BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput))
                return false;
            // Uninstall templates
            if (!Core.Templates.uninstall(conn, this, ref messageOutput))
                return false;
            return true;
        }
        public override bool handler_handleRequest(Data data)
        {
            switch (data.PathInfo[0])
            {
                case "article":
                    switch (data.PathInfo[1])
                    {
                        case "create":
                            return pageEditor(data, false);
                        case "edit":
                            return pageEditor(data, true);
                        default:
                            return pageArticle(data, false);
                    }
                case "articles":
                    switch (data.PathInfo[1])
                    {
                        // -- Viewing
                        case null:
                        case "latest":
                            return pageArticles_browserArticles(data, Article.Sorting.Latest, null, null, 2, false);
                        case "oldest":
                            return pageArticles_browserArticles(data, Article.Sorting.Oldest, null, null, 2, false);
                        case "title_az":
                            return pageArticles_browserArticles(data, Article.Sorting.TitleAZ, null, null, 2, false);
                        case "title_za":
                            return pageArticles_browserArticles(data, Article.Sorting.TitleZA, null, null, 2, false);
                        case "pending":
                            return pageArticles_browserArticles(data, Article.Sorting.Oldest, null, null, 3, true);
                        case "render":
                            Article.Sorting sorting;
                            switch (data.PathInfo[3])
                            {
                                default:
                                case "latest":
                                    sorting = Article.Sorting.Latest; break;
                                case "oldest":
                                    sorting = Article.Sorting.Oldest; break;
                                case "title_az":
                                    sorting = Article.Sorting.TitleAZ; break;
                                case "title_za":
                                    sorting = Article.Sorting.TitleZA; break;
                            }
                            return pageRenderStream(data, sorting, data.PathInfo[2], false, 4);
                        // -- Operations
                        case "search":
                            if (data.Request.QueryString["query"] != null)
                                return pageArticles_browserArticles(data, Article.Sorting.TitleZA, data.Request.QueryString["query"], null, 3, false);
                            break;
                        case "rebuild":
                            return pageArticles_browserRebuild(data);
                        case "tag":
                            return pageArticles_browserArticles(data, Article.Sorting.Latest, null, data.PathInfo[2], 4, false);
                        case "tags":
                            return pageArticles_browserTags(data);
                        default:
                            return false;
                    }
                    break;
                case "articles_home":
                    return pageRenderStream(data, Article.Sorting.Latest, Core.Settings[Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG].get<string>(), true, 1);
                case "thread":
                    return pageThread(data);
                default:
                    return pageArticle(data, true);
            }
            return false;
        }
        // Methods - Pages *********************************************************************************************
        private bool pageEditor(Data data, bool edit)
        {
            // Fetch the current user model
            User user = BasicSiteAuth.BasicSiteAuth.getCurrentUser(data);
            // Check if we're in edit-mode; if so, fetch the article
            Article article = null;
            if (edit)
            {
                UUID temp = UUID.parse(data.PathInfo[2]);
                if (temp == null)
                    return false;
                else if ((article = Article.load(data.Connector, temp, Article.Text.Raw)) == null)
                    return false;
            }
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Check the user has permission to create/edit an article
            if (!ArticleThreadPermissions.isAuthorised(user, edit ? ArticleThreadPermissions.Action.Edit : ArticleThreadPermissions.Action.Create, null, article))
                return false;
            // Prepare for postback
            string error = rebuilding ? REBUILD_MESSAGE : null;
            // Check for postback
            ArticleCreatePostback postback;
            if (data.Request.Form["article_display_raw"] != null)
                postback = ArticleCreatePostback.Source;
            else if (data.Request.Form["article_display_rendered"] != null)
                postback = ArticleCreatePostback.Render;
            else if (data.Request.Form["article_create"] != null)
                postback = ArticleCreatePostback.CreateEdit;
            else
                postback = ArticleCreatePostback.None;
            // Fetch postback values
            string title = data.Request.Form["article_title"];
            string url = data.Request.Form["article_url"];
            string raw = data.Request.Form["article_raw"];
            bool html = data.Request.Form["article_html"] != null || (postback == ArticleCreatePostback.None && article != null && article.HTML);
            bool hidePanel = data.Request.Form["article_hide_panel"] != null || (postback == ArticleCreatePostback.None && article != null && article.HidePanel);
            bool comments = data.Request.Form["article_comments"] != null || (postback == ArticleCreatePostback.None && article != null && article.Comments);
            bool createNew = edit && data.Request.Form["article_create_new"] != null;
            // Handle (possible) postback data
            if (error == null && postback != ArticleCreatePostback.None && title != null && (edit || url != null) && raw != null)
            {
                // Check if we're making or modifying an article
                if (article == null)
                    article = new Article();
                else if (createNew)
                {
                    Article temp = article;
                    article = new Article();
                    article.UUIDThread = temp.UUIDThread;
                }
                else
                    article.DateTimeModified = DateTime.Now;
                // Set the model's data
                article.Title = title;
                article.TextRaw = raw;
                article.HTML = html;
                article.HidePanel = hidePanel;
                article.Comments = comments;
                article.UserIdAuthor = user != null ? user.UserID : -1;
                // Check if to render the article
                if (postback == ArticleCreatePostback.CreateEdit || postback == ArticleCreatePostback.Render)
                {
                    article.rebuild(data);
                    BaseUtils.headerAppend(article.HeaderData.compile(), ref data);
                }
                // Check if to persist the article
                if (error == null && postback == ArticleCreatePostback.CreateEdit)
                {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                    if (error == null && !Captcha.isCaptchaCorrect(data))
                        error = "Invalid captcha verification code!";
#endif
                    // Check no errors have occurred thus far with security/validation
                    if (error == null)
                    {
                        // If the user has publish permissions, the article will be published under their account
                        bool canPublish = ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Publish, null, article);
                        if (canPublish)
                        {
                            article.UserIdPublisher = user.UserID;
                            article.Published = true;
                            article.DateTimePublished = DateTime.Now;
                        }
                        else
                        {
                            article.Published = false;
                            article.DateTimePublished = DateTime.MinValue;
                        }
                        // Attempt to persist the model
                        Article.PersistStatus ps = article.save(data.Connector);
                        if (ps != Article.PersistStatus.Success)
                        { // Failed to persist...check why...
                            switch (ps)
                            {
                                case Article.PersistStatus.Invalid_uuid_article:
                                case Article.PersistStatus.Invalid_thread:
                                case Article.PersistStatus.Error:
                                    error = "An error occurred creating the article, please try again later!"; break;
                                case Article.PersistStatus.Invalid_text_length:
                                    error = "Source/raw article must be " + Core.Settings[Settings.SETTINGS__TEXT_LENGTH_MIN].get<int>() + " to " + Core.Settings[Settings.SETTINGS__TEXT_LENGTH_MAX].get<int>() + " characters in length!"; break;
                                case Article.PersistStatus.Invalid_title_length:
                                    error = "Title must be " + Core.Settings[Settings.SETTINGS__TITLE_LENGTH_MIN].get<int>() + " to " + Core.Settings[Settings.SETTINGS__TITLE_LENGTH_MAX].get<int>() + " characters in length!"; break;
                                default:
                                    error = "Unknown issue occurred persisting the article!"; break;
                            }
                        }
                        else
                        {
                            // Fetch/create article thread based on the URL if this is a new article
                            if (!edit)
                            {
                                ArticleThread at = null;
                                ArticleThread.CreateThread ct = ArticleThread.createFetch(data.Connector, this, url, out at);
                                // Check we have fetched/created a thread
                                if (ct != ArticleThread.CreateThread.Success)
                                { // Failed...
                                    switch (ct)
                                    {
                                        case ArticleThread.CreateThread.UrlUsed:
                                            error = "URL already in-use!"; break;
                                        case ArticleThread.CreateThread.UrlInvalid:
                                            error = "Invalid URL!"; break;
                                        case ArticleThread.CreateThread.Error:
                                            error = "Unknown error fetching the thread!"; break;
                                    }
                                    article.remove(data.Connector, at);
                                }
                                else
                                {
                                    // Assign thread to the article and set the article as the current article for the thread
                                    article.UUIDThread = at.UUIDThread;
                                    at.UUIDArticleCurrent = article.UUIDArticle;
                                    // Persist the model data
                                    if (article.save(data.Connector) != Article.PersistStatus.Success)
                                    {
                                        article.remove(data.Connector, at);
                                        error = "Unknown error occurred (article persistence)!";
                                    }
                                    else if (!at.save(data.Connector))
                                    {
                                        article.remove(data.Connector, at);
                                        error = "Unknown error occurred (thread persistence)!";
                                    }
                                    else
                                    {
                                        // Redirect to the article
                                        BaseUtils.redirectAbs(data, "/article/" + article.UUIDArticle.Hex);
                                    }
                                }
                            }
                            else if (createNew && canPublish)
                            {
                                // Set the current article for the thread
                                ArticleThread at = ArticleThread.load(data.Connector, article.UUIDThread);
                                if (at != null)
                                {
                                    at.UUIDArticleCurrent = article.UUIDArticle;
                                    at.save(data.Connector);
                                }
                                BaseUtils.redirectAbs(data, "/article/" + article.UUIDArticle.Hex);
                            }
                            else
                                BaseUtils.redirectAbs(data, "/article/" + article.UUIDArticle.Hex);
                        }
                    }
                }
            }
            // Setup the page
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            BaseUtils.headerAppendJs("/content/js/basic_articles.js", ref data);
            // Set content
            data["Title"] = edit ? "Articles - Edit" : "Articles - Create";
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/create");
            // Set fields
            data["article_title"] = HttpUtility.HtmlEncode(title != null ? title : article != null ? article.Title : string.Empty);
            if (!edit)
                data["article_url"] = HttpUtility.HtmlEncode(url);
            else
                data["article_edit"] = article.UUIDArticle.Hex;
            data["article_raw"] = HttpUtility.HtmlEncode(raw != null ? raw : article != null ? article.TextRaw : null);
            if (postback == ArticleCreatePostback.Render)
                data["article_rendered"] = article.TextCache;
            // Set flags
            if (html)
                data.setFlag("article_html");
            if (hidePanel)
                data.setFlag("article_hide_panel");
            if (comments)
                data.setFlag("article_comments");
            // Set error message
            if (error != null)
                data["article_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageRenderStream(Data data, Article.Sorting sorting, string tagFilter, bool homepage, int pageIndex)
        {
            if (tagFilter == null && !homepage)
                return false;
            // Check page number
            int page;
            if (data.PathInfo[pageIndex] == null || !int.TryParse(data.PathInfo[pageIndex], out page) || page < 1)
                page = 1;
            // Load articles
            int articlesPerPage = Core.Settings[Settings.SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE].get<int>();
            Article[] articles = Article.load(data.Connector, null, sorting, tagFilter, null, articlesPerPage, page, Article.Text.Rendered, true, Article.PublishFilter.Published, true);
            // Display articles
            StringBuilder content = new StringBuilder();
            {
                ArticleThread thread;
                Article article;
                Data dataRender;
                int count = articles.Length > articlesPerPage ? articlesPerPage : articles.Length;
                StringBuilder buffer;
                string template = Core.Templates.get(data.Connector, "basic_articles/renderstream_item");
                for (int i = 0; i < count; i++)
                {
                    article = articles[i];
                    if ((thread = ArticleThread.load(data.Connector, article.UUIDThread)) != null)
                    {
                        dataRender = new Data(null, null);
                        dataRender.Connector = data.Connector;
                        buffer = new StringBuilder(template);
                        // Set render parameters
                        dataRender["article_uuid"] = article.UUIDArticle.Hex;
                        dataRender["thread_uuid"] = thread.UUIDThread.Hex;
                        dataRender["title"] = HttpUtility.HtmlEncode(article.Title);
                        if (thread.Thumbnail)
                            dataRender["thumbnail"] = thread.UrlThumbnail;
                        dataRender["url"] = thread.Url != null ? "/" + thread.Url.FullPath : "/thread/" + thread.UUIDThread.Hex;
                        if (thread.Description != null)
                            dataRender["description"] = HttpUtility.HtmlEncode(thread.Description);
                        dataRender["datetime_published"] = HttpUtility.HtmlEncode(BaseUtils.dateTimeToHumanReadable(article.DateTimePublished));
                        dataRender["datetime_published_full"] = HttpUtility.HtmlEncode(article.DateTimePublished.ToString());
                        dataRender["content"] = article.TextCache;
                        // Render and append to content
                        Core.Templates.render(ref buffer, ref dataRender);
                        content.Append(buffer.ToString());
                        // Add header data to page
                        if (article.HeaderData != null)
                            BaseUtils.headerAppend(article.HeaderData.compile(), ref data);
                    }
                }
            }
            // Set page data
            data["Title"] = homepage ? Core.Settings[Settings.SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE].get<string>() : tagFilter != null ? (tagFilter.Length > 1 ? tagFilter.Substring(0, 1).ToUpper() + tagFilter.Substring(1) : tagFilter) : "Articles - Stream";
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/renderstream");
            data["articles"] = content.ToString();
            if (page > 1)
                data["articles_url_prev"] = homepage ? "/articles_home/" + (page-1) : "/articles/render/" + (tagFilter != null ? HttpUtility.UrlEncode(tagFilter) + "/" : string.Empty) + (sorting == Article.Sorting.Latest ? "latest/" : sorting == Article.Sorting.Oldest ? "oldest/" : sorting == Article.Sorting.TitleAZ ? "title_az/" : sorting == Article.Sorting.TitleZA ? "title_za/" : "latest/") + (page-1);
            if (page < int.MaxValue && articles.Length > articlesPerPage)
                data["articles_url_next"] = homepage ? "/articles_home/" + (page+1) : "/articles/render/" + (tagFilter != null ? HttpUtility.UrlEncode(tagFilter) + "/" : string.Empty) + (sorting == Article.Sorting.Latest ? "latest/" : sorting == Article.Sorting.Oldest ? "oldest/" : sorting == Article.Sorting.TitleAZ ? "title_az/" : sorting == Article.Sorting.TitleZA ? "title_za/" : "latest/") + (page+1);
            data["articles_page_curr"] = page.ToString();
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            return true;
        }
        // Methods - Pages - Articles **********************************************************************************
        private bool pageArticle(Data data, bool urlPath)
        {
            string action = data.PathInfo[2];
            Article article = null;
            ArticleThread thread = null;
            ArticleThreadPermissions perms = null;
            User user = BasicSiteAuth.BasicSiteAuth.getCurrentUser(data);
            // Load the article and thread models
            if (urlPath)
            {
                if ((thread = ArticleThread.load(data.Connector, data.PathInfo.FullPath)) == null)
                    return false;
                else if (thread.UUIDArticleCurrent == null)
                    return false;
                else if ((article = Article.load(data.Connector, thread.UUIDArticleCurrent, action == "rebuild" ? Article.Text.Raw : Article.Text.Rendered)) == null)
                    return false;
            }
            else if (data.PathInfo[0] == "thread")
            {
                UUID t = UUID.parse(data.PathInfo[1]);
                if (t == null)
                    return false;
                else if ((thread = ArticleThread.load(data.Connector, t)) == null)
                    return false;
                else if (thread.UUIDArticleCurrent == null || (article = Article.load(data.Connector, thread.UUIDArticleCurrent, action == "rebuild" ? Article.Text.Raw : Article.Text.Rendered)) == null)
                    return false;
            }
            else
            {
                UUID t = UUID.parse(data.PathInfo[1]);
                if (t == null)
                    return false;
                else if ((article = Article.load(data.Connector, t, action == "rebuild" ? Article.Text.Raw : Article.Text.Rendered)) == null)
                    return false;
                else if ((thread = ArticleThread.load(data.Connector, article.UUIDThread)) == null)
                    return false;
            }
            // Load permissions and check the user is authorised to at least view the article
            perms = ArticleThreadPermissions.load(data.Connector, thread.UUIDThread);
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.View, perms, article))
                return false;
            // Check the article has been published, else enact safe-mode (no content is shown to non-publishers)
            bool safeMode = !article.Published && !ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Publish, perms, article);
            // Set initial page layout
            data["Title"] = HttpUtility.HtmlEncode(article.Title);
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/article");
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            // Check which action to perform
            switch (action)
            {
                case null:
                case "print":
                    if (!pageArticle_view(data, thread, article, user, perms, safeMode, action == "print"))
                        return false;
                    break;
                case "publish":
                    if (!pageArticle_publish(data, thread, article, user, perms))
                        return false;
                    break;
                case "set":
                    if (!pageArticle_set(data, thread, article, user, perms))
                        return false;
                    break;
                case "rebuild":
                    if (!pageArticle_rebuild(data, thread, article, user, perms))
                        return false;
                    break;
                case "delete":
                    if (!pageArticle_delete(data, thread, article, user, perms))
                        return false;
                    break;
                default:
                    return false;
            }
            // Set common data
            data["article_uuid"] = article.UUIDArticle.Hex;
            data["thread_uuid"] = thread.UUIDThread.Hex;
            if (article.Published)
            {
                data["article_published"] = BaseUtils.dateTimeToHumanReadable(article.DateTimePublished);
                data["article_published_datetime"] = article.DateTimePublished.ToString();
            }
            else if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Publish, perms, article))
                data.setFlag("article_can_publish");
            if (article.DateTimeModified != DateTime.MinValue)
            {
                data["article_modified"] = BaseUtils.dateTimeToHumanReadable(article.DateTimeModified);
                data["article_modified_datetime"] = article.DateTimeModified.ToString();
            }
            data["article_created"] = BaseUtils.dateTimeToHumanReadable(article.DateTimeCreated);
            data["article_created_datetime"] = article.DateTimeCreated.ToString();
            if (thread.Url != null)
                data["article_url"] = BaseUtils.getAbsoluteURL(data, thread.Url.FullPath);
            // Set flags
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Edit, perms, article))
                data.setFlag("article_modify");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Rebuild, perms, article))
                data.setFlag("article_rebuild");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Delete, perms, article))
                data.setFlag("article_delete");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditPermissions, perms, article))
                data.setFlag("thread_permissions");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditThreadInfo, perms, article))
            {
                data.setFlag("thread_info");
                if (thread.UUIDArticleCurrent != article.UUIDArticle)
                    data.setFlag("article_set");
            }
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.DeleteThread, perms, article))
                data.setFlag("thread_delete");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.MoveThread, perms, null))
                data.setFlag("thread_move");
            return true;
        }
        private bool pageArticle_view(Data data, ArticleThread thread, Article article, User user, ArticleThreadPermissions perms, bool safeMode, bool printMode)
        {
            // -- View the article
            data["article_content"] = safeMode ? Core.Templates.get(data.Connector, "basic_articles/not_published") : article.TextCache;
            // -- Build tags
            StringBuilder bufferTags = new StringBuilder();
            ArticleThreadTags tags = ArticleThreadTags.load(data.Connector, thread.UUIDThread);
            string template = Core.Templates.get(data.Connector, "basic_articles/article_tag");
            string temp;
            foreach (Tag tag in tags)
            {
                temp = template;
                temp = temp.Replace("%TAG_KEYWORD%", HttpUtility.HtmlEncode(tag.Keyword));
                bufferTags.Append(temp);
            }
            if (bufferTags.Length > 0)
                data["article_tags"] = bufferTags.ToString();
            // Set flags
            if (!article.HidePanel || data.Request.QueryString["show_panel"] != null)
                data.setFlag("article_show_panel");
            else
                data.setFlag("article_showpanel_button");
            // Add header-data
            if (article.HeaderData != null)
                BaseUtils.headerAppend(article.HeaderData.compile(), ref data);
            // Check if we're in print-mode, if so change the layout/template
            if (printMode)
                data["Page"] = Core.Templates.get(data.Connector, "basic_articles/layout_print");
            return true;
        }
        private bool pageArticle_set(Data data, ArticleThread thread, Article article, User user, ArticleThreadPermissions perms)
        {
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditThreadInfo, perms, article) || thread.UUIDArticleCurrent == article.UUIDArticle)
                return false;
            string error = rebuilding ? REBUILD_MESSAGE : null;
            if (error == null && data.Request.Form["article_set"] != null)
            {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        return false;
#endif
                thread.UUIDArticleCurrent = article.UUIDArticle;
                if (!thread.save(data.Connector))
                    error = "Failed to set this article as the current article!";
                else
                    BaseUtils.redirectAbs(data, "/thread/" + thread.UUIDThread.Hex);
            }
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/article_set");
            if (error != null)
                data["article_error"] = HttpUtility.HtmlEncode(error);
            data.setFlag("article_show_panel");
            return true;
        }
        private bool pageArticle_publish(Data data, ArticleThread thread, Article article, User user, ArticleThreadPermissions perms)
        {
            if (article.Published || !ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Publish, perms, article))
                return false;
            string error = rebuilding ? REBUILD_MESSAGE : null;
            if (error == null && data.Request.Form["article_publish"] != null)
            {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        return false;
#endif
                article.Published = true;
                article.DateTimePublished = DateTime.Now;
                if (user != null)
                    article.UserIdPublisher = user.UserID;
                if (article.save(data.Connector) != Article.PersistStatus.Success)
                    error = "Unable to publish article, please try again!";
                else
                    BaseUtils.redirectAbs(data, "/article/" + article.UUIDArticle.Hex);
            }
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/article_publish");
            if (error != null)
                data["article_error"] = HttpUtility.HtmlEncode(error);
            data.setFlag("article_show_panel");
            return true;
        }
        private bool pageArticle_rebuild(Data data, ArticleThread thread, Article article, User user, ArticleThreadPermissions perms)
        {
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Rebuild, perms, article))
                return false;
            string error = rebuilding ? REBUILD_MESSAGE : null;
            if (error == null && data.Request.Form["article_rebuild"] != null)
            {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        return false;
#endif
                // Rebuild the article
                article.rebuild(data);
                // Persist the new data
                Article.PersistStatus tps;
                if ((tps = article.save(data.Connector)) != Article.PersistStatus.Success)
                    error = "An unknown error occurred rebuilding the article (" + tps.ToString() + "), please try again!";
                else
                    BaseUtils.redirectAbs(data, "/article/" + article.UUIDArticle.Hex);
            }
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/article_rebuild");
            if (error != null)
                data["article_error"] = HttpUtility.HtmlEncode(error);
            data.setFlag("article_show_panel");
            return true;
        }
        private bool pageArticle_delete(Data data, ArticleThread thread, Article article, User user, ArticleThreadPermissions perms)
        {
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Delete, perms, article))
                return false;
            string error = rebuilding ? REBUILD_MESSAGE : null;
            if (error == null && data.Request.Form["article_delete"] != null)
            {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        return false;
#endif
                // Delete the article
                article.remove(data.Connector, thread);
                // Redirect to home
                BaseUtils.redirectAbs(data, "/articles");
            }
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/article_delete");
            if (error != null)
                data["article_error"] = HttpUtility.HtmlEncode(error);
            data.setFlag("article_show_panel");
            return true;
        }
        // Methods - Pages - Threads ***********************************************************************************
        private bool pageThread(Data data)
        {
            string action = data.PathInfo[2];
            if (action == null || action.Length == 0)
                return pageArticle(data, false);
            User user = BasicSiteAuth.BasicSiteAuth.getCurrentUser(data);
            ArticleThread thread = null;
            // Load the thread
            {
                UUID temp;
                if ((temp = UUID.parse(data.PathInfo[1])) == null || (thread = ArticleThread.load(data.Connector, temp)) == null)
                    return false;
            }
            ArticleThreadPermissions perms = ArticleThreadPermissions.load(data.Connector, thread.UUIDThread);
            // Set common data
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/article");
            data["thread_uuid"] = thread.UUIDThread.Hex;
            data["article_uuid"] = thread.UUIDArticleCurrent != null ? thread.UUIDArticleCurrent.Hex : string.Empty;
            data.setFlag("article_thread");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditPermissions, perms, null))
                data.setFlag("thread_permissions");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditThreadInfo, perms, null))
                data.setFlag("thread_info");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.DeleteThread, perms, null))
                data.setFlag("thread_delete");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.MoveThread, perms, null))
                data.setFlag("thread_move");
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            // Handle action
            switch (action)
            {
                case "revisions":
                    return pageThread_revisions(data, thread, user, perms);
                case "permissions":
                    return pageThread_permissions(data, thread, user, perms);
                case "info":
                    return pageThread_info(data, thread, user, perms);
                case "delete":
                    return pageThread_delete(data, thread, user, perms);
                case "move":
                    return pageThread_move(data, thread, user, perms);
                default:
                    return false;
            }
        }
        private bool pageThread_revisions(Data data, ArticleThread thread, User user, ArticleThreadPermissions perms)
        {
            // Parse page options
            string praw = data.PathInfo[3];
            int page;
            if (praw == null || !int.TryParse(praw, out page) || page < 1)
                page = 1;
            // Build list of articles
            StringBuilder revisions = new StringBuilder();
            string templateRevision = Core.Templates.get(data.Connector, "basic_articles/thread_revisions_article");
            StringBuilder temp;
            Data tempData;
            int limit = Core.Settings[Settings.SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE].get<int>();
            Article[] articles = Article.load(data.Connector, thread.UUIDThread, Article.Sorting.Latest, null, null, limit, page, Article.Text.None, false, Article.PublishFilter.Both, true);
            foreach (Article article in articles)
            {
                temp = new StringBuilder(templateRevision);
                tempData = new Data(null, null);
                // Set render parameters
                tempData["uuid_article"] = article.UUIDArticle.Hex;
                tempData["title"] = HttpUtility.HtmlEncode(article.Title);
                tempData["datetime_created"] = HttpUtility.HtmlEncode(BaseUtils.dateTimeToHumanReadable(article.DateTimeCreated));
                tempData["datetime_created_full"] = HttpUtility.HtmlEncode(article.DateTimeCreated.ToString());
                if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Edit, perms, article))
                    tempData.setFlag("article_modify");
                if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Rebuild, perms, article))
                    tempData.setFlag("article_rebuild");
                if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Delete, perms, article))
                    tempData.setFlag("article_delete");
                if (article.UUIDArticle == thread.UUIDArticleCurrent)
                    tempData.setFlag("article_selected");
                // Render and append
                Core.Templates.render(ref temp, ref tempData);
                revisions.Append(temp);
            }
            // Set data
            data["Title"] = "Thread - Revisions";
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/thread_revisions");
            data["article_page"] = page.ToString();
            if (page > 1)
                data["article_page_prev"] = (page - 1).ToString();
            if (page < int.MaxValue && articles.Length > limit)
                data["article_page_next"] = (page + 1).ToString();
            if (revisions.Length > 0)
                data["article_revisions"] = revisions.ToString();
            return true;
        }
        private bool pageThread_permissions(Data data, ArticleThread thread, User user, ArticleThreadPermissions perms)
        {
            // Check the user is allowed to modify permissions
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditPermissions, perms, null))
                return false;
            string success = null;
            string error = rebuilding ? REBUILD_MESSAGE : null;
            // Check for postback
            bool postback = data.Request.Form["thread_permissions"] != null;
            if (error == null && postback)
            {
                BasicSiteAuth.BasicSiteAuth bsa = BasicSiteAuth.BasicSiteAuth.getCurrentInstance();
                // Clear permissions
                perms.clear();
                // Readd
                int groupid;
                const string ARTICLE_PERMISSION_FIELD_NAME = "article_permission_";
                foreach (string s in data.Request.Form.AllKeys)
                {
                    if (s.StartsWith(ARTICLE_PERMISSION_FIELD_NAME) && s.Length > ARTICLE_PERMISSION_FIELD_NAME.Length && int.TryParse(s.Substring(ARTICLE_PERMISSION_FIELD_NAME.Length), out groupid))
                    {
                        if (bsa.UserGroups.contains(groupid))
                            perms.add(groupid);
                        else
                            error = "Group with identifier '" + groupid + "' does not exist!";
                    }
                }
                if (error == null)
                {
                    // Persist the model
                    if (!perms.save(data.Connector))
                        error = "An error occurred persisting the new permissions!";
                    else
                        success = "Updated thread viewing permission(s)! " + perms.UserGroups.Count;
                }
            }
            // Build user groups list
            StringBuilder buffer = new StringBuilder();
            string template = Core.Templates.get(data.Connector, "basic_articles/thread_permissions_item");
            StringBuilder temp;
            Data tempData;
            foreach (KeyValuePair<int,UserGroup> group in BasicSiteAuth.BasicSiteAuth.getCurrentInstance().UserGroups)
            {
                temp = new StringBuilder(template);
                tempData = new Data(null, null);
                // Set data
                tempData["usergroup_id"] = group.Value.GroupID.ToString();
                tempData["usergroup_title"] = HttpUtility.HtmlEncode(group.Value.Title);
                if ((postback && data.Request.Form["article_permission_" + group.Value.GroupID] != null) || (!postback && perms.contains(group.Value)))
                    tempData.setFlag("usergroup_selected");
                // Render and append
                Core.Templates.render(ref temp, ref tempData);
                buffer.Append(temp);
            }
            if (buffer.Length > 0)
                data["thread_permissions"] = buffer.ToString();
            // Set data
            data["Title"] = "Thread - Permissions";
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/thread_permissions");
            if (success != null)
                data["thread_perms_success"] = HttpUtility.HtmlEncode(success);
            if (error != null)
                data["thread_perms_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageThread_info(Data data, ArticleThread thread, User user, ArticleThreadPermissions perms)
        {
            // Check the user is allowed to modify permissions
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.EditThreadInfo, perms, null))
                return false;
            // Load tags
            ArticleThreadTags tags = ArticleThreadTags.load(data.Connector, thread.UUIDThread);
            string  error = rebuilding ? REBUILD_MESSAGE : null,
                    success = null;
            // Check for postback
            string pbDescription = data.Request.Form["description"];
            string pbTags = data.Request.Form["tags"];
            // -- Thumbnail
            if (error == null && data.Request.Files["thumbnail"] != null && data.Request.Files["thumbnail"].ContentLength > 0)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    string actionRaw = data.Request.Form["thumbnail_resize"];
                    BaseUtils.ResizeAction action = actionRaw != null ? actionRaw == "resize" ? BaseUtils.ResizeAction.Resize : actionRaw == "crop" ? BaseUtils.ResizeAction.CropEdges : BaseUtils.ResizeAction.None : BaseUtils.ResizeAction.None;
                    HttpPostedFile temp = data.Request.Files["thumbnail"];
                    int t = temp.FileName.LastIndexOf('.');
                    switch (thread.thumbnailUpdate(temp.InputStream, temp.ContentLength, t < 0 || t >= temp.FileName.Length - 1 ? string.Empty : temp.FileName.Substring(t + 1), action))
                    {
                        case ArticleThread.UpdateThumbnail.InvalidData:
                            error = "Invalid image data/type!";
                            break;
                        case ArticleThread.UpdateThumbnail.InvalidSize:
                            error = "Invalid file-size; must be between " + BaseUtils.getBytesString((float)Core.Settings[Settings.SETTINGS__THREAD_IMAGE_LENGTH_MIN].get<int>()) + " to " + BaseUtils.getBytesString((float)Core.Settings[Settings.SETTINGS__THREAD_IMAGE_LENGTH_MAX].get<int>()) + " in size!";
                            break;
                        case ArticleThread.UpdateThumbnail.Error:
                            error = "An unknown exception occurred, please try again!";
                            break;
                        case ArticleThread.UpdateThumbnail.Success:
                            success = "Successfully updated article thumbnail!";
                            break;
                    }
                }
            }
            // -- Thumbnail reset
            if (error == null && data.Request.Form["thumbnail_reset"] != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    thread.thumbnailReset();
                    success = "Thread thumbnail reset.";
                }
            }
            // -- Tags
            if (error == null && pbTags != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    string[] rawtags = pbTags.Split(',');
                    tags.clear();
                    foreach (string s in rawtags)
                    {
                        if (s.Length != 0 && !tags.add(s, data.Connector))
                            error = "Invalid tag/keyword '" + s + "', must be " + Core.Settings[Settings.SETTINGS__THREAD_TAG_LENGTH_MIN].get<int>() + " to " + Core.Settings[Settings.SETTINGS__THREAD_TAG_LENGTH_MAX].get<int>() + " characters with alpha-numeric characters (undeerscroll, hyphen and space allowed)!";
                    }
                    if (error == null)
                    {
                        ArticleThreadTags.PersistStatus ps = tags.save(data.Connector);
                        switch(ps)
                        {
                            case ArticleThreadTags.PersistStatus.Error:
                                error = "Failed to persist tags, unknown error occurred - please try again!";
                                break;
                            case ArticleThreadTags.PersistStatus.TooManyTags:
                                error = "A maximum of " + Core.Settings[Settings.SETTINGS__THREAD_TAGS_MAX].get<int>() + " tags are allowed!";
                                break;
                            case ArticleThreadTags.PersistStatus.Success:
                                success = "Successfully updated thread tags!";
                                break;
                        }
                    }
                }
            }
            // -- Description
            if (error == null && pbDescription != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    if (!thread.descriptionUpdate(pbDescription))
                        error = "Description must be " + Core.Settings[Settings.SETTINGS__THREAD_DESCRIPTION_LENGTH_MIN].get<int>() + " to " + Core.Settings[Settings.SETTINGS__THREAD_DESCRIPTION_LENGTH_MAX].get<int>() + " characters in length!";
                    else
                        success = "Successfully updated description!";
                }
            }
            // -- Check if to persist thread changes
            if (thread.IsModified && !thread.save(data.Connector))
                error = "Failed to persist thread data!";
            // Set data
            data["Title"] = "Thread - Information";
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/thread_info");
            data["thread_thumbnail_url"] = thread.UrlThumbnail;
            if (pbTags != null)
                data["thread_tags"] = HttpUtility.HtmlEncode(data.Request.Form["tags"]);
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (Tag tag in tags)
                    sb.Append(tag.Keyword).Append(",");
                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
                data["thread_tags"] = HttpUtility.HtmlEncode(sb.ToString());
            }
            data["thread_description"] = pbDescription ?? thread.Description ?? string.Empty;
            if (success != null)
                data["thread_info_success"] = HttpUtility.HtmlEncode(success);
            if (error != null)
                data["thread_info_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageThread_delete(Data data, ArticleThread thread, User user, ArticleThreadPermissions perms)
        {
            // Check the user is allowed to modify permissions
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.DeleteThread, perms, null))
                return false;
            data["Title"] = "Thread - Delete";
            string error = rebuilding ? REBUILD_MESSAGE : null;
            if (data.Request.Form["thread_delete"] != null)
            {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        return false;
#endif
                // Remove the thread
                thread.removeForce(data.Connector);
                // Redirect to home
                BaseUtils.redirectAbs(data, "/articles");
            }
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/thread_delete");
            if (error != null)
                data["thread_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageThread_move(Data data, ArticleThread thread, User user, ArticleThreadPermissions perms)
        {
            // Check the user is allowed to modify permissions
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.MoveThread, perms, null))
                return false;
            string error = null;
            // Check for postback
            string threadUrl = data.Request.Form["thread_url"];
            // -- csrf
            if (threadUrl != null)
            {
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        return false;
#endif
                if (error == null)
                {
                    UrlRewriting.PersistStatus ps = thread.move(this, data.Connector, threadUrl);
                    switch (ps)
                    {
                        case UrlRewriting.PersistStatus.Success:
                            if (thread.save(data.Connector))
                                BaseUtils.redirectAbs(data, thread.Url.FullPath);
                            else
                                error = "Failed to persist thread data! Please try again...";
                            break;
                        case UrlRewriting.PersistStatus.InUse:
                            error = "URL is already in-use!";
                            break;
                        case UrlRewriting.PersistStatus.InvalidPath:
                            error = "Invalid URL/path specified!";
                            break;
                        case UrlRewriting.PersistStatus.Error:
                        default:
                            error = "An unknown error occurred, please try again!";
                            break;
                    }
                }
            }
            // Set data
            data["Title"] = "Thread - Move";
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/thread_move");
            data["thread_url_current"] = thread.Url.FullPath;
            if (threadUrl != null)
                data["thread_url"] = HttpUtility.HtmlEncode(threadUrl);
            return true;
        }
        // Methods - Pages - Browsing **********************************************************************************
        private bool pageArticles_browserTags(Data data)
        {
            // Set the view
            if (!pageArticles_browser(data))
                return false;
            // Get the page
            int page;
            if (data.PathInfo[3] == null || !int.TryParse(data.PathInfo[3], out page) || page < 1)
                page = 1;
            // Fetch tags and build
            int tagsPerPage = Core.Settings[Settings.SETTINGS__BROWSER_TAGS_PER_PAGE].get<int>();
            Tag[] tags = Tag.load(data.Connector, page, tagsPerPage, Tag.Sorting.Keyword, true);
            StringBuilder content = new StringBuilder();
            string template = Core.Templates.get(data.Connector, "basic_articles/browser_tags_tag");
            StringBuilder buffer;
            Tag tag;
            int count = tags.Length > tagsPerPage ? tagsPerPage : tags.Length;
            for(int i = 0; i < count; i++)
            {
                buffer = new StringBuilder(template);
                tag = tags[i];
                buffer.Replace("<THREADS>", string.Format("{0:n0}", tag.ThreadReferences));
                buffer.Replace("<TAG_URL>", HttpUtility.UrlEncode(tag.Keyword));
                buffer.Replace("<TAG>", HttpUtility.HtmlEncode(tag.Keyword));
                content.Append(buffer.ToString());
            }
            data["articles_tags"] = content.ToString();
            data["browser_content"] = Core.Templates.get(data.Connector, "basic_articles/browser_tags");
            data["articles_page_curr"] = page.ToString();
            if (page > 1)
                data["articles_page_prev"] = (page - 1).ToString();
            if (page < int.MaxValue && tags.Length > tagsPerPage)
                data["articles_page_next"] = (page + 1).ToString();
            data["Title"] = "Articles - Tags";
            return true;
        }
        private bool pageArticles_browserArticles(Data data, Article.Sorting sorting, string search, string tagFilter, int pageIndex, bool pending)
        {
            // Set the view
            if (!pageArticles_browser(data))
                return false;
            // Fetch the articles to display
            int page;
            if (data.PathInfo[pageIndex] == null || !int.TryParse(data.PathInfo[pageIndex], out page) || page < 1)
                page = 1;
            Article[] articles;
            int articlesPerPage = Core.Settings[Settings.SETTINGS__BROWSER_ARTICLES_PER_PAGE].get<int>();
            if (pending)
            {
                articles = Article.load(data.Connector, null, sorting, null, search, articlesPerPage, page, Article.Text.None, false, Article.PublishFilter.NonPublished, true);
                data["Title"] = "Articles - Awaiting Publication";
            }
            else if (search != null)
            {
                articles = Article.load(data.Connector, null, sorting, null, search, articlesPerPage, page, Article.Text.Raw, true, Article.PublishFilter.Published, true);
                data["Title"] = "Articles - Seach Results for `" + HttpUtility.HtmlEncode(search) + "`";
            }
            else
            {
                articles = Article.load(data.Connector, null, sorting, tagFilter, null, articlesPerPage, page, Article.Text.None, true, Article.PublishFilter.Published, true);
                string title = "Articles - ";
                if (tagFilter != null)
                    title += "Tag `" + HttpUtility.HtmlEncode(tagFilter) + "` - ";
                switch (sorting)
                {
                    case Article.Sorting.Latest:
                        title += "Latest";
                        break;
                    case Article.Sorting.Oldest:
                        title += "Oldest";
                        break;
                    case Article.Sorting.TitleAZ:
                        title += "Title A-Z";
                        break;
                    case Article.Sorting.TitleZA:
                        title += "Title Z-A";
                        break;
                }
                data["Title"] = title;
            }
            // Check there are articles if we're viewing a tag, else 404...
            if (tagFilter != null && articles.Length == 0)
                return false;
            // Build articles
            {
                if (articles.Length > 0)
                {
                    StringBuilder buffer = new StringBuilder();
                    string template = Core.Templates.get(data.Connector, "basic_articles/browser_article");
                    StringBuilder item;
                    ArticleThread th;
                    Article a;
                    int count = articles.Length > articlesPerPage ? articlesPerPage : articles.Length;
                    for (int i = 0; i < count; i++)
                    {
                        a = articles[i];
                        // Load the thread
                        th = ArticleThread.load(data.Connector, a.UUIDThread);
                        if (th != null)
                        {
                            item = new StringBuilder(template);
                            item.Replace("<URL>", pending ? "/article/" + a.UUIDArticle.Hex : th.Url != null ? "/" + th.Url.FullPath : "/thread/" + th.UUIDThread.Hex);
                            item.Replace("<TITLE>", HttpUtility.HtmlEncode(a.Title));
                            item.Replace("<THUMBNAIL>", th.UrlThumbnail);
                            item.Replace("<DESCRIPTION>", th.Description == null ? "(none)" : th.Description);
                            item.Replace("<DATETIME_PUBLISHED>", BaseUtils.dateTimeToHumanReadable(a.DateTimePublished));
                            item.Replace("<DATETIME_PUBLISHED_FULL>", HttpUtility.HtmlEncode(a.DateTimePublished.ToString()));
                            buffer.Append(item.ToString());
                        }
                    }
                    data["articles"] = buffer.ToString();
                }
                else
                    data["articles"] = Core.Templates.get(data.Connector, "basic_articles/browser_noarticles");
            }
            // Set data
            data["articles_page_curr"] = page.ToString();
            data["browser_content"] = Core.Templates.get(data.Connector, "basic_articles/browser_articles");
            string url = pending ? "/articles/pending/" : search != null ? "/articles/search/" : tagFilter != null ? "/articles/tag/" + HttpUtility.UrlEncode(tagFilter) + "/" : "/articles/";
            url += sorting == Article.Sorting.Latest ? "latest/" : sorting == Article.Sorting.Oldest ? "oldest/" : sorting == Article.Sorting.TitleAZ ? "title_az/" : sorting == Article.Sorting.TitleZA ? "title_za/" : "";
            url += "<PAGE>";
            if (search != null)
                url += "?query=" + HttpUtility.UrlEncode(search);
            if (page > 1)
                data["articles_url_prev"] = url.Replace("<PAGE>", (page - 1).ToString());
            if (page < int.MaxValue && articles.Length > articlesPerPage)
                data["articles_url_next"] = url.Replace("<PAGE>", (page + 1).ToString());
            return true;
        }
        private bool pageArticles_browserRebuild(Data data)
        {
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Set the view
            if (!pageArticles_browser(data))
                return false;
            // Check for postback
            string error = null;
            string pbTag = data.Request.Form["articles_rebuild_tag"];
            bool pbAll = data.Request.Form["articles_rebuild_all"] != null;
            if (!rebuilding)
            {
                // Check for postback
                if (pbAll || pbTag != null)
                {
                    // Check security
#if CSRFP
                    if (!CSRFProtection.authenticated(data))
                        error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                    if (error == null && !Captcha.isCaptchaCorrect(data))
                        error = "Invalid captcha verification code!";
#endif
                    if (error == null)
                    {
                        // Fetch articles to rebuild and add to queue
                        UUID temp;
                        PreparedStatement ps = new PreparedStatement(pbAll ? "SELECT uuid_article FROM ba_articles_rebuild_all;" : "SELECT uuid_article FROM ba_articles_rebuild_tag WHERE keyword=?keyword");
                        if (!pbAll)
                            ps["keyword"] = pbTag;
                        foreach (ResultRow article in data.Connector.queryRead(ps))
                        {
                            if ((temp = UUID.parse(article["uuid_article"])) != null)
                                rbQueue.Add(temp);
                        }
                        // Launch worker
                        rebuildQueue();
                    }
                }
            }
            else
                error = REBUILD_MESSAGE;
            // Set data
            data["Title"] = "Articles - Rebuild";
            data["browser_content"] = Core.Templates.get(data.Connector, "basic_articles/browser_rebuild");
            if (error != null)
                data["article_error"] = HttpUtility.HtmlEncode(error);
            if (pbTag != null)
                data["articles_rebuild_tag"] = HttpUtility.HtmlEncode(pbTag);
            return true;
        }
        private bool pageArticles_browser(Data data)
        {
            // Load current user
            User user = BasicSiteAuth.BasicSiteAuth.getCurrentUser(data);
            // Build tags
            {
                StringBuilder buffer = new StringBuilder();
                string template = Core.Templates.get(data.Connector, "basic_articles/browser_tag");
                Tag[] tags = Tag.load(data.Connector, 1, Core.Settings[Settings.SETTINGS__BROWSER_TAGS_POPULATED_LIMIT].get<int>(), Tag.Sorting.Population, false);
                StringBuilder item;
                foreach (Tag tag in tags)
                {
                    item = new StringBuilder(template);
                    item.Replace("<TAG_URL>", HttpUtility.UrlEncode(tag.Keyword));
                    item.Replace("<TAG>", HttpUtility.HtmlEncode(tag.Keyword));
                    item.Replace("<THREADS>", string.Format("{0:n0}", tag.ThreadReferences));
                    buffer.Append(item.ToString());
                }
                if (buffer.Length > 0)
                    data["article_tags"] = buffer.ToString();
            }
            // Set data
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/browser");
            data["articles_search"] = data.Request.QueryString["query"];
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Publish, null, null))
            {
                int pending = Article.getTotalPendingArticles(data.Connector);
                data.setFlag("articles_publish");
                if (pending > 0)
                    data["articles_pending"] = string.Format("{0:n0}", pending);
            }
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Rebuild, null, null))
                data.setFlag("articles_rebuild");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Create, null, null))
                data.setFlag("articles_create");
            return true;
        }
        // Methods - Rebuilding ****************************************************************************************
        private static List<UUID> rbQueue = new List<UUID>();       // Identifiers of articles to be rebuilt.
        private static Thread thRb = null;                          // The thread responsible for rebuilding articles.
        private void rebuildQueue()
        {
            lock (this)
            {
                if (rebuilding || thRb != null)
                    return;
                rebuilding = true;
                thRb = new Thread(delegate()
                    {
                        rebuildQueueThreadWorker();
                    });
                thRb.Start();
            }
        }
        private void rebuildQueueThreadWorker()
        {
            // Setup worker
            Connector conn = Core.connectorCreate(true);
            // Rebuild each article
            Article a;
            Data data;
            foreach (UUID uuidArticle in rbQueue)
            {
                data = new Data(null, null);
                data.Connector = conn;
                a = Article.load(conn, uuidArticle, Article.Text.Raw);
                if (a != null)
                {
                    a.rebuild(data);
                    a.save(conn);
                }
            }
            // Reset flag
            lock (this)
            {
                thRb = null;
                rebuilding = false;
            }
        }
    }
}