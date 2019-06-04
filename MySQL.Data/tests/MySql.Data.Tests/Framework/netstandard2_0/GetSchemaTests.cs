// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
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

using System;
using System.Data;
using System.Linq;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class GetSchemaTests : TestBase
  {
    public GetSchemaTests(TestFixture fixture) : base(fixture)
    {
      //ts.CreateDatabase("1");
    }

    [Fact]
    public void Collections()
    {
      DataTable dt = Connection.GetSchema();

      int row = 0;
      Assert.Equal("MetaDataCollections", dt.Rows[row++][0]);
      Assert.Equal("DataSourceInformation", dt.Rows[row++][0]);
      Assert.Equal("DataTypes", dt.Rows[row++][0]);
      Assert.Equal("Restrictions", dt.Rows[row++][0]);
      Assert.Equal("ReservedWords", dt.Rows[row++][0]);
      Assert.Equal("Databases", dt.Rows[row++][0]);
      Assert.Equal("Tables", dt.Rows[row++][0]);
      Assert.Equal("Columns", dt.Rows[row++][0]);
      Assert.Equal("Users", dt.Rows[row++][0]);
      Assert.Equal("Foreign Keys", dt.Rows[row++][0]);
      Assert.Equal("IndexColumns", dt.Rows[row++][0]);
      Assert.Equal("Indexes", dt.Rows[row++][0]);
      Assert.Equal("Foreign Key Columns", dt.Rows[row++][0]);
      Assert.Equal("UDF", dt.Rows[row++][0]);
      Assert.Equal("Views", dt.Rows[row++][0]);
      Assert.Equal("ViewColumns", dt.Rows[row++][0]);
      Assert.Equal("Procedure Parameters", dt.Rows[row++][0]);
      Assert.Equal("Procedures", dt.Rows[row++][0]);
      Assert.Equal("Triggers", dt.Rows[row++][0]);
    }

    /// <summary>
    /// Bug #25907 DataType Column of DataTypes collection does'nt contain the correct CLR Datatype 
    /// Bug #25947 CreateFormat/CreateParameters Column of DataTypes collection incorrect for CHAR 
    /// </summary>
    [Fact]
    public void DataTypes()
    {
      DataTable dt = Connection.GetSchema("DataTypes", new string[] { });

      foreach (DataRow row in dt.Rows)
      {
        string type = row["TypeName"].ToString();
        Type systemType = Type.GetType(row["DataType"].ToString());
        if (type == "BIT")
          Assert.Equal(typeof(System.UInt64), systemType);
        else if (type == "DATE" || type == "DATETIME" ||
          type == "TIMESTAMP")
          Assert.Equal(typeof(System.DateTime), systemType);
        else if (type == "BLOB" || type == "TINYBLOB" ||
             type == "MEDIUMBLOB" || type == "LONGBLOB")
          Assert.Equal(typeof(System.Byte[]), systemType);
        else if (type == "TIME")
          Assert.Equal(typeof(System.TimeSpan), systemType);
        else if (type == "CHAR" || type == "VARCHAR")
        {
          Assert.Equal(typeof(System.String), systemType);
          Assert.False(Convert.ToBoolean(row["IsFixedLength"]));
          string format = type + "({0})";
          Assert.Equal(format, row["CreateFormat"].ToString());
        }
        else if (type == "SET" || type == "ENUM")
          Assert.Equal(typeof(System.String), systemType);
        else if (type == "DOUBLE")
          Assert.Equal(typeof(System.Double), systemType);
        else if (type == "SINGLE")
          Assert.Equal(typeof(System.Single), systemType);
        else if (type == "TINYINT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(typeof(System.Byte), systemType);
          else
            Assert.Equal(typeof(System.SByte), systemType);
        }
        else if (type == "SMALLINT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(typeof(System.UInt16), systemType);
          else
            Assert.Equal(typeof(System.Int16), systemType);
        }
        else if (type == "MEDIUMINT" || type == "INT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(typeof(System.UInt32), systemType);
          else
            Assert.Equal(typeof(System.Int32), systemType);
        }
        else if (type == "BIGINT")
        {
          if (row["CreateFormat"].ToString().EndsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(typeof(System.UInt64), systemType);
          else
            Assert.Equal(typeof(System.Int64), systemType);
        }
        else if (type == "DECIMAL")
        {
          Assert.Equal(typeof(System.Decimal), systemType);
          Assert.Equal("DECIMAL({0},{1})", row["CreateFormat"].ToString());
        }
        else if (type == "TINYINT")
          Assert.Equal(typeof(System.Byte), systemType);
      }
    }

    [Fact]
    public void Tables()
    {
      executeSQL("DROP TABLE IF EXISTS test1");
      executeSQL("CREATE TABLE test1 (id int)");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "test1";
      DataTable dt = Connection.GetSchema("Tables", restrictions);
      Assert.True(dt.Columns["VERSION"].DataType == typeof(UInt64)
        || dt.Columns["VERSION"].DataType == typeof(Int64));
      Assert.True(dt.Columns["TABLE_ROWS"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["AVG_ROW_LENGTH"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["DATA_LENGTH"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["MAX_DATA_LENGTH"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["INDEX_LENGTH"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["DATA_FREE"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["AUTO_INCREMENT"].DataType == typeof(UInt64));
      Assert.True(dt.Columns["CHECKSUM"].DataType == typeof(UInt64)
        || dt.Columns["CHECKSUM"].DataType == typeof(Int64));
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Tables", dt.TableName);
      Assert.Equal("test1", dt.Rows[0][2]);
    }

    [Fact]
    public void Columns()
    {
      executeSQL(@"CREATE TABLE test (col1 int, col2 decimal(20,5), 
        col3 varchar(50) character set utf8, col4 tinyint unsigned, 
        col5 varchar(20) default 'boo')");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "test";
      DataTable dt = Connection.GetSchema("Columns", restrictions);
      Assert.Equal(5, dt.Rows.Count);
      Assert.Equal("Columns", dt.TableName);
      if (Connection.driver.Version.isAtLeast(8, 0, 1))
      {
        Assert.True(dt.Columns["ORDINAL_POSITION"].DataType == typeof(UInt32));
        Assert.True(dt.Columns["CHARACTER_MAXIMUM_LENGTH"].DataType == typeof(Int64));
        Assert.True(dt.Columns["NUMERIC_PRECISION"].DataType == typeof(UInt64));
        Assert.True(dt.Columns["NUMERIC_SCALE"].DataType == typeof(UInt64));
      }
      else
      {
        Assert.True(dt.Columns["ORDINAL_POSITION"].DataType == typeof(UInt64));
        Assert.True(dt.Columns["CHARACTER_MAXIMUM_LENGTH"].DataType == typeof(UInt64));
        Assert.True(dt.Columns["NUMERIC_PRECISION"].DataType == typeof(UInt64));
        Assert.True(dt.Columns["NUMERIC_SCALE"].DataType == typeof(UInt64));
      }

      // first column
      Assert.Equal((Connection.Database).ToUpper(), dt.Rows[0]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.Equal("COL1", dt.Rows[0]["COLUMN_NAME"].ToString().ToUpper());
      Assert.Equal(1, Convert.ToInt32(dt.Rows[0]["ORDINAL_POSITION"]));
      Assert.Equal("YES", dt.Rows[0]["IS_NULLABLE"]);
      Assert.Equal("INT", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());

      // second column
      Assert.Equal((Connection.Database).ToUpper(), dt.Rows[1]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.Equal("COL2", dt.Rows[1]["COLUMN_NAME"].ToString().ToUpper());
      Assert.Equal(2, Convert.ToInt32(dt.Rows[1]["ORDINAL_POSITION"]));
      Assert.Equal("YES", dt.Rows[1]["IS_NULLABLE"]);
      Assert.Equal("DECIMAL", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("DECIMAL(20,5)", dt.Rows[1]["COLUMN_TYPE"].ToString().ToUpper());
      Assert.Equal(20, Convert.ToInt32(dt.Rows[1]["NUMERIC_PRECISION"]));
      Assert.Equal(5, Convert.ToInt32(dt.Rows[1]["NUMERIC_SCALE"]));

      // third column
      Assert.Equal((Connection.Database).ToUpper(), dt.Rows[2]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.Equal("COL3", dt.Rows[2]["COLUMN_NAME"].ToString().ToUpper());
      Assert.Equal(3, Convert.ToInt32(dt.Rows[2]["ORDINAL_POSITION"]));
      Assert.Equal("YES", dt.Rows[2]["IS_NULLABLE"]);
      Assert.Equal("VARCHAR", dt.Rows[2]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("VARCHAR(50)", dt.Rows[2]["COLUMN_TYPE"].ToString().ToUpper());

      // fourth column
      Assert.Equal((Connection.Database).ToUpper(), dt.Rows[3]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.Equal("COL4", dt.Rows[3]["COLUMN_NAME"].ToString().ToUpper());
      Assert.Equal(4, Convert.ToInt32(dt.Rows[3]["ORDINAL_POSITION"]));
      Assert.Equal("YES", dt.Rows[3]["IS_NULLABLE"]);
      Assert.Equal("TINYINT", dt.Rows[3]["DATA_TYPE"].ToString().ToUpper());

      // fifth column
      Assert.Equal((Connection.Database).ToUpper(), dt.Rows[4]["TABLE_SCHEMA"].ToString().ToUpper());
      Assert.Equal("COL5", dt.Rows[4]["COLUMN_NAME"].ToString().ToUpper());
      Assert.Equal(5, Convert.ToInt32(dt.Rows[4]["ORDINAL_POSITION"]));
      Assert.Equal("YES", dt.Rows[4]["IS_NULLABLE"]);
      Assert.Equal("VARCHAR", dt.Rows[4]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("VARCHAR(20)", dt.Rows[4]["COLUMN_TYPE"].ToString().ToUpper());
      Assert.Equal("BOO", dt.Rows[4]["COLUMN_DEFAULT"].ToString().ToUpper());
    }


    ///<summary>
    ///Testing out schema information about generated columns
    /// only in version 5.7.6 or later    
    ///</summary>
    [Fact(Skip = "Not compatible with linux")]
    public void CanGetSchemaInformationGeneratedColumns()
    {
      if (Fixture.Version < new Version(5, 7, 6)) return;

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE `Test` (`ID` int NOT NULL AUTO_INCREMENT PRIMARY KEY, `Name` char(35) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL)");

      var cmd = new MySqlCommand("ALTER TABLE test ADD COLUMN Name_ci char(35) CHARACTER SET utf8 AS (Name) STORED;", Connection);
      cmd.ExecuteNonQuery();

      DataTable dt = Connection.GetSchema("Columns", new string[] { null, null, "test", null });
      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal("Columns", dt.TableName);
      if (Fixture.Version.Major >= 5 && Fixture.Version.Minor >= 7 && Fixture.Version.Build >= 6)
      {
        Assert.Equal("char", dt.Rows[2]["DATA_TYPE"]);
        Assert.Equal("Name", dt.Rows[2]["GENERATION_EXPRESSION"].ToString().Trim('`'));
        Assert.Equal("STORED GENERATED", dt.Rows[2]["EXTRA"]);
      }
    }


    /// <summary> 
    /// Bug #46270 connection.GetSchema("Columns") fails on MySQL 4.1  
    /// </summary> 
    [Fact]
    public void EnumAndSetColumns()
    {
      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test (col1 set('A','B','C'), col2 enum('A','B','C'))");

      DataTable dt = Connection.GetSchema("Columns", new string[] { null, null, "test", null });
      Assert.Equal(2, dt.Rows.Count);
      Assert.Equal("set", dt.Rows[0]["DATA_TYPE"]);
      Assert.Equal("enum", dt.Rows[1]["DATA_TYPE"]);
      Assert.Equal("set('A','B','C')", dt.Rows[0]["COLUMN_TYPE"]);
      Assert.Equal("enum('A','B','C')", dt.Rows[1]["COLUMN_TYPE"]);
    }

    [Fact]
    public void Procedures()
    {
      executeSQL("DROP PROCEDURE IF EXISTS spTest");
      executeSQL("CREATE PROCEDURE spTest (id int) BEGIN SELECT 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("Procedures", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Procedures", dt.TableName);
      Assert.Equal("spTest", dt.Rows[0][3]);
    }

    [Fact]
    public void ProceduresWithParameters()
    {
      executeSQL("DROP PROCEDURE IF EXISTS spTest");
      executeSQL("CREATE PROCEDURE spTest (id int) BEGIN SELECT 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("PROCEDURES WITH PARAMETERS", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Procedures", dt.TableName);
      Assert.Equal("spTest", dt.Rows[0][3]);
      Assert.Equal("id int", dt.Rows[0][dt.Columns.Count - 1]);
    }

    [Fact]
    public void Functions()
    {
      executeSQL("DROP FUNCTION IF EXISTS spFunc");
      executeSQL("CREATE FUNCTION spFunc (id int) RETURNS INT BEGIN RETURN 1; END");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spFunc";
      DataTable dt = Connection.GetSchema("Procedures", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Procedures", dt.TableName);
      Assert.Equal("spFunc", dt.Rows[0][3]);
    }

    [Fact]
    public void Indexes()
    {
      executeSQL("CREATE TABLE test (id int, PRIMARY KEY(id))");
      string[] restrictions = new string[4];
      restrictions[2] = "test";
      restrictions[1] = Connection.Database;
      DataTable dt = Connection.GetSchema("Indexes", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.True((bool)dt.Rows[0]["PRIMARY"]);
      Assert.True((bool)dt.Rows[0]["UNIQUE"]);

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test (id int, name varchar(50), " +
        "UNIQUE KEY key2 (name))");

      dt = Connection.GetSchema("Indexes", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("key2", dt.Rows[0]["INDEX_NAME"]);
      Assert.False((bool)dt.Rows[0]["PRIMARY"]);
      Assert.True((bool)dt.Rows[0]["UNIQUE"]);

      restrictions[3] = "key2";
      dt = Connection.GetSchema("Indexes", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("key2", dt.Rows[0]["INDEX_NAME"]);
      Assert.False((bool)dt.Rows[0]["PRIMARY"]);
      Assert.True((bool)dt.Rows[0]["UNIQUE"]);

      /// <summary> 
      /// Bug #48101	MySqlConnection.GetSchema on "Indexes" throws when there's a table named "b`a`d" 
      /// </summary> 
      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL(@"CREATE TABLE `te``s``t` (id int, name varchar(50), " +
        "KEY key2 (name))");

      restrictions[2] = "te`s`t";
      dt = Connection.GetSchema("Indexes", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("te`s`t", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("key2", dt.Rows[0]["INDEX_NAME"]);
      Assert.False((bool)dt.Rows[0]["PRIMARY"]);
      Assert.False((bool)dt.Rows[0]["UNIQUE"]);
    }

    [Fact]
    public void IndexColumns()
    {
      executeSQL("CREATE TABLE test (id int, PRIMARY KEY(id))");
      string[] restrictions = new string[5];
      restrictions[2] = "test";
      restrictions[1] = Connection.Database;
      DataTable dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("id", dt.Rows[0]["COLUMN_NAME"]);

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test (id int, id1 int, id2 int, " +
        "INDEX key1 (id1, id2))");
      restrictions[2] = "test";
      restrictions[1] = Connection.Database;
      restrictions[4] = "id2";
      dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("id2", dt.Rows[0]["COLUMN_NAME"]);
      Assert.Equal(2, dt.Rows[0]["ORDINAL_POSITION"]);

      restrictions[3] = "key1";
      dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("id2", dt.Rows[0]["COLUMN_NAME"]);
      Assert.Equal(2, dt.Rows[0]["ORDINAL_POSITION"]);

      restrictions = new string[3];
      restrictions[1] = Connection.Database;
      restrictions[2] = "test";
      dt = Connection.GetSchema("IndexColumns", restrictions);
      Assert.Equal(2, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["TABLE_NAME"]);
      Assert.Equal("id1", dt.Rows[0]["COLUMN_NAME"]);
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("test", dt.Rows[1]["TABLE_NAME"]);
      Assert.Equal("id2", dt.Rows[1]["COLUMN_NAME"]);
      Assert.Equal(2, dt.Rows[1]["ORDINAL_POSITION"]);

      restrictions = new string[4];
      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test (id int primary key, id1 int, KEY key1 (id1))");
      restrictions[2] = "test";
      restrictions[1] = Connection.Database;
      restrictions[3] = "PRIMARY";
      dt = Connection.GetSchema("IndexColumns", restrictions);
    }

    [Fact]
    public void Views()
    {
      executeSQL("DROP VIEW IF EXISTS vw");
      executeSQL("CREATE VIEW vw AS SELECT Now() as theTime");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "vw";
      DataTable dt = Connection.GetSchema("Views", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Views", dt.TableName);
      Assert.Equal("vw", dt.Rows[0]["TABLE_NAME"]);
    }

    [Fact]
    public void ViewColumns()
    {
      executeSQL("DROP VIEW IF EXISTS vw");
      executeSQL("CREATE VIEW vw AS SELECT Now() as theTime");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "vw";
      DataTable dt = Connection.GetSchema("ViewColumns", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("ViewColumns", dt.TableName);
      Assert.Equal(Connection.Database.ToLower(), dt.Rows[0]["VIEW_SCHEMA"].ToString().ToLower());
      Assert.Equal("vw", dt.Rows[0]["VIEW_NAME"]);
      Assert.Equal("theTime", dt.Rows[0]["COLUMN_NAME"]);
    }

    [Fact]
    public void SingleForeignKey()
    {
      executeSQL("DROP TABLE IF EXISTS child");
      executeSQL("DROP TABLE IF EXISTS parent");
      executeSQL("CREATE TABLE parent (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      executeSQL("CREATE TABLE child (id INT, parent_id INT, INDEX par_ind (parent_id), " +
        "CONSTRAINT c1 FOREIGN KEY (parent_id) REFERENCES parent(id) ON DELETE CASCADE) ENGINE=INNODB");
      string[] restrictions = new string[4];
      restrictions[0] = null;
      restrictions[1] = Connection.Database;
      restrictions[2] = "child";
      DataTable dt = Connection.GetSchema("Foreign Keys", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      DataRow row = dt.Rows[0];
      Assert.Equal(Connection.Database.ToLower(), row[1].ToString().ToLower());
      Assert.Equal("c1", row[2]);
      Assert.Equal(Connection.Database.ToLower(), row[4].ToString().ToLower());
      Assert.Equal("child", row[5]);
      Assert.Equal(Connection.Database.ToLower(), row[10].ToString().ToLower());
      Assert.Equal("parent", row[11]);
    }

    /// <summary>
    /// Bug #26660 MySqlConnection.GetSchema fails with NullReferenceException for Foreign Keys 
    /// </summary>
    [Fact(Skip = "Not compatible with linux")]
    public void ForeignKeys()
    {
      executeSQL("DROP TABLE IF EXISTS product_order");
      executeSQL("DROP TABLE IF EXISTS customer");
      executeSQL("DROP TABLE IF EXISTS product");

      executeSQL("CREATE TABLE product (category INT NOT NULL, id INT NOT NULL, " +
            "price DECIMAL, PRIMARY KEY(category, id)) ENGINE=INNODB");
      executeSQL("CREATE TABLE customer (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      executeSQL("CREATE TABLE product_order (no INT NOT NULL AUTO_INCREMENT, " +
        "product_category INT NOT NULL, product_id INT NOT NULL, customer_id INT NOT NULL, " +
        "PRIMARY KEY(no), INDEX (product_category, product_id), " +
        "FOREIGN KEY (product_category, product_id) REFERENCES product(category, id) " +
        "ON UPDATE CASCADE ON DELETE RESTRICT, INDEX (customer_id), " +
        "FOREIGN KEY (customer_id) REFERENCES customer(id)) ENGINE=INNODB");

      DataTable dt = Connection.GetSchema("Foreign Keys");
      Assert.True(dt.Columns.Contains("REFERENCED_TABLE_CATALOG"));
    }

    [Fact]
    public void MultiSingleForeignKey()
    {
      executeSQL("DROP TABLE IF EXISTS product_order");
      executeSQL("DROP TABLE IF EXISTS customer");
      executeSQL("DROP TABLE IF EXISTS product");

      executeSQL("CREATE TABLE product (category INT NOT NULL, id INT NOT NULL, " +
            "price DECIMAL, PRIMARY KEY(category, id)) ENGINE=INNODB");
      executeSQL("CREATE TABLE customer (id INT NOT NULL, PRIMARY KEY (id)) ENGINE=INNODB");
      executeSQL("CREATE TABLE product_order (no INT NOT NULL AUTO_INCREMENT, " +
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
      Assert.Equal(2, dt.Rows.Count);
      DataRow row = dt.Rows[0];
      Assert.Equal(Connection.Database.ToLower(), row[1].ToString().ToLower());
      Assert.Equal("product_order_ibfk_1", row[2]);
      Assert.Equal(Connection.Database.ToLower(), row[4].ToString().ToLower());
      Assert.Equal("product_order", row[5]);
      Assert.Equal(Connection.Database.ToLower(), row[10].ToString().ToLower());
      Assert.Equal("product", row[11]);

      row = dt.Rows[1];
      Assert.Equal(Connection.Database.ToLower(), row[1].ToString().ToLower());
      Assert.Equal("product_order_ibfk_2", row[2]);
      Assert.Equal(Connection.Database.ToLower(), row[4].ToString().ToLower());
      Assert.Equal("product_order", row[5]);
      Assert.Equal(Connection.Database.ToLower(), row[10].ToString().ToLower());
      Assert.Equal("customer", row[11]);
    }

    [Fact]
    public void Triggers()
    {
      executeSQL("DROP TABLE IF EXISTS test1");
      executeSQL("CREATE TABLE test1 (id int)");
      executeSQL("CREATE TABLE test2 (count int)");
      executeSQL("INSERT INTO test2 VALUES (0)");
      string sql = String.Format("CREATE TRIGGER `{0}`.trigger1 AFTER INSERT ON test1 FOR EACH ROW BEGIN " +
        "UPDATE test2 SET count = count+1; END", Connection.Database);
      executeSQL(sql);

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "test1";
      DataTable dt = Connection.GetSchema("Triggers", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Triggers", dt.TableName);
      Assert.Equal("trigger1", dt.Rows[0]["TRIGGER_NAME"]);
      Assert.Equal("INSERT", dt.Rows[0]["EVENT_MANIPULATION"]);
      Assert.Equal("test1", dt.Rows[0]["EVENT_OBJECT_TABLE"]);
      Assert.Equal("ROW", dt.Rows[0]["ACTION_ORIENTATION"]);
      Assert.Equal("AFTER", dt.Rows[0]["ACTION_TIMING"]);
    }

    [Fact]
    public void UsingQuotedRestrictions()
    {
      executeSQL("DROP TABLE IF EXISTS test1");
      executeSQL("CREATE TABLE test1 (id int)");

      string[] restrictions = new string[4];
      restrictions[1] = Connection.Database;
      restrictions[2] = "`test1`";
      DataTable dt = Connection.GetSchema("Tables", restrictions);
      Assert.True(dt.Rows.Count == 1);
      Assert.Equal("Tables", dt.TableName);
      Assert.Equal("test1", dt.Rows[0][2]);
      Assert.Equal("`test1`", restrictions[2]);
    }

    [Fact]
    public void ReservedWords()
    {
      DataTable dt = Connection.GetSchema("ReservedWords");
      foreach (DataRow row in dt.Rows)
        Assert.False(String.IsNullOrEmpty(row[0] as string));
    }

    [Fact]
    public void GetSchemaCollections()
    {
      executeSQL("CREATE TABLE parent (id int, name_parent VARCHAR(20), PRIMARY KEY(id))");
      executeSQL(@"CREATE TABLE child (id int, name_child VARCHAR(20), parent_id INT, 
        PRIMARY KEY(id), INDEX par_id (parent_id), FOREIGN KEY (parent_id) REFERENCES parent(id) ON DELETE CASCADE)");
      executeSQL("INSERT INTO parent VALUES(1, 'parent_1')");
      executeSQL("INSERT INTO child VALUES(1, 'child_1', 1)");

      SchemaProvider schema = new SchemaProvider(Connection);
      string[] restrictions = new string[5];
      restrictions[2] = "parent";
      restrictions[1] = Connection.Database;

      MySqlSchemaCollection schemaCollection = schema.GetSchema("columns", restrictions);

      Assert.True(schemaCollection.Columns.Count == 20);
      Assert.True(schemaCollection.Rows.Count == 2);
      Assert.Equal("parent", schemaCollection.Rows[0]["TABLE_NAME"]);
      Assert.Equal("id", schemaCollection.Rows[0]["COLUMN_NAME"]);

      schemaCollection = schema.GetForeignKeys(restrictions);
      Assert.True(schemaCollection.AsDataTable().Columns.Contains("REFERENCED_TABLE_NAME"));

      schemaCollection = schema.GetForeignKeyColumns(restrictions);
      Assert.True(schemaCollection.AsDataTable().Columns.Contains("REFERENCED_COLUMN_NAME"));

      schemaCollection = schema.GetUDF(restrictions);
      Assert.True(schemaCollection.AsDataTable().Columns.Contains("RETURN_TYPE"));

      schemaCollection = schema.GetUsers(restrictions);
      Assert.True(schemaCollection.AsDataTable().Columns.Contains("USERNAME"));
    }

    /// <summary> 
    /// Bug #26876582 Unexpected ColumnSize for Char(36) and Blob in GetSchemaTable. 
    /// Setting OldGuids to True so CHAR(36) is treated as CHAR.
    /// </summary>
    [Fact]
    public void ColumnSizeWithOldGuids()
    {
      string connString = Connection.ConnectionString;

      executeSQL("DROP TABLE IF EXISTS test");

      using (MySqlConnection conn = new MySqlConnection(connString + ";oldguids=True;"))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand("CREATE TABLE test(char36 char(36) CHARSET utf8mb4, binary16 binary(16), char37 char(37), `tinyblob` tinyblob, `blob` blob);", conn);
        cmd.ExecuteNonQuery();

        using (MySqlDataReader reader = ExecuteReader("SELECT * FROM test;"))
        {
          DataTable schemaTable = reader.GetSchemaTable();

          Assert.Equal(36, schemaTable.Rows[0]["ColumnSize"]);
          Assert.Equal(16, schemaTable.Rows[1]["ColumnSize"]);
          Assert.Equal(37, schemaTable.Rows[2]["ColumnSize"]);
          Assert.Equal(255, schemaTable.Rows[3]["ColumnSize"]);
          Assert.Equal(65535, schemaTable.Rows[4]["ColumnSize"]);
        }
      }
    }

    /// <summary> 
    /// Bug #26876592 Unexpected ColumnSize, IsLong in GetSChemaTable for LongText and LongBlob column.
    /// Added validation when ColumnLenght equals -1 that is when lenght exceeds Int max size.
    /// </summary>
    //[Fact]
    //public void IsLongProperty()
    //{
    //  st.execSQL("DROP TABLE IF EXISTS test;");
    //  st.execSQL("CREATE TABLE test(`longtext` longtext, `longblob` longblob, `tinytext` tinytext, `tinyblob` tinyblob, `text` text, `blob` blob);");

    //  using (MySqlDataReader reader = ExecuteReader("SELECT * FROM test;"))
    //  {
    //    DataTable schemaTable = reader.GetSchemaTable();

    //    Assert.Equal(-1, schemaTable.Rows[0]["ColumnSize"]);
    //    Assert.True((bool)schemaTable.Rows[0]["IsLong"]);
    //    Assert.Equal(-1, schemaTable.Rows[1]["ColumnSize"]);
    //    Assert.True((bool)schemaTable.Rows[1]["IsLong"]);
    //    Assert.Equal(255, schemaTable.Rows[2]["ColumnSize"]);
    //    Assert.False((bool)schemaTable.Rows[2]["IsLong"]);
    //    Assert.Equal(255, schemaTable.Rows[3]["ColumnSize"]);
    //    Assert.False((bool)schemaTable.Rows[3]["IsLong"]);
    //    Assert.Equal(65535, schemaTable.Rows[4]["ColumnSize"]);
    //    Assert.True((bool)schemaTable.Rows[4]["IsLong"]);
    //    Assert.Equal(65535, schemaTable.Rows[5]["ColumnSize"]);
    //    Assert.True((bool)schemaTable.Rows[5]["IsLong"]);
    //  }
    //}

    /// <summary> 
    /// Bug #26954812 Decimal with numericScale of 0 has wrong numericPrecision in GetSchemaTable.
    /// </summary>
    //[Fact]
    //public void NumericPrecisionProperty()
    //{
    //  st.execSQL("DROP TABLE IF EXISTS test;");
    //  st.execSQL("CREATE TABLE test(decimal0 decimal(8,0), decimal1 decimal(8), decimal2 decimal(8,2), decimal3 decimal(8,1) UNSIGNED);");

    //  using (MySqlDataReader reader = st.execReader("SELECT * FROM test;"))
    //  {
    //    DataTable schemaTable = reader.GetSchemaTable();

    //    Assert.Equal(8, schemaTable.Rows[0]["NumericPrecision"]);
    //    Assert.Equal(0, schemaTable.Rows[0]["NumericScale"]);
    //    Assert.Equal(8, schemaTable.Rows[1]["NumericPrecision"]);
    //    Assert.Equal(0, schemaTable.Rows[1]["NumericScale"]);
    //    Assert.Equal(8, schemaTable.Rows[2]["NumericPrecision"]);
    //    Assert.Equal(2, schemaTable.Rows[2]["NumericScale"]);
    //    Assert.Equal(8, schemaTable.Rows[3]["NumericPrecision"]);
    //    Assert.Equal(1, schemaTable.Rows[3]["NumericScale"]);
    //  }
    //}

    /// <summary>
    /// Bug #29536344 MYSQLCONNECTION.GETSCHEMA RETURNS WRONG ORDER OF RECORDS FOR COLUMNS INFORMATION
    /// </summary>
    [Fact]
    public void GetSchemaReturnColumnsByOrdinalPosition()
    {
      executeSQL("CREATE TABLE foo (x int, a VARCHAR(20), z int, c int)");
      executeSQL("INSERT INTO foo VALUES(1, 'columnName', 2, 3)");

      string[] restrictions = new string[5];
      restrictions[2] = "foo";
      restrictions[1] = Connection.Database;
      var rows = Connection.GetSchema("COLUMNS", restrictions).Rows.OfType<DataRow>();

      Assert.Collection(rows, row => Assert.Equal("x", row["COLUMN_NAME"]),
        row => Assert.Equal("a", row["COLUMN_NAME"]),
        row => Assert.Equal("z", row["COLUMN_NAME"]),
        row => Assert.Equal("c", row["COLUMN_NAME"]));
    }
  }
}
