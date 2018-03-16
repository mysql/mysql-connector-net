/*ALTER TABLE my_aspnet_sessions CONVERT TO CHARACTER SET DEFAULT;*/
ALTER TABLE my_aspnet_sessions MODIFY SessionItems LONGBLOB;

UPDATE my_aspnet_schemaversion SET version=6;
