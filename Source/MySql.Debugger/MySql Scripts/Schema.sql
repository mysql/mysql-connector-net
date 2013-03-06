

drop database if exists `serversidedebugger` //

CREATE DATABASE `serversidedebugger` //

USE `serversidedebugger` //

--
-- Table structure for table `debugcallstack`
--

DROP TABLE IF EXISTS `debugcallstack` //

CREATE TABLE `debugcallstack` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `DebugSessionId` int(11) DEFAULT NULL,
  `RoutineName` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=latin1 //

--
-- Table structure for table `debugdata`
--

DROP TABLE IF EXISTS `debugdata` //

CREATE TABLE `debugdata` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) DEFAULT NULL,
  `Val` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=latin1 //

--
-- Dumping data for table `debugdata`
--

LOCK TABLES `debugdata` WRITE //

INSERT INTO `debugdata` VALUES (1,'ScopeLevel','-1'),(2,'last_insert_id','0'),(3,'row_count','0'),( 4, 'NoDebugging', '0' ) //

UNLOCK TABLES //



--
-- Table structure for table `debuglocker`
--

DROP TABLE IF EXISTS `debuglocker` //

CREATE TABLE `debuglocker` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `MyData` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=latin1 //



--
-- Table structure for table `debugscope`
--

DROP TABLE IF EXISTS `debugscope` //

CREATE TABLE `debugscope` (
  `Id` int auto_increment primary key,
  `DebugSessionId` int(11) NOT NULL DEFAULT '0',
  `DebugScopeLevel` int(11) NOT NULL DEFAULT '0',
  `VarName` varchar(30) NOT NULL DEFAULT '',
  `VarValue` varbinary(50000) DEFAULT NULL,
  `Stamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  index `DebugScope2ndIndex` (`DebugSessionId`,`DebugScopeLevel`,`VarName`, `Id` )
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //



--
-- Table structure for table `debugsessions`
--

DROP TABLE IF EXISTS `debugsessions` //

CREATE TABLE `debugsessions` (
  `DebugSessionId` int(11) NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`DebugSessionId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //



--
-- Table structure for table `debugtbl`
--

DROP TABLE IF EXISTS `debugtbl` //
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `debugtbl` (
  `Id` int(11) NOT NULL DEFAULT '0',
  `Val` int(11) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //


--
-- Dumping data for table `debugtbl`
--

LOCK TABLES `debugtbl` WRITE //
/*!40000 ALTER TABLE `debugtbl` DISABLE KEYS */;
INSERT INTO `debugtbl` VALUES (1,0) //
/*!40000 ALTER TABLE `debugtbl` ENABLE KEYS */;
UNLOCK TABLES //
