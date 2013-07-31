-- Create tables
CREATE TABLE IF NOT EXISTS bsa_user_groups
(
	groupid INT PRIMARY KEY AUTO_INCREMENT,
	title VARCHAR(32) NOT NULL,
	description TEXT,

	pages_create VARCHAR(1) NOT NULL DEFAULT 0,
	pages_modify VARCHAR(1) NOT NULL DEFAULT 0,
	pages_modify_own VARCHAR(1) NOT NULL DEFAULT 0,
	pages_delete VARCHAR(1) NOT NULL DEFAULT 0,
	pages_delete_own VARCHAR(1) NOT NULL DEFAULT 0,
	pages_publish VARCHAR(1) NOT NULL DEFAULT 0,
	
	comments_create VARCHAR(1) NOT NULL DEFAULT 0,
	comments_modify_own VARCHAR(1) NOT NULL DEFAULT 0,
	comments_delete VARCHAR(1) NOT NULL DEFAULT 0,
	comments_delete_own VARCHAR(1) NOT NULL DEFAULT 0,
	comments_publish VARCHAR(1) NOT NULL DEFAULT 0,

	media_create VARCHAR(1) NOT NULL DEFAULT 0,
	media_modify VARCHAR(1) NOT NULL DEFAULT 0,
	media_modify_own VARCHAR(1) NOT NULL DEFAULT 0,
	media_delete VARCHAR(1) NOT NULL DEFAULT 0,
	media_delete_own VARCHAR(1) NOT NULL DEFAULT 0,
	
	moderator VARCHAR(1) NOT NULL DEFAULT 0,
	administrator VARCHAR(1) NOT NULL DEFAULT 0,
	login VARCHAR(1) NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS bsa_users
(
	userid INT PRIMARY KEY AUTO_INCREMENT,
	username VARCHAR(32) UNIQUE NOT NULL,
	password VARCHAR(512) NOT NULL,
	password_salt VARCHAR(16) NOT NULL,
	email VARCHAR(64) NOT NULL,
	secret_question VARCHAR(64),
	secret_answer VARCHAR(64),
	groupid INT NOT NULL,
	FOREIGN KEY(`groupid`) REFERENCES `bsa_user_groups`(`groupid`) ON UPDATE CASCADE ON DELETE CASCADE,
	datetime_register TIMESTAMP
);
CREATE TABLE IF NOT EXISTS bsa_user_bans
(
	banid INT PRIMARY KEY AUTO_INCREMENT,
	userid INT NOT NULL,
	FOREIGN KEY(`userid`) REFERENCES `bsa_users`(`userid`) ON UPDATE CASCADE ON DELETE CASCADE,
	reason TEXT,
	datetime_start TIMESTAMP NOT NULL,
	datetime_end TIMESTAMP,
	banned_by INT,
	FOREIGN KEY(`banned_by`) REFERENCES `bsa_users`(`userid`) ON UPDATE CASCADE ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS bsa_account_event_types
(
	eventtypeid INT PRIMARY KEY AUTO_INCREMENT,
	title VARCHAR(128),
	description TEXT,
	render_classpath VARCHAR(128) NOT NULL,
	render_function VARCHAR(64) NOT NULL
);
CREATE TABLE IF NOT EXISTS bsa_account_events
(
	eventid INT PRIMARY KEY AUTO_INCREMENT,
	userid INT NOT NULL,
	FOREIGN KEY(`userid`) REFERENCES `bsa_users`(`userid`) ON UPDATE CASCADE ON DELETE CASCADE,
	eventtypeid INT NOT NULL,
	FOREIGN KEY(`eventtypeid`) REFERENCES `bsa_account_event_types`(`eventtypeid`) ON UPDATE CASCADE,
	datetime TIMESTAMP NOT NULL,
	param1 TEXT,
	param1_datatype VARCHAR(1),
	param2 TEXT,
	param2_datatype VARCHAR(1)
);
CREATE TABLE IF NOT EXISTS bsa_recovery_codes
(
	code VARCHAR(32) PRIMARY KEY,
	userid INT NOT NULL,
	FOREIGN KEY(`userid`) REFERENCES `bsa_users`(`userid`) ON UPDATE CASCADE ON DELETE CASCADE,
	datetime_created TIMESTAMP NOT NULL
);
CREATE TABLE IF NOT EXISTS bsa_authentication_failed_attempts
(
	-- 45 characters for ipv4 tunneling with ipv6
	ip VARCHAR(45) NOT NULL,
	datetime TIMESTAMP NOT NULL,
	type VARCHAR(1) NOT NULL
);
CREATE INDEX bsa_index_authentication_failed_attempts ON bsa_authentication_failed_attempts(ip);
