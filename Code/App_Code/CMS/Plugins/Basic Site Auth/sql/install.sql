-- Create tables
CREATE TABLE IF NOT EXISTS bsa_user_groups
(
	groupid INT PRIMARY KEY AUTO_INCREMENT,
	title VARCHAR(32) NOT NULL,
	description TEXT,

	pages_create VARCHAR(1) DEFAULT 0,
	pages_modify VARCHAR(1) DEFAULT 0,
	pages_modify_own VARCHAR(1) DEFAULT 0,
	pages_delete VARCHAR(1) DEFAULT 0,
	pages_delete_own VARCHAR(1) DEFAULT 0,
	pages_publish VARCHAR(1) DEFAULT 0,
	
	comments_create VARCHAR(1) DEFAULT 0,
	comments_modify_own VARCHAR(1) DEFAULT 0,
	comments_delete VARCHAR(1) DEFAULT 0,
	comments_delete_own VARCHAR(1) DEFAULT 0,
	comments_publish VARCHAR(1) DEFAULT 0,

	media_create VARCHAR(1) DEFAULT 0,
	media_modify VARCHAR(1) DEFAULT 0,
	media_modify_own VARCHAR(1) DEFAULT 0,
	media_delete VARCHAR(1) DEFAULT 0,
	media_delete_own VARCHAR(1) DEFAULT 0,
	
	moderator VARCHAR(1) DEFAULT 0,
	administrator VARCHAR(1) DEFAULT 0,
	login VARCHAR(1) DEFAULT 1
);
CREATE TABLE IF NOT EXISTS bsa_users
(
	userid INT PRIMARY KEY AUTO_INCREMENT,
	username VARCHAR(32) NOT NULL,
	password VARCHAR(512) NOT NULL,
	email VARCHAR(64) NOT NULL,
	secret_question VARCHAR(64),
	secret_answer VARCHAR(64),
	groupid INT NOT NULL,
	FOREIGN KEY(`groupid`) REFERENCES `bsa_user_groups`(`groupid`) ON UPDATE CASCADE ON DELETE CASCADE
);
