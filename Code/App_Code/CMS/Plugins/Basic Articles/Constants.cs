using System;
using System.Collections.Generic;
using System.Web;

namespace CMS.BasicArticles
{
    public static class Settings
    {
        // Constants - Article Thread **********************************************************************************
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
        // Tag keyword minimum length
        public const string     SETTINGS__TAG_KEYWORD_LENGTH_MIN = "basic_articles/lengths/tag_min";
        public const string     SETTINGS__TAG_KEYWORD_LENGTH_MIN__DESC = "The minimum length of a tag's keyword.";
        public const int        SETTINGS__TAG_KEYWORD_LENGTH_MIN__DEFAULT = 1;
        // Tag keyword maximum length
        public const string     SETTINGS__TAG_KEYWORD_LENGTH_MAX = "basic_articles/lengths/tag_max";
        public const string     SETTINGS__TAG_KEYWORD_LENGTH_MAX__DESC = "The maximum length of a tag's keyword.";
        public const int        SETTINGS__TAG_KEYWORD_LENGTH_MAX__DEFAULT = 32;
    }
}