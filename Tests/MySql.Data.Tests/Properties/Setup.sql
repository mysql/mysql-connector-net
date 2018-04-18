DROP DATABASE IF EXISTS `[database0]`; CREATE DATABASE `[database0]`;
DROP USER IF EXISTS 'test'@'localhost';
CREATE USER 'test'@'localhost' IDENTIFIED BY 'test';
DROP USER IF EXISTS 'test'@'%';
CREATE USER 'test'@'%' IDENTIFIED BY 'test';
GRANT ALL ON `[database0]`.* to 'test'@'localhost';
GRANT ALL ON `[database0]`.* to 'test'@'%';

DROP DATABASE IF EXISTS `[database1]`; CREATE DATABASE `[database1]`;
GRANT ALL ON `[database1]`.* to 'test'@'localhost';
GRANT ALL ON `[database1]`.* to 'test'@'%';

FLUSH PRIVILEGES;

SET GLOBAL max_allowed_packet = 1048576;

DELETE FROM mysql.user WHERE length(user) = 0;
DELETE FROM mysql.user WHERE user='nopass';
DELETE FROM mysql.user WHERE user='quotedUser';

FLUSH PRIVILEGES;
