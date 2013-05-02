DROP TABLE IF EXISTS `tablewithtimestamp`;

CREATE TABLE tablewithtimestamp (
  id INT NOT NULL AUTO_INCREMENT,
  value VARCHAR(45) NOT NULL,
  a_timestamp_field` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id)
) ENGINE=InnoDB;





               