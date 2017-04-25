DROP TABLE IF EXISTS `Order`;
DROP TABLE IF EXISTS Customer;
DROP TABLE IF EXISTS Products;
DROP TABLE IF EXISTS SalariedEmployees;
DROP TABLE IF EXISTS Employees;
DROP TABLE IF EXISTS EmployeeChildren;
DROP TABLE IF EXISTS Toys;
DROP TABLE IF EXISTS Companies;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS Shops;
DROP TABLE IF EXISTS Books;
DROP TABLE IF EXISTS Authors;
DROP TABLE IF EXISTS Publishers;
DROP TABLE IF EXISTS DataTypeTests;
DROP TABLE IF EXISTS DesktopComputers;
DROP TABLE IF EXISTS LaptopComputers;
DROP TABLE IF EXISTS TabletComputers;
DROP TABLE IF EXISTS Computers;
drop table if exists VideoGamePlatform;
drop table if exists GamingPlatform;
drop table if exists VideoGameTitle;
drop table if exists myeditionsinmybooks;
drop table if exists myeditions;
drop table if exists mybooks;
drop table if exists myauthors;

CREATE TABLE Employees(
	Id INT NOT NULL PRIMARY KEY,
	LastName NVARCHAR(20) NOT NULL, 
	FirstName NVARCHAR(10) NOT NULL,
	Age INT) ENGINE=InnoDB;

INSERT INTO Employees VALUES (1, 'Flintstone', 'Fred', 43);
INSERT INTO Employees VALUES (2, 'Flintstone', 'Wilma', 37);
INSERT INTO Employees VALUES (3, 'Rubble', 'Barney', 41);
INSERT INTO Employees VALUES (4, 'Rubble', 'Betty', 35);
INSERT INTO Employees VALUES (5, 'Slate', 'S', 62);
INSERT INTO Employees VALUES (6, 'Doo', 'Scooby', 7);
INSERT INTO Employees VALUES (7, 'Underdog', 'J', 12);

CREATE TABLE SalariedEmployees(
	EmployeeId INT NOT NULL PRIMARY KEY,
	Salary INT NOT NULL,
	CONSTRAINT FOREIGN KEY (EmployeeId) REFERENCES Employees (Id)) Engine=InnoDB;
	
INSERT INTO salariedEmployees VALUES (5, 500);
INSERT INTO salariedEmployees VALUES (7, 50);

CREATE TABLE EmployeeChildren(
	Id INT UNSIGNED NOT NULL PRIMARY KEY,
	EmployeeId INT NOT NULL,
	LastName NVARCHAR(20) NOT NULL,
	FirstName NVARCHAR(10) NOT NULL,
	BirthTime TIME,
	Weight DOUBLE,
	LastModified TIMESTAMP NOT NULL);

INSERT INTO EmployeeChildren VALUES (1, 1, 'Flintstone', 'Pebbles', NULL, NULL, NULL);

CREATE TABLE Companies (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(100) NOT NULL,
	`DateBegan` DATETIME,
	`NumEmployees` INT,
	`Address` VARCHAR(50),
	`City` VARCHAR(50),
	`State` CHAR(2),
	`ZipCode` CHAR(9),
	CONSTRAINT PK_Companies PRIMARY KEY (Id)) ENGINE=InnoDB;

INSERT INTO Companies VALUES (1, 'Hasbro', '1996-11-15 5:18:23', 200, '123 My Street', 'Nashville', 'TN', 12345);
INSERT INTO Companies VALUES (2, 'Acme', NULL, 55, '45 The Lane', 'St. Louis', 'MO', 44332);
INSERT INTO Companies VALUES (3, 'Bandai America', NULL, 376, '1 Infinite Loop', 'Cupertino', 'CA', 54321);
INSERT INTO Companies VALUES (4, 'Lego Group', NULL, 700, '222 Park Circle', 'Lexington', 'KY', 32323);
INSERT INTO Companies VALUES (5, 'Mattel', NULL, 888, '111 Parkwood Ave', 'San Jose', 'CA', 55676);
INSERT INTO Companies VALUES (6, 'K''NEX', NULL, 382, '7812 N. 51st', 'Dallas', 'TX', 11239);
INSERT INTO Companies VALUES (7, 'Playmobil', NULL, 541, '546 Main St.', 'Omaha', 'NE', 78439);

CREATE TABLE Toys (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`SupplierId` INT NOT NULL,
	`Name` varchar(100) NOT NULL,
	`MinAge` int NOT NULL,
	CONSTRAINT PK_Toys PRIMARY KEY (Id),
	KEY `SupplierId` (`SupplierId`),
	FOREIGN KEY (SupplierId) REFERENCES Companies(Id) ) ENGINE=InnoDB;
	
INSERT INTO Toys VALUES (1, 3, 'Slinky', 2);	
INSERT INTO Toys VALUES (2, 2, 'Rubiks Cube', 5);	
INSERT INTO Toys VALUES (3, 1, 'Lincoln Logs', 3);	
INSERT INTO Toys VALUES (4, 4, 'Legos', 4);	

CREATE TABLE Computers (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`Brand` varchar(100) NOT NULL,
	CONSTRAINT PK_Computers PRIMARY KEY (Id)) ENGINE=InnoDB;

INSERT INTO Computers VALUES (1, 'Dell');		
INSERT INTO Computers VALUES (2, 'Acer');
INSERT INTO Computers VALUES (3, 'Toshiba');		
INSERT INTO Computers VALUES (4, 'Sony');
INSERT INTO Computers VALUES (5, 'Apple');		
INSERT INTO Computers VALUES (6, 'HP');

CREATE TABLE DesktopComputers (
  `IdDesktopComputer` INT NOT NULL ,
  `Color` VARCHAR(15) NULL DEFAULT NULL ,
  PRIMARY KEY (`IdDesktopComputer`) ,
  CONSTRAINT FK_DesktopComputer_Computer
    FOREIGN KEY (IdDesktopComputer)
    REFERENCES Computers (Id)) ENGINE = InnoDB;
    
INSERT INTO DesktopComputers VALUES (1, 'White');
INSERT INTO DesktopComputers VALUES (2, 'Black');

CREATE TABLE LaptopComputers (
  `IdLaptopComputer` INT NOT NULL ,
  `Size` VARCHAR(45) NULL DEFAULT NULL ,
  `IsCertified` BIT(1) NULL DEFAULT NULL ,
  PRIMARY KEY (IdLaptopComputer) ,
  CONSTRAINT FK_LaptopComputer_Computer
    FOREIGN KEY (IdLaptopComputer)
    REFERENCES Computers(Id)) ENGINE = InnoDB;

INSERT INTO LaptopComputers VALUES (3, '13.2 x 9.4', 1);
INSERT INTO LaptopComputers VALUES (4, '19.5 x 13', 0);

CREATE TABLE TabletComputers (
  `IdTabletComputer` INT NOT NULL ,
  `IsAvailable` BIT(1) NULL DEFAULT NULL ,
  `ReleaseDate` DATETIME NULL DEFAULT NULL ,
  PRIMARY KEY (IdTabletComputer) ,
  CONSTRAINT FK_TabletComputer_Computer
    FOREIGN KEY (IdTabletComputer)
    REFERENCES Computers(Id)) ENGINE = InnoDB;

INSERT INTO TabletComputers VALUES (5, 1, '2011-05-04');
INSERT INTO TabletComputers VALUES (6, 1, '2010-06-09');

CREATE TABLE Shops (
	id INT PRIMARY KEY,
	`name` VARCHAR(50) NOT NULL,
	address VARCHAR(50),
	city VARCHAR(50),
	state CHAR(2),
	zipcode CHAR(9)	
	) ENGINE=InnoDB;
INSERT INTO Shops VALUES (1, 'Target', '2417 N. Haskell Ave', 'Dallas', 'TX', '75204');
INSERT INTO Shops VALUES (2, 'K-Mart', '4225 W. Indian School Rd.', 'Phoenix', 'AZ', '85019');
INSERT INTO Shops VALUES (3, 'Wal-Mart', '1238 Putty Hill Ave', 'Towson', 'MD', '21286');	
	
	
CREATE TABLE Orders (
	id INT PRIMARY KEY,
	shopId INT NOT NULL,
	freight DOUBLE NOT NULL,
	FOREIGN KEY (shopId) REFERENCES Shops(id)) ENGINE=InnoDB;
INSERT INTO Orders VALUES (1, 1, 65.3);
INSERT INTO Orders VALUES (2, 2, 127.8);
INSERT INTO Orders VALUES (3, 3, 254.78);	
INSERT INTO Orders VALUES (4, 1, 165.8);
INSERT INTO Orders VALUES (5, 2, 85.2);
INSERT INTO Orders VALUES (6, 3, 98.5);	
INSERT INTO Orders VALUES (7, 1, 222.3);
INSERT INTO Orders VALUES (8, 2, 125);
INSERT INTO Orders VALUES (9, 3, 126.4);	
INSERT INTO Orders VALUES (10, 3, 350.54721);	


CREATE TABLE Authors(
	id INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
	`name` VARCHAR(20) NOT NULL,
	age INT) ENGINE=InnoDB;
INSERT INTO Authors VALUES (1, 'Tom Clancy', 65);
INSERT INTO Authors VALUES (2, 'Stephen King', 57);
INSERT INTO Authors VALUES (3, 'John Grisham', 49);
INSERT INTO Authors VALUES (4, 'Dean Koontz', 52);
INSERT INTO Authors VALUES (5, 'Don Box', 44);


CREATE TABLE Publishers(
	id INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
	`name` VARCHAR(20) NOT NULL) ENGINE=InnoDB;
INSERT INTO Publishers VALUES (1, 'Acme Publishing');
	
CREATE TABLE Books (
	id INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
	`name` VARCHAR(20) NOT NULL,
	pages int,
	author_id int NOT NULL,
	publisher_id int NOT NULL,
	FOREIGN KEY (author_id) REFERENCES Authors(id),
	FOREIGN KEY (publisher_id) REFERENCES Publishers(id)) ENGINE=InnoDB;
INSERT INTO Books VALUES (1, 'Debt of Honor', 200, 1, 1);
INSERT INTO Books VALUES (2, 'Insomnia', 350, 2, 1);
INSERT INTO Books VALUES (3, 'Rainmaker', 475, 3, 1);
INSERT INTO Books VALUES (4, 'Hallo', 175, 3, 1);
INSERT INTO Books VALUES (5, 'Debt of Honor', 200, 1, 1);

SET @guid=UUID();
CREATE TABLE DataTypeTests(
	id CHAR(36) CHARACTER SET utf8 NOT NULL PRIMARY KEY,
	id2 CHAR(36) BINARY NOT NULL,
	idAsChar VARCHAR(36));
INSERT INTO DataTypeTests VALUES (@guid, @guid, @guid);	
INSERT INTO DataTypeTests VALUES ('481A6506-03A3-4ef9-A05A-B247E75A2FB4',
	'481A6506-03A3-4ef9-A05A-B247E75A2FB4', '481A6506-03A3-4ef9-A05A-B247E75A2FB4');

CREATE  TABLE Products (
  Id INT NOT NULL AUTO_INCREMENT,
  Name VARCHAR(45) NOT NULL,
  CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (Id));
  
CREATE TABLE Customer (
  Id int(11) NOT NULL auto_increment,
  Name varchar(255) default NULL,
  PRIMARY KEY  (ID)
) ENGINE=InnoDB AUTO_INCREMENT=5;

insert  into Customer(Id,Name) 
        values (1,'Fred'),(2,'Barney'),(3,'Wilma'),(4,'Betty');

CREATE TABLE `Order` (
  Id int(11) NOT NULL auto_increment,
  Order_Date datetime NOT NULL,
  Customer_Id int(11) default NULL,
  PRIMARY KEY  (ID),
  CONSTRAINT FK_ORDER_CUSTOMER FOREIGN KEY (Customer_Id) REFERENCES Customer (Id)
) ENGINE=InnoDB AUTO_INCREMENT=7;

insert  into `Order`(Id,Order_Date,Customer_Id) 
        values (1,'2011-06-24 00:00:00',1),(2,'2011-06-25 00:00:00',1),(3,'2011-06-26 00:00:00',1),
               (4,'2011-06-01 00:00:00',2),(5,'2011-06-02 00:00:00',2),(6,'2011-06-03 00:00:00',3);  
               
create table `GamingPlatform` ( 
	Id int( 11 ) not null auto_increment,
	Name varchar( 50 ) default null,
	Developer varchar( 50 ) default null,
	primary key ( Id )
) ENGINE=InnoDB;

insert into `GamingPlatform`( Id, Name, Developer )
	values ( 1, 'Playstation 3', 'Sony' ), ( 2, 'Xbox 360', 'Microsoft' ), ( 3, 'Wii', 'Nintendo' ), ( 4, 'PC', NULL );

create table `VideoGameTitle` (
    Id int ( 11 ),
    Name varchar( 50 ),
    Developer varchar( 50 ),
    primary key ( Id )
) ENGINE=InnoDB;

insert into `VideoGameTitle` ( Id, Name, Developer )
	values ( 1, 'Halo 3', 'Bungie' ), ( 2, 'Gears of War', 'Epyx' ), ( 3, 'Call of Duty: Black Ops', 'Treyarch' ),
	( 4, 'Resistance', 'Insomniac' ), ( 5, 'FIFA 11', 'EA' ), ( 6, 'Fallout 3', 'Bethesda' ), 
	( 7, '3D Dot Game Heroes', 'From' ), ( 8, 'Mario Galaxy', 'Nintendo' );

create view `viVideoGameTitle` as
select vi.`Id`, vi.`Name`, vi.`Developer`
from `VideoGameTitle` vi;

create table `VideoGamePlatform` ( 
	IdGamingPlatform int( 11 ) not null,
	IdVideoGameTitle int( 11 ) not null,
	Category varchar( 50 ),
	primary key ( IdGamingPlatform, IdVideoGameTitle ),
	CONSTRAINT fk_VideoGamePlatform_Platform FOREIGN KEY (IdGamingPlatform) REFERENCES GamingPlatform ( Id ),
	constraint fk_VideoGamePlatform_VideoGameTitle FOREIGN KEY ( IdVideoGameTitle ) REFERENCES VideoGameTitle ( Id )
) ENGINE=InnoDB;

insert into `VideoGamePlatform` ( IdGamingPlatform, IdVideoGameTitle, Category )
	values ( 1, 3, 'FPS' ), ( 1, 4, 'FPS' ), ( 1, 5, 'Soccer' ), ( 1, 6, 'RPG' ), ( 1, 7, 'RPG-Action' ), ( 2, 1, 'FPS' ), ( 2, 2, '3PS' ), ( 2, 3, 'FPS' ), 
    ( 2, 5, 'Soccer' ), ( 2, 6, 'RPG' ),	( 3, 5, 'Soccer' ), ( 3, 8, 'Platformer' );

create table myeditions ( id int(11) not null, title varchar(45) not null, primary key (id)) engine=innodb default charset=latin1;
create table myauthors (id int(11) not null, name varchar(45) not null, primary key (id))  engine=innodb default charset=latin1;
create table mybooks ( id int(11) not null, authorid int(11) not null, primary key (id),  key fk_authors_mybooks (authorid), constraint fk_authors_mybooks foreign key (authorid) references myauthors (id) on delete no action on update no action) engine=innodb default charset=latin1;
create table myeditionsinmybooks ( bookid int(11) not null, editionid int(11) not null, primary key (bookid,editionid), key fk1 (bookid), key fk2 (editionid), constraint fk1 foreign key (bookid) references mybooks (id) on delete no action on update no action,
  constraint fk2 foreign key (editionid) references myeditions (id) on delete no action on update no action
) engine=innodb default charset=latin1;

insert into `myEditions` values (1,'Some Book First Edition'),(2,'Another Book First Edition'),(3,'Another Book Second Edition'),(4,'Another Book Third Edition');
insert into `myAuthors` values (1,'Some Author'),(2,'Another Author');
insert into `myBooks` values (8,1),(9,1);
insert into `myEditionsinmyBooks` values (8,1),(9,2),(9,3),(9,4);
               