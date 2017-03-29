
create table my_aspnet_sitemap(
	Id int auto_increment primary key,
	Title varchar( 50 ),
	Description varchar( 512 ),
	Url varchar( 512 ),
	Roles varchar( 1000 ),
	ParentId int default null
) engine=Innodb;

UPDATE my_aspnet_schemaversion SET version=9;