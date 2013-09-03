CREATE TABLE textrendering_providers
(
	uuid								BINARY(16) PRIMARY KEY,
	uuid_plugin							BINARY(16),
	FOREIGN KEY(`uuid_plugin`)			REFERENCES `cms_plugins`(`uuid`) ON UPDATE CASCADE ON DELETE CASCADE,
	classpath							VARCHAR(64) NOT NULL,
	title								VARCHAR(64) NOT NULL,
	description							TEXT,
	enabled								VARCHAR(1) DEFAULT 0,
	priority							INT DEFAULT 0
);

CREATE OR REPLACE VIEW view_textrendering_providers AS
	SELECT HEX(uuid) AS uuid, HEX(uuid_plugin) AS uuid_plugin, title, description, enabled FROM textrendering_providers;
