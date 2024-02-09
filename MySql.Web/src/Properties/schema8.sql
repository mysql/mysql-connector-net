DELETE FROM my_aspnet_sessioncleanup; 

ALTER TABLE my_aspnet_sessioncleanup ADD ApplicationId INT NOT NULL;
ALTER TABLE my_aspnet_sessioncleanup ADD Primary Key (ApplicationId);

UPDATE my_aspnet_schemaversion SET version=8;