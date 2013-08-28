-- Drop tables
SET FOREIGN_KEY_CHECKS=0;
DROP TABLE IF EXISTS bsa_authentication_failed_attempts;
DROP TABLE IF EXISTS bsa_account_codes;
DROP TABLE IF EXISTS bsa_account_events;
DROP TABLE IF EXISTS bsa_account_event_types;
DROP TABLE IF EXISTS bsa_user_bans;
DROP TABLE IF EXISTS bsa_users;
DROP TABLE IF EXISTS bsa_user_groups;
SET FOREIGN_KEY_CHECKS=1;