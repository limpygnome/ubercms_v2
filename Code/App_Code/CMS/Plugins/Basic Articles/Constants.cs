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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/Constants.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A class of constants for settings related to this plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A class of constants for settings related to this plugin.
    /// </summary>
    public static class Settings
    {
        // Constants - Articles & Article Thread ***********************************************************************
        // URL minimum length
        public const string     SETTINGS__ARTICLE_THREAD_URL_MIN = "basic_articles/lengths/url_min";
        public const string     SETTINGS__ARTICLE_THREAD_URL_MIN__DESC = "The minimum length of an article's URL.";
        public const int        SETTINGS__ARTICLE_THREAD_URL_MIN__DEFAULT = 1;
        // URL maximum length
        public const string     SETTINGS__ARTICLE_THREAD_URL_MAX = "basic_articles/lengths/url_max";
        public const string     SETTINGS__ARTICLE_THREAD_URL_MAX__DESC = "The maximum length of an article's URL.";
        public const int        SETTINGS__ARTICLE_THREAD_URL_MAX__DEFAULT = 128;
        // Title minimum length
        public const string     SETTINGS__TITLE_LENGTH_MIN = "basic_articles/lengths/title_min";
        public const string     SETTINGS__TITLE_LENGTH_MIN__DESC = "The minimum length of an article's title.";
        public const int        SETTINGS__TITLE_LENGTH_MIN__DEFAULT = 1;
        // Title maximum length
        public const string     SETTINGS__TITLE_LENGTH_MAX = "basic_articles/lengths/title_max";
        public const string     SETTINGS__TITLE_LENGTH_MAX__DESC = "The maximum length of an article's title.";
        public const int        SETTINGS__TITLE_LENGTH_MAX__DEFAULT = 128;
        // Text raw minimum length
        public const string     SETTINGS__TEXT_LENGTH_MIN = "basic_articles/lengths/text_min";
        public const string     SETTINGS__TEXT_LENGTH_MIN__DESC = "The minimum length of an article's raw text.";
        public const int        SETTINGS__TEXT_LENGTH_MIN__DEFAULT = 0;
        // Text raw maximum length
        public const string     SETTINGS__TEXT_LENGTH_MAX = "basic_articles/lengths/text_max";
        public const string     SETTINGS__TEXT_LENGTH_MAX__DESC = "The maximum length of an article's raw text.";
        public const int        SETTINGS__TEXT_LENGTH_MAX__DEFAULT = 32000;
        // Revisions per page
        public const string     SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE = "basic_articles/thread_revisions/articlesperpage";
        public const string     SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE__DESC = "The number of articles to display on a thread revisions page.";
        public const int        SETTINGS__THREAD_REVISIONS_ARTICLES_PER_PAGE__VALUE = 8;
        // Image length min
        public const string     SETTINGS__THREAD_IMAGE_LENGTH_MIN = "basic_articles/thread_image/length_min";
        public const string     SETTINGS__THREAD_IMAGE_LENGTH_MIN__DESC = "The minimum length of a thread image.";
        public const int        SETTINGS__THREAD_IMAGE_LENGTH_MIN__VALUE = 20;
        // Image length max
        public const string     SETTINGS__THREAD_IMAGE_LENGTH_MAX = "basic_articles/thread_image/length_max";
        public const string     SETTINGS__THREAD_IMAGE_LENGTH_MAX__DESC = "The maximum length of a thread image.";
        public const int        SETTINGS__THREAD_IMAGE_LENGTH_MAX__VALUE = 1572864;
        // Image allowed types/extensions
        public const string     SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS = "basic_articles/thead_image/allowed_extensions";
        public const string     SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS__DESC = "The allowed image extensions.";
        public const string     SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS__VALUE = ":jpg:jpeg:gif:png:bmp:";
        // Image width
        public const string     SETTINGS__THREAD_IMAGE_WIDTH = "basic_articles/thread_image/width";
        public const string     SETTINGS__THREAD_IMAGE_WIDTH__DESC = "The width of thread thumbnail images.";
        public const int        SETTINGS__THREAD_IMAGE_WIDTH__VALUE = 240;
        // Image height
        public const string     SETTINGS__THREAD_IMAGE_HEIGHT = "basic_articles/thread_image/height";
        public const string     SETTINGS__THREAD_IMAGE_HEIGHT__DESC = "The height of thread thumbnail images.";
        public const int        SETTINGS__THREAD_IMAGE_HEIGHT__VALUE = 180;
        // Thread description - length min
        public const string     SETTINGS__THREAD_DESCRIPTION_LENGTH_MIN = "basic_articles/thread_description/length_min";
        public const string     SETTINGS__THREAD_DESCRIPTION_LENGTH_MIN__DESC = "The mimimum length of a thread's description.";
        public const int        SETTINGS__THREAD_DESCRIPTION_LENGTH_MIN__VALUE = 0;
        // Thread description - length max
        public const string     SETTINGS__THREAD_DESCRIPTION_LENGTH_MAX = "basic_articles/thread_description/length_max";
        public const string     SETTINGS__THREAD_DESCRIPTION_LENGTH_MAX__DESC = "The maximum length of a thread's description.";
        public const int        SETTINGS__THREAD_DESCRIPTION_LENGTH_MAX__VALUE = 256;
        // Constants - Tags ********************************************************************************************
        // Tag keyword length min
        public const string     SETTINGS__THREAD_TAG_LENGTH_MIN = "basic_articles/thread_tags/length_min";
        public const string     SETTINGS__THREAD_TAG_LENGTH_MIN__DESC = "The minimum length of a tag keyword.";
        public const int        SETTINGS__THREAD_TAG_LENGTH_MIN__VALUE = 1;
        // Tag keyword length max
        public const string     SETTINGS__THREAD_TAG_LENGTH_MAX = "basic_articles/thread_tags/length_max";
        public const string     SETTINGS__THREAD_TAG_LENGTH_MAX__DESC = "The maximum length of a tag keyword.";
        public const int        SETTINGS__THREAD_TAG_LENGTH_MAX__VALUE = 32;
        // Max tags per thread
        public const string     SETTINGS__THREAD_TAGS_MAX = "basic_articles/thread_tags/max";
        public const string     SETTINGS__THREAD_TAGS_MAX__DESC = "The maximum number of tags per thread.";
        public const int        SETTINGS__THREAD_TAGS_MAX__VALUE = 32;
        // Constants - Browsing ****************************************************************************************
        // Articles per page
        public const string     SETTINGS__BROWSER_ARTICLES_PER_PAGE = "basic_articles/browser/articlesperpage";
        public const string     SETTINGS__BROWSER_ARTICLES_PER_PAGE__DESC = "The articles per page displayed when browsing.";
        public const int        SETTINGS__BROWSER_ARTICLES_PER_PAGE__VALUE = 16;
        // Most populated tags limit
        public const string     SETTINGS__BROWSER_TAGS_POPULATED_LIMIT = "basic_articles/browser/populated_tags_limit";
        public const string     SETTINGS__BROWSER_TAGS_POPULATED_LIMIT__DESC = "The number of populated tags to display on the browser.";
        public const int        SETTINGS__BROWSER_TAGS_POPULATED_LIMIT__VALUE = 15;
        // Tags per page
        public const string     SETTINGS__BROWSER_TAGS_PER_PAGE = "basic_articles/browser/tagsperpage";
        public const string     SETTINGS__BROWSER_TAGS_PER_PAGE__DESC = "The number of tags displayed per page when viewing all tags via the browser.";
        public const int        SETTINGS__BROWSER_TAGS_PER_PAGE__VALUE = 20;
        // Constants - Render Stream ***********************************************************************************
        // Articles per page
        public const string     SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE = "basic_articles/render_stream/articlesperpage";
        public const string     SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE__DESC = "The number of articles rendered per each page.";
        public const int        SETTINGS__RENDERSTREAM_ARTICLES_PER_PAGE__VALUE = 5;
        // Default tag
        public const string     SETTINGS__RENDERSTREAM_DEFAULT_TAG = "basic_articles/render_stream/default_tag";
        public const string     SETTINGS__RENDERSTREAM_DEFAULT_TAG__DESC = "The default tag for articles rendered at /articles_home (URL); for use as a home-page/default handler.";
        public const string     SETTINGS__RENDERSTREAM_DEFAULT_TAG__VALUE = "news";
        // Default tag's page title
        public const string     SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE = "basic_articles/render_stream/default_title";
        public const string     SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE__DESC = "The title of the page at /articles_home (URL).";
        public const string     SETTINGS__RENDERSTREAM_DEFAULT_TAG_TITLE__VALUE = "Home";
    }
}