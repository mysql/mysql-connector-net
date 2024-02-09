 ALTER TABLE mysql_Membership 
            ADD PasswordKey char(32) AFTER Password, 
            ADD PasswordFormat tinyint AFTER PasswordKey, 
            CHANGE Email Email VARCHAR(128), COMMENT='2';
            
            
                