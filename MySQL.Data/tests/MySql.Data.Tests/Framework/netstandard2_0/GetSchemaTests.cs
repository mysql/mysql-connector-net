// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.Common;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Data;
using System.Linq;

namespace MySql.Data.MySqlClient.Tests
{
  public class GetSchemaTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void Collections()
    {
      DataTable dt = Connection.GetSchema();

      int row = 0;
      Assert.That(dt.Rows[row++][0], Is.EqualTo("MetaDataCollections"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("DataSourceInformation"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("DataTypes"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Restrictions"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("ReservedWords"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Databases"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Tables"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Columns"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Users"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Foreign Keys"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("IndexColumns"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Indexes"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Foreign Key Columns"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("UDF"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Views"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("ViewColumns"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Procedure Parameters"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Procedures"));
      Assert.That(dt.Rows[row++][0], Is.EqualTo("Triggers"));
    }

    /// <summary>
    /// Bug #25907 DataType Column of DataTypes collection does'nt contain the correct CLR Datatype 
    /// Bug #25947 CreateFormat/CreateParameters Column of DataTypes collection incorrect for CHAR 
    /// </summary>
    [Test]
    public void DataTypes()
    {
      Assume.That(Version >= new Version(9, 0, 0), "This test is for MySql 9.0 or higher.");

      DataTable dt = Connection.GetSchema("DataTypes", new string[] { });

      foreach (DataRow row in dt.Rows)
      {
        string type = row["TypeName"].ToString();
        Type systemType = Type.GetType(row["DataType"].ToString());
        if (type == "BIT")
          Assert.That(systemType, Is.EqualTo(typeof(System.UInt64)));
        else if (type == "DATE" || type == "DATETIME" ||
          type == "TIMESTAMP")
          Assert.That(systemType, Is.EqualTo(typeof(System.DateTime)));
        else if (type == "BLOB" || type == "TINYBLOB" ||
             type == "MEDIUMBLOB" || type == "LONGBLOB")
          Assert.That(systemType, Is.EqualTo(typeof(System.Byte[])));
        else if (type == "TIME")
          Assert.That(systemType, Is.EqualTo(typeof(System.TimeSpan)));
        else if (type == "CHAR" || type == "VARCHAR")
        {
          Assert.That(systemType, Is.EqualTo(typeof(System.String)));
          Assert.That(!Convert.ToBoolean(row["IsFixedLength"]));
          string format = type + "({0})";
          Assert.That(row["CreateFormat"].ToString(), Is.EqualTo(format));
        }
        else if (type == "SET" || type == "ENUM")
          Assert.That(systemType, Is.EqualTo(typeof(System.String)));
        else if (type == "DOUBLE")
          Assert.That(systemType, Is.EqualTo(typeof(System.Double)));
        else if (type == "SINGLE")
          Assert.That(systemType, Is.EqualTo(typeof(System.Single)));
        else if (type == "TINYINT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.That(systemType, Is.EqualTo(typeof(System.Byte)));
          else
            Assert.That(systemType, Is.EqualTo(typeof(System.SByte)));
        }
        else if (type == "SMALLINT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.That(systemType, Is.EqualTo(typeof(System.UInt16)));
          else
            Assert.That(systemType, Is.EqualTo(typeof(System.Int16)));
        }
        else if (type == "MEDIUMINT" || type == "INT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.That(systemType, Is.EqualTo(typeof(System.UInt32)));
          else
            Assert.That(systemType, Is.EqualTo(typeof(System.Int32)));
        }
        else if (type == "BIGINT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.That(systemType, Is.EqualTo(typeof(System.UInt64)));
          else
            Assert.That(systemType, Is.EqualTo(typeof(System.Int64)));
        }
        else if (type == "DECIMAL")
        {
          Assert.That(systemType, Is.EqualTo(typeof(System.Decimal)));
          Assert.That(row["CreateFormat"].ToString(), Is.EqualTo("DECIMAL({0},{1})"));
        }
        else if (type == "TINYINT")
          Assert.That(systemType, Is.EqualTo(typeof(System.Byte)));
      }
    }

    [Test]
    public void Tables()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test1");
      ExecuteSQL("CREATE TABLE test1 (id int)");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "test1";
      DataTable dt = Connection.GetSchema("Tables", restrictions);
      Assert.That(dt.Columns["VERSION"].DataType == typeof(UInt64)
        || dt.Columns["VERSION"].DataType == typeof(Int64));
      Assert.That(dt.Columns["TABLE_ROWS"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["AVG_ROW_LENGTH"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["DATA_LENGTH"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["MAX_DATA_LENGTH"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["INDEX_LENGTH"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["DATA_FREE"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["AUTO_INCREMENT"].DataType == typeof(UInt64));
      Assert.That(dt.Columns["CHECKSUM"].DataType == typeof(UInt64)
        || dt.Columns["CHECKSUM"].DataType == typeof(Int64));
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("Tables"));
      Assert.That(dt.Rows[0][2], Is.EqualTo("test1"));
    }

    [Test]
    public void Columns()
    {
      ExecuteSQL(@"CREATE TABLE Test (col1 int, col2 decimal(20,5), 
        col3 varchar(50) character set utf8, col4 tinyint unsigned, 
        col5 varchar(20) default 'boo')");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "Test";
      DataTable dt = Connection.GetSchema("Columns", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(5));
      Assert.That(dt.TableName, Is.EqualTo("Columns"));
      if (Connection.driver.Version.isAtLeast(8, 0, 1))
      {
        Assert.That(dt.Columns["ORDINAL_POSITION"].DataType == typeof(UInt32));
        Assert.That(dt.Columns["CHARACTER_MAXIMUM_LENGTH"].DataType == typeof(Int64));
        Assert.That(dt.Columns["NUMERIC_PRECISION"].DataType == typeof(UInt64));
        Assert.That(dt.Columns["NUMERIC_SCALE"].DataType == typeof(UInt64));
      }
      else
      {
        Assert.That(dt.Columns["ORDINAL_POSITION"].DataType == typeof(UInt64));
        Assert.That(dt.Columns["CHARACTER_MAXIMUM_LENGTH"].DataType == typeof(UInt64));
        Assert.That(dt.Columns["NUMERIC_PRECISION"].DataType == typeof(UInt64));
        Assert.That(dt.Columns["NUMERIC_SCALE"].DataType == typeof(UInt64));
      }

      // first column
      Assert.That(dt.Rows[0]["TABLE_SCHEMA"].ToString().ToUpper(), Is.EqualTo((Connection.Database).ToUpper()));
      Assert.That(dt.Rows[0]["COLUMN_NAME"].ToString().ToUpper(), Is.EqualTo("COL1"));
      Assert.That(Convert.ToInt32(dt.Rows[0]["ORDINAL_POSITION"]), Is.EqualTo(1));
      Assert.That(dt.Rows[0]["IS_NULLABLE"], Is.EqualTo("YES"));
      Assert.That(dt.Rows[0]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      // second column
      Assert.That(dt.Rows[1]["TABLE_SCHEMA"].ToString().ToUpper(), Is.EqualTo((Connection.Database).ToUpper()));
      Assert.That(dt.Rows[1]["COLUMN_NAME"].ToString().ToUpper(), Is.EqualTo("COL2"));
      Assert.That(Convert.ToInt32(dt.Rows[1]["ORDINAL_POSITION"]), Is.EqualTo(2));
      Assert.That(dt.Rows[1]["IS_NULLABLE"], Is.EqualTo("YES"));
      Assert.That(dt.Rows[1]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("DECIMAL"));
      Assert.That(dt.Rows[1]["COLUMN_TYPE"].ToString().ToUpper(), Is.EqualTo("DECIMAL(20,5)"));
      Assert.That(Convert.ToInt32(dt.Rows[1]["NUMERIC_PRECISION"]), Is.EqualTo(20));
      Assert.That(Convert.ToInt32(dt.Rows[1]["NUMERIC_SCALE"]), Is.EqualTo(5));

      // third column
      Assert.That(dt.Rows[2]["TABLE_SCHEMA"].ToString().ToUpper(), Is.EqualTo((Connection.Database).ToUpper()));
      Assert.That(dt.Rows[2]["COLUMN_NAME"].ToString().ToUpper(), Is.EqualTo("COL3"));
      Assert.That(Convert.ToInt32(dt.Rows[2]["ORDINAL_POSITION"]), Is.EqualTo(3));
      Assert.That(dt.Rows[2]["IS_NULLABLE"], Is.EqualTo("YES"));
      Assert.That(dt.Rows[2]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[2]["COLUMN_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR(50)"));

      // fourth column
      Assert.That(dt.Rows[3]["TABLE_SCHEMA"].ToString().ToUpper(), Is.EqualTo((Connection.Database).ToUpper()));
      Assert.That(dt.Rows[3]["COLUMN_NAME"].ToString().ToUpper(), Is.EqualTo("COL4"));
      Assert.That(Convert.ToInt32(dt.Rows[3]["ORDINAL_POSITION"]), Is.EqualTo(4));
      Assert.That(dt.Rows[3]["IS_NULLABLE"], Is.EqualTo("YES"));
      Assert.That(dt.Rows[3]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("TINYINT"));

      // fifth column
      Assert.That(dt.Rows[4]["TABLE_SCHEMA"].ToString().ToUpper(), Is.EqualTo((Connection.Database).ToUpper()));
      Assert.That(dt.Rows[4]["COLUMN_NAME"].ToString().ToUpper(), Is.EqualTo("COL5"));
      Assert.That(Convert.ToInt32(dt.Rows[4]["ORDINAL_POSITION"]), Is.EqualTo(5));
      Assert.That(dt.Rows[4]["IS_NULLABLE"], Is.EqualTo("YES"));
      Assert.That(dt.Rows[4]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[4]["COLUMN_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR(20)"));
      Assert.That(dt.Rows[4]["COLUMN_DEFAULT"].ToString().ToUpper(), Is.EqualTo("BOO"));
    }


    ///<summary>
    ///Testing out schema information about generated columns
    /// only in version 5.7.6 or later    
    ///</summary>
    [Test]
    public void CanGetSchemaInformationGeneratedColumns()
    {
      Assume.That(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows));
      Assume.That(Version >= new Version(5, 7, 6));

      ExecuteSQL("CREATE TABLE `Test` (`ID` int NOT NULL AUTO_INCREMENT PRIMARY KEY, `Name` char(35) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL)");

      var cmd = new MySqlCommand("ALTER TABLE Test ADD COLUMN Name_ci char(35) CHARACTER SET utf8 AS (Name) STORED;", Connection);
      cmd.ExecuteNonQuery();

      DataTable dt = Connection.GetSchema("Columns", new string[] { null, null, "Test", null });
      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.TableName, Is.EqualTo("Columns"));
      if (Version.Major >= 5 && Version.Minor >= 7 && Version.Build >= 6)
      {
        Assert.That(dt.Rows[2]["DATA_TYPE"], Is.EqualTo("char"));
        Assert.That(dt.Rows[2]["GENERATION_EXPRESSION"].ToString().Trim('`'), Is.EqualTo("Name"));
        Assert.That(dt.Rows[2]["EXTRA"], Is.EqualTo("STORED GENERATED"));
      }
    }


    /// <summary> 
    /// Bug #46270 connection.GetSchema("Columns") fails on MySQL 4.1  
    /// </summary> 
    [Test]
    public void EnumAndSetColumns()
    {
      ExecuteSQL("CREATE TABLE Test (col1 set('A','B','C'), col2 enum('A','B','C'))");

      DataTable dt = Connection.GetSchema("Columns", new string[] { null, null, "Test", null });
      Assert.That(dt.Rows.Count, Is.EqualTo(2));
      Assert.That(dt.Rows[0]["DATA_TYPE"], Is.EqualTo("set"));
      Assert.That(dt.Rows[1]["DATA_TYPE"], Is.EqualTo("enum"));
      Assert.That(dt.Rows[0]["COLUMN_TYPE"], Is.EqualTo("set('A','B','C')"));
      Assert.That(dt.Rows[1]["COLUMN_TYPE"], Is.EqualTo("enum('A','B','C')"));
    }

    [Test]
    public void Procedures()
    {
      ExecuteSQL("DROP PROCEDURE IF EXISTS spTest");
      ExecuteSQL("CREATE PROCEDURE spTest (id int) BEGIN SELECT 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("Procedures", restrictions);
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("Procedures"));
      Assert.That(dt.Rows[0][3], Is.EqualTo("spTest"));
    }

    /// <summary>
    /// Bug #33674814 - Empty result from MySqlConnection.GetSchema("Procedure Parameters") call.
    /// Incorrect casting on [NUMERIC_PRECISION] column whn looking for "PROCEDURE PARAMETERS" collection without any restriction
    /// </summary>
    [Test]
    public void ProcedureParameters()
    {
      var dt = Connection.GetSchema("PROCEDURE PARAMETERS");

      Assert.That(dt.TableName, Is.EqualTo("Procedure Parameters"));
      Assert.That(dt.Rows.Count > 0);
    }

    [Test]
    public void Functions()
    {
      ExecuteSQL("DROP FUNCTION IF EXISTS spFunc");
      ExecuteSQL("CREATE FUNCTION spFunc (id int) RETURNS INT DETERMINISTIC NO SQL BEGIN RETURN 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spFunc";
      DataTable dt = Connection.GetSchema("Procedures", restrictions);
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("Procedures"));
      Assert.That(dt.Rows[0][3], Is.EqualTo("spFunc"));
    }

    [Test]
    public void Indexes()
    {
      ExecuteSQL("CREATE TABLE Test (id int, PRIMARY KEY(id))");
      string[] restrictions = new string[4];
      restrictions[2] = "Test";
      restrictions[1] = Connection.Database;
      DataTable dt = Connection.GetSchema("Indexes", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That((bool)dt.Rows[0]["PRIMARY"]);
      Assert.That((bool)dt.Rows[0]["UNIQUE"]);

      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id int, name varchar(50), " +
        "UNIQUE KEY key2 (name))");

      dt = Connection.GetSchema("Indexes", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[0]["INDEX_NAME"], Is.EqualTo("key2"));
      Assert.That(!(bool)dt.Rows[0]["PRIMARY"]);
      Assert.That((bool)dt.Rows[0]["UNIQUE"]);

      restrictions[3] = "key2";
      dt = Connection.GetSchema("Indexes", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[0]["INDEX_NAME"], Is.EqualTo("key2"));
      Assert.That(!(bool)dt.Rows[0]["PRIMARY"]);
      Assert.That((bool)dt.Rows[0]["UNIQUE"]);

      /// <summary> 
      /// Bug #48101	MySqlConnection.GetSchema on "Indexes" throws when there's a table named "b`a`d" 
      /// </summary> 
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL(@"CREATE TABLE `Te``s``t` (id int, name varchar(50), " +
        "KEY key2 (name))");

      restrictions[2] = "Te`s`t";
      dt = Connection.GetSchema("Indexes", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("te`s`t").IgnoreCase);
      Assert.That(dt.Rows[0]["INDEX_NAME"], Is.EqualTo("key2"));
      Assert.That(!(bool)dt.Rows[0]["PRIMARY"]);
      Assert.That(!(bool)dt.Rows[0]["UNIQUE"]);
    }

    [Test]
    public void IndexColumns()
    {
      ExecuteSQL("CREATE TABLE Test (id int, PRIMARY KEY(id))");
      string[] restrictions = new string[5];
      restrictions[2] = "Test";
      restrictions[1] = Connection.Database;
      DataTable dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[0]["COLUMN_NAME"], Is.EqualTo("id"));

      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id int, id1 int, id2 int, " +
        "INDEX key1 (id1, id2))");
      restrictions[2] = "Test";
      restrictions[1] = Connection.Database;
      restrictions[4] = "id2";
      dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[0]["COLUMN_NAME"], Is.EqualTo("id2"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(2));

      restrictions[3] = "key1";
      dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[0]["COLUMN_NAME"], Is.EqualTo("id2"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(2));

      restrictions = new string[3];
      restrictions[1] = Connection.Database;
      restrictions[2] = "Test";
      dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(2));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[0]["COLUMN_NAME"], Is.EqualTo("id1"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["TABLE_NAME"].ToString(), Is.EqualTo("test").IgnoreCase);
      Assert.That(dt.Rows[1]["COLUMN_NAME"], Is.EqualTo("id2"));
      Assert.That(dt.Rows[1]["ORDINAL_POSITION"], Is.EqualTo(2));

      restrictions = new string[4];
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id int primary key, id1 int, KEY key1 (id1))");
      restrictions[2] = "Test";
      restrictions[1] = Connection.Database;
      restrictions[3] = "PRIMARY";
      dt = Connection.GetSchema("IndexColumns", restrictions);
    }

    [Test]
    public void Views()
    {
      ExecuteSQL("DROP VIEW IF EXISTS vw");
      ExecuteSQL("CREATE VIEW vw AS SELECT Now() as theTime");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "vw";
      DataTable dt = Connection.GetSchema("Views", restrictions);
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("Views"));
      Assert.That(dt.Rows[0]["TABLE_NAME"], Is.EqualTo("vw"));
    }

    [Test]
    public void ViewColumns()
    {
      ExecuteSQL("DROP VIEW IF EXISTS vw");
      ExecuteSQL("CREATE VIEW vw AS SELECT Now() as theTime");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "vw";
      DataTable dt = Connection.GetSchema("ViewColumns", restrictions);
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("ViewColumns"));
      Assert.That(dt.Rows[0]["VIEW_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[0]["VIEW_NAME"], Is.EqualTo("vw"));
      Assert.That(dt.Rows[0]["COLUMN_NAME"], Is.EqualTo("theTime"));
    }

    [Test]
    public void SingleForeignKey()
    {
      ExecuteSQL("DROP TABLE IF EXISTS child");
      ExecuteSQL("DROP TABLE IF EXISTS parent");
      ExecuteSQL("CREATE TABLE parent (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      ExecuteSQL("CREATE TABLE child (id INT, parent_id INT, INDEX par_ind (parent_id), " +
        "CONSTRAINT c1 FOREIGN KEY (parent_id) REFERENCES parent(id) ON DELETE CASCADE) ENGINE=INNODB");
      string[] restrictions = new string[4];
      restrictions[0] = null;
      restrictions[1] = Connection.Database;
      restrictions[2] = "child";
      DataTable dt = Connection.GetSchema("Foreign Keys", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      DataRow row = dt.Rows[0];
      Assert.That(row[1].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[2], Is.EqualTo("c1"));
      Assert.That(row[4].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[5], Is.EqualTo("child"));
      Assert.That(row[10].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[11], Is.EqualTo("parent"));
    }

    /// <summary>
    /// Bug #26660 MySqlConnection.GetSchema fails with NullReferenceException for Foreign Keys 
    /// </summary>
    [Test]
    public void ForeignKeys()
    {
      Assume.That(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows));

      ExecuteSQL("DROP TABLE IF EXISTS product_order");
      ExecuteSQL("DROP TABLE IF EXISTS customer");
      ExecuteSQL("DROP TABLE IF EXISTS product");

      ExecuteSQL("CREATE TABLE product (category INT NOT NULL, id INT NOT NULL, " +
            "price DECIMAL, PRIMARY KEY(category, id)) ENGINE=INNODB");
      ExecuteSQL("CREATE TABLE customer (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      ExecuteSQL("CREATE TABLE product_order (no INT NOT NULL AUTO_INCREMENT, " +
        "product_category INT NOT NULL, product_id INT NOT NULL, customer_id INT NOT NULL, " +
        "PRIMARY KEY(no), INDEX (product_category, product_id), " +
        "FOREIGN KEY (product_category, product_id) REFERENCES product(category, id) " +
        "ON UPDATE CASCADE ON DELETE RESTRICT, INDEX (customer_id), " +
        "FOREIGN KEY (customer_id) REFERENCES customer(id)) ENGINE=INNODB");

      DataTable dt = Connection.GetSchema("Foreign Keys");
      Assert.That(dt.Columns.Contains("REFERENCED_TABLE_CATALOG"));
    }

    [Test]
    public void MultiSingleForeignKey()
    {
      ExecuteSQL("DROP TABLE IF EXISTS product_order");
      ExecuteSQL("DROP TABLE IF EXISTS customer");
      ExecuteSQL("DROP TABLE IF EXISTS product");

      ExecuteSQL("CREATE TABLE product (category INT NOT NULL, id INT NOT NULL, " +
            "price DECIMAL, PRIMARY KEY(category, id)) ENGINE=INNODB");
      ExecuteSQL("CREATE TABLE customer (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      ExecuteSQL("CREATE TABLE product_order (no INT NOT NULL AUTO_INCREMENT, " +
        "product_category INT NOT NULL, product_id INT NOT NULL, customer_id INT NOT NULL, " +
        "PRIMARY KEY(no), INDEX (product_category, product_id), " +
        "FOREIGN KEY (product_category, product_id) REFERENCES product(category, id) " +
        "ON UPDATE CASCADE ON DELETE RESTRICT, INDEX (customer_id), " +
        "FOREIGN KEY (customer_id) REFERENCES customer(id)) ENGINE=INNODB");

      string[] restrictions = new string[4];
      restrictions[0] = null;
      restrictions[1] = Connection.Database;
      restrictions[2] = "product_order";
      DataTable dt = Connection.GetSchema("Foreign Keys", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(2));
      DataRow row = dt.Rows[0];
      Assert.That(row[1].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[2], Is.EqualTo("product_order_ibfk_1"));
      Assert.That(row[4].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[5], Is.EqualTo("product_order"));
      Assert.That(row[10].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[11], Is.EqualTo("product"));

      row = dt.Rows[1];
      Assert.That(row[1].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[2], Is.EqualTo("product_order_ibfk_2"));
      Assert.That(row[4].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[5], Is.EqualTo("product_order"));
      Assert.That(row[10].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(row[11], Is.EqualTo("customer"));
    }

    [Test]
    public void Triggers()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test1");
      ExecuteSQL("CREATE TABLE test1 (id int)");
      ExecuteSQL("CREATE TABLE test2 (count int)");
      ExecuteSQL("INSERT INTO test2 VALUES (0)");
      string sql = String.Format("CREATE TRIGGER `{0}`.trigger1 AFTER INSERT ON test1 FOR EACH ROW BEGIN " +
        "UPDATE test2 SET count = count+1; END", Connection.Database);
      ExecuteSQL(sql);

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "test1";
      DataTable dt = Connection.GetSchema("Triggers", restrictions);
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("Triggers"));
      Assert.That(dt.Rows[0]["TRIGGER_NAME"], Is.EqualTo("trigger1"));
      Assert.That(dt.Rows[0]["EVENT_MANIPULATION"], Is.EqualTo("INSERT"));
      Assert.That(dt.Rows[0]["EVENT_OBJECT_TABLE"], Is.EqualTo("test1"));
      Assert.That(dt.Rows[0]["ACTION_ORIENTATION"], Is.EqualTo("ROW"));
      Assert.That(dt.Rows[0]["ACTION_TIMING"], Is.EqualTo("AFTER"));
    }

    [Test]
    public void UsingQuotedRestrictions()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test1");
      ExecuteSQL("CREATE TABLE test1 (id int)");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "`test1`";
      DataTable dt = Connection.GetSchema("Tables", restrictions);
      Assert.That(dt.Rows.Count == 1);
      Assert.That(dt.TableName, Is.EqualTo("Tables"));
      Assert.That(dt.Rows[0][2], Is.EqualTo("test1"));
      Assert.That(restrictions[2], Is.EqualTo("`test1`"));
    }

    [Test]
    public void ReservedWords()
    {
      DataTable dt = Connection.GetSchema("ReservedWords");
      foreach (DataRow row in dt.Rows)
        Assert.That(!String.IsNullOrEmpty(row[0] as string));
      Assert.That(dt.Rows.Count, Is.EqualTo(235)); // number of keywords: 235
    }

    [Test]
    public void GetSchemaCollections()
    {
      ExecuteSQL("CREATE TABLE parent (id int, name_parent VARCHAR(20), PRIMARY KEY(id))");
      ExecuteSQL(@"CREATE TABLE child (id int, name_child VARCHAR(20), parent_id INT, 
        PRIMARY KEY(id), INDEX par_id (parent_id), FOREIGN KEY (parent_id) REFERENCES parent(id) ON DELETE CASCADE)");
      ExecuteSQL("INSERT INTO parent VALUES(1, 'parent_1')");
      ExecuteSQL("INSERT INTO child VALUES(1, 'child_1', 1)");

      SchemaProvider schema = new SchemaProvider(Connection);
      string[] restrictions = new string[5];
      restrictions[2] = "parent";
      restrictions[1] = Connection.Database;

      MySqlSchemaCollection schemaCollection = schema.GetSchema("columns", restrictions);

      Assert.That(schemaCollection.Columns.Count == 20);
      Assert.That(schemaCollection.Rows.Count == 2);
      Assert.That(schemaCollection.Rows[0]["TABLE_NAME"], Is.EqualTo("parent"));
      Assert.That(schemaCollection.Rows[0]["COLUMN_NAME"], Is.EqualTo("id"));

      schemaCollection = schema.GetForeignKeysAsync(restrictions, false).GetAwaiter().GetResult();
      Assert.That(schemaCollection.AsDataTable().Columns.Contains("REFERENCED_TABLE_NAME"));

      schemaCollection = schema.GetForeignKeyColumnsAsync(restrictions, false).GetAwaiter().GetResult();
      Assert.That(schemaCollection.AsDataTable().Columns.Contains("REFERENCED_COLUMN_NAME"));

      schemaCollection = schema.GetUDFAsync(restrictions, false).GetAwaiter().GetResult();
      Assert.That(schemaCollection.AsDataTable().Columns.Contains("RETURN_TYPE"));

      schemaCollection = schema.GetUsersAsync(restrictions, false).GetAwaiter().GetResult();
      Assert.That(schemaCollection.AsDataTable().Columns.Contains("USERNAME"));

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        var table = conn.GetSchema();
        foreach (DataRow row in table.Rows)
          foreach (DataColumn col in table.Columns)
          {
            Assert.That(col.ColumnName, Is.Not.Null);
            Assert.That(row[col], Is.Not.Null);
          }
      }

    }

    /// <summary> 
    /// Bug #26876582 Unexpected ColumnSize for Char(36) and Blob in GetSchemaTable. 
    /// Setting OldGuids to True so CHAR(36) is treated as CHAR.
    /// </summary>
    [Test]
    public void ColumnSizeWithOldGuids()
    {
      string connString = Connection.ConnectionString;

      using (MySqlConnection conn = new MySqlConnection(connString + ";oldguids=True;"))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand("CREATE TABLE Test(char36 char(36) CHARSET utf8mb4, binary16 binary(16), char37 char(37), `tinyblob` tinyblob, `blob` blob);", conn);
        cmd.ExecuteNonQuery();

        using (MySqlDataReader reader = ExecuteReader("SELECT * FROM Test;"))
        {
          DataTable schemaTable = reader.GetSchemaTable();

          Assert.That(schemaTable.Rows[0]["ColumnSize"], Is.EqualTo(36));
          Assert.That(schemaTable.Rows[1]["ColumnSize"], Is.EqualTo(16));
          Assert.That(schemaTable.Rows[2]["ColumnSize"], Is.EqualTo(37));
          Assert.That(schemaTable.Rows[3]["ColumnSize"], Is.EqualTo(255));
          Assert.That(schemaTable.Rows[4]["ColumnSize"], Is.EqualTo(65535));
        }
      }
    }

    /// <summary> 
    /// Bug #26876592 Unexpected ColumnSize, IsLong in GetSChemaTable for LongText and LongBlob column.
    /// Added validation when ColumnLenght equals -1 that is when lenght exceeds Int max size.
    /// </summary>
    [Test]
    public void IsLongProperty()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");
      Assume.That(Version >= new Version(5, 7, 6), "This test is for MySql 5.7.6 or higher");
      ExecuteSQL("Drop table if exists datatypes1");
      ExecuteSQL("create table datatypes1(`longtext` longtext,`longblob` longblob)");
      ExecuteSQL("insert into datatypes1 values('test', _binary'test')");
      using (var cmd = Connection.CreateCommand())
      {
        cmd.CommandText = "SELECT * FROM datatypes1;";
        using (var reader = cmd.ExecuteReader())
        {
          var schemaTable = reader.GetSchemaTable();
          Assert.That(schemaTable.Rows[0]["ColumnSize"].ToString(), Is.EqualTo("-1"), "Matching the Column Size");
          Assert.That(schemaTable.Rows[0]["IsLong"].ToString(), Is.EqualTo("True"), "Matching the Column Size");
          Assert.That(schemaTable.Rows[1]["ColumnSize"].ToString(), Is.EqualTo("-1"), "Matching the Column Size");
          Assert.That(schemaTable.Rows[1]["IsLong"].ToString(), Is.EqualTo("True"), "Matching the Column Size");
        }
      }
    }

    /// <summary> 
    /// Bug #26954812 Decimal with numericScale of 0 has wrong numericPrecision in GetSchemaTable.
    /// </summary>
    [Test]
    public void NumericPrecisionProperty()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");
      Assume.That(Version >= new Version(5, 7, 6), "This test is for MySql 5.7.6 or higher");
      ExecuteSQL("Drop table if exists datatypes2");
      ExecuteSQL("create table datatypes2(decimal0 decimal(8, 0))");
      using (var cmd = Connection.CreateCommand())
      {
        cmd.CommandText = "SELECT * FROM datatypes2;";
        using (var reader = cmd.ExecuteReader())
        {
          var schemaTable = reader.GetSchemaTable();
          Assert.That(schemaTable.Rows[0]["NumericPrecision"], Is.EqualTo(8));
          Assert.That(schemaTable.Rows[0]["NumericScale"], Is.EqualTo(0));
        }
      }
    }

    /// <summary>
    /// Bug #29536344 MYSQLCONNECTION.GETSCHEMA RETURNS WRONG ORDER OF RECORDS FOR COLUMNS INFORMATION
    /// </summary>
    [Test]
    public void GetSchemaReturnColumnsByOrdinalPosition()
    {
      ExecuteSQL("CREATE TABLE foo (x int, a VARCHAR(20), z int, c int)");
      ExecuteSQL("INSERT INTO foo VALUES(1, 'columnName', 2, 3)");

      string[] restrictions = new string[5];
      restrictions[2] = "foo";
      restrictions[1] = Connection.Database;
      var rows = Connection.GetSchema("COLUMNS", restrictions).Rows.OfType<DataRow>().ToList();
      string[] expected = new string[] { "x", "a", "z", "c" };

      for (int i = 0; i < rows.Count; i++)
        Assert.That(expected[i], Is.EqualTo(rows[i]["COLUMN_NAME"]));
    }

    [Test, Description("Test to verify different variations in Generated Coloumns")]
    public void GeneratedColumnsVariations()
    {
      Assume.That(Version >= new Version(5, 7, 0), "This test is for MySql 5.7 or higher");

      using (var conn = new MySqlConnection(Settings.ConnectionString))
      {
        conn.Open();
        var cmd = new MySqlCommand(
          @"create table Test(c1 int, 
          c2 double GENERATED ALWAYS AS(c1 * 101 / 102) Stored COMMENT 'First Gen Col', 
          c3 bigint GENERATED ALWAYS as (c1*10000) VIRTUAL UNIQUE KEY Comment '3rd Col' NOT NULL)", conn);
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("insert into Test(c1) values(1000)", conn);
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("select * from Test", conn);
        cmd.ExecuteNonQuery();

        using (var reader = cmd.ExecuteReader())
        {
          Assert.That(reader.Read(), "Matching the values");
          Assert.That(reader.GetInt32(0).Equals(1000), "Matching the values");
          Assert.That(reader.GetDouble(1).Equals(990.196078431), "Matching the values");
          Assert.That(reader.GetInt64(2).Equals(10000000), "Matching the values");
        }

        var dt = conn.GetSchema("Columns", new[] { null, null, "Test", null });
        Assert.That(dt.Rows.Count, Is.EqualTo(3), "Matching the values");
        Assert.That(dt.TableName, Is.EqualTo("Columns"), "Matching the values");
        Assert.That(dt.Rows[0]["DATA_TYPE"].ToString(), Is.EqualTo("int"), "Matching the values");
        Assert.That(dt.Rows[1]["DATA_TYPE"].ToString(), Is.EqualTo("double"), "Matching the values");
        Assert.That(dt.Rows[2]["DATA_TYPE"].ToString(), Is.EqualTo("bigint"), "Matching the values");
        Assert.That(dt.Rows[0]["GENERATION_EXPRESSION"].ToString(), Is.EqualTo(""), "Matching the values");
        Assert.That(dt.Rows[0]["EXTRA"].ToString(), Is.EqualTo(""), "Matching the values");
        Assert.That(dt.Rows[1]["EXTRA"].ToString(), Is.EqualTo("STORED GENERATED"), "Matching the values");
        Assert.That(dt.Rows[2]["EXTRA"].ToString(), Is.EqualTo("VIRTUAL GENERATED"), "Matching the values");
      }
    }

    /// <summary>
    /// Bug #20266825 - SQLNULLVALUEEXCEPTION THROWN WHEN CALLING MYSQLCONNECTION::GETSCHEMA
    /// Changed how "COLLATION" column value was retrieved
    /// </summary>
    [Test]
    public void GetIndexColumnsWithFullTextIndex()
    {
      string indexName = "idxTest";
      ExecuteSQL($"CREATE TABLE Test (id INT, name VARCHAR(20), FULLTEXT {indexName}(name))");

      if (Version >= new Version(8, 0))
      {
        string cmdText = $"SELECT name, index_id, table_id, space from INFORMATION_SCHEMA.INNODB_INDEXES WHERE name = '{indexName}'";
        MySqlCommand cmd = new MySqlCommand(cmdText, Connection);
        Assert.That(cmd.ExecuteScalar().ToString(), Is.EqualTo(indexName).IgnoreCase);
      }

      var indexColumns = Connection.GetSchema("IndexColumns");
      var row = indexColumns.Select("TABLE_NAME = 'Test'");
      Assert.That(row[0]["INDEX_NAME"].ToString(), Is.EqualTo(indexName).IgnoreCase);
    }
  }
}
