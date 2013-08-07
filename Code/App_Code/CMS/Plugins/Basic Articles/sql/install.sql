-- Create tables
CREATE TABLE ba_article_thread
(
	uuid_thread				CHAR(16) PRIMARY KEY,
	full_path				TEXT NOT NULL,
	uuid_article_current	CHAR(16),
	thumbnail				MEDIUMBLOB
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
	FOREIGN KEY(`uuid_thread`) REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,

	title					VARCHAR(128),
	text_raw				TEXT,
	text_cache				TEXT,
	datetime_created		TIMESTAMP NOT NULL,
	datetime_edited			TIMESTAMP NOT NULL,

	published				VARCHAR(1) DEFAULT 0,
	comments				VARCHAR(1) DEFAULT 0,
	html					VARCHAR(1) DEFAULT 0,
	hide_panel				VARCHAR(1) DEFAULT 0,

	userid_author			INT,
	FOREIGN KEY(`userid_author`) REFERENCES bsa_users(`userid`) ON UPDATE CASCADE ON DELETE NO ACTION,
	userid_publisher		INT,
	FOREIGN KEY(`userid_publisher`) REFERENCES bsa_users(`userid`) ON UPDATE CASCADE ON DELETE NO ACTION
);
ALTER TABLE `ba_article` CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;
ALTER TABLE `ba_article` ADD INDEX uuid_article(uuid_article);
ALTER TABLE `ba_article_thread` ADD INDEX uuid_article_current(uuid_article_current);
ALTER TABLE `ba_article_thread` ADD CONSTRAINT `FK_uuid_article_current` FOREIGN KEY(`uuid_article_current`) REFERENCES ba_article(`uuid_article`) ON UPDATE CASCADE ON DELETE SET NULL;

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

-- Create triggers
-- -- Update the current article for a thread when an article is deleted
CREATE TRIGGER ba_trigger_thread_currentarticle
	AFTER UPDATE ON `ba_article_thread` FOR EACH ROW
	BEGIN
		DECLARE temp_uuid CHAR(16);
		SET @temp_uuid := (SELECT uuid_article FROM ba_article WHERE uuid_thread=OLD.uuid_thread LIMIT 1);
		IF (NEW.uuid_article_current IS NULL) AND (SELECT @temp_uuid) IS NOT NULL THEN
			UPDATE ba_article_thread SET uuid_article_current=(SELECT uuid_article FROM ba_article WHERE uuid_thread=OLD.uuid_thread LIMIT 1);
		END IF;
	END;

-- Create views
