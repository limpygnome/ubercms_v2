﻿using System;
using System.Text;
using System.Web;
using CMS.Base;
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
            // Install settings
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__ARTICLE_THREAD_URL_MIN, Settings.SETTINGS__ARTICLE_THREAD_URL_MIN__DESC, Settings.SETTINGS__ARTICLE_THREAD_URL_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__ARTICLE_THREAD_URL_MAX, Settings.SETTINGS__ARTICLE_THREAD_URL_MAX__DESC, Settings.SETTINGS__ARTICLE_THREAD_URL_MAX__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TITLE_LENGTH_MIN, Settings.SETTINGS__TITLE_LENGTH_MIN__DESC, Settings.SETTINGS__TITLE_LENGTH_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TITLE_LENGTH_MAX, Settings.SETTINGS__TITLE_LENGTH_MAX__DESC, Settings.SETTINGS__TITLE_LENGTH_MAX__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TEXT_LENGTH_MIN, Settings.SETTINGS__TEXT_LENGTH_MIN__DESC, Settings.SETTINGS__TEXT_LENGTH_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TEXT_LENGTH_MAX, Settings.SETTINGS__TEXT_LENGTH_MAX__DESC, Settings.SETTINGS__TEXT_LENGTH_MAX__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TAG_KEYWORD_LENGTH_MIN, Settings.SETTINGS__TAG_KEYWORD_LENGTH_MIN__DESC, Settings.SETTINGS__TAG_KEYWORD_LENGTH_MIN__DEFAULT);
            Core.Settings.setInt(this, Base.Settings.SetAction.AddOrUpdate, Settings.SETTINGS__TAG_KEYWORD_LENGTH_MAX, Settings.SETTINGS__TAG_KEYWORD_LENGTH_MAX__DESC, Settings.SETTINGS__TAG_KEYWORD_LENGTH_MAX__DEFAULT);
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
            if (!BaseUtils.urlRewritingInstall(conn, this, new string[] { "articles_home", "articles", "article" }, ref messageOutput))
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
                            return pageArticle_create(data);
                        default:
                            return pageArticle_view(data);
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
                            }
                            break;
                    }
                    break;
                case "articles_home":
                    return pageArticles_render(data, Article.Sorting.Latest, "news");
            }
            return false;
        }

        // Methods - Pages *********************************************************************************************
        /// <summary>
        /// The editor/creator page for creating and updating articles; this will also create new threads automatically
        /// for articles at paths which have yet to be defined by a thread.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool pageArticle_create(Data data)
        {
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            string error = null;
            string rendered = null;
            string headerData = null;
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
            bool html = data.Request.Form["article_html"] != null;
            bool hidePanel = data.Request.Form["article_hide_panel"] != null;
            bool comments = data.Request.Form["article_comments"] != null;
            // Handle (possible) postback data
            if (postback != ArticleCreatePostback.None && title != null && url != null && raw != null)
            {
                // Check if to render the article
                if (postback == ArticleCreatePostback.CreateEdit || postback == ArticleCreatePostback.Render)
                {
                    StringBuilder text = new StringBuilder(raw);
                    StringBuilder header = new StringBuilder();
                    // Render text
#if TextRenderer
                    TextRenderer tr = (TextRenderer)Core.Plugins[UUID.parse(TextRenderer.TR_UUID)];
                    if (tr != null)
                    {
                        tr.render(data, ref header, ref text, RenderProvider.RenderType.Objects | RenderProvider.RenderType.TextFormatting);
                        // Set rendered field for display/persisting
                        rendered = text.ToString();
                        headerData = header.ToString();
                        // Append header data
                        BaseUtils.headerAppend(header.ToString(), ref data);
                    }
                    else
                        error = "The text renderer plugin is not running within the CMS's runtime; cannot continue, unable to render article source!";
#endif
                            
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
                        // Create a new article
                        Article a = new Article();
                        a.Title = title;
                        a.TextRaw = raw;
                        a.HTML = html;
                        a.HidePanel = hidePanel;
                        a.Comments = comments;
                        a.HeaderData = headerData;
                        // Attempt to persist the model
                        Article.PersistStatus ps = a.save(data.Connector);
                        if (ps != Article.PersistStatus.Success)
                        { // Failed to persist...check why...
                            switch (ps)
                            {
                                case Article.PersistStatus.Invalid_uuid_article:
                                case Article.PersistStatus.Invalid_thread:
                                case Article.PersistStatus.Error:
                                    error = "An error occurred creating the article, please try again later!"; break;
                                case Article.PersistStatus.Invalid_text_length:
                                    error = "Source/raw article must be x to x characters in length!"; break;
                                case Article.PersistStatus.Invalid_title_length:
                                    error = "Title must be x to x characters in length!"; break;
                                default:
                                    error = "Unknown issue occurred persisting the article!"; break;
                            }
                        }
                        else
                        {
                            // Fetch/create article thread based on the URL
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
                                a.remove(data.Connector);
                            }
                            else
                            {
                                // Assign thread to the article and set the article as the current article for the thread
                                a.UUIDThread = at.UUIDThread;
                                at.UUIDArticleCurrent = a.UUIDArticle;
                                // Persist the model data
                                if (a.save(data.Connector) != Article.PersistStatus.Success)
                                {
                                    a.remove(data.Connector);
                                    at.remove(data.Connector);
                                    error = "Unknown error occurred (article persistence)!";
                                }
                                else if (!at.save(data.Connector))
                                {
                                    a.remove(data.Connector);
                                    at.remove(data.Connector);
                                    error = "Unknown error occurred (thread persistence)!";
                                }
                                else
                                {
                                    // Redirect to the article
                                    BaseUtils.redirectAbs(data, "/article/" + a.UUIDArticle.Hex);
                                }
                            }
                        }
                    }
                }
            }
            // Setup the page
            BaseUtils.headerAppendCss("/content/css/basic_articles.css", ref data);
            BaseUtils.headerAppendJs("/content/js/basic_articles.js", ref data);
            // Set content
            data["Title"] = "Articles - Create";
            data["Content"] = Core.Templates.get(data.Connector, "basic_articles/create");
            // Set fields
            data["article_title"] = HttpUtility.HtmlEncode(title);
            data["article_url"] = HttpUtility.HtmlEncode(url);
            data["article_raw"] = HttpUtility.HtmlEncode(raw);
            if (postback == ArticleCreatePostback.Render)
                data["article_rendered"] = rendered;
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
        /// <summary>
        /// Views an article.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool pageArticle_view(Data data)
        {
            string uuid = data.PathInfo[1];
            // Fetch model
            return true;
        }
        /// <summary>
        /// Rebuilds the text of every article, with confirmation.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tagFilter"></param>
        /// <returns></returns>
        public bool pageArticles_rebuild(Data data, string tagFilter)
        {
            return true;
        }
        public bool pageArticles_browser(Data data, Article.Sorting sorting, string tagFilter)
        {
            return true;
        }
        public bool pageArticles_render(Data data, Article.Sorting sorting, string tagFilter)
        {
            return true;
        }

        public bool pageArticles_changeLog(Data data)
        {
            return true;
        }

        public bool pageArticles_pendingReview(Data data)
        {
            return true;
        }
        public bool pageArticles_search(Data data)
        {
            return true;
        }
        // Methods - Static - Rendering ********************************************************************************
    }
}