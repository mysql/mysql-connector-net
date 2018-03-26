/* Provider schema block -- version 3 */

/* create our application and user tables */
create table my_aspnet_applications(id INT PRIMARY KEY AUTO_INCREMENT, name VARCHAR(256), description VARCHAR(256));
create table my_aspnet_users(id INT PRIMARY KEY AUTO_INCREMENT, 
		applicationId INT NOT NULL, name VARCHAR(256) NOT NULL, 
		isAnonymous TINYINT(1) NOT NULL DEFAULT 1, lastActivityDate DATETIME);
create table my_aspnet_profiles(userId INT PRIMARY KEY, valueindex longtext, stringdata longtext, binarydata longblob, lastUpdatedDate timestamp);
create table my_aspnet_schemaversion(version INT);

insert into my_aspnet_schemaversion VALUES (3);
 
/* now we need to migrate all applications into our apps table */
insert into my_aspnet_applications (name) select ApplicationName from mysql_Membership UNION select ApplicationName from mysql_UsersInRoles;

/* now we make our changes to the existing tables */
alter table mysql_Membership
          rename to my_aspnet_membership,
          drop primary key,
          drop column pkid,
          drop column IsOnline,
          add column userId INT FIRST,
          add column applicationId INT AFTER userId;
          
alter table mysql_Roles
          rename to my_aspnet_roles,
          drop key Rolename,
          add column id INT PRIMARY KEY AUTO_INCREMENT FIRST,
          add column applicationId INT NOT NULL AFTER id;
          
alter table mysql_UsersInRoles
          drop key Username,
          rename to my_aspnet_usersinroles,
          add column userId INT FIRST,
          add column roleId INT AFTER userId,
          add column applicationId INT AFTER roleId;

/*ALTER TABLE my_aspnet_membership CONVERT TO CHARACTER SET DEFAULT;
ALTER TABLE my_aspnet_roles CONVERT TO CHARACTER SET DEFAULT;
ALTER TABLE my_aspnet_usersinroles CONVERT TO CHARACTER SET DEFAULT;*/

/* these next lines set the application Id on our tables appropriately */          
update my_aspnet_membership m, my_aspnet_applications a set m.applicationId = a.id where a.name=m.ApplicationName;
update my_aspnet_roles r, my_aspnet_applications a set r.applicationId = a.id where a.name=r.ApplicationName;
update my_aspnet_usersinroles u, my_aspnet_applications a set u.applicationId = a.id where a.name=u.ApplicationName;

/* now merge our usernames into our users table */
insert into my_aspnet_users (applicationId, name) 
        select applicationId, Username from my_aspnet_membership
        UNION select applicationId, Username from my_aspnet_usersinroles; 
          
/* now set the user ids in our tables accordingly */        
update my_aspnet_membership m, my_aspnet_users u set m.userId = u.id where u.name=m.Username AND u.applicationId=m.applicationId;
update my_aspnet_usersinroles r, my_aspnet_users u set r.userId = u.id where u.name=r.Username AND u.applicationId=r.applicationId;

/* now update the isanonymous and last activity date fields for the users */        
update my_aspnet_users u, my_aspnet_membership m 
        set u.isAnonymous=0, u.lastActivityDate=m.LastActivityDate 
        where u.name = m.Username;

/* make final changes to our tables */        
alter table my_aspnet_membership
          drop column Username,
          drop column ApplicationName,
          drop column applicationId,
          add primary key (userId);
          
/* next we set our role id values appropriately */
update my_aspnet_usersinroles u, my_aspnet_roles r set u.roleId = r.id where u.Rolename = r.Rolename and r.applicationId=u.applicationId;

/* now we make the final changes to our roles tables */                    
alter table my_aspnet_roles
          drop column ApplicationName,
          change column Rolename name VARCHAR(255) NOT NULL;
          
alter table my_aspnet_usersinroles
          drop column ApplicationName,
          drop column applicationId,
          drop column Username,
          drop column Rolename,
          add primary key (userId, roleId);
          
          
          
          