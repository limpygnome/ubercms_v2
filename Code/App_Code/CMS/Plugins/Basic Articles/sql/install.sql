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
CREATE TABLE ba_article_headerdata
(
	hash								VARCHAR(32) PRIMARY KEY,
	headerdata							TEXT
);
CREATE TABLE ba_article
(
	uuid_article						BINARY(16) PRIMARY KEY,
	uuid_thread							BINARY(16),
	FOREIGN KEY(`uuid_thread`)			REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,

	title								VARCHAR(128),
	text_raw							TEXT,
	text_cache							TEXT,
	headerdata_hash						VARCHAR(32),
	FOREIGN KEY(`headerdata_hash`)		REFERENCES ba_article_headerdata(`hash`) ON UPDATE CASCADE ON DELETE SET NULL,
	datetime_created					TIMESTAMP NOT NULL,
	datetime_modified					TIMESTAMP,
	datetime_published					TIMESTAMP,

	published							VARCHAR(1) DEFAULT 0,
	comments							VARCHAR(1) DEFAULT 0,
	html								VARCHAR(1) DEFAULT 0,
	hide_panel							VARCHAR(1) DEFAULT 0,

	userid_author						INT,
	FOREIGN KEY(`userid_author`)		REFERENCES bsa_users(`userid`) ON UPDATE CASCADE ON DELETE NO ACTION,
	userid_publisher					INT,
	FOREIGN KEY(`userid_publisher`)		REFERENCES bsa_users(`userid`) ON UPDATE CASCADE ON DELETE NO ACTION
);
ALTER TABLE `ba_article_headerdata` CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;
ALTER TABLE `ba_article` CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;
CREATE TABLE ba_tags
(
	tagid								INT	PRIMARY KEY AUTO_INCREMENT,
	keyword								VARCHAR(32) NOT NULL UNIQUE
);
CREATE TABLE ba_tags_thread
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
	SELECT bat.uuid_thread AS uuid_thread_raw, HEX(bat.uuid_thread) AS uuid_thread, url.urlid, url.uuid, url.full_path, url.priority, HEX(bat.uuid_article_current) AS uuid_article_current, bat.thumbnail FROM ba_article_thread AS bat LEFT OUTER JOIN cms_urlrewriting AS url ON url.urlid=bat.urlid;
-- Contains both raw and rendered text.
CREATE OR REPLACE VIEW ba_view_load_article AS
	SELECT a.uuid_article AS uuid_article_raw, HEX(a.uuid_article) AS uuid_article, a.uuid_thread AS uuid_thread_raw, HEX(a.uuid_thread) AS uuid_thread, a.title, a.text_raw, a.text_cache, hd.headerdata, a.headerdata_hash, a.datetime_created, a.datetime_modified, a.datetime_published, a.published, a.comments, a.html, a.hide_panel, a.userid_author, a.userid_publisher FROM ba_article AS a LEFT OUTER JOIN ba_article_headerdata AS hd ON hd.hash=a.headerdata_hash;
-- -- Should only be used for viewing the full raw text of an article (excludes cache).
CREATE OR REPLACE VIEW ba_view_load_article_raw AS
	SELECT a.uuid_article AS uuid_article_raw, HEX(a.uuid_article) AS uuid_article, a.uuid_thread AS uuid_thread_raw, HEX(a.uuid_thread) AS uuid_thread, a.title, a.text_raw, hd.headerdata, a.headerdata_hash, a.datetime_created, a.datetime_modified, a.datetime_published, a.published, a.comments, a.html, a.hide_panel, a.userid_author, a.userid_publisher FROM ba_article AS a LEFT OUTER JOIN ba_article_headerdata AS hd ON hd.hash=a.headerdata_hash;
-- -- Should only be used for viewing an article (excludes raw text).
CREATE OR REPLACE VIEW ba_view_load_article_rendered AS
	SELECT a.uuid_article AS uuid_article_raw, HEX(a.uuid_article) AS uuid_article, a.uuid_thread AS uuid_thread_raw, HEX(a.uuid_thread) AS uuid_thread, a.title, a.text_cache, hd.headerdata, a.headerdata_hash, a.datetime_created, a.datetime_modified, a.datetime_published, a.published, a.comments, a.html, a.hide_panel, a.userid_author, a.userid_publisher FROM ba_article AS a LEFT OUTER JOIN ba_article_headerdata AS hd ON hd.hash=a.headerdata_hash;
-- Contains no article content; this should be used for information only.
CREATE OR REPLACE VIEW ba_view_load_article_nocontent AS
	SELECT a.uuid_article AS uuid_article_raw, HEX(a.uuid_article) AS uuid_article, a.uuid_thread AS uuid_thread_raw, HEX(a.uuid_thread) AS uuid_thread, a.title, hd.headerdata, a.headerdata_hash, a.datetime_created, a.datetime_modified, a.datetime_published, a.published, a.comments, a.html, a.hide_panel, a.userid_author, a.userid_publisher FROM ba_article AS a LEFT OUTER JOIN ba_article_headerdata AS hd ON hd.hash=a.headerdata_hash;

CREATE OR REPLACE VIEW ba_view_tags AS
	SELECT tt.uuid_thread AS uuid_thread_raw, HEX(tt.uuid_thread) AS uuid_thread, t.tagid, t.keyword FROM ba_tags_thread AS tt LEFT OUTER JOIN ba_tags AS t ON t.tagid=tt.tagid ORDER BY t.keyword ASC;

-- -- Used for looking up an existing article thread based on the URL
CREATE OR REPLACE VIEW ba_article_thread_createfetch AS
	SELECT HEX(at.uuid_thread) AS uuid_thread, rw.urlid, rw.uuid, rw.full_path, rw.priority FROM ba_article_thread AS at LEFT OUTER JOIN cms_urlrewriting AS rw ON rw.urlid=at.urlid;
