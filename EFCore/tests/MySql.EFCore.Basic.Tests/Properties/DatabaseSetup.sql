DROP USER 'test';
create user 'test'@'localhost' identified by 'test';
grant all privileges on *.* to 'test'@'localhost';
flush privileges;

DROP DATABASE IF EXISTS `[database0]`; CREATE DATABASE `[database0]`;
GRANT ALL ON `[database0]`.* to 'test'@'localhost' IDENTIFIED BY 'test';
GRANT ALL ON `[database0]`.* to 'test'@'%' IDENTIFIED BY 'test';
FLUSH PRIVILEGES;

SET GLOBAL max_allowed_packet = 1048576;

FLUSH PRIVILEGES;