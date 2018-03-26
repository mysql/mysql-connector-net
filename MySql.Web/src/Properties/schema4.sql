/*ALTER TABLE my_aspnet_membership CONVERT TO CHARACTER SET DEFAULT;
ALTER TABLE my_aspnet_roles CONVERT TO CHARACTER SET DEFAULT;
ALTER TABLE my_aspnet_usersinroles CONVERT TO CHARACTER SET DEFAULT;*/

UPDATE my_aspnet_schemaversion SET version=4 WHERE version=3;
