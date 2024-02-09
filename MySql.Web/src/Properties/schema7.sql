RENAME TABLE my_aspnet_Applications TO my_aspnet_applications;
RENAME TABLE my_aspnet_Membership TO my_aspnet_membership;
RENAME TABLE my_aspnet_Profiles TO my_aspnet_profiles;
RENAME TABLE my_aspnet_Roles TO my_aspnet_roles;
RENAME TABLE my_aspnet_SchemaVersion TO my_aspnet_schemaversion;
RENAME TABLE my_aspnet_SessionCleanup TO my_aspnet_sessioncleanup;
RENAME TABLE my_aspnet_Sessions TO my_aspnet_sessions; 
RENAME TABLE my_aspnet_Users TO my_aspnet_users;
RENAME TABLE my_aspnet_UsersInRoles TO my_aspnet_usersinroles;

UPDATE my_aspnet_schemaversion SET version=7;