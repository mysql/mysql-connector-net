CREATE TABLE my_aspnet_personalizationperuser(
id INT PRIMARY KEY AUTO_INCREMENT,
applicationId INT NOT NULL, 
pathId VARCHAR(36) DEFAULT NULL,
userId INT,
pageSettings BLOB NOT NULL,
lastUpdatedDate DATETIME NOT NULL)DEFAULT CHARSET=latin1 ;


CREATE TABLE my_aspnet_personalizationallusers(
pathId VARCHAR(36) PRIMARY KEY,
pageSettings BLOB NOT NULL,
lastUpdatedDate DATETIME NOT NULL)DEFAULT CHARSET=latin1 ;

CREATE TABLE my_aspnet_paths 
(
applicationId INT NOT NULL,
pathId	VARCHAR(36) PRIMARY KEY,
path	VARCHAR(256) NOT NULL,
loweredPath	VARCHAR(256) NOT NULL
)DEFAULT CHARSET=latin1 ;

UPDATE my_aspnet_schemaversion SET version=10;