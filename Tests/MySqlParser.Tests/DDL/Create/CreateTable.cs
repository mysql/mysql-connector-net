// Copyright © 2012, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NUnit.Framework;


namespace MySql.Parser.Tests.Create
{
  [TestFixture]
  public class CreateTable
  {
    [Test]
    public void Simple()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CREATE TABLE T1 ( id int, name varchar( 20 ) )");
    }

    [Test]
    public void CreateSelect()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE test (a INT NOT NULL AUTO_INCREMENT,
				PRIMARY KEY (a) )
				ENGINE=MyISAM SELECT b,c FROM test2;");
    }

    [Test]
    public void Complex1()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE IF NOT EXISTS `schema`.`Employee` (
				`idEmployee` VARCHAR(45) NOT NULL ,
				`Name` VARCHAR(255) NULL ,
				`idAddresses` VARCHAR(45) NULL ,
				PRIMARY KEY (`idEmployee`) ,
				CONSTRAINT `fkEmployee_Addresses`
				FOREIGN KEY `fkEmployee_Addresses` (`idAddresses`)
				REFERENCES `schema`.`Addresses` (`idAddresses`)
				ON DELETE NO ACTION
				ON UPDATE NO ACTION)
				ENGINE = InnoDB,
				DEFAULT CHARACTER SET = utf8,
				COLLATE = utf8_bin");
    }

    [Test]
    public void MergeUnion()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "create temporary table tmp2 ( Id int primary key, Name varchar( 50 ) ) engine merge union (tmp1);");
    }

    [Test]
    public void AllOptions()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
@"
create temporary table if not exists Table1 ( id int ) 
engine = innodb, auto_increment = 7, avg_row_length = 100,
default character set = latin1, checksum = 1, collate = 'latin1_swedish_ci', comment = 'A test script',
connection = 'unknown', data directory = '/home/user/data', delay_key_write = 0, index directory = '/tmp',
insert_method = last, max_rows = 65536, min_rows = 1, pack_keys = default, password = 'ndn789w4^%$tf', 
row_format = dynamic, union = ( `db1`.`table2` );
");
    }

    [Test]
    public void Partition()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t1 (col1 INT, col2 CHAR(5), col3 DATETIME)
PARTITION BY HASH ( YEAR(col3) );");
    }

    [Test]
    public void Partition2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE tk (col1 INT, col2 CHAR(5), col3 DATE)
PARTITION BY KEY(col3)
PARTITIONS 4;");
    }

    [Test]
    public void Partition3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE tk (col1 INT, col2 CHAR(5), col3 DATE)
PARTITION BY LINEAR KEY(col3)
PARTITIONS 5;");
    }

    [Test]
    public void Partition4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t1 (
year_col  INT,
some_data INT
)
PARTITION BY RANGE (year_col) (
PARTITION p0 VALUES LESS THAN (1991),
PARTITION p1 VALUES LESS THAN (1995),
PARTITION p2 VALUES LESS THAN (1999),
PARTITION p3 VALUES LESS THAN (2002),
PARTITION p4 VALUES LESS THAN (2006),
PARTITION p5 VALUES LESS THAN MAXVALUE
);");
    }

    [Test]
    public void Partition5()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE client_firms (
id   INT,
name VARCHAR(35)
)
PARTITION BY LIST (id) (
PARTITION r0 VALUES IN (1, 5, 9, 13, 17, 21),
PARTITION r1 VALUES IN (2, 6, 10, 14, 18, 22),
PARTITION r2 VALUES IN (3, 7, 11, 15, 19, 23),
PARTITION r3 VALUES IN (4, 8, 12, 16, 20, 24)
);");
    }

    [Test]
    public void Partition6()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"
CREATE TABLE th (id INT, name VARCHAR(30), adate DATE)
PARTITION BY LIST(YEAR(adate))
(
PARTITION p1999 VALUES IN (1995, 1999, 2003)
DATA DIRECTORY = '/var/appdata/95/data'
INDEX DIRECTORY = '/var/appdata/95/idx',
PARTITION p2000 VALUES IN (1996, 2000, 2004)
DATA DIRECTORY = '/var/appdata/96/data'
INDEX DIRECTORY = '/var/appdata/96/idx',
PARTITION p2001 VALUES IN (1997, 2001, 2005)
DATA DIRECTORY = '/var/appdata/97/data'
INDEX DIRECTORY = '/var/appdata/97/idx',
PARTITION p2002 VALUES IN (1998, 2002, 2006)
DATA DIRECTORY = '/var/appdata/98/data'
INDEX DIRECTORY = '/var/appdata/98/idx'
);
");
    }

    [Test]
    public void Partition7()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE tn (c1 INT)
      PARTITION BY LIST(1 DIV c1) (
        PARTITION p0 VALUES IN (NULL),
        PARTITION p1 VALUES IN (1) )");
    }

    [Test]
    public void Partition8()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE tu (c1 BIGINT UNSIGNED)
    PARTITION BY RANGE(c1 - 10) (
      PARTITION p0 VALUES LESS THAN (-5),
      PARTITION p1 VALUES LESS THAN (0),
      PARTITION p2 VALUES LESS THAN (5),
      PARTITION p3 VALUES LESS THAN (10),
      PARTITION p4 VALUES LESS THAN (MAXVALUE) )");
    }

    [Test]
    public void Partition9()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE tkc (c1 CHAR)
PARTITION BY KEY(c1)
PARTITIONS 4;
");
    }

    [Test]
    public void Partition10()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE ts (
id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
name VARCHAR(30)
)
PARTITION BY KEY() 
PARTITIONS 4;");
    }

    [Test]
    public void Partition11()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE ts (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
      name VARCHAR(30)
  )
  PARTITION BY RANGE(id)
  SUBPARTITION BY KEY()
  SUBPARTITIONS 4
  (
      PARTITION p0 VALUES LESS THAN (100),
      PARTITION p1 VALUES LESS THAN (MAXVALUE)
  );
");
    }

    [Test]
    public void Partition12()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t1 (col1 INT, col2 CHAR(5), col3 DATETIME)
PARTITION BY HASH ( YEAR(col3) );");
    }

    [Test]
    public void Partition13()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t1 (col1 INT, col2 CHAR(5), col3 DATETIME)
PARTITION BY HASH ( YEAR(col3) );");
    }

    [Test]
    public void PartitionColumns_51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE members (
    firstname VARCHAR(25) NOT NULL,
    lastname VARCHAR(25) NOT NULL,
    username VARCHAR(16) NOT NULL,
    email VARCHAR(35),
    joined DATE NOT NULL
)
PARTITION BY RANGE COLUMNS(joined) (
    PARTITION p0 VALUES LESS THAN ('1960-01-01'),
    PARTITION p1 VALUES LESS THAN ('1970-01-01'),
    PARTITION p2 VALUES LESS THAN ('1980-01-01'),
    PARTITION p3 VALUES LESS THAN ('1990-01-01'),
    PARTITION p4 VALUES LESS THAN MAXVALUE;", true, out sb, new Version(5, 1));
      Assert.IsTrue(sb.ToString().IndexOf(" no viable alternative at input 'COLUMNS'") != -1);
    }

    [Test]
    public void PartitionColumns_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE members (
    firstname VARCHAR(25) NOT NULL,
    lastname VARCHAR(25) NOT NULL,
    username VARCHAR(16) NOT NULL,
    email VARCHAR(35),
    joined DATE NOT NULL
)
PARTITION BY RANGE COLUMNS(joined) (
    PARTITION p0 VALUES LESS THAN ('1960-01-01'),
    PARTITION p1 VALUES LESS THAN ('1970-01-01'),
    PARTITION p2 VALUES LESS THAN ('1980-01-01'),
    PARTITION p3 VALUES LESS THAN ('1990-01-01'),
    PARTITION p4 VALUES LESS THAN MAXVALUE );", false, out sb, new Version(5, 5));
    }

    [Test]
    public void PartitionColumns_2_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE members (
    firstname VARCHAR(25) NOT NULL,
    lastname VARCHAR(25) NOT NULL,
    username VARCHAR(16) NOT NULL,
    email VARCHAR(35),
    joined DATE NOT NULL
)
PARTITION BY LIST COLUMNS(joined) (
    PARTITION p0 VALUES LESS THAN ('1960-01-01'),
    PARTITION p1 VALUES LESS THAN ('1970-01-01'),
    PARTITION p2 VALUES LESS THAN ('1980-01-01'),
    PARTITION p3 VALUES LESS THAN ('1990-01-01'),
    PARTITION p4 VALUES LESS THAN MAXVALUE );", false, out sb, new Version(5, 5));
    }

    [Test]
    public void PartitionColumns_3_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t1 (
year_col  INT,
some_data INT
)
PARTITION BY RANGE (year_col) (
PARTITION p0 VALUES LESS THAN (1991, 1995, 1999, 2002, 2006));", false, out sb, new Version(5, 5));
    }

    [Test]
    public void PartitionColumns_2_51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE members (
    firstname VARCHAR(25) NOT NULL,
    lastname VARCHAR(25) NOT NULL,
    username VARCHAR(16) NOT NULL,
    email VARCHAR(35),
    joined DATE NOT NULL
)
PARTITION BY LIST COLUMNS(joined) (
    PARTITION p0 VALUES LESS THAN ('1960-01-01'),
    PARTITION p1 VALUES LESS THAN ('1970-01-01'),
    PARTITION p2 VALUES LESS THAN ('1980-01-01'),
    PARTITION p3 VALUES LESS THAN ('1990-01-01'),
    PARTITION p4 VALUES LESS THAN MAXVALUE;", true, out sb, new Version(5, 1));
      Assert.IsTrue(sb.ToString().IndexOf("'columns'", StringComparison.OrdinalIgnoreCase ) != -1);
    }

    [Test]
    public void Select()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE bar (m INT) SELECT n FROM foo;");
    }

    [Test]
    public void Select2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE artists_and_works
  SELECT artist.name, count( * ), COUNT(work.artist_id) AS number_of_works
  FROM artist LEFT JOIN work ON artist.id = work.artist_id
  GROUP BY artist.id;");

    }

    [Test]
    public void Select3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE bar (UNIQUE (n)) SELECT n FROM foo;");
    }

    [Test]
    public void Select4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE foo (a TINYINT NOT NULL) SELECT b+1 AS a FROM bar;");
    }

    [Test]
    public void Default()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t1 (i1 INT DEFAULT 0, i2 INT, i3 INT, i4 INT);");
    }

    [Test]
    public void IfNotExists()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE IF NOT EXISTS t1 (c1 CHAR(10)) SELECT 1, 2;");
    }

    [Test]
    public void Enum()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t
(
  c1 VARCHAR(10) CHARACTER SET binary,
  c2 TEXT CHARACTER SET binary,
  c3 ENUM('a','b','c') CHARACTER SET binary
);");
    }

    [Test]
    public void Enum2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t
(
  c1 VARBINARY(10),
  c2 BLOB,
  c3 ENUM('a','b','c') CHARACTER SET binary
);");
    }

    //[Test]
    //public void f1()
    //{
    //    MySQL51Parser.program_return r = Utility.ParseSql("");
    //}

    [Test]
    public void TableType50()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t
(
  c1 VARBINARY(10),
  c2 BLOB,
  c3 ENUM('a','b','c') CHARACTER SET binary
) type=innodb;", false, out sb, new Version(5, 0));
    }

    [Test]
    public void TableType51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE t
(
  c1 VARBINARY(10),
  c2 BLOB,
  c3 ENUM('a','b','c') CHARACTER SET binary
) type=innodb;", true, out sb, new Version(5, 1));
      Assert.IsTrue(sb.ToString().IndexOf("missing EndOfFile at 'type'") != -1);
    }

    [Test]
    public void Charset()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
          @"CREATE TABLE `city` ( `Name` char(35) NOT NULL DEFAULT '', `CountryCode` char(3) NOT NULL DEFAULT '', 
  `District` char(20) NOT NULL DEFAULT '', `Population` int(11) NOT NULL DEFAULT '0', `ID` int(11) NOT NULL AUTO_INCREMENT, 
  PRIMARY KEY (`ID`) ) ENGINE=MyISAM AUTO_INCREMENT=4080 DEFAULT CHARSET=latin1;", false, out sb );
    }

    [Test]
    public void Charset2()
    {
      StringBuilder sb;      
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CREATE TABLE `city` ( `Name` char(35) NOT NULL DEFAULT '', `CountryCode` char(3) NOT NULL DEFAULT '', 
  `District` char(20) NOT NULL DEFAULT '', `Population` int(11) NOT NULL DEFAULT '0', `ID` int(11) NOT NULL AUTO_INCREMENT, 
  PRIMARY KEY (`ID`) ) ENGINE=MyISAM AUTO_INCREMENT=4080 DEFAULT CHARACTER SET=latin1;", false, out sb );
    }
  }
}
