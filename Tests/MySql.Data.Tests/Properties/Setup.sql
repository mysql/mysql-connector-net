DROP DATABASE IF EXISTS `[database0]`; CREATE DATABASE `[database0]`;
GRANT ALL ON `[database0]`.* to 'test'@'localhost' IDENTIFIED BY 'test';
GRANT ALL ON `[database0]`.* to 'test'@'%' IDENTIFIED BY 'test';

DROP DATABASE IF EXISTS `[database1]`; CREATE DATABASE `[database1]`;
GRANT ALL ON `[database1]`.* to 'test'@'localhost' IDENTIFIED BY 'test';
GRANT ALL ON `[database1]`.* to 'test'@'%' IDENTIFIED BY 'test';

FLUSH PRIVILEGES;

SET GLOBAL max_allowed_packet = 1048576;

DELETE FROM mysql.user WHERE length(user) = 0;
DELETE FROM mysql.user WHERE user='nopass';
DELETE FROM mysql.user WHERE user='quotedUser';

FLUSH PRIVILEGES;
