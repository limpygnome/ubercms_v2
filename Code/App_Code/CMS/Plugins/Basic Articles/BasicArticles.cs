using System;
using System.Collections.Generic;
using System.Text;
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
                            return pageArticles_browser(data, Article.Sorting.Latest, null);
                        case "oldest":
                            return pageArticles_browser(data, Article.Sorting.Oldest, null);
                        case "title_az":
                            return pageArticles_browser(data, Article.Sorting.TitleAZ, null);
                        case "title_za":
                            return pageArticles_browser(data, Article.Sorting.TitleZA, null);
                        //case "popular":
                        //    return pageArticles_browser(data, Article.Sorting.Popular, null);
                        case "pending":
                            return pageArticles_pendingReview(data);
                        // -- Operations
                        case "change_log":
                            return pageArticles_changeLog(data);
                        case "search":
                            return pageArticles_search(data);
                        case "rebuild":
                            return pageArticles_rebuild(data, null);
                        case "tag":
                            switch (data.PathInfo[3])
                            {
                                // -- Viewing
                                case null:
                                case "latest":
                                    return pageArticles_browser(data, Article.Sorting.Latest, data.PathInfo[2]);
                                case "oldest":
                                    return pageArticles_browser(data, Article.Sorting.Oldest, data.PathInfo[2]);
                                case "title_az":
                                    return pageArticles_browser(data, Article.Sorting.TitleAZ, data.PathInfo[2]);
                                case "title_za":
                                    return pageArticles_browser(data, Article.Sorting.TitleZA, data.PathInfo[2]);
                                //case "popular":
                                //    return pageArticles_browser(data, Article.Sorting.Popular, data.PathInfo[2]);
                                // -- Operations
                                case "rebuild":
                                    return pageArticles_rebuild(data, data.PathInfo[2]);
                                default:
                                    return false; // view specific tag.
                            }
                            break;
                    }
                    break;
                case "articles_home":
                    return pageArticles_render(data, Article.Sorting.Latest, "news");
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
                else if ((article = Article.load(data.Connector, temp)) == null)
                    return false;
            }
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Check the user has permission to create/edit an article
            if (!ArticleThreadPermissions.isAuthorised(user, edit ? ArticleThreadPermissions.Action.Edit : ArticleThreadPermissions.Action.Create, null, article))
                return false;
            // Prepare for postback
            string error = null;
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
            if (postback != ArticleCreatePostback.None && title != null && (edit || url != null) && raw != null)
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
                else if ((article = action == "rebuild" ? Article.load(data.Connector, thread.UUIDArticleCurrent) : Article.loadRendered(data.Connector, thread.UUIDArticleCurrent)) == null)
                    return false;
            }
            else
            {
                UUID t = UUID.parse(data.PathInfo[1]);
                if (t == null)
                    return false;
                else if ((article = action == "rebuild" ? Article.load(data.Connector, t) : Article.loadRendered(data.Connector, t)) == null)
                    return false;
                else if ((thread = ArticleThread.load(data.Connector, article.UUIDThread)) == null)
                    return false;
            }
            // Load permissions and check the user is authorised to view the thread
            perms = ArticleThreadPermissions.load(data.Connector, thread.UUIDThread);
            if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.View, perms, article))
                return false;
            // Set initial page layout
            data["Title"] = HttpUtility.HtmlEncode(article.Title);
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/article");
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            // Check which action to perform
            switch (action)
            {
                case null:
                    // -- View the article
                    data["article_content"] = article.TextCache;
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
                    break;
                case "rebuild":
                    if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Rebuild, perms, article))
                        return false;
                    string error = null;
                    if (data.Request.Form["article_rebuild"] != null)
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
                    break;
                case "delete":
                    if (!ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.Delete, perms, article))
                        return false;
                    if (data.Request.Form["article_delete"] != null)
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
                    data.setFlag("article_show_panel");
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
                data.setFlag("thread_info");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.DeleteThread, perms, article))
                data.setFlag("thread_delete");
            if (ArticleThreadPermissions.isAuthorised(user, ArticleThreadPermissions.Action.MoveThread, perms, null))
                data.setFlag("thread_move");
            return true;
        }
        private bool pageThread(Data data)
        {
            User user = BasicSiteAuth.BasicSiteAuth.getCurrentUser(data);
            string action = data.PathInfo[2];
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
            Article[] articles = Article.loadNoContent(data.Connector, thread.UUIDThread, Article.Sorting.Latest, limit, page);
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
            if (page < int.MaxValue && limit == articles.Length)
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
            string error = null;
            // Check for postback
            bool postback = data.Request.Form["thread_permissions"] != null;
            if (postback)
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
            string  error = null,
                    success = null;
            // Check for postback
            // -- Thumbnail
            if (data.Request.Files["thumbnail"] != null && data.Request.Files["thumbnail"].ContentLength > 0)
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
            if (error == null && data.Request.Form["tags"] != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    string[] rawtags = data.Request.Form["tags"].Split(',');
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
            // -- Check if to persist thread changes
            if (thread.IsModified && !thread.save(data.Connector))
                error = "Failed to persist thread data!";
            // Set data
            data["Title"] = "Thread - Information";
            data["article_content"] = Core.Templates.get(data.Connector, "basic_articles/thread_info");
            data["thread_thumbnail_url"] = thread.UrlThumbnail;
            if (data.Request.Form["tags"] != null)
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
        // rebuilds all articles
        private bool pageArticles_rebuild(Data data, string tagFilter)
        {
            return true;
        }
        // browse articles by tag etc
        private bool pageArticles_browser(Data data, Article.Sorting sorting, string tagFilter)
        {
            return true;
        }
        // changelog activity
        private bool pageArticles_changeLog(Data data)
        {
            return true;
        }
        // articles which havent been published
        private bool pageArticles_pendingReview(Data data)
        {
            return true;
        }
        // search articles
        private bool pageArticles_search(Data data)
        {
            return true;
        }
        // for rendering multiple articles like a blog/news-reel
        private bool pageArticles_render(Data data, Article.Sorting sorting, string tagFilter)
        {
            return true;
        }
        // general health
        private bool pageArticles_admin(Data data)
        {
            // check health, usage, dead header data, dead URL rewriting etc.
            return true;
        }
    }
}