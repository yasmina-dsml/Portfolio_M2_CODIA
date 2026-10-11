CREATE USER 'phpmyadmin'@'%' IDENTIFIED WITH caching_sha2_password BY 'phpmyadmin';
GRANT SELECT, INSERT, UPDATE, DELETE ON phpmyadmin.* TO 'phpmyadmin'@'%';

CREATE DATABASE IF NOT EXISTS okeydatabase;
CREATE USER 'okeyapi'@'%' IDENTIFIED BY 'okeyapi';
GRANT ALL PRIVILEGES ON okeydatabase.* TO 'okeyapi'@'%';
CREATE USER 'okeyserver'@'%' IDENTIFIED BY 'okeyserver';
GRANT ALL PRIVILEGES ON okeydatabase.* TO 'okeyserver'@'%';

CREATE USER 'readonly'@'%' IDENTIFIED BY 'readonly_pass';
GRANT SELECT ON *.* TO 'readonly'@'%';

CREATE USER 'admin'@'%' IDENTIFIED BY 'admin_pass';
GRANT SELECT ON *.* TO 'admin'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON okeydatabase.* TO 'admin'@'%';
