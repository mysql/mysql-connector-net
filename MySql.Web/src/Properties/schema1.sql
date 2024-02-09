CREATE TABLE  mysql_Membership(`PKID` varchar(36) NOT NULL,
              Username varchar(255) NOT NULL, 
              ApplicationName varchar(255) NOT NULL,
              Email varchar(128) NOT NULL, 
              Comment varchar(255) default NULL,
              Password varchar(128) NOT NULL, 
              PasswordQuestion varchar(255) default NULL,
              PasswordAnswer varchar(255) default NULL, 
              IsApproved tinyint(1) default NULL,
              LastActivityDate datetime default NULL, 
              LastLoginDate datetime default NULL,
              LastPasswordChangedDate datetime default NULL, 
              CreationDate datetime default NULL,
              IsOnline tinyint(1) default NULL, 
              IsLockedOut tinyint(1) default NULL,
              LastLockedOutDate datetime default NULL, 
              FailedPasswordAttemptCount int(10) unsigned default NULL,
              FailedPasswordAttemptWindowStart datetime default NULL,
              FailedPasswordAnswerAttemptCount int(10) unsigned default NULL,
              FailedPasswordAnswerAttemptWindowStart datetime default NULL,
              PRIMARY KEY  (`PKID`)) DEFAULT CHARSET=latin1 COMMENT='1';
              
CREATE TABLE  mysql_UsersInRoles(`Username` varchar(255) NOT NULL,
                `Rolename` varchar(255) NOT NULL, `ApplicationName` varchar(255) NOT NULL,
                KEY `Username` (`Username`,`Rolename`,`ApplicationName`)
                ) DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;

CREATE TABLE mysql_Roles(`Rolename` varchar(255) NOT NULL,
                `ApplicationName` varchar(255) NOT NULL, 
                KEY `Rolename` (`Rolename`,`ApplicationName`)
                ) DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
                
                