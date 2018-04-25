DROP DATABASE IF EXISTS `[database0]`; CREATE DATABASE `[database0]`;
CREATE USER 'test'@'localhost' IDENTIFIED BY 'test';
CREATE USER 'test'@'%' IDENTIFIED BY 'test';

DROP DATABASE IF EXISTS `[database1]`; CREATE DATABASE `[database1]`;

GRANT ALL ON *.* to 'test'@'localhost';
GRANT ALL ON *.* to 'test'@'%';

FLUSH PRIVILEGES;

SET GLOBAL max_allowed_packet = 1048576;

DELETE FROM mysql.user WHERE length(user) = 0;
DELETE FROM mysql.user WHERE user='nopass';
DELETE FROM mysql.user WHERE user='quotedUser';

FLUSH PRIVILEGES;
