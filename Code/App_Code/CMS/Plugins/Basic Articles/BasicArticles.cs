using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    public partial class BasicArticles : Plugin
    {
        // Methods - Constructors **************************************************************************************
        public BasicArticles(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version)
            : base(uuid, title, directory, state, handlerInfo, version)
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
    }
}