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
	pluginid INT PRIMARY KEY AUTO_INCREMENT,
	title TEXT NOT NULL,
	directory TEXT NOT NULL,
	classpath TEXT NOT NULL,
	priority INT DEFAULT 0,
	state INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS cms_plugin_handlers
(
	pluginid INT PRIMARY KEY AUTO_INCREMENT,
	request_start VARCHAR(1) DEFAULT 0,
	request_end VARCHAR(1) DEFAULT 0,
	page_error VARCHAR(1) DEFAULT 0,
	page_not_found VARCHAR(1) DEFAULT 0,
	cms_start VARCHAR(1) DEFAULT 0,
	cms_end VARCHAR(1) DEFAULT 0,
	cms_plugin_action VARCHAR(1) DEFAULT 0,
	cycle_interval INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS cms_settings
(
	path VARCHAR(128) PRIMARY KEY,
	pluginid INT,
	FOREIGN KEY(`pluginid`) REFERENCES `cms_plugins`(`pluginid`) ON UPDATE CASCADE ON DELETE CASCADE,
	value TEXT,
	description TEXT
);
CREATE TABLE IF NOT EXISTS cms_urlrewriting
(
	urlid INT PRIMARY KEY AUTO_INCREMENT,
	parent INT,
	FOREIGN KEY(`parent`) REFERENCES `cms_urlrewriting`(`urlid`) ON UPDATE CASCADE ON DELETE CASCADE,
	pluginid INT,
	FOREIGN KEY(`pluginid`) REFERENCES `cms_plugins`(`pluginid`) ON UPDATE CASCADE ON DELETE CASCADE,
	full_path TEXT NOT NULL,
	priority INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS cms_templates
(
	path VARCHAR(128) PRIMARY KEY,
	pluginid INT,
	FOREIGN KEY(`pluginid`) REFERENCES `cms_plugins`(`pluginid`) ON UPDATE CASCADE ON DELETE CASCADE,
	description TEXT,
	html TEXT
);
CREATE TABLE IF NOT EXISTS cms_template_handlers
(
	-- path: the path/function-name when called from a template
	-- classpath: the location of the class in the assembly e.g. CMS.Base.Templates
	-- function_name: the name of the function within the class to be invoked.
	path VARCHAR(128) PRIMARY KEY,
	pluginid INT,
	FOREIGN KEY(`pluginid`) REFERENCES `cms_plugins`(`pluginid`) ON UPDATE CASCADE ON DELETE CASCADE,
	classpath TEXT,
	function_name TEXT
);
CREATE TABLE IF NOT EXISTS cms_email_queue
(
	emailid INT PRIMARY KEY AUTO_INCREMENT,
	email TEXT,
	subject TEXT,
	body TEXT,
	html VARCHAR(1) DEFAULT 1
);
-- Create views
CREATE OR REPLACE VIEW cms_view_plugins_loadinfo AS
	SELECT p.pluginid, p.title, p.directory, p.classpath, p.priority, p.state, ph.request_start, ph.request_end, ph.page_error, ph.page_not_found, ph.cms_start, ph.cms_end, ph.cms_plugin_action, ph.cycle_interval FROM cms_plugins AS p LEFT OUTER JOIN cms_plugin_handlers AS ph ON ph.pluginid=p.pluginid ORDER BY p.priority DESC;

CREATE OR REPLACE VIEW cms_view_settings_load AS
	SELECT path, pluginid, value, description FROM cms_settings;

-- Insert core settings
INSERT INTO cms_settings (path, pluginid, value, description) VALUES('core/default_handler', NULL, 'home', 'The default module-handler for the home-page/an empty request path.');

-- Insert default template handlers
INSERT INTO cms_template_handlers (path, pluginid, classpath, function_name) VALUES('include', NULL, 'CMS.Base.Templates', 'handler_include');