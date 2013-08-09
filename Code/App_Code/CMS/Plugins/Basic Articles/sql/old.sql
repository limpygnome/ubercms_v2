-- Create functions
DELIMITER $$
CREATE FUNCTION increment_views(ip_addr VARCHAR(45), uuid_thread CHAR(16))
	RETURNS INT
	LANGUAGE SQL
	READS SQL DATA
BEGIN
	DECLARE ipid INT;
	DECLARE temp INT;

	SELECT ipid INTO ipid FROM ba_article_thread_views_ip WHERE ip=ip_addr;
	IF ipid IS NULL THEN
		INSERT INTO ba_article_thread_views_ip (ip) VALUES(ip_aadr);
		SELECT LAST_INSERT_ID() INTO ipid;
	END IF;
	SELECT COUNT('') INTO temp FROM ba_article_thread_views WHERE ipid=ipid AND uuid_thread=uuid_thread;
	IF temp = 0 THEN
		INSERT INTO ba_article_thread_views(uuid_thread, ip) VALUES(uuid_thread, ip_addr);
	END IF;
	RETURN 1;
END;
$$
DELIMITER ;


CREATE TABLE ba_article_thread_views
(
	uuid_thread							CHAR(16) NOT NULL,
	FOREIGN KEY(`uuid_thread`)			REFERENCES ba_article_thread(`uuid_thread`) ON UPDATE CASCADE ON DELETE CASCADE,
	ipid								INT NOT NULL,
	FOREIGN KEY(`ipid`)					REFERENCES ba_article_thread_views_ip(`ipid`) ON UPDATE CASCADE ON DELETE CASCADE,
	PRIMARY KEY(uuid_thread, ipid)
);
CREATE TABLE ba_article_thread_views_ip
(
	ipid								INT PRIMARY KEY AUTO_INCREMENT,
	ip									VARCHAR(45) UNIQUE NOT NULL
);





too complicated, issues with concurrency and cross-platform (in terms of DBMS).