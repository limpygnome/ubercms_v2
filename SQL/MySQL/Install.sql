-- Drop tables
SET FOREIGN_KEY_CHECKS=0;
DROP TABLE IF EXISTS `cms_plugins`;
DROP TABLE IF EXISTS `cms_settings`;
DROP TABLE IF EXISTS `cms_urlrewriting`;
DROP TABLE IF EXISTS `cms_templates`;
DROP TABLE IF EXISTS `cms_email_queue`;
DROP TABLE IF EXISTS `cms_plugin_handlers`;
SET FOREIGN_KEY_CHECKS=1;

-- Create tables
CREATE TABLE IF NOT EXISTS cms_plugins
(
	uuid CHAR(16) PRIMARY KEY,
	title TEXT NOT NULL,
	directory TEXT NOT NULL,
	classpath TEXT NOT NULL,
	priority INT DEFAULT 0,
	state INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS cms_plugin_handlers
(
	uuid CHAR(16) PRIMARY KEY,
	FOREIGN KEY(`uuid`) REFERENCES `cms_plugins`(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	request_start VARCHAR(1) DEFAULT 0,
	request_end VARCHAR(1) DEFAULT 0,
	page_error VARCHAR(1) DEFAULT 0,
	page_not_found VARCHAR(1) DEFAULT 0,
	plugin_start VARCHAR(1) DEFAULT 0,
	plugin_stop VARCHAR(1) DEFAULT 0,
	plugin_action VARCHAR(1) DEFAULT 0,
	cycle_interval INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS cms_settings
(
	path VARCHAR(128) PRIMARY KEY,
	uuid CHAR(16),
	FOREIGN KEY(`uuid`) REFERENCES `cms_plugins`(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	type VARCHAR(1) NOT NULL,
	value TEXT,
	description TEXT
);
CREATE TABLE IF NOT EXISTS cms_urlrewriting
(
	urlid INT PRIMARY KEY AUTO_INCREMENT,
	parent INT,
	FOREIGN KEY(`parent`) REFERENCES `cms_urlrewriting`(`urlid`) ON UPDATE CASCADE ON DELETE CASCADE,
	uuid CHAR(16),
	FOREIGN KEY(`uuid`) REFERENCES `cms_plugins`(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	full_path TEXT NOT NULL,
	priority INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS cms_templates
(
	path VARCHAR(128) PRIMARY KEY,
	uuid CHAR(16),
	FOREIGN KEY(`uuid`) REFERENCES `cms_plugins`(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	description TEXT,
	html TEXT
);
CREATE TABLE IF NOT EXISTS cms_template_handlers
(
	-- path: the path/function-name when called from a template
	-- classpath: the location of the class in the assembly e.g. CMS.Base.Templates
	-- function_name: the name of the function within the class to be invoked.
	path VARCHAR(128) PRIMARY KEY,
	uuid CHAR(16),
	FOREIGN KEY(`uuid`) REFERENCES `cms_plugins`(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	classpath VARCHAR(128) NOT NULL,
	function_name VARCHAR(128) NOT NULL
);
CREATE TABLE IF NOT EXISTS cms_email_queue
(
	emailid INT PRIMARY KEY AUTO_INCREMENT,
	email TEXT,
	subject TEXT,
	body TEXT,
	html VARCHAR(1) DEFAULT 1,
	errors INT DEFAULT 0,
	last_sent TIMESTAMP
);
-- Create views
CREATE OR REPLACE VIEW cms_view_plugins_loadinfo AS
	SELECT HEX(p.uuid) AS uuid, p.uuid AS uuid_raw, p.title, p.directory, p.classpath, p.priority, p.state, ph.request_start, ph.request_end, ph.page_error, ph.page_not_found, ph.plugin_start, ph.plugin_stop, ph.plugin_action, ph.cycle_interval FROM cms_plugins AS p LEFT OUTER JOIN cms_plugin_handlers AS ph ON ph.uuid=p.uuid ORDER BY p.priority DESC;

CREATE OR REPLACE VIEW cms_view_settings_load AS
	SELECT path, HEX(uuid) AS uuid, type, value, description FROM cms_settings;

CREATE OR REPLACE VIEW cms_view_template_handlers AS
	SELECT HEX(uuid) AS uuid, path, classpath, function_name FROM cms_template_handlers;

CREATE OR REPLACE VIEW cms_view_email_queue AS
	SELECT emailid, email, subject, body, html FROM cms_email_queue WHERE (errors=0 OR (last_sent IS NULL OR last_sent < (CURRENT_TIMESTAMP - INTERVAL 15 minute))) ORDER BY errors ASC, emailid ASC;

-- Insert core settings
INSERT INTO cms_settings (path, uuid, type, value, description) VALUES
('core/default_handler', NULL, '0', 'home', 'The default module-handler for the home-page/an empty request path.'),
('core/title', NULL, '0', 'Untitled CMS', 'A label/title for this CMS/website/community; this may be used by plugins for e.g. e-mails.')
;

-- Insert default template handlers
INSERT INTO cms_template_handlers (path, uuid, classpath, function_name) VALUES('include', NULL, 'CMS.Base.Templates', 'handler_include');