



-- -----------------------------------------------------------
-- Entity Designer DDL Script for MySQL Server 4.1 and higher
-- -----------------------------------------------------------
-- Date Created: 04/26/2012 16:08:15
-- Generated from EDMX file: C:\src\MainSource\features\bug64779-CascadingDelete\Tests\MySql.Data.Entity.Tests\ModelFirstModel1.edmx
-- Target version: 2.0.0.0
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- NOTE: if the constraint does not exist, an ignorable error will be reported.
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------
SET foreign_key_checks = 0;
SET foreign_key_checks = 1;

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Students'

CREATE TABLE `Students` (
    `Id` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` longtext  NOT NULL
);

-- Creating table 'Kardexes'

CREATE TABLE `Kardexes` (
    `Id` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `StudentId` int  NOT NULL,
    `Score` double  NOT NULL
);



-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------



-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on `StudentId` in table 'Kardexes'

ALTER TABLE `Kardexes`
ADD CONSTRAINT `FK_StudentKardex`
    FOREIGN KEY (`StudentId`)
    REFERENCES `Students`
        (`Id`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_StudentKardex'

CREATE INDEX `IX_FK_StudentKardex` 
    ON `Kardexes`
    (`StudentId`);

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
