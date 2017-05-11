DROP USER IF EXISTS 'test'@'localhost';
DROP USER IF EXISTS 'testNoPass'@'localhost';
CREATE USER 'test'@'localhost' identified by 'test';
GRANT ALL PRIVILEGES  ON *.*  TO 'test'@'localhost';
CREATE USER 'testNoPass'@'localhost';
FLUSH PRIVILEGES;



