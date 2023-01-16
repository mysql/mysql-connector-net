// Copyright (c) 2017, 2023, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class CollectionIndexTests : BaseTest
  {
    public string longval = "123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567";

    [Test]
    public void IncorrectlyFormatedIndexDefinition()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"type\": \"INDEX\" }"));
      Assert.AreEqual("Field 'fields' is mandatory.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT\" } ], \"unexpectedField\" : false }"));
      Assert.AreEqual("Field name 'unexpectedField' is not allowed.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"fields\":$.myField, \"types\":\"TEXT\" } ] }"));
      Assert.AreEqual("Field 'field' is mandatory.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"types\":\"TEXT\" } ] }"));
      Assert.AreEqual("Field 'type' is mandatory.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT\", \"unexpectedField\" : false } ] }"));
      Assert.AreEqual("Field name 'unexpectedField' is not allowed.", ex.Message);

      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      collection = CreateCollection("test");

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ], \"type\":\"indexa\" }"));
      Assert.AreEqual("Argument value 'indexa' for index type is invalid", ex.Message);

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ], \"type\":\"OTheR\" }"));
      Assert.AreEqual("Argument value 'OTheR' for index type is invalid", ex.Message);

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex1", "{\"fields\": [ { \"field\":$.myGeoJsonField, \"type\":\"GEOJSON\" } ], \"type\":\"Spatial\" }"));
      Assert.AreEqual("GEOJSON index requires 'constraint.required: TRUE", ex.Message);

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex2", "{\"fields\": [ { \"field\":$.myGeoJsonField, \"type\":\"GEOJSON\" } ], \"type\":\"spatial\" }"));
      Assert.AreEqual("GEOJSON index requires 'constraint.required: TRUE", ex.Message);

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex3", "{\"fields\": [ { \"field\":$.myGeoJsonField, \"type\":\"GEOJSON\" } ], \"type\":\"sPaTiAl\" }"));
      Assert.AreEqual("GEOJSON index requires 'constraint.required: TRUE", ex.Message);
    }

    [Test]
    public void CreateIndexOnSingleFieldWithDefaultOptions()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT(10)\" } ] }");
      ValidateIndex("myIndex", "test", "t10", false, false, false, 1, 10);
    }

    [Test]
    public void CreateIndexOnSingleFieldWithAllOptions()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\", \"required\": true } ] }");
      ValidateIndex("myIndex", "test", "i", false, true, false, 1);
    }

    [Test]
    public void CreateIndexOnMultipleFields()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" }, { \"field\":$.myField2, \"type\":\"INT\" }, { \"field\":$.myField3, \"type\":\"INT\" } ] }");
      ValidateIndex("myIndex", "test", "i", false, false, false, 1);
      ValidateIndex("myIndex", "test", "i", false, false, false, 2);
      ValidateIndex("myIndex", "test", "i", false, false, false, 3);
    }

    [Test]
    public void CreateIndexOnMultipleFieldsWithAllOptions()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" }, { \"field\":$.myField2, \"type\":\"INT\", \"required\":true }, { \"field\":$.myField3, \"type\":\"INT UNSIGNED\", \"required\":false } ] }");
      ValidateIndex("myIndex", "test", "i", false, false, false, 1);
      ValidateIndex("myIndex", "test", "i", false, true, false, 2);
      ValidateIndex("myIndex", "test", "i_u", false, false, true, 3);
    }

    [Test]
    public void CreateTypeSpecificIndexesCaseInsensitive()
    {
      var collection = CreateCollection("test");

      // Datetime index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"daTEtiME\" } ] }");
      ValidateIndex("myIndex", "test", "dd", false, false, false, 1);

      // Timestamp index.
      collection.DropIndex("myIndex");
      string explicitDefaultTimestamp = session.SQL("SHOW VARIABLES LIKE 'explicit_defaults_for_timestamp'").Execute().FetchAll()[0][1].ToString();
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TIMESTAMP\" } ] }");
      ValidateIndex("myIndex", "test", "ds", false, explicitDefaultTimestamp == "OFF" ? true : false, false, 1);

      // Time index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"time\" } ] }");
      ValidateIndex("myIndex", "test", "dt", false, false, false, 1);

      // Date index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"DATE\" } ] }");
      ValidateIndex("myIndex", "test", "d", false, false, false, 1);

      // Numeric index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"NUMERIC UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "xn_u", false, false, true, 1);

      // Decimal index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"DECIMAL\" } ] }");
      ValidateIndex("myIndex", "test", "xd", false, false, false, 1);

      // Double index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"double UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "fd_u", false, false, true, 1);

      // Float index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"FLOAT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "f_u", false, false, true, 1);

      // Real index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"REAL UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "fr_u", false, false, true, 1);

      // Bigint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"BIGINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "ib_u", false, false, true, 1);

      // Int index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INTEGER unsigned\" } ] }");
      ValidateIndex("myIndex", "test", "i_u", false, false, true, 1);

      // Mediumint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"MEDIUMINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "im_u", false, false, true, 1);

      // Smallint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"SMALLINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "is_u", false, false, true, 1);

      // Tinyint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TINYINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "it_u", false, false, true, 1);

      // Geojson index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"required\":true, \"options\":2, \"srid\":4326 } ], \"type\":\"SPATIAL\" }");
      ValidateIndex("myIndex", "test", "gj", false, true, false, 1);
    }

    [Test]
    public void DropIndex()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT(64)\" }, { \"field\":$.myField2, \"type\":\"TEXT(10)\", \"required\":true }, { \"field\":$.myField3, \"type\":\"INT UNSIGNED\", \"required\":false } ] }");
      ValidateIndex("myIndex", "test", "t64", false, false, false, 1, 64);
      ValidateIndex("myIndex", "test", "t10", false, true, false, 2, 10);
      ValidateIndex("myIndex", "test", "i_u", false, false, true, 3);

      collection.DropIndex("myIndex");
      Exception ex = Assert.Throws<Exception>(() => ValidateIndex("myIndex", "test", "t10", false, false, false, 1, 10));
      Assert.AreEqual("Index not found.", ex.Message);
      ex = Assert.Throws<Exception>(() => ValidateIndex("myIndex", "test", "t10", false, true, false, 2, 10));
      Assert.AreEqual("Index not found.", ex.Message);
      ex = Assert.Throws<Exception>(() => ValidateIndex("myIndex", "test", "i_u", false, false, true, 3));
      Assert.AreEqual("Index not found.", ex.Message);
    }

    [Test]
    public void CreateIndexWithDuplicateName()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myUniqueIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ] }");
      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myUniqueIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ] }"));
      Assert.AreEqual("Duplicate key name 'myUniqueIndex'", ex.Message);
    }

    [Test]
    public void CreateIndexWithInvalidJsonDocumentDefinition()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\" = $.myField, \"type\" = \"TEXT\" } ] }"));
      Assert.AreEqual("The value provided is not a valid JSON document. Expected token ':'", ex.Message);
      ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT\" ] }"));
      Assert.AreEqual("The value provided is not a valid JSON document. Expected token ','", ex.Message);
      ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, type:\"TEXT\" } }"));
      Assert.AreEqual("The value provided is not a valid JSON document. Expected token '\"'", ex.Message);
    }

    [Test]
    public void CreateIndexWithInvalidJsonDocumentStructure()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\", \"myCustomField\":\"myCustomValue\" } ] }"));
      Assert.AreEqual("Field name 'myCustomField' is not allowed.", ex.Message);
      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"mytype\":\"INT\" } ] }"));
      Assert.AreEqual("Field 'type' is mandatory.", ex.Message);
      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"myfield\":$.myField, \"type\":\"INT\" } ] }"));
      Assert.AreEqual("Field 'field' is mandatory.", ex.Message);
      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT\" , \"myCustomField\":\"myCustomValue\"} ] }"));
      Assert.AreEqual("Field name 'myCustomField' is not allowed.", ex.Message);
      ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", ""));
      Assert.AreEqual("The value provided is not a valid JSON document. Index was outside the bounds of the array.", ex.Message);
    }

    [Test]
    public void CreateIndexWithTypeNotIndexOrSpatial()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ], \"type\":\"indexa\" }"));
      Assert.AreEqual("Argument value 'indexa' for index type is invalid", ex.Message);
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ], \"type\":\"OTheR\" }"));
      Assert.AreEqual("Argument value 'OTheR' for index type is invalid", ex.Message);
    }

    [Test]
    public void CreateSpatialIndexWithRequiredSetToFalse()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"required\":false, \"options\":2, \"srid\":4326 } ], \"type\":\"SPATIAL\" }"));
      Assert.AreEqual("GEOJSON index requires 'constraint.required: TRUE", ex.Message);
    }

    [Test]
    public void CreateIndexWithInvalidType()
    {
      var collection = CreateCollection("test");

      // Missing key length for text type.
      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"Text\" } ] }"));
      StringAssert.EndsWith("used in key specification without a key length", ex.Message);

      // Invalid index types.
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"Texta\" } ] }"));
      StringAssert.EndsWith("Invalid or unsupported type specification 'Texta'", ex.Message);
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"INTO\" } ] }"));
      StringAssert.EndsWith("Invalid or unsupported type specification 'INTO'", ex.Message);
      try
      {
        collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"Datetime\"} ] }");
        collection.DropIndex("myIndex");
        collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"Real\"} ] }");
        collection.DropIndex("myIndex");
        collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"Integer\"} ] }");
        collection.DropIndex("myIndex");
      }
      catch (Exception)
      {
        Assert.Fail("Unexpected Exception: " + ex.Message);
      }
    }

    [Test]
    public void CreateIndexWithInvalidGeojsonOptions()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"INT\", \"options\":2, \"srid\":4326 } ] }"));
      Assert.AreEqual("Unsupported argument specification for '$.myField'", ex.Message);

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"options\":2, \"srid\":4326 } ], \"type\":\"SPATIAL\" }"));
      Assert.AreEqual("GEOJSON index requires 'constraint.required: TRUE", ex.Message);
    }

    [Test]
    public void ValidIndexNames()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("01myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"TEXT(64)\" } ] }");
      ValidateIndex("01myIndex", "test", "t64", false, false, false, 1, 64);
      collection.DropIndex("01myIndex");

      collection.CreateIndex("!myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"TEXT(64)\" } ] }");
      ValidateIndex("!myIndex", "test", "t64", false, false, false, 1, 64);
      collection.DropIndex("!myIndex");

      collection.CreateIndex("-myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"TEXT(64)\" } ] }");
      ValidateIndex("-myIndex", "test", "t64", false, false, false, 1, 64);
      collection.DropIndex("-myIndex");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex(null, "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(64)\" , \"required\":true} ] }"));
      Assert.AreEqual("Invalid value for argument 'name'", ex.Message);
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex1243536464r4urgfu4rgh43urvbnu4rgh4u3rive39irgf9r4fri3jgnfi", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(64)\" , \"required\":true} ] }"));
      Assert.AreEqual("Identifier name 'myIndex1243536464r4urgfu4rgh43urvbnu4rgh4u3rive39irgf9r4fri3jgnfi' is too long", ex.Message);
    }

    [Test]
    public void CreateIndexWithArrayOption()
    {
      var collection = CreateCollection("test");
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");

      // Supported types
      var doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("CHAR(128)", collection);

      doc = new[] { new { _id = 1, name = "Time", myField = new[] { "01:01:01.001", "23:59:59.15", "12:00:00" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("TIME", collection);

      doc = new[] { new { _id = 1, name = "Date", myField = new[] { "2019-01-01", "2019-01-01 12:15:00" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("DATE", collection);

      doc = new[] { new { _id = 1, name = "Int", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("SIGNED", collection);

      doc = new[] { new { _id = 1, name = "Decimal", myField = new[] { "3.0", "254.51", "19" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("DECIMAL(10,2)", collection);

      testSchema.DropCollection("test");
      collection = CreateCollection("test");

      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"TINYINT\", \"array\": true}]}"));
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"required\":true, \"options\":2, \"srid\":4326, \"array\": true } ], \"type\":\"SPATIAL\" }"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(128)\", \"array\": true, \"required\":true}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("index_1", "{\"fields\": [{\"field\": $.field1, \"type\":\"CHAR(128)\", \"array\": true}, " +
        "{\"field\": $.field2, \"type\":\"CHAR(128)\", \"array\": true}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"TINYINT\", \"array\": null}]}"));
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"TINYINT\", \"array\": NULL}]}"));
    }

    #region WL14389

    [Test, Description("Add Collection index")]
    public void AddIndexAndInsertRecords()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      Collection testColl = CreateCollection("test1");
      Assert.IsTrue(testColl.ExistsInDatabase());
      testColl.CreateIndex("testIndex", "{\"fields\": [ { \"field\":$.myId, \"type\":\"INTEGER UNSIGNED\" , \"required\":true} ] }");
      testColl.CreateIndex("testIndex1", "{\"fields\": [ { \"field\":$.myAge, \"type\":\"FLOAT UNSIGNED\" , \"required\":true} ] }");
      var result = testColl.Add(new { myId = 1, myAge = 35.1, _id = 1 }).Add(new { myId = 2, myAge = 41.9, _id = 2 }).Execute();
      Assert.True(result.AffectedItemsCount > 0);
      Collection testCol2 = CreateCollection("test2");
      testCol2.CreateIndex("testIndex2", "{\"fields\": [ { \"field\":$.myId, \"type\":\"INT\" , \"required\":true} ] }");
      result = testCol2.Add(new { myId = 1 }).Execute();
      Assert.True(result.AffectedItemsCount > 0);
    }

    [Test, Description("Create valid index on a single key with all options")]
    public void InsertWithValidIndexAndNoIndex()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      Result result = col.Add(new { name = "Sakila", age = 15 }).Execute();
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(64)\" , \"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { age = 10 })));
      col.DropIndex("myIndex");
      result = col.Add(new { age = 10 }).Execute();
      Assert.AreEqual(1, result.AffectedItemsCount);
    }

    [Test, Description("Create a valid index on a single key of all DATATYPES.Datatypes supported: INT [UNSIGNED] TINYINT [UNSIGNED] SMALLINT [UNSIGNED] MEDIUMINT [UNSIGNED] INTEGER [UNSIGNED] BIGINT [UNSIGNED] REAL [UNSIGNED] FLOAT [UNSIGNED] DOUBLE [UNSIGNED] DECIMAL [UNSIGNED] NUMERIC [UNSIGNED] DATE TIME TIMESTAMP DATETIME TEXT[(length)]")]
    public void IndexOfAllDatatypes()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");

      Result result = col.Add(new { name = "Sakila", age = 15, date_time = "2010-01-01 00:00:00", time_stamp = "2015-01-01 00:11:02", date_check = "2117-12-17", time_check = "12:14:07", real_check = 12E+4, decimal_check = 122.134, float_check = 11.223, double_check = 23.32343425, numeric_check = 1122.3434, tiny_int = 112, medium_int = 12345, big_int = 1234567, int_check = 174734 }).Execute();
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(64)\" , \"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { age = 21 })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.age, \"type\":\"INTEGER UNSIGNED\" , \"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.time_stamp, \"type\":\"TIMESTAMP\", \"required\":true } ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.time_check, \"type\":\"TIME\", \"required\":true } ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.real_check, \"type\":\"REAL\", \"required\":true } ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.decimal_check, \"type\":\"DECIMAL UNSIGNED\",\"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.float_check, \"type\":\"FLOAT UNSIGNED\", \"required\":true  } ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.decimal_check, \"type\":\"DECIMAL UNSIGNED\" , \"required\":true } ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.numeric_check, \"type\":\"NUMERIC UNSIGNED\", \"required\":true } ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.tiny_int, \"type\":\"TINYINT\" , \"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.medium_int, \"type\":\"MEDIUMINT\" , \"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));

      col.DropIndex("myIndex");
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.int_check, \"type\":\"INT\" , \"required\":true} ] }");
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { name = "abc" })));
    }

    [Test, Description("Create an index with mismatched data types")]
    public void IndexWithMismatchedDatatypes()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      Result result = col.Add(new { name = "Sakila", age = 15 }).Execute();
      Thread.Sleep(2000);
      Exception ex = Assert.Throws<MySqlException>(() => col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"DATETIME\"} ] }"));
      StringAssert.Contains("Incorrect datetime value", ex.Message);
    }

    [Test, Description("Create an index specifiying SPATIAL as the index type for a non spatial data type and vice versa")]
    public void IndexSpatialForNonSpatialDatatype()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      Result result = col.Add(new { name = "Sakila", age = 15 }).Execute();
      Assert.Throws<MySqlException>(() => col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT\"} ] , \"type\":\"SPATIAL\" }"));
      col.DropIndex("myIndex");
      Assert.Throws<MySqlException>(() => col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"GEOJSON\"} ] , \"type\":\"INDEX\" }"));
    }

    /// <summary>
    ///   Bug 28343828(8.0.13) fix - Server should raise error whenever a length isn't provided for the TEXT index type
    /// </summary>
    [Test, Description("Create valid index with index definition given as DbDoc")]
    public void CreateIndexGivenDbDoc()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      Result result = col.Add(new { name = "Sakila", age = 15 }).Execute();

      var data2 = new DbDoc(@"{
                    ""fields"": [
                      {""field"" : $.name, ""type"" : ""TEXT(64)"",""required"" : true}
                    ]
                }");
      col.CreateIndex("myIndex", data2);
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(new { age = 10 })));
    }

    [Test, Description("Create valid index on member of Array type as key")]
    public void IndexOnArrayMember()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");

      var col = CreateCollection("my_collection");

      var t1 = "{\"_id1\": \"1002\", \"ARR1\":[\"name1\",\"name2\",\"name3\"]}";
      col.Add(t1).Execute();

      col.CreateIndex("myIndex1", "{\"fields\": [{\"field\":\"$.ARR1\",\"type\": \"TEXT(64)\"}]}");
      ValidateIndex("myIndex1", "my_collection", "t64", false, false, false, 1, 64);
      col.DropIndex("myIndex1");

      var t2 = "{\"_id2\": \"2\",\"address2\": {\"zip2\":\"325226\", \"city2\": \"San Francisco\"}}";
      col.Add(t2).Execute();
      col.CreateIndex("myIndex2", "{\"fields\": [{\"field\":\"$.address2[0].city2\",\"type\": \"TEXT(100)\"}]}");
      ValidateIndex("myIndex2", "my_collection", "t100", false, false, false, 1, 100);
      col.DropIndex("myIndex2");

      var t3 = "{\"_id3\": \"32\",\"address3\": [{\"zip3\":\"325227\", \"city3\": \"New York\"}]}";
      col.Add(t3).Execute();
      col.CreateIndex("myIndex3", "{\"fields\": [{\"field\":\"$.address3[0].city3\",\"type\": \"TEXT(100)\"}]}");
      ValidateIndex("myIndex3", "my_collection", "t100", false, false, false, 1, 100);
      col.DropIndex("myIndex3");

      col.CreateIndex("myIndex4", "{\"fields\": [{\"field\":\"$.address3\",\"type\": \"TEXT(64)\"}]}");
      ValidateIndex("myIndex4", "my_collection", "t64", false, false, false, 1, 64);
      col.DropIndex("myIndex4");

      var t4 = "{\"_id\": \"4\",\"address4\": [{\"zip4\":\"32522\", \"city4\": \"New Delhi\"},{\"zip5\": \"325228\", \"city5\":\"Bangalore\"}]}";
      col.Add(t4).Execute();
      col.CreateIndex("myIndex5", "{\"fields\": [{\"field\":\"$.address4[0].city4\",\"type\": \"TEXT(160)\"}]}");
      ValidateIndex("myIndex5", "my_collection", "t160", false, false, false, 1, 160);
      col.DropIndex("myIndex5");

      col.CreateIndex("myIndex6", "{\"fields\": [{\"field\":\"$.address4\",\"type\": \"TEXT(60)\"}]}");
      ValidateIndex("myIndex6", "my_collection", "t60", false, false, false, 1, 60);
      col.DropIndex("myIndex6");
    }

    [Test, Description("Create valid index on member of DbDoc type as key")]
    public void IndexOnDbDocMember()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      DbDoc data2 = new DbDoc();
      data2.SetValue("name", "Sakila");
      data2.SetValue("age", 20);
      Result result = col.Add(data2).Execute();
      Assert.Throws<MySqlException>(() => col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT\" , \"required\":true} ] }"));
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(64)\" , \"required\":true} ] }");
      DbDoc data1 = new DbDoc();
      data1.SetValue("age", 20);
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(col.Add(data1)));
    }

    [Test, Description("Create valid index perform CRUD operations")]
    public void CountRecordsInsertedWithValidIndex()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      Result result = col.Add(new { name = "Sakila", age = 15 }).Execute();
      Assert.Throws<MySqlException>(() => col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT\" , \"required\":true} ] }"));
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(30)\" , \"required\":true} ] }");
      result = col.Add(new { name = "Maria", age = 20 }).Execute();
      result = col.Add(new { name = "Maria", age = 21 }).Execute();
      var Removing = col.Remove("name='Maria'").Execute();
      Assert.AreEqual(2, (int)Removing.AffectedItemsCount, "Matches");
    }

    [Test, Description("Create invalid index with non-existent key")]
    public void InvalidIndexNonExistentKey()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      Result result = col.Add(new { age = 15 }).Execute();
      Assert.Throws<MySqlException>(() => col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT\" , \"required\":\"true\"} ]}"));
    }

    [Test, Description("Create a valid index in async way")]
    public async Task CreateIndexInAsync()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var col = CreateCollection("my_collection");
      await col.Add(new { name = "Sakila", age = 15 }).ExecuteAsync();
      col.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"TEXT(64)\" , \"required\":true} ] }");
    }

    /// <summary>
    /// //Bug29692534
    /// </summary>
    [Test, Description("Create valid index using a document field type of array and setting array to true with single key on all possible datatypes-data inserted and then index created")]
    public void IndexWithArrayOptionSingleKeyAfterInsertData()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");
      var collection = CreateCollection("test");

      // Supported types
      var doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("CHAR(128)", collection);
      collection.Add(doc).Execute();
      CreateArrayIndex("CHAR(128)", collection);

      doc = new[] { new { _id = 1, name = "Binary", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("BINARY(128)", collection);
      collection.Add(doc).Execute();
      CreateArrayIndex("BINARY(128)", collection);
      var doc22 = new[] { new { _id = true, name = "Time", myField = new[] { "01:01:01.001", "23:59:59.15", "12:00:00", "1" } } };
      doc = new[] { new { _id = 1, name = "Time", myField = new[] { "01:01:01.001", "23:59:59.15", "12:00:01", "1" } } };
      var doc11 = new[] { new { _id = 2, name = "Time", myField = "1" } };
      collection.Add(doc).Execute();
      collection.Add(doc22).Execute();
      collection.Add(doc11).Execute();
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      var docResult = collection.Find("'23:59:59.15' in myField").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID");
      docResult = collection.Find("'1' in $.myField").Execute();
      Assert.AreEqual(3, docResult.FetchAll().Count, "Matching the document ID");
      docResult = collection.Find("1 in $.myField").Execute();
      Assert.AreEqual(3, docResult.FetchAll().Count, "Matching the document ID");
      docResult = collection.Find(":myField IN $.myField").Bind("myField", "23:59:59.15").Execute();
      docResult = collection.Find(":myField IN $._id").Bind("myField", true).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

      collection = CreateCollection("test");
      collection.Add(doc).Execute();
      collection.Add(doc22).Execute();
      CreateArrayIndex("TIME", collection);

      collection = CreateCollection("test");
      collection.Add(doc).Execute();
      collection.Add(doc22).Execute();
      CreateArrayIndex("TIME", collection);

      collection = CreateCollection("test");
      //No Array
      var docx = new[] { new { _id = 1, name = "Date", myField = "2019-01-01 12:15:00" } };
      collection.Add(docx).Execute();
      CreateArrayIndex("DATE", collection);
      collection.Add(docx).Execute();
      CreateArrayIndex("DATE", collection);

      doc = new[] { new { _id = 1, name = "Date", myField = new[] { "2019-01-01", "2019-01-01 12:15:00" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("DATE", collection);
      collection.Add(doc).Execute();
      CreateArrayIndex("DATE", collection);

      doc = new[] { new { _id = 1, name = "DateTime", myField = new[] { "1000-01-01 00:00:00", "9999-12-31 23:59:59" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("DATETIME", collection);
      collection.Add(doc).Execute();
      CreateArrayIndex("DATETIME", collection);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Int", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"INT\", \"array\": true}]}"));

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Int", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"INTEGER\", \"array\": true}]}"));

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Signed", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("SIGNED", collection);

      doc = new[] { new { _id = 1, name = "Unsigned", myField = new[] { "-3", "-254", "-19" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("UNSIGNED", collection);

      doc = new[] { new { _id = 1, name = "Unsigned Int", myField = new[] { "3", "-254", "-19" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("UNSIGNED", collection);

      doc = new[] { new { _id = 1, name = "Decimal", myField = new[] { "3.0", "254.51", "19" } } };
      collection.Add(doc).Execute();
      CreateArrayIndex("DECIMAL(10,2)", collection);

      collection = CreateCollection("test");

      // Invalid datatypes
      doc = new[] { new { _id = 1, name = "TINYINT", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("TINYINT", collection);

      doc = new[] { new { _id = 1, name = "TINYINT", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("TINYINT(10)", collection);

      doc = new[] { new { _id = 1, name = "SMALLINT", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("SMALLINT", collection);

      doc = new[] { new { _id = 1, name = "MEDIUMINT", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("MEDIUMINT", collection);

      doc = new[] { new { _id = 1, name = "BIGINT", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("BIGINT", collection);

      doc = new[] { new { _id = 1, name = "Real", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("REAL", collection);

      doc = new[] { new { _id = 1, name = "Float", myField = new[] { "-3.1", "254.50", "19.19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("FLOAT", collection);

      doc = new[] { new { _id = 1, name = "Double", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("DOUBLE", collection);

      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("TEXT(64)", collection);

      doc = new[] { new { _id = 1, name = "BLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("BLOB", collection);

      doc = new[] { new { _id = 1, name = "TINYBLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("TINYBLOB", collection);

      doc = new[] { new { _id = 1, name = "MEDIUMBLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("MEDIUMBLOB", collection);

      doc = new[] { new { _id = 1, name = "LONGBLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("LONGBLOB", collection);

      doc = new[] { new { _id = 1, name = "ENUM", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("ENUM", collection);

      doc = new[] { new { _id = 1, name = "Byte", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("BYTE(128)", collection);

      doc = new[] { new { _id = 1, name = "TimeStamp", myField = new[] { "1970-01-01 00:00:01", "2038-01-19 03:14:07" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("TIMESTAMP", collection);

      doc = new[] { new { _id = 1, name = "Year", myField = new[] { "2019", "2155" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("YEAR", collection);

      collection = CreateCollection("test");

      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.Add(doc).Execute();
      collection.CreateIndex("myIndex1", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}]," +
          "\"type\":\"index\"}");
      ValidateIndex("myIndex1", "test", "CHAR(128)", false, false, false, 1, null, true);
      collection.DropIndex("myIndex1");
      collection.RemoveOne(1);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.Add(doc).Execute();
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex1", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}],\"type\":\"indexax\"}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"required\":true, " +
            "\"options\":2, \"srid\":4326, \"array\": true } ], \"type\":\"SPATIAL\" }"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.name, \"type\":\"GEOJSON\", \"required\":true } ],\"type\":\"spatial\" }"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(128)\", \"array\": true, " +
            "\"required\":true}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("index_1", "{\"fields\": [{\"field\": $.field1, \"type\":\"CHAR(128)\", \"array\": true}, " +
          "{\"field\": $.field2, \"type\":\"CHAR(128)\", \"array\": true}]}"));

      collection = CreateCollection("test");
      collection.CreateIndex("index_11", "{\"fields\": [{\"field\": $.field1, \"type\":\"CHAR(128)\"}, " +
        "{\"field\": $.field2, \"type\":\"CHAR(128)\", \"array\": true}]}");
      ValidateIndex("index_11", "test", "c128", false, false, false, 1, null, false);
      ValidateIndex("index_11", "test", "CHAR(128)", false, false, false, 2, null, true);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 15, name = "Char", myField = new[] { longval } } };
      collection.Add(doc).Execute();
      collection.CreateIndex("index_15", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(255)\", \"array\": true}]}");

      collection = CreateCollection("test");
      var doc1 = new[] { new { _id = 16, name = "Char", myField = new[] { "" } } };
      collection.Add(doc1).Execute();
      Assert.Throws<MySqlException>(() => collection.CreateIndex("index_16", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(0)\", \"array\": true}]}"));

      collection = CreateCollection("test");
      doc1 = new[] { new { _id = 17, name = "Char", myField = new[] { "" } } };
      collection.Add(doc1).Execute();
      Assert.Throws<MySqlException>(() => collection.CreateIndex("index_17", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(0)\", \"array\": false}]}"));

    }

    /// <summary>
    /// Bug 29692534
    /// </summary>
    [Test, Description("Create valid index using a document field type of array and setting array to true with single key on all possible datatypes-data " +
               "inserted and then index created")]
    public void IndexWithArrayOptionSingleKeyBeforeInsertData()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");

      var collection = CreateCollection("test");

      // Supported types
      var doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("CHAR(128)", collection, true);

      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com" } } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(11)" + "\", \"array\": true}]}");
      Exception ex = Assert.Throws<MySqlException>(() => collection.Add(doc).Execute());
      Assert.AreEqual("Data too long for functional index 'myIndex'.", ex.Message);

      collection = CreateCollection("test");
      var doc1 = new[] { new { _id = 1, name = "Char", myField = "foo@mail.com" } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(11)" + "\", \"array\": true}]}");
      ex = Assert.Throws<MySqlException>(() => collection.Add(doc1).Execute());
      Assert.AreEqual("Data too long for functional index 'myIndex'.", ex.Message);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Binary", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "BINARY(128)" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("BINARY(128)", collection, true);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Time", myField = new[] { "01:01:01.001", "23:59:59.15", "12:00:00" } } };//Bug29692534
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "TIME" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("TIME", collection, true);

      //No Array
      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Date", myField = new[] { "2019-01-01", "2019-01-01 12:15:00" } } };//Bug29692534
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "DATE" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("DATE", collection, true);

      doc = new[] { new { _id = 1, name = "DateTime", myField = new[] { "1000-01-01 00:00:00", "9999-12-31 23:59:59" } } };//Bug29692534
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "DATETIME" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("DATETIME", collection, true);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Int", myField = new[] { "3", "254", "19" } } };
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"INT\", \"array\": true}]}"));

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Int", myField = new[] { "3", "254", "19" } } };
      collection.Add(doc).Execute();
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"INTEGER\", \"array\": true}]}"));

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Signed", myField = new[] { "3", "254", "19" } } };//Bug29692534
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "SIGNED" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("SIGNED", collection, true);


      doc = new[] { new { _id = 1, name = "Signed Int", myField = new[] { "3", "254", "19" } } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "SIGNED INTEGER" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("SIGNED INTEGER", collection, true);

      doc = new[] { new { _id = 1, name = "Unsigned", myField = new[] { "-3", "-254", "-19" } } };//Bug29692534
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "UNSIGNED" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("UNSIGNED", collection, true);

      doc = new[] { new { _id = 1, name = "Unsigned Int", myField = new[] { "3", "-254", "-19" } } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "UNSIGNED INTEGER" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("UNSIGNED INTEGER", collection, true);

      doc = new[] { new { _id = 1, name = "Decimal", myField = new[] { "3.0", "254.51", "19" } } };
      collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "DECIMAL(10,2)" + "\", \"array\": true}]}");
      collection.Add(doc).Execute();
      CreateArrayIndex("DECIMAL(10,2)", collection, true);

      collection = CreateCollection("test");
      // Invalid datatypes
      doc = new[] { new { _id = 1, name = "TINYINT", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("TINYINT", collection);

      doc = new[] { new { _id = 1, name = "TINYINT", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("TINYINT(10)", collection);

      doc = new[] { new { _id = 1, name = "SMALLINT", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("SMALLINT", collection);

      doc = new[] { new { _id = 1, name = "MEDIUMINT", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("MEDIUMINT", collection);

      doc = new[] { new { _id = 1, name = "BIGINT", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("BIGINT", collection);

      doc = new[] { new { _id = 1, name = "Real", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("REAL", collection);

      doc = new[] { new { _id = 1, name = "Float", myField = new[] { "-3.1", "254.50", "19.19" } } };
      InvalidCreateArrayIndex("FLOAT", collection);

      doc = new[] { new { _id = 1, name = "Double", myField = new[] { "3", "254", "19" } } };
      InvalidCreateArrayIndex("DOUBLE", collection);

      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      collection.Add(doc).Execute();
      InvalidCreateArrayIndex("TEXT(64)", collection);

      doc = new[] { new { _id = 1, name = "BLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      InvalidCreateArrayIndex("BLOB", collection);

      doc = new[] { new { _id = 1, name = "TINYBLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      InvalidCreateArrayIndex("TINYBLOB", collection);

      doc = new[] { new { _id = 1, name = "MEDIUMBLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      InvalidCreateArrayIndex("MEDIUMBLOB", collection);

      doc = new[] { new { _id = 1, name = "LONGBLOB", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      InvalidCreateArrayIndex("LONGBLOB", collection);

      doc = new[] { new { _id = 1, name = "ENUM", myField = new[] { "fooi@mail.com", "bar1@mail.com", "qux1@mail.com" } } };
      InvalidCreateArrayIndex("ENUM", collection);

      doc = new[] { new { _id = 1, name = "Byte", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      InvalidCreateArrayIndex("BYTE(128)", collection);

      doc = new[] { new { _id = 1, name = "TimeStamp", myField = new[] { "1970-01-01 00:00:01", "2038-01-19 03:14:07" } } };
      InvalidCreateArrayIndex("TIMESTAMP", collection);

      doc = new[] { new { _id = 1, name = "Year", myField = new[] { "2019", "2155" } } };
      InvalidCreateArrayIndex("YEAR", collection);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };

      collection.CreateIndex("myIndex1", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}]," +
          "\"type\":\"index\"}");
      collection.Add(doc).Execute();
      ValidateIndex("myIndex1", "test", "CHAR(128)", false, false, false, 1, null, true);
      collection.DropIndex("myIndex1");
      collection.RemoveOne(1);

      collection = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex1", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}],\"type\":\"indexax\"}"));
      collection.Add(doc).Execute();

    }

    [Test, Description("Create index with array set as null,NULL,multile  vel arrays,blank arrays,empty arrays.Also test in multikey scenarios")]
    public void IndexArrayCombinations()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");
      var expectedException = "Index field 'array' member must be boolean.";
      var coll = CreateCollection("test");

      coll.CreateIndex("intArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}]}");
      coll.CreateIndex("uintArrayIndex", "{\"fields\": [{\"field\": \"$.uintField\", \"type\": \"UNSIGNED INTEGER\", \"array\": true}]}");
      coll.CreateIndex("floatArrayIndex", "{\"fields\": [{\"field\": \"$.floatField\", \"type\": \"DECIMAL(10,2)\", \"array\": true}]}");
      coll.CreateIndex("dateArrayIndex", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": true}]}");
      coll.CreateIndex("datetimeArrayIndex", "{\"fields\": [{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": true}]}");
      coll.CreateIndex("timeArrayIndex", "{\"fields\": [{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": true}]}");
      coll.CreateIndex("charArrayIndex", "{\"fields\": [{\"field\": \"$.charField\", \"type\": \"CHAR(256)\", \"array\": true}]}");
      coll.CreateIndex("binaryArrayIndex", "{\"fields\": [{\"field\": \"$.binaryField\", \"type\": \"BINARY(256)\", \"array\": true}]}");

      Assert.Throws<FormatException>(() => coll.CreateIndex("charArrayIndex1", "{\"fields\": [{\"field\": \"$.charField1\", \"array\": true}]}"));

      Exception e = Assert.Throws<MySqlException>(() => coll.CreateIndex("intArrayIndex1", "{\"fields\": [{\"field\": \"$.intField1\", \"type\": \"SIGNED INTEGER\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("intArrayIndex1", "{\"fields\": [{\"field\": \"$.intField1\", \"type\": \"SIGNED INTEGER\", \"array\": NULL}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("uintArrayIndex1", "{\"fields\": [{\"field\": \"$.uintField\", \"type\": \"UNSIGNED INTEGER\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("floatArrayIndex1", "{\"fields\": [{\"field\": \"$.floatField\", \"type\": \"DECIMAL(10,2)\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("floatArrayIndex1", "{\"fields\": [{\"field\": \"$.floatField\", \"type\": \"DECIMAL(10,2)\", \"array\": NULL}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("dateArrayIndex1", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("datetimeArrayIndex1", "{\"fields\": [{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("timeArrayIndex1", "{\"fields\": [{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("charArrayIndex1", "{\"fields\": [{\"field\": \"$.charField\", \"type\": \"CHAR(256)\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      e = Assert.Throws<MySqlException>(() => coll.CreateIndex("binaryArrayIndex1", "{\"fields\": [{\"field\": \"$.binaryField\", \"type\": \"BINARY(256)\", \"array\": null}]}"));
      Assert.AreEqual(expectedException, e.Message, "Matching the expected exception");

      Assert.Throws<MySqlException>(() => ExecuteAddStatement(coll.Add("{\"intField\" : [1,[2, 3],4], \"uintField\" : [51,[52, 53],54], \"dateField\" : [\"2019-1-1\", [\"2019-2-1\", \"2019-3-1\"], \"2019-4-1\"], " +
            "\"datetimeField\" : [\"9999-12-30 23:59:59\", [\"9999-12-31 23:59:59\"], \"9999-12-31 23:59:59\"], \"charField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], " +
            "\"binaryField\" : [\"abcd1\", [\"abcd1\", \"abcd2\"], \"abcd4\"],\"timeField\" : [\"10.30\", \"11.30\", \"12.30\"], \"floatField\" : [51.2,[52.4],53.6]}")));

      string[] fields = new string[] {"{\"intField\" : null}", "{\"uintField\" : null}", "{\"dateField\" : null}","{\"datetimeField\" : null}", "{\"charField\" : null}",
                      "{\"binaryField\" : null}","{\"timeField\" : null}", "{\"floatField\" : null}" };
      for (int i = 0; i < fields.Length; i++)
      {
        Assert.Throws<MySqlException>(() => ExecuteAddStatement(coll.Add(fields[i])));
      }

      fields = new string[] { "{\"dateField\" : []}", "{\"datetimeField\" : []}", "{\"timeField\" : []}", "{\"floatField\" : []}" };
      for (int i = 0; i < fields.Length; i++)
      {
        var res = coll.Add(fields[i]).Execute();
        Assert.AreEqual(1, res.AffectedItemsCount);
      }

      fields = new string[] {"{\"intField\" : []}", "{\"uintField\" : []}", "{\"charField\" : []}",
                      "{\"binaryField\" : []}"};
      for (int i = 0; i < fields.Length; i++)
      {
        var res = coll.Add(fields[i]).Execute();
        Assert.AreEqual(1, res.AffectedItemsCount);
      }

      fields = new string[] { "[]", "[]", "[]", "[]", "[]", "[]", "[]", "[]" };
      for (int i = 0; i < fields.Length; i++)
      {
        Assert.Throws<Exception>(() => ExecuteAddStatement(coll.Add(fields[i])));
      }

      fields = new string[] { "\"\"", "\"\"", "\"\"", "\"\"", "\"\"", "\"\"", "\"\"", "\"\"" };
      for (int i = 0; i < fields.Length; i++)
      {
        Assert.Throws<Exception>(() => ExecuteAddStatement(coll.Add(fields[i])));
      }

      var collection = CreateCollection("test");
      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": }, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": }, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": }, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": }]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": null}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": null}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex11", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": null}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => collection.CreateIndex("multiArrayIndex12", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": null}]}"));

      Assert.Throws<FormatException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\"}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": true}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": \"\"}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<FormatException>(() => collection.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\"}, " +
          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": \"\"}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      var testColl = CreateCollection("mycoll30");
      testColl.Add("{\"intField\" : [1,[2, 3],4], \"uintField\" : [51,[52, 53],54], \"dateField\" : [\"2019-1-1\", [\"2019-2-1\", \"2019-3-1\"], \"2019-4-1\"], " +
              "\"datetimeField\" : [\"9999-12-30 23:59:59\", [\"9999-12-31 23:59:59\"], \"9999-12-31 23:59:59\"], \"arrayField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], " +
              "\"binaryField\" : [\"abcd1\", [\"abcd1\", \"abcd2\"], \"abcd4\"],\"timeField\" : [\"10.30\", \"11.30\", \"12.30\"], \"floatField\" : [51.2,[52.4],53.6]}").Execute();
      Assert.Throws<MySqlException>(() => session.SQL("ALTER TABLE `test`.`mycoll30` ADD COLUMN `$dateField` DATE GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$.dateField')) NOT NULL, ADD INDEX `notnullIndex`(`$dateField`)").Execute());

      testColl = CreateCollection("mycoll30");
      testColl.Add("{\"intField\" : [1,[2, 3],4], \"uintField\" : [51,[52, 53],54], \"dateField\" : [\"2019-1-1\", [\"2019-2-1\", \"2019-3-1\"], \"2019-4-1\"], " +
              "\"datetimeField\" : [\"9999-12-30 23:59:59\", [\"9999-12-31 23:59:59\"], \"9999-12-31 23:59:59\"], \"textField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], " +
              "\"binaryField\" : [\"abcd1\", \"abcd2\", \"abcd3\", \"abcd4\"],\"timeField\" : [\"10.30\", \"11.30\", \"12.30\"], \"floatField\" : [51.2,[52.4],53.6]}").Execute();
      var stmt = session.SQL("ALTER TABLE `test`.`mycoll30` ADD COLUMN `$textField` TEXT(64) GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$.textField')) NOT NULL, ADD INDEX `notnullIndex`(`$textField`)");

      Assert.Throws<MySqlException>(() => ExecuteSQLStatement(stmt));

      var db = session.GetSchema(schemaName);
      db.DropCollection("mycoll30");
      Assert.Throws<MySqlException>(() => testColl.CreateIndex("i3", "{\"fields\": [{\"field\": \"$.FLD1\", \"type\": \"TEXT(13)\", \"required\": true}, " +
          "{\"field\": \"$.FLD4\", \"type\": \"TEXT(14)\", \"required\": true}, " +
          "{\"field\": \"$.FLD2\", \"type\": \"TEXT(4)\", \"required\": true}, " +
          "{\"field\": \"$.FLD3\", \"type\": \"DOUBLE(255,30)\", \"required\": true}],  \"type\" : \"INDEX\"}"));

      coll = CreateCollection("test");
      coll.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"INT\", \"array\": false}," +
                                                        "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}," +
                                                        "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL\", \"array\": true}," +
                                                        "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}," +
                                                        "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}," +
                                                        "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}]}");
      coll.Add("{\"intField\" : 12, \"uintField\" : [51,52,53], \"dateField\" : \"2019-1-1\", \"datetimeField\" : \"9999-12-31 23:59:59\", \"charField\" : \"abcd1\", \"binaryField\" : \"abcd1\", \"timeField\" : \"10.30\", \"decimalField\" : [51.2, 57.6, 55.8]}").Execute();
      coll.Add("{\"intField\" : 12, \"uintField\" : [51,52,53], \"dateField\" : \"2019-1-1\", \"datetimeField\" : \"9999-12-31 23:59:59\", \"charField\" : \"abcd1\", \"binaryField\" : \"abcd1\", \"timeField\" : \"10.30\", \"decimalField\" : [51.2, 57.6, 55.8]}").Execute();
      coll.Add("{\"intField\" : 12, \"uintField\" : [51,52,53], \"dateField\" : \"2019-1-1\", \"datetimeField\" : \"9999-12-31 23:59:59\", \"charField\" : \"abcd1\", \"binaryField\" : \"abcd1\", \"timeField\" : \"10.30\", \"decimalField\" : [51.2, 57.6, 55.8]}").Execute();
      coll.Add("{\"intField\" : 18, \"uintField\" : [51,52,53], \"dateField\" : \"2019-1-1\", \"datetimeField\" : \"9999-12-31 23:59:59\", \"charField\" : \"abcd1\", \"binaryField\" : \"abcd1\", \"timeField\" : \"10.30\", \"decimalField\" : [51.2, 57.6, 55.8]}").Execute();
      coll.Add("{\"intField\" : 18, \"uintField\" : [51,52,53], \"dateField\" : \"2019-1-1\", \"datetimeField\" : \"9999-12-31 23:59:59\", \"charField\" : \"abcd1\", \"binaryField\" : \"abcd1\", \"timeField\" : \"10.30\", \"decimalField\" : 57.6}").Execute();
      var cFind = coll.Find(":decimalField in $.decimalField");
      var docs = cFind.Bind("decimalField", 57.6).Execute();
      DbDoc doc = docs.FetchOne();
      var findStatement = coll.Find("57.6");
      var res1 = findStatement.Execute().FetchAll();
      Assert.AreEqual(5, res1.Count, "Matching the find count");

      coll = CreateCollection("test");

      // The Char size should be max 255
      var doc1 = new[] { new { _id = 10, name = "Char", myField = new[] { longval } } };
      coll.Add(doc1).Execute();
      Assert.Throws<MySqlException>(() => coll.CreateIndex("index_12", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(256)\", \"array\": false}]}"));

      coll = CreateCollection("test");
      doc1 = new[] { new { _id = 14, name = "Char", myField = new[] { longval } } };
      coll.Add(doc1).Execute();
      Assert.Throws<MySqlException>(() => coll.CreateIndex("index_14", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(513)\", \"array\": true}]}"));

      coll = CreateCollection("test");
      coll.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"SIGNED INTEGER\", \"array\": true}, " +
                          "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
                          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
                          "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
                          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
                          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}");
      coll.Add("{\"intField\" : [15,25,35], \"dateField\" : \"2019-1-1\", \"charField\" : \"abcd1\", \"timeField\" : \"10.30\"}").Execute();
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(coll.Add("{\"dateField\" : [\"2019-1-1\",\"2020-12-12\"], \"charField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], \"timeField\" : \"10.30\"}")));

      Assert.Throws<MySqlException>(() => ExecuteAddStatement(coll.Add("{\"dateField\" : \"2019-1-1\", \"charField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], \"timeField\" : [\"10.30\",\"5.20\"]}")));

      Assert.Throws<MySqlException>(() => ExecuteAddStatement(coll.Add("{\"datetimeField\" : [\"9999-12-31 23:59:59\",\"9999-12-31 23:59:59\"], \"charField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], \"timeField\" : \"10.30\"}")));

      Assert.Throws<MySqlException>(() => ExecuteAddStatement(coll.Add("{\"decimalField\" : [\"3.0\",\"19.45\"], \"charField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], \"timeField\" : \"10.30\"}")));

      coll = CreateCollection("test");
      coll.CreateIndex("multiArrayIndex1", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
            "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}");
      ValidateIndex("multiArrayIndex1", "test", "DATE", false, false, false, 1, null, true);
      ValidateIndex("multiArrayIndex1", "test", "CHAR(255)", false, false, false, 2, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DATETIME", false, false, false, 3, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DECIMAL(10,2)", false, false, false, 4, null, true);
      ValidateIndex("multiArrayIndex1", "test", "TIME", false, false, false, 5, null, true);

      coll = CreateCollection("test");
      coll.CreateIndex("multiArrayIndex1", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
            "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": true}, " +
          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}");
      ValidateIndex("multiArrayIndex1", "test", "DATE", false, false, false, 1, null, true);
      ValidateIndex("multiArrayIndex1", "test", "CHAR(255)", false, false, false, 2, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DATETIME", false, false, false, 3, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DECIMAL(10,2)", false, false, false, 4, null, true);
      ValidateIndex("multiArrayIndex1", "test", "TIME", false, false, false, 5, null, true);

      coll = CreateCollection("test");

      coll.CreateIndex("multiArrayIndex1", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
            "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": true}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}");
      ValidateIndex("multiArrayIndex1", "test", "DATE", false, false, false, 1, null, true);
      ValidateIndex("multiArrayIndex1", "test", "CHAR(255)", false, false, false, 2, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DATETIME", false, false, false, 3, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DECIMAL(10,2)", false, false, false, 4, null, true);
      ValidateIndex("multiArrayIndex1", "test", "TIME", false, false, false, 5, null, true);

      coll = CreateCollection("test");
      coll.CreateIndex("multiArrayIndex1", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}, " +
            "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": true}]}");
      ValidateIndex("multiArrayIndex1", "test", "DATE", false, false, false, 1, null, true);
      ValidateIndex("multiArrayIndex1", "test", "CHAR(255)", false, false, false, 2, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DATETIME", false, false, false, 3, null, true);
      ValidateIndex("multiArrayIndex1", "test", "DECIMAL(10,2)", false, false, false, 4, null, true);
      ValidateIndex("multiArrayIndex1", "test", "TIME", false, false, false, 5, null, true);
    }

    [Test, Description("Index Array Date bug with workaround")]
    public void IndexArrayBugWorkAround()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");
      var coll = CreateCollection("test");
      var doc = new[] { new { _id = 1, name = "Time", myField = "9:00:00" } };//Bug29692534
      var doc22 = new[] { new { _id = 2, name = "Time", myField = new[] { "01:01:01.001", "23:59:59.15", "12:00:00", "1" } } };//Bug29692534
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      var docResult = coll.Find("CAST(CAST('12:00:00' as TIME) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time");
      docResult = coll.Find("CAST(CAST('9:00:00' as TIME) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time");
      docResult = coll.Find("CAST(CAST(:dt as TIME) as JSON) in $.myField").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as TIME) as JSON) in $.myField").Bind("dt", "12:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "01:01:01.001").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Decimal", myField = "5" } };
      doc22 = new[] { new { _id = 2, name = "Decimal", myField = new[] { "3.0", "254.51", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Decimal" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('3.0' as DECIMAL) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal");
      docResult = coll.Find("CAST(CAST('5' as DECIMAL) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal");
      docResult = coll.Find("CAST(CAST(:dt as DECIMAL) as JSON) in $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as DECIMAL) as JSON) in $.myField").Bind("dt", "3.0").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 5).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 3.0).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Date", myField = "2019-01-04" } };
      doc22 = new[] { new { _id = 2, name = "Date", myField = new[] { "2019-01-01", "2019-01-01 12:15:00" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Date" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('2019-01-04' as DATE) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date");
      docResult = coll.Find("CAST(CAST('2019-01-01' as DATE) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date");
      docResult = coll.Find("CAST(CAST(:dt as DATE) as JSON) in $.myField").Bind("dt", "2019-01-04").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as DATE) as JSON) in $.myField").Bind("dt", "2019-01-01").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 2019 - 01 - 04).Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "2019-01-04").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "2019-01-01").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "DateTime", myField = "9999-02-02 01:20:33" } };
      doc22 = new[] { new { _id = 2, name = "DateTime", myField = new[] { "1000-01-01 00:00:00", "9999-12-31 23:59:59" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "DateTime" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('1000-01-01 00:00:00' as DATETIME) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime");
      docResult = coll.Find("CAST(CAST('9999-02-02 01:20:33' as DATETIME) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime");
      docResult = coll.Find("CAST(CAST(:dt as DATETIME) as JSON) in $.myField").Bind("dt", "9999-02-02 01:20:33").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as DATETIME) as JSON) in $.myField").Bind("dt", "9999-12-31 23:59:59").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "9999-02-02 01:20:33").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "1000-01-01 00:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime-With Bind-Bug");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Signed", myField = "100" } };
      doc22 = new[] { new { _id = 2, name = "Signed", myField = new[] { "3", "254", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Signed" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('100' as SIGNED) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED");
      docResult = coll.Find("CAST(CAST('3' as SIGNED) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED) as JSON) in $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED) as JSON) in $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 100).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "SIGNED INTEGER", myField = "100" } };
      doc22 = new[] { new { _id = 2, name = "SIGNED INTEGER", myField = new[] { "3", "254", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "SIGNED INTEGER" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('100' as SIGNED INTEGER) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER");
      docResult = coll.Find("CAST(CAST('3' as SIGNED INTEGER) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED INTEGER) as JSON) in $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED INTEGER) as JSON) in $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 100).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 3).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Unsigned", myField = "100" } };
      doc22 = new[] { new { _id = 2, name = "Unsigned", myField = new[] { "3", "254", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Unsigned" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('100' as UNSIGNED) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned");
      docResult = coll.Find("CAST(CAST('3' as UNSIGNED) as JSON) in $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned");
      docResult = coll.Find("CAST(CAST(:dt as UNSIGNED) as JSON) in $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as UNSIGNED) as JSON) in $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 100).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", 3).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Char", myField = "too@mail.com" } };
      doc22 = new[] { new { _id = 2, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}]}");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "too@mail.com").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-CHAR(128)-With Bind");
      docResult = coll.Find(":dt IN $.myField").Bind("dt", "foo@mail.com").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-CHAR(128)-With Bind");
    }

    /// <summary>
    /// Bug 29692534
    /// </summary>
    [Test, Description("Index Array Date bug with workaround")]
    public void IndexArrayWorkAroundOverlaps()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");
      var coll = CreateCollection("test");
      var doc = new[] { new { _id = 1, name = "Time", myField = "9:00:00" } };
      var doc22 = new[] { new { _id = 2, name = "Time", myField = new[] { "01:01:01.001", "23:59:59.15", "12:00:00", "1" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      var docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug before index creation");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug before index creation");
      docResult = coll.Find("CAST(CAST('01:01:01.001' as TIME) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the document ID-Time before index creation");
      docResult = coll.Find("CAST(CAST('01:01:01.001' as TIME) as JSON) not overlaps $.myField").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-Time before index creation");

      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Time" + "\", \"array\": true}]}");

      docResult = coll.Find("CAST(CAST('12:00:00' as TIME) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time");
      docResult = coll.Find("CAST(CAST('9:00:00' as TIME) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time");
      docResult = coll.Find("CAST(CAST(:dt as TIME) as JSON) overlaps $.myField").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as TIME) as JSON) overlaps $.myField").Bind("dt", "12:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "01:01:01.001").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "01:01:01.001").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug");
      docResult = coll.Find(":dt not IN $.myField").Bind("dt", "01:01:01.001").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug");
      docResult = coll.Find("CAST(CAST(:dt as TIME) as JSON) not overlaps $.myField").Bind("dt", "12:00:00").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind");

      coll = CreateCollection("test");

      doc = new[] { new { _id = 1, name = "Decimal", myField = "5" } };
      doc22 = new[] { new { _id = 2, name = "Decimal", myField = new[] { "3.0", "254.51", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DECIMAL-With Bind-Bug before index creation");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DECIMAL-With Bind-Bug before index creation");
      docResult = coll.Find("CAST(CAST('5' as DECIMAL) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the document ID-DECIMAL before index creation");
      docResult = coll.Find("CAST(CAST('5' as DECIMAL) as JSON) not overlaps $.myField").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-DECIMAL before index creation");

      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Decimal" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('3.0' as DECIMAL) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal");
      docResult = coll.Find("CAST(CAST('5' as DECIMAL) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal");
      docResult = coll.Find("CAST(CAST(:dt as DECIMAL) as JSON) overlaps $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as DECIMAL) as JSON) overlaps $.myField").Bind("dt", "3.0").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 5).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 3.0).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "5").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", 3.0).Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind-Bug");
      docResult = coll.Find("CAST(CAST(:dt as DECIMAL) as JSON) not overlaps $.myField").Bind("dt", "3.0").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-Decimal-With Bind");

      coll = CreateCollection("test");

      doc = new[] { new { _id = 1, name = "Date", myField = "2019-01-04" } };
      doc22 = new[] { new { _id = 2, name = "Date", myField = new[] { "2019-01-01", "2019-01-01 12:15:00" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Date" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('2019-01-04' as DATE) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date");
      docResult = coll.Find("CAST(CAST('2019-01-01' as DATE) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date");
      docResult = coll.Find("CAST(CAST(:dt as DATE) as JSON) overlaps $.myField").Bind("dt", "2019-01-04").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as DATE) as JSON) overlaps $.myField").Bind("dt", "2019-01-01").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 2019 - 01 - 04).Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "2019-01-04").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "2019-01-01").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "2019-01-01").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind-Bug");
      docResult = coll.Find("CAST(CAST(:dt as DATE) as JSON) not overlaps $.myField").Bind("dt", "2019-01-04").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");

      coll = CreateCollection("test");

      doc = new[] { new { _id = 1, name = "DateTime", myField = "9999-02-02 01:20:33" } };
      doc22 = new[] { new { _id = 2, name = "DateTime", myField = new[] { "1000-01-01 00:00:00", "9999-12-31 23:59:59" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "DateTime" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('1000-01-01 00:00:00' as DATETIME) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime");
      docResult = coll.Find("CAST(CAST('9999-02-02 01:20:33' as DATETIME) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime");
      docResult = coll.Find("CAST(CAST(:dt as DATETIME) as JSON) overlaps $.myField").Bind("dt", "9999-02-02 01:20:33").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as DATETIME) as JSON) overlaps $.myField").Bind("dt", "9999-12-31 23:59:59").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "9999-02-02 01:20:33").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "1000-01-01 00:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "1000-01-01 00:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-DateTime-With Bind-Bug");
      docResult = coll.Find("CAST(CAST(:dt as DATETIME) as JSON) not overlaps $.myField").Bind("dt", "9999-12-31 23:59:59").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-Date-With Bind");

      coll = CreateCollection("test");

      doc = new[] { new { _id = 1, name = "Signed", myField = "100" } };
      doc22 = new[] { new { _id = 2, name = "Signed", myField = new[] { "3", "254", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Signed" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('100' as SIGNED) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED");
      docResult = coll.Find("CAST(CAST('3' as SIGNED) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED) as JSON) overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED) as JSON) overlaps $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 100).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Signed-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED) as JSON) not overlaps $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID-SIGNED-With Bind");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "SIGNED INTEGER", myField = "100" } };
      doc22 = new[] { new { _id = 2, name = "SIGNED INTEGER", myField = new[] { "3", "254", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "SIGNED INTEGER" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('100' as SIGNED INTEGER) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER");
      docResult = coll.Find("CAST(CAST('3' as SIGNED INTEGER) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED INTEGER) as JSON) overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED INTEGER) as JSON) overlaps $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 100).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 3).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind-Bug");
      docResult = coll.Find("CAST(CAST(:dt as SIGNED INTEGER) as JSON) overlaps $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-SIGNED INTEGER-With Bind");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Unsigned", myField = "100" } };
      doc22 = new[] { new { _id = 2, name = "Unsigned", myField = new[] { "3", "254", "19" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "Unsigned" + "\", \"array\": true}]}");
      docResult = coll.Find("CAST(CAST('100' as UNSIGNED) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned");
      docResult = coll.Find("CAST(CAST('3' as UNSIGNED) as JSON) overlaps $.myField").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned");
      docResult = coll.Find("CAST(CAST(:dt as UNSIGNED) as JSON) overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind");
      docResult = coll.Find("CAST(CAST(:dt as UNSIGNED) as JSON) overlaps $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 100).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "100").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", 3).Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind-Bug");
      docResult = coll.Find("CAST(CAST(:dt as UNSIGNED) as JSON) overlaps $.myField").Bind("dt", "19").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Unsigned-With Bind");

      coll = CreateCollection("test");
      doc = new[] { new { _id = 1, name = "Char", myField = "too@mail.com" } };
      doc22 = new[] { new { _id = 2, name = "Char", myField = new[] { "foo@mail.com", "bar@mail.com", "qux@mail.com" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + "CHAR(128)" + "\", \"array\": true}]}");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "too@mail.com").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-CHAR(128)-With Bind");
      docResult = coll.Find(":dt overlaps $.myField").Bind("dt", "foo@mail.com").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-CHAR(128)-With Bind");
      docResult = coll.Find(":dt not overlaps $.myField").Bind("dt", "foo@mail.com").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-CHAR(128)-With Bind");
    }

    [Test, Description("Index using overlaps")]
    public void IndexArrayWithOverlaps()
    {
      var coll = CreateCollection("test");
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");

      var doc = new[] { new { _id = 1, name = "Time", myField1 = "9:00:00" } };
      var doc22 = new[] { new { _id = 2, name = "Time", myField2 = new[] { "01:01:01.001", "23:59:59.15", "12:00:00", "1" }, myField4 = new[] { "01:01:01.001", "23:59:59.15", "12:00:00", "1" } } };
      var doc33 = new[] { new { _id = 3, name = "Time", myField5 = "9:00:00", myField3 = new[] { "01:01:01.001", "23:59:59.15", "12:00:00", "1", "9:00:00" } } };
      var doc34 = new[] { new { _id = 4, name = "Time", myField5 = "8:00", myField3 = new[] { "02:01:01" } } };
      coll.Add(doc).Execute();
      coll.Add(doc22).Execute();
      coll.Add(doc33).Execute();
      coll.Add(doc34).Execute();

      var docResult = coll.Find(":dt overlaps $.myField1").Bind("dt", "9:00:00").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID-Time-With Bind-Bug before index creation");
      coll.CreateIndex("myIndex1", "{\"fields\": [{\"field\": $.myField1, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      coll.CreateIndex("myIndex2", "{\"fields\": [{\"field\": $.myField2, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      coll.CreateIndex("myIndex3", "{\"fields\": [{\"field\": $.myField3, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      coll.CreateIndex("myIndex4", "{\"fields\": [{\"field\": $.myField1, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      coll.CreateIndex("myIndex5", "{\"fields\": [{\"field\": $.myField3, \"type\":\"" + "Time" + "\", \"array\": true}]}");
      docResult = coll.Find("myIndex1 overlaps myIndex2").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("myIndex2 overlaps myIndex3").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("myIndex2 not overlaps myIndex3").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using not overlaps");
      docResult = coll.Find("myIndex1 overlaps myIndex4").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("'myIndex3' overlaps 'myIndex5'").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("$.myField3 overlaps $.myField2").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("$.myField1 overlaps $.myField3").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("$.myField2 overlaps $.myField4").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("$.myField2 not overlaps $.myField4").Execute();
      Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the indexes using not overlaps");
      docResult = coll.Find("$.myField5 overlaps $.myField3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the indexes using overlaps");
      docResult = coll.Find("$.myField5 not overlaps $.myField3").Execute();
      Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the indexes using not overlaps");
    }

    [Test, Description("multikey scenarios with observations")]
    public void IndexArrayMultiKey()
    {
      var coll = CreateCollection("test");
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");

      coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"CHAR(255)\", \"array\": false}]}");

      Collection testColl = CreateCollection("mycoll30");
      testColl.Add("{\"intField\" : [1,[2, 3],4], \"uintField\" : [51,[52, 53],54], \"dateField\" : [\"2019-1-1\", [\"2019-2-1\", \"2019-3-1\"], \"2019-4-1\"], " +
              "\"datetimeField\" : [\"9999-12-30 23:59:59\", [\"9999-12-31 23:59:59\"], \"9999-12-31 23:59:59\"], \"arrayField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], " +
              "\"binaryField\" : [\"abcd1\", [\"abcd1\", \"abcd2\"], \"abcd4\"],\"timeField\" : [\"10.30\", \"11.30\", \"12.30\"], \"floatField\" : [51.2,[52.4],53.6]}").Execute();

      testColl = CreateCollection("mycoll30");
      testColl.Add("{\"intField\" : [1,[2, 3],4], \"uintField\" : [51,[52, 53],54], \"dateField\" : [\"2019-1-1\", [\"2019-2-1\", \"2019-3-1\"], \"2019-4-1\"], " +
              "\"datetimeField\" : [\"9999-12-30 23:59:59\", [\"9999-12-31 23:59:59\"], \"9999-12-31 23:59:59\"], \"arrayField\" : [\"abcd1\", \"abcd1\", \"abcd2\", \"abcd4\"], " +
              "\"binaryField\" : [\"abcd1\", \"abcd2\", \"abcd3\", \"abcd4\"],\"timeField\" : [\"10.30\", \"11.30\", \"12.30\"], \"floatField\" : [51.2,[52.4],53.6]}").Execute();

      Assert.Throws<MySqlException>(() => coll.CreateIndex("multiArrayIndex", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"INT\", \"array\": false}," +
                                                        "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}," +
                                                        "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL\", \"array\": false}," +
                                                        "{\"field\": \"$.binaryField\", \"type\": \"BINARY\", \"array\": true}," +
                                                        "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}," +
                                                        "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}," +
                                                        "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}]}"));

      coll = CreateCollection("test");
      Assert.Throws<MySqlException>(() => coll.CreateIndex("multiArrayIndex2", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
          "{\"field\": \"$.binField\", \"type\": \"BINARY(128)\", \"array\": false}, " +
            "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => coll.CreateIndex("multiArrayIndex3", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
          "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
            "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
          "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
          "{\"field\": \"$.signedField\", \"type\": \"SIGNED\", \"array\": false}, " +
          "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => coll.CreateIndex("multiArrayIndex4", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
            "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
              "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
            "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
            "{\"field\": \"$.signedintField\", \"type\": \"SIGNED INTEGER\", \"array\": false}, " +
            "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => coll.CreateIndex("multiArrayIndex5", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
            "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
              "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
            "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
            "{\"field\": \"$.unsignedField\", \"type\": \"UNSIGNED\", \"array\": false}, " +
            "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));

      Assert.Throws<MySqlException>(() => coll.CreateIndex("multiArrayIndex6", "{\"fields\": [{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}, " +
            "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": true}, " +
              "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}, " +
            "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL(10,2)\", \"array\": false}, " +
            "{\"field\": \"$.unsignedintField\", \"type\": \"UNSIGNED INTEGER\", \"array\": false}, " +
            "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}]}"));
    }

    [Test, Description("Test MySQLX plugin Create Collection Multiple Index Type")]
    public void CreateCollectionMultipleIndexDataType()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      Collection testColl = CreateCollection("test");
      Assert.AreEqual(true, testColl.ExistsInDatabase(), "ExistsInDatabase failed");

      testColl.CreateIndex("testIndex12", "{\"fields\": [ { \"field\":$.myId, \"type\":\"DOUBLE\" , \"required\":true} ] }");
      testColl.CreateIndex("testIndex", "{\"fields\": [ { \"field\":$.myId, \"type\":\"DOUBLE UNSIGNED\" , \"required\":true} ] }");
      testColl.CreateIndex("testIndex1", "{\"fields\": [ { \"field\":$.myAge, \"type\":\"BIGINT UNSIGNED\" , \"required\":true} ] }");

      Assert.Throws<MySqlException>(() => testColl.Add(
          new { myId = 990.196078431, myAge = 10000000 }).Add(new { myId = -990.196078431, myAge = -10000000 }).Execute());

      testColl.Add(new { myId = 990.196078431, myAge = 10000000 }).Execute();
      testColl.DropIndex("testIndex");
      var result = testColl.Add(new { myId = 990.196078431, myAge = 10000000 }).Execute();
      Assert.AreEqual(1, result.AffectedItemsCount);

      testColl.DropIndex("testIndex1");
      result = testColl.Add(new { myId = 990.196078431, myAge = 10000000 }).Execute();
      Assert.AreEqual(1, result.AffectedItemsCount);
      Assert.Throws<MySqlException>(() => testColl.CreateIndex("testIndex2", "{\"fields\": [ { \"field\":$.myName, \"type\":\"TEXT\" , \"required\":true} ] }"));
    }

    [Test, Description("Test MySQLX plugin Create Collection Multiple Index Stress")]
    public void CreateCollectionMultipleIndexStress()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      Collection testColl = CreateCollection("test");
      Assert.AreEqual(true, testColl.ExistsInDatabase(), "ExistsInDatabase failed");

      testColl.CreateIndex("testIndex", "{\"fields\": [ { \"field\":$.myId, \"type\":\"TINYINT UNSIGNED\" , \"required\":true} ] }");
      testColl.CreateIndex("testIndex1", "{\"fields\": [ { \"field\":$.myAge, \"type\":\"SMALLINT UNSIGNED\" , \"required\":true} ] }");
      testColl.Add(new { myId = 126, myAge = 255 }).Add(new { myId = 1, myAge = 129 }).Execute();
      testColl.Add(new { myId = 126, myAge = 255 }).Execute();
      testColl.DropIndex("testIndex");
      testColl.Add(new { myId = 126, myAge = 255 }).Execute();

      for (int i = 0; i < 20; i++)
      {
        testColl.DropIndex("testIndex1");
        testColl.CreateIndex("testIndex1", "{\"fields\": [ { \"field\":$.myAge, \"type\":\"DECIMAL\" , \"required\":true} ] }");
        var result = testColl.Add(new { myAge = i }).Add(new { myAge = i + 99999 }).Execute();
      }
    }

    #endregion WL14389

    #region Methods

    private void InvalidCreateArrayIndex(string dataType, Collection collection, object doc = null)
    {
      collection = CreateCollection("test");
      Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + dataType + "\", \"array\": true}]}"));
    }
    private void CreateArrayIndex(string dataType, XDevAPI.Collection collection, bool isIndexCreated = false)
    {
      if (!isIndexCreated)
      {
        collection.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"" + dataType + "\", \"array\": true}]}");
      }
      if (dataType.CompareTo("SIGNED INTEGER") == 0)
      {
        dataType = "SIGNED";
      }
      if (dataType.CompareTo("UNSIGNED INTEGER") == 0)
      {
        dataType = "UNSIGNED";
      }
      ValidateIndex("myIndex", "test", dataType, false, false, false, 1, null, true);
      collection.DropIndex("myIndex");
      collection.RemoveOne("1");
    }

    private void ValidateIndex(string fieldName, string collectionName, string dataType, bool unique, bool required, bool isUnsigned, int sequence, int? length = null, bool array = false)
    {
      bool indexFound = false;
      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        var command = new MySqlCommand($"SHOW INDEX FROM `test`.`{collectionName}`", connection);
        var reader = command.ExecuteReader();
        if (!reader.HasRows)
          throw new Exception("No indexes found.");

        while (reader.Read())
        {
          if (fieldName == reader["Key_name"].ToString())
          {
            if (sequence != Convert.ToInt32(reader["Seq_in_index"]))
              continue;

            indexFound = true;
            Assert.AreEqual(collectionName, reader["Table"]);
            Assert.AreEqual(unique ? 0 : 1, Convert.ToInt16(reader["Non_unique"]));

            if (!array && !string.IsNullOrEmpty(reader["Column_name"].ToString()))
            {
              var columnNameTokens = reader["Column_name"].ToString().Split('_');
              Assert.AreEqual(dataType, isUnsigned ? string.Format("{0}_{1}", columnNameTokens[1], columnNameTokens[2]) : columnNameTokens[1]);
            }
            else if (array && !string.IsNullOrEmpty(reader["Expression"].ToString()))
            {
              string expression = reader["Expression"].ToString();
              int pos = reader["Expression"].ToString().IndexOf(" as ");
              expression = expression.Substring(pos + 4);
              StringAssert.Contains("array", expression);
              expression = expression.Substring(0, expression.IndexOf(" array"));
              Assert.That(dataType, Is.EqualTo(expression.Replace(" ", string.Empty)).IgnoreCase);
            }

            Assert.AreEqual(required ? "" : "YES", reader["Null"]);
            if (length != null)
              Assert.AreEqual(length, Convert.ToInt32(reader["Sub_part"]));
            break;
          }
        }

        if (!indexFound)
          throw new Exception("Index not found.");
      }
    }

    #endregion Methods
  }
}