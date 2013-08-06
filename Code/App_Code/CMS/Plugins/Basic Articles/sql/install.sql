-- Create tables
CREATE TABLE ba_article_thread
(
	uuid_thread				CHAR(16) PRIMARY KEY,
	full_path				TEXT NOT NULL,
	uuid_article_current	CHAR(16),
	FOREIGN KEY(`uuid_article_current`) REFERENCES ba_article(`uuid_article_current`) ON UPDATE CASCADE ON DELETE SET NULL
);
CREATE TABLE ba_article_thread_permissions
(
	uuid_thread				CHAR(16) PRIMARY KEY,
	groupid					INT,
	FOREIGN KEY(`groupid`) REFERENCES bsa_user_groups(`groupid`) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE TABLE ba_article
(
	uuid_article			CHAR(16) PRIMARY KEY,
	uuid_thread				CHAR(16),
	FOREIGN KEY(`uuid_thread`) REFERENCES ba_article_threads(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,

	title					VARCHAR(128),
	text_raw				TEXT,
	text_cache				TEXT,
	datetime_created		TIMESTAMP NOT NULL,
	datetime_edited			TIMESTAMP NOT NULL,

	published				VARCHAR(1) DEFAULT 0,
	comments				VARCHAR(1) DEFAULT 0,
	html					VARCHAR(1) DEFAULT 0,
	hide_panel				VARCHAR(1) DEFAULT 0,
);
ALTER TABLE `ba_article` CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;

CREATE TABLE ba_tags
(
	tagid					INT	PRIMARY KEY AUTO_INCREMENT,
	keyword					VARCHAR(32) NOT NULL UNIQUE
);
CREATE TABLE bsa_tags_thread
(
	tagid					INT NOT NULL,
	FOREIGN KEY(`tagid`) REFERENCES ba_tags(`tagid) ON UPDATE CASCADE ON DELETE CASCADE,
	uuid_thread				CHAR(16) NOT NULL,
	FOREIGN KEY(uuid_thread) REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE ba_render_handlers
(
	handlerid			INT PRIMARY KEY,
	uuid_plugin			CHAR(16),
	FOREIGN KEY(`uuid_plugin`) REFERENCES cms_plugins(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	classpath			VARCHAR(128),
	method_name			VARCHAR(32)
);

-- Create views
