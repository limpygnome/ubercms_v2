using System;
using System.Collections.Generic;
using System.Web;

namespace CMS.BasicArticles
{
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
    }
}