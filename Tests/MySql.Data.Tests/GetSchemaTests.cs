// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Globalization;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class GetSchemaTests : BaseTest
  {
    [Test]
    public void Collections()
    {
      DataTable dt = conn.GetSchema();

      int row = 0;
      Assert.AreEqual("MetaDataCollections", dt.Rows[row++][0]);
      Assert.AreEqual("DataSourceInformation", dt.Rows[row++][0]);
      Assert.AreEqual("DataTypes", dt.Rows[row++][0]);
      Assert.AreEqual("Restrictions", dt.Rows[row++][0]);
      Assert.AreEqual("ReservedWords", dt.Rows[row++][0]);
      Assert.AreEqual("Databases", dt.Rows[row++][0]);
      Assert.AreEqual("Tables", dt.Rows[row++][0]);
      Assert.AreEqual("Columns", dt.Rows[row++][0]);
      Assert.AreEqual("Users", dt.Rows[row++][0]);
      Assert.AreEqual("Foreign Keys", dt.Rows[row++][0]);
      Assert.AreEqual("IndexColumns", dt.Rows[row++][0]);
      Assert.AreEqual("Indexes", dt.Rows[row++][0]);
      Assert.AreEqual("Foreign Key Columns", dt.Rows[row++][0]);
      Assert.AreEqual("UDF", dt.Rows[row++][0]);
      Assert.AreEqual("Views", dt.Rows[row++][0]);
      Assert.AreEqual("ViewColumns", dt.Rows[row++][0]);
      Assert.AreEqual("Procedure Parameters", dt.Rows[row++][0]);
      Assert.AreEqual("Procedures", dt.Rows[row++][0]);
      Assert.AreEqual("Triggers", dt.Rows[row++][0]);
    }

    /// <summary>
    /// Bug #25907 DataType Column of DataTypes collection does'nt contain the correct CLR Datatype 
    /// Bug #25947 CreateFormat/CreateParameters Column of DataTypes collection incorrect for CHAR 
    /// </summary>
    [Test]
    public void DataTypes()
    {
      DataTable dt = conn.GetSchema("DataTypes", new string[] { });

      foreach (DataRow row in dt.Rows)
      {
        string type = row["TYPENAME"].ToString();
        Type systemType = Type.GetType(row["DATATYPE"].ToString());
        if (type == "BIT")
          Assert.AreEqual(typeof(System.UInt64), systemType);
        else if (type == "DATE" || type == "DATETIME" ||
          type == "TIMESTAMP")
          Assert.AreEqual(typeof(System.DateTime), systemType);
        else if (type == "BLOB" || type == "TINYBLOB" ||
             type == "MEDIUMBLOB" || type == "LONGBLOB")
          Assert.AreEqual(typeof(System.Byte[]), systemType);
        else if (type == "TIME")
          Assert.AreEqual(typeof(System.TimeSpan), systemType);
        else if (type == "CHAR" || type == "VARCHAR")
        {
          Assert.AreEqual(typeof(System.String), systemType);
          Assert.IsFalse(Convert.ToBoolean(row["IsFixedLength"]));
          string format = type + "({0})";
          Assert.AreEqual(format, row["CreateFormat"].ToString());
        }
        else if (type == "SET" || type == "ENUM")
          Assert.AreEqual(typeof(System.String), systemType);
        else if (type == "DOUBLE")
          Assert.AreEqual(typeof(System.Double), systemType);
        else if (type == "SINGLE")
          Assert.AreEqual(typeof(System.Single), systemType);
        else if (type == "TINYINT")
        {
          if (row["CREATEFORMAT"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.AreEqual(typeof(System.Byte), systemType);
          else
            Assert.AreEqual(typeof(System.SByte), systemType);
        }
        else if (type == "SMALLINT")
        {
          if (row["CREATEFORMAT"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.AreEqual(typeof(System.UInt16), systemType);
          else
            Assert.AreEqual(typeof(System.Int16), systemType);
        }
        else if (type == "MEDIUMINT" || type == "INT")
        {
          if (row["CREATEFORMAT"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.AreEqual(typeof(System.UInt32), systemType);
          else
            Assert.AreEqual(typeof(System.Int32), systemType);
        }
        else if (type == "BIGINT")
        {
          if (row["CREATEFORMAT"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.AreEqual(typeof(System.UInt64), systemType);
          else
            Assert.AreEqual(typeof(System.Int64), systemType);
        }
        else if (type == "DECIMAL")
        {
          Assert.AreEqual(typeof(System.Decimal), systemType);
          Assert.AreEqual("DECIMAL({0},{1})", row["CreateFormat"].ToString());
        }
        else if (type == "TINYINT")
          Assert.AreEqual(typeof(System.Byte), systemType);
      }
    }

    [Test]
    public void Databases()
    {
      DataTable dt = conn.GetSchema("Databases");
      Assert.AreEqual("Databases", dt.TableName);

      bool foundZero = false;
      bool foundOne = false;
      foreach (DataRow row in dt.Rows)
      {
        string dbName = row[1].ToString().ToLower();
        if (dbName == database0.ToLower())
          foundZero = true;
        else if (dbName == database1.ToLower())
          foundOne = true;
      }
      Assert.IsTrue(foundZero);
      Assert.IsTrue(foundOne);

      dt = conn.GetSchema("Databases", new string[1] { database0 });
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual(database0.ToLower(), dt.Rows[0][1].ToString().ToLower());
    }

    [Test]
    public void Tables()
    {
      execSQL("CREATE TABLE test1 (id int)");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "test1";
      DataTable dt = conn.GetSchema("Tables", restrictions);
      if (Version.Major >= 5 && Version.Minor >= 1)
      {
        Assert.IsTrue(dt.Columns["VERSION"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["TABLE_ROWS"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["AVG_ROW_LENGTH"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["DATA_LENGTH"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["MAX_DATA_LENGTH"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["INDEX_LENGTH"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["DATA_FREE"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["AUTO_INCREMENT"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["CHECKSUM"].DataType == typeof(UInt64));
      }
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("Tables", dt.TableName);
      Assert.AreEqual("test1", dt.Rows[0][2]);
    }

    [Test]
    public void Columns()
    {
      execSQL(@"CREATE TABLE test (col1 int, col2 decimal(20,5), 
				col3 varchar(50) character set utf8, col4 tinyint unsigned, 
				col5 varchar(20) default 'boo')");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "test";
      DataTable dt = conn.GetSchema("Columns", restrictions);
      Assert.AreEqual(5, dt.Rows.Count);
      Assert.AreEqual("Columns", dt.TableName);
      if (Version.Major >= 5 && Version.Minor >= 1)
      {
        Assert.IsTrue(dt.Columns["ORDINAL_POSITION"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["CHARACTER_MAXIMUM_LENGTH"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["NUMERIC_PRECISION"].DataType == typeof(UInt64));
        Assert.IsTrue(dt.Columns["NUMERIC_SCALE"].DataType == typeof(UInt64));
      }

      // first column
      Assert.AreEqual(database0.ToUpper(), dt.Rows[0]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.AreEqual("COL1", dt.Rows[0]["COLUMN_NAME"].ToString().ToUpper());
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("YES", dt.Rows[0]["IS_NULLABLE"]);
      Assert.AreEqual("INT", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());

      // second column
      Assert.AreEqual(database0.ToUpper(), dt.Rows[1]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.AreEqual("COL2", dt.Rows[1]["COLUMN_NAME"].ToString().ToUpper());
      Assert.AreEqual(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.AreEqual("YES", dt.Rows[1]["IS_NULLABLE"]);
      Assert.AreEqual("DECIMAL", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual("DECIMAL(20,5)", dt.Rows[1]["COLUMN_TYPE"].ToString().ToUpper());
      Assert.AreEqual(20, dt.Rows[1]["NUMERIC_PRECISION"]);
      Assert.AreEqual(5, dt.Rows[1]["NUMERIC_SCALE"]);

      // third column
      Assert.AreEqual(database0.ToUpper(), dt.Rows[2]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.AreEqual("COL3", dt.Rows[2]["COLUMN_NAME"].ToString().ToUpper());
      Assert.AreEqual(3, dt.Rows[2]["ORDINAL_POSITION"]);
      Assert.AreEqual("YES", dt.Rows[2]["IS_NULLABLE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[2]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual("VARCHAR(50)", dt.Rows[2]["COLUMN_TYPE"].ToString().ToUpper());

      // fourth column
      Assert.AreEqual(database0.ToUpper(), dt.Rows[3]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.AreEqual("COL4", dt.Rows[3]["COLUMN_NAME"].ToString().ToUpper());
      Assert.AreEqual(4, dt.Rows[3]["ORDINAL_POSITION"]);
      Assert.AreEqual("YES", dt.Rows[3]["IS_NULLABLE"]);
      Assert.AreEqual("TINYINT", dt.Rows[3]["DATA_TYPE"].ToString().ToUpper());

      // fifth column
      Assert.AreEqual(database0.ToUpper(), dt.Rows[4]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.AreEqual("COL5", dt.Rows[4]["COLUMN_NAME"].ToString().ToUpper());
      Assert.AreEqual(5, dt.Rows[4]["ORDINAL_POSITION"]);
      Assert.AreEqual("YES", dt.Rows[4]["IS_NULLABLE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[4]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual("VARCHAR(20)", dt.Rows[4]["COLUMN_TYPE"].ToString().ToUpper());
      Assert.AreEqual("BOO", dt.Rows[4]["COLUMN_DEFAULT"].ToString().ToUpper());
    }

    /// <summary> 
    /// Bug #46270 connection.GetSchema("Columns") fails on MySQL 4.1  
    /// </summary> 
    [Test]
    public void EnumAndSetColumns()
    {
      execSQL("DROP TABLE IF EXISTS test");
      execSQL("CREATE TABLE test (col1 set('A','B','C'), col2 enum('A','B','C'))");

      DataTable dt = conn.GetSchema("Columns", new string[] { null, null, "test", null });
      Assert.AreEqual(2, dt.Rows.Count);
      Assert.AreEqual("set", dt.Rows[0]["DATA_TYPE"]);
      Assert.AreEqual("enum", dt.Rows[1]["DATA_TYPE"]);
      Assert.AreEqual("set('A','B','C')", dt.Rows[0]["COLUMN_TYPE"]);
      Assert.AreEqual("enum('A','B','C')", dt.Rows[1]["COLUMN_TYPE"]);
    }

    [Test]
    public void Procedures()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("DROP PROCEDURE IF EXISTS spTest");
      execSQL("CREATE PROCEDURE spTest (id int) BEGIN SELECT 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "spTest";
      DataTable dt = conn.GetSchema("Procedures", restrictions);
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("Procedures", dt.TableName);
      Assert.AreEqual("spTest", dt.Rows[0][3]);
    }

    [Test]
    public void Functions()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("DROP FUNCTION IF EXISTS spFunc");
      execSQL("CREATE FUNCTION spFunc (id int) RETURNS INT BEGIN RETURN 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "spFunc";
      DataTable dt = conn.GetSchema("Procedures", restrictions);
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("Procedures", dt.TableName);
      Assert.AreEqual("spFunc", dt.Rows[0][3]);
    }

    [Test]
    public void Indexes()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE TABLE test (id int, PRIMARY KEY(id))");
      string[] restrictions = new string[4];
      restrictions[2] = "test";
      restrictions[1] = database0;
      DataTable dt = conn.GetSchema("Indexes", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual(true, dt.Rows[0]["PRIMARY"]);
      Assert.AreEqual(true, dt.Rows[0]["UNIQUE"]);

      execSQL("DROP TABLE IF EXISTS test");
      execSQL("CREATE TABLE test (id int, name varchar(50), " +
        "UNIQUE KEY key2 (name))");

      dt = conn.GetSchema("Indexes", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("key2", dt.Rows[0]["INDEX_NAME"]);
      Assert.AreEqual(false, dt.Rows[0]["PRIMARY"]);
      Assert.AreEqual(true, dt.Rows[0]["UNIQUE"]);

      restrictions[3] = "key2";
      dt = conn.GetSchema("Indexes", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("key2", dt.Rows[0]["INDEX_NAME"]);
      Assert.AreEqual(false, dt.Rows[0]["PRIMARY"]);
      Assert.AreEqual(true, dt.Rows[0]["UNIQUE"]);

      /// <summary> 
      /// Bug #48101	MySqlConnection.GetSchema on "Indexes" throws when there's a table named "b`a`d" 
      /// </summary> 
      execSQL("DROP TABLE IF EXISTS test");
      execSQL(@"CREATE TABLE `te``s``t` (id int, name varchar(50), " +
        "KEY key2 (name))");

      restrictions[2] = "te`s`t";
      dt = conn.GetSchema("Indexes", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("te`s`t", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("key2", dt.Rows[0]["INDEX_NAME"]);
      Assert.AreEqual(false, dt.Rows[0]["PRIMARY"]);
      Assert.AreEqual(false, dt.Rows[0]["UNIQUE"]);
    }

    [Test]
    public void IndexColumns()
    {
      execSQL("CREATE TABLE test (id int, PRIMARY KEY(id))");
      string[] restrictions = new string[5];
      restrictions[2] = "test";
      restrictions[1] = database0;
      DataTable dt = conn.GetSchema("IndexColumns", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("id", dt.Rows[0]["COLUMN_NAME"]);

      execSQL("DROP TABLE IF EXISTS test");
      execSQL("CREATE TABLE test (id int, id1 int, id2 int, " +
        "INDEX key1 (id1, id2))");
      restrictions[2] = "test";
      restrictions[1] = database0;
      restrictions[4] = "id2";
      dt = conn.GetSchema("IndexColumns", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("id2", dt.Rows[0]["COLUMN_NAME"]);
      Assert.AreEqual(2, dt.Rows[0]["ORDINAL_POSITION"]);

      restrictions[3] = "key1";
      dt = conn.GetSchema("IndexColumns", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("id2", dt.Rows[0]["COLUMN_NAME"]);
      Assert.AreEqual(2, dt.Rows[0]["ORDINAL_POSITION"]);

      restrictions = new string[3];
      restrictions[1] = database0;
      restrictions[2] = "test";
      dt = conn.GetSchema("IndexColumns", restrictions);
      Assert.AreEqual(2, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.AreEqual("id1", dt.Rows[0]["COLUMN_NAME"]);
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("test", dt.Rows[1]["TABLE_NAME"]);
      Assert.AreEqual("id2", dt.Rows[1]["COLUMN_NAME"]);
      Assert.AreEqual(2, dt.Rows[1]["ORDINAL_POSITION"]);

      restrictions = new string[4];
      execSQL("DROP TABLE IF EXISTS test");
      execSQL("CREATE TABLE test (id int primary key, id1 int, KEY key1 (id1))");
      restrictions[2] = "test";
      restrictions[1] = database0;
      restrictions[3] = "PRIMARY";
      dt = conn.GetSchema("IndexColumns", restrictions);
    }

    [Test]
    public void Views()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("DROP VIEW IF EXISTS vw");
      execSQL("CREATE VIEW vw AS SELECT Now() as theTime");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "vw";
      DataTable dt = conn.GetSchema("Views", restrictions);
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("Views", dt.TableName);
      Assert.AreEqual("vw", dt.Rows[0]["TABLE_NAME"]);
    }

    [Test]
    public void ViewColumns()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("DROP VIEW IF EXISTS vw");
      execSQL("CREATE VIEW vw AS SELECT Now() as theTime");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "vw";
      DataTable dt = conn.GetSchema("ViewColumns", restrictions);
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("ViewColumns", dt.TableName);
      Assert.AreEqual(database0.ToLower(), dt.Rows[0]["VIEW_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("vw", dt.Rows[0]["VIEW_NAME"]);
      Assert.AreEqual("theTime", dt.Rows[0]["COLUMN_NAME"]);
    }

    [Test]
    public void SingleForeignKey()
    {
      execSQL("CREATE TABLE parent (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      execSQL("CREATE TABLE child (id INT, parent_id INT, INDEX par_ind (parent_id), " +
        "CONSTRAINT c1 FOREIGN KEY (parent_id) REFERENCES parent(id) ON DELETE CASCADE) ENGINE=INNODB");
      string[] restrictions = new string[4];
      restrictions[0] = null;
      restrictions[1] = database0;
      restrictions[2] = "child";
      DataTable dt = conn.GetSchema("Foreign Keys", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      DataRow row = dt.Rows[0];
      Assert.AreEqual(database0.ToLower(), row["CONSTRAINT_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("c1", row["CONSTRAINT_NAME"]);
      Assert.AreEqual(database0.ToLower(), row["TABLE_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("child", row["TABLE_NAME"]);
      Assert.AreEqual(database0.ToLower(), row["REFERENCED_TABLE_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("parent", row["REFERENCED_TABLE_NAME"]);
    }

    /// <summary>
    /// Bug #26660 MySqlConnection.GetSchema fails with NullReferenceException for Foreign Keys 
    /// </summary>
    [Test]
    public void ForeignKeys()
    {
      execSQL("CREATE TABLE product (category INT NOT NULL, id INT NOT NULL, " +
            "price DECIMAL, PRIMARY KEY(category, id)) ENGINE=INNODB");
      execSQL("CREATE TABLE customer (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      execSQL("CREATE TABLE product_order (no INT NOT NULL AUTO_INCREMENT, " +
        "product_category INT NOT NULL, product_id INT NOT NULL, customer_id INT NOT NULL, " +
        "PRIMARY KEY(no), INDEX (product_category, product_id), " +
        "FOREIGN KEY (product_category, product_id) REFERENCES product(category, id) " +
        "ON UPDATE CASCADE ON DELETE RESTRICT, INDEX (customer_id), " +
        "FOREIGN KEY (customer_id) REFERENCES customer(id)) ENGINE=INNODB");

      DataTable dt = conn.GetSchema("Foreign Keys");
      Assert.IsTrue(dt.Columns.Contains("REFERENCED_TABLE_CATALOG"));
    }

    [Test]
    public void MultiSingleForeignKey()
    {
      execSQL("CREATE TABLE product (category INT NOT NULL, id INT NOT NULL, " +
            "price DECIMAL, PRIMARY KEY(category, id)) ENGINE=INNODB");
      execSQL("CREATE TABLE customer (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      execSQL("CREATE TABLE product_order (no INT NOT NULL AUTO_INCREMENT, " +
        "product_category INT NOT NULL, product_id INT NOT NULL, customer_id INT NOT NULL, " +
        "PRIMARY KEY(no), INDEX (product_category, product_id), " +
        "FOREIGN KEY (product_category, product_id) REFERENCES product(category, id) " +
        "ON UPDATE CASCADE ON DELETE RESTRICT, INDEX (customer_id), " +
        "FOREIGN KEY (customer_id) REFERENCES customer(id)) ENGINE=INNODB");

      string[] restrictions = new string[4];
      restrictions[0] = null;
      restrictions[1] = database0;
      restrictions[2] = "product_order";
      DataTable dt = conn.GetSchema("Foreign Keys", restrictions);
      Assert.AreEqual(2, dt.Rows.Count);
      DataRow row = dt.Rows[0];
      Assert.AreEqual(database0.ToLower(), row["CONSTRAINT_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("product_order_ibfk_1", row["CONSTRAINT_NAME"]);
      Assert.AreEqual(database0.ToLower(), row["TABLE_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("product_order", row["TABLE_NAME"]);
      Assert.AreEqual(database0.ToLower(), row["REFERENCED_TABLE_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("product", row["REFERENCED_TABLE_NAME"]);

      row = dt.Rows[1];
      Assert.AreEqual(database0.ToLower(), row["CONSTRAINT_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("product_order_ibfk_2", row["CONSTRAINT_NAME"]);
      Assert.AreEqual(database0.ToLower(), row["TABLE_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("product_order", row["TABLE_NAME"]);
      Assert.AreEqual(database0.ToLower(), row["REFERENCED_TABLE_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("customer", row["REFERENCED_TABLE_NAME"]);
    }

    [Test]
    public void Triggers()
    {
      if (Version < new Version(5, 1, 6)) return;

      execSQL("CREATE TABLE test1 (id int)");
      execSQL("CREATE TABLE test2 (count int)");
      execSQL("INSERT INTO test2 VALUES (0)");
      string sql = String.Format("CREATE TRIGGER `{0}`.trigger1 AFTER INSERT ON test1 FOR EACH ROW BEGIN " +
        "UPDATE test2 SET count = count+1; END", database0);
      suExecSQL(sql);

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "test1";
      DataTable dt = rootConn.GetSchema("Triggers", restrictions);
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("Triggers", dt.TableName);
      Assert.AreEqual("trigger1", dt.Rows[0]["TRIGGER_NAME"]);
      Assert.AreEqual("INSERT", dt.Rows[0]["EVENT_MANIPULATION"]);
      Assert.AreEqual("test1", dt.Rows[0]["EVENT_OBJECT_TABLE"]);
      Assert.AreEqual("ROW", dt.Rows[0]["ACTION_ORIENTATION"]);
      Assert.AreEqual("AFTER", dt.Rows[0]["ACTION_TIMING"]);
    }

    [Test]
    public void UsingQuotedRestrictions()
    {
      execSQL("CREATE TABLE test1 (id int)");

      string[] restrictions = new string[4];
      restrictions[1] = database0;
      restrictions[2] = "`test1`";
      DataTable dt = conn.GetSchema("Tables", restrictions);
      Assert.IsTrue(dt.Rows.Count == 1);
      Assert.AreEqual("Tables", dt.TableName);
      Assert.AreEqual("test1", dt.Rows[0][2]);
      Assert.AreEqual("`test1`", restrictions[2]);
    }

    [Test]
    public void ReservedWords()
    {
      DataTable dt = conn.GetSchema("ReservedWords");
      foreach (DataRow row in dt.Rows)
        Assert.IsFalse(String.IsNullOrEmpty(row[0] as string));
    }

    /// <summary>
    /// Test fix for bug http://bugs.mysql.com/bug.php?id=67901.
    /// GetSchema was not returning ArgumentException in case of invalid collection name.
    /// This also makes the method compliant with the standard API convetions from System.Data.Common.DbConnection.GetSchema.
    /// </summary>
    [Test]
    public void ArgumentExceptionTest()
    {
      try
      {
        DataTable dt = conn.GetSchema("Table");
        Assert.Fail();
      }
      catch (ArgumentException)
      { 
      }
      catch (Exception)
      {
        Assert.Fail();
      }
    }
  }
}
