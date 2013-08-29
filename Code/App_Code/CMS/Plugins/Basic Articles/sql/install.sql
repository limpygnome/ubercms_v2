-- Create tables
SET FOREIGN_KEY_CHECKS=0;

CREATE TABLE ba_article_thread
(
	uuid_thread							BINARY(16) PRIMARY KEY,
	urlid								INT UNIQUE,
	FOREIGN KEY(`urlid`)				REFERENCES cms_urlrewriting(`urlid`) ON UPDATE CASCADE ON DELETE SET NULL,
	uuid_article_current				BINARY(16),
	FOREIGN KEY(`uuid_article_current`)	REFERENCES ba_article(`uuid_article`) ON UPDATE CASCADE ON DELETE SET NULL,
	thumbnail							VARCHAR(1) DEFAULT 0
);
CREATE TABLE ba_article_thread_permissions
(
	uuid_thread							BINARY(16) NOT NULL,
	FOREIGN KEY(`uuid_thread`)			REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,
	groupid								INT NOT NULL,
	FOREIGN KEY(`groupid`)				REFERENCES bsa_user_groups(`groupid`) ON UPDATE CASCADE ON DELETE CASCADE,
	PRIMARY KEY(`uuid_thread`, `groupid`)
);
CREATE TABLE ba_article
(
	uuid_article						BINARY(16) PRIMARY KEY,
	uuid_thread							BINARY(16),
	FOREIGN KEY(`uuid_thread`)			REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,

	title								VARCHAR(128),
	text_raw							TEXT,
	text_cache							TEXT,
	datetime_created					TIMESTAMP NOT NULL,
	datetime_modified					TIMESTAMP NOT NULL,

	published							VARCHAR(1) DEFAULT 0,
	comments							VARCHAR(1) DEFAULT 0,
	html								VARCHAR(1) DEFAULT 0,
	hide_panel							VARCHAR(1) DEFAULT 0,

	userid_author						INT,
	FOREIGN KEY(`userid_author`)		REFERENCES bsa_users(`userid`) ON UPDATE CASCADE ON DELETE NO ACTION,
	userid_publisher					INT,
	FOREIGN KEY(`userid_publisher`)		REFERENCES bsa_users(`userid`) ON UPDATE CASCADE ON DELETE NO ACTION
);
ALTER TABLE `ba_article` CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;

CREATE TABLE ba_tags
(
	tagid								INT	PRIMARY KEY AUTO_INCREMENT,
	keyword								VARCHAR(32) NOT NULL UNIQUE
);
CREATE TABLE bsa_tags_thread
(
	tagid								INT NOT NULL,
	FOREIGN KEY(`tagid`)				REFERENCES ba_tags(`tagid`) ON UPDATE CASCADE ON DELETE CASCADE,
	uuid_thread							BINARY(16) NOT NULL,
	FOREIGN KEY(`uuid_thread`)			REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,
	PRIMARY KEY(`tagid`, `uuid_thread`)
);

SET FOREIGN_KEY_CHECKS=1;

-- Create views
CREATE OR REPLACE VIEW ba_view_load_article_thread AS
	SELECT HEX(bat.uuid_thread) AS uuid_thread, bat.urlid, url.full_path, HEX(bat.uuid_article_current) AS uuid_article_current, bat.thumbnail FROM ba_article_thread AS bat LEFT OUTER JOIN cms_urlrewriting AS url ON url.urlid=bat.urlid;

CREATE OR REPLACE VIEW ba_view_load_article AS
	SELECT uuid_article AS uuid_article_raw, HEX(uuid_article) AS uuid_article, HEX(uuid_thread) AS uuid_thread, title, text_raw, text_cache, datetime_created, datetime_modified, published, comments, html, hide_panel, userid_author, userid_publisher FROM ba_article;
-- -- Should only be used for viewing the full raw text of an article (excludes cache)
CREATE OR REPLACE VIEW ba_view_load_article_raw AS
	SELECT uuid_article AS uuid_article_raw, HEX(uuid_article) AS uuid_article, HEX(uuid_thread) AS uuid_thread, title, text_raw, datetime_created, datetime_modified, published, comments, html, hide_panel, userid_author, userid_publisher FROM ba_article;
-- -- Should only be used for viewing an article (excludes raw text).
CREATE OR REPLACE VIEW ba_view_load_article_rendered AS
	SELECT uuid_article AS uuid_article_raw, HEX(uuid_article) AS uuid_article, HEX(uuid_thread) AS uuid_thread, title, text_cache, datetime_created, datetime_modified, published, comments, html, hide_panel, userid_author, userid_publisher FROM ba_article;

CREATE OR REPLACE VIEW ba_view_tags AS
	SELECT tt.uuid_thread AS uuid_thread_raw, HEX(tt.uuid_thread) AS uuid_thread, t.tagid, t.keyword FROM bsa_tags_thread AS tt LEFT OUTER JOIN ba_tags AS t ON t.tagid=tt.tagid ORDER BY t.keyword ASC;

-- -- Used for looking up an existing article thread based on the URL
CREATE OR REPLACE VIEW ba_article_thread_createfetch AS
	SELECT HEX(at.uuid_thread) AS uuid_thread, rw.urlid, rw.uuid, rw.full_path, rw.priority FROM ba_article_thread AS at LEFT OUTER JOIN cms_urlrewriting AS rw ON rw.urlid=at.urlid;
