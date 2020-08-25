// Copyright (c) 2015, 2020 Oracle and/or its affiliates.
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

using System.Collections.Generic;
using MySqlX.XDevAPI;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System;
using MySqlX.XDevAPI.Common;

namespace MySqlX.Data.Tests
{
  public class CollectionTests : BaseTest
  {
    [Test]
    public void GetAllCollections()
    {
      Collection book = CreateCollection("book");
      List<Collection> collections = book.Schema.GetCollections();
      Assert.That(collections, Has.One.Items);
      Assert.True(collections[0].Name == "book");
    }

    [Test]
    public void CreateAndDropCollection()
    {
      Session s = GetSession();
      Schema test = s.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      Assert.True(CollectionExistsInDatabase(testColl));

      // Drop existing collection.
      test.DropCollection("test");
      Assert.False(CollectionExistsInDatabase(testColl));

      // Drop non-existing collection.
      test.DropCollection("test");
      Assert.False(CollectionExistsInDatabase(testColl));

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => test.DropCollection(string.Empty));
      Assert.Throws<ArgumentNullException>(() => test.DropCollection(" "));
      Assert.Throws<ArgumentNullException>(() => test.DropCollection("  "));
      Assert.Throws<ArgumentNullException>(() => test.DropCollection(null));
    }

    [Test]
    public void CreateCollectionIndex()
    {
      Session session = GetSession();
      Schema test = session.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      Assert.True(CollectionExistsInDatabase(testColl), "ExistsInDatabase failed");
      testColl.CreateIndex("testIndex", "{ \"fields\": [ { \"field\":$.myId, \"type\":\"INT\", \"required\":true } ] }");
      var result = ExecuteAddStatement(testColl.Add(new { myId = 1 }).Add(new { myId = 2 }));
      Assert.AreEqual(result.AffectedItemsCount, 2);
    }

    [Test]
    public void DropCollectionIndex()
    {
      Session session = GetSession();
      Schema test = session.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      testColl.CreateIndex("testIndex", "{ \"fields\": [ { \"field\":$.myId, \"type\":\"INT\", \"required\":true } ] }");

      // Drop existing index.
      testColl.DropIndex("testIndex");

      // Drop non-existing index.
      testColl.DropIndex("testIndex");

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(string.Empty));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(" "));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex("  "));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(null));
    }

    [Test]
    public void ValidateExistence()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema("test");
      var ex = Assert.Throws<MySqlException>(() => schema.GetCollection("nonExistentCollection", true));
      Assert.AreEqual("Collection 'nonExistentCollection' does not exist.", ex.Message);
    }

    [Test]
    public void CountCollection()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema("test");
      schema.CreateCollection("testCount");
      var count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];

      // Zero records
      var collection = schema.GetCollection("testCount");
      Assert.AreEqual(count, collection.Count());
      var table = schema.GetTable("testCount");
      Assert.AreEqual(count, table.Count());

      // Insert some records
      var stm = collection.Add(@"{ ""_id"": 1, ""foo"": 1 }")
        .Add(@"{ ""_id"": 2, ""foo"": 2 }")
        .Add(@"{ ""_id"": 3, ""foo"": 3 }");
      stm.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      table.Insert("doc").Values(@"{ ""_id"": 4, ""foo"": 4 }").Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      Assert.AreEqual(count, table.Count());

      collection.RemoveOne(2);
      count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
      Assert.AreEqual(count, table.Count());

      // Collection/Table does not exist
      var ex = Assert.Throws<MySqlException>(() => schema.GetCollection("testCount_").Count());
      Assert.AreEqual("Collection 'testCount_' does not exist in schema 'test'.", ex.Message);
      ex = Assert.Throws<MySqlException>(() => schema.GetTable("testCount_").Count());
      Assert.AreEqual("Table 'testCount_' does not exist in schema 'test'.", ex.Message);
    }

    [Test]
    public void ModifyCollectionNoLevelNorSchema()
    {
      Session s = GetSession();
      Schema test = s.GetSchema("test");

      // Modify a Collection without passing schema and Level, (Bug#30660917)
      test.CreateCollection("coll");
      ModifyCollectionOptions options1 = new ModifyCollectionOptions();
      options1.Validation = new Validation() { };
      Assert.Throws<MySqlException>(() => test.ModifyCollection("coll", options1));
    }

    [Test]
    public void CreateCollectionWithOptions()
    {
      Session s = GetSession();
      Schema test = s.GetSchema("test");

      // CreateCollection Test Cases

      // Create a Collection passing a valid schema and Level
      CreateCollectionOptions options = new CreateCollectionOptions();
      Validation val = new Validation();
      val.Level = ValidationLevel.STRICT;
      string str = "{\"id\": \"http://json-schema.org/geo\","
             + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
             + "\"description\": \"A geographical coordinate\","
             + "\"type\": \"object\","
             + "\"properties\": {"
             + "\"latitude\": {"
             + "\"type\": \"number\""
             + " },"
             + "\"longitude\": {"
             + "\"type\": \"number\""
             + "}"
             + "},"
             + "\"required\": [\"latitude\", \"longitude\"]"
             + "}";
      val.Schema = str;
      options.ReuseExisting = false;
      options.Validation = val;
      Collection testColl = test.CreateCollection("testWithSchemaValidation", options);
      Assert.True(CollectionExistsInDatabase(testColl));

      //Bug #30830962
      options = new CreateCollectionOptions();
      val = new Validation() { };
      options.Validation = val;
      var testbug1 = test.CreateCollection("bug_0962", options);  //create collection with empty options
      testbug1.Add(@"{ ""latitude"": 20, ""longitude"": 30 }").Execute();
      testbug1.Add(@"{ ""sexo"": 1, ""edad"": 20 }").Execute();
      int.TryParse(session.SQL("SELECT COUNT(*) FROM test.bug_0962").Execute().FetchOne()[0].ToString(), out int expected_count);
      Assert.AreEqual(2, expected_count);  //Collection is created as STRICT with empty json schema,both records were inserted

      options = new CreateCollectionOptions();
      val = new Validation() { Schema = str };
      options.Validation = val;
      testbug1 = test.CreateCollection("bug_0962b", options);// adding an schema from
      testbug1.Add(@"{ ""latitude"": 20, ""longitude"": 30 }").Execute();
      var invalidEx = Assert.Throws<MySqlException>(() => testbug1.Add(@"{ ""sexo"": 1, ""edad"": 20 }").Execute());
      StringAssert.Contains("Document is not valid according to the schema assigned to collection", invalidEx.Message);

      // Create a Collection passing a reuse_existing parameter to server
      CreateCollectionOptions options_reuse = new CreateCollectionOptions();
      options_reuse.ReuseExisting = false;
      options_reuse.Validation = val;
      Collection testCol2 = test.CreateCollection("testReuseExisting_1", options_reuse);
      Assert.True(CollectionExistsInDatabase(testCol2));

      //Insert Valid record with Level Strict
      var insert_statement = testColl.Add(@"{ ""latitude"": 20, ""longitude"": 30 }");
      insert_statement.Execute();
      var count = session.SQL("SELECT COUNT(*) FROM test.testWithSchemaValidation").Execute().FetchOne()[0];
      Assert.AreEqual(count, testColl.Count());

      //Insert invalid record with Level Strict
      insert_statement = testColl.Add(@"{ ""OtherField"": ""value"", ""Age"": 30 }");
      var invalidInsertEx = Assert.Throws<MySqlException>(() => insert_statement.Execute());
      StringAssert.Contains("Document is not valid according to the schema assigned to collection", invalidInsertEx.Message);

      //Test: Old MySQL Server Version exceptions
      if (!(session.Version.isAtLeast(8, 0, 19)))
      {
        //FR6.2
        var ex1 = Assert.Throws<MySqlException>(() => test.CreateCollection("testInvalid", options));
        StringAssert.Contains("Invalid number of arguments, expected 2 but got 3, " +
        "The server doesn't support the requested operation. Please update the MySQL Server and/or Client library", ex1.Message);

        //FR6.3
        test.CreateCollection("testInvalid");
        ModifyCollectionOptions modifyOptions = new ModifyCollectionOptions();
        modifyOptions.Validation = val;
        var ex2 = Assert.Throws<MySqlException>(() => test.ModifyCollection("testInvalid", modifyOptions));
        StringAssert.Contains("Invalid mysqlx command modify_collection_options, " +
        "The server doesn't support the requested operation. Please update the MySQL Server and/or Client library", ex2.Message);
      }

      //Create collection with json schema and level OFF. Try to insert document matches this schema
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Level = ValidationLevel.OFF, Schema = str };
      Collection col_test = test.CreateCollection("Test_2b_1", options);
      Assert.True(CollectionExistsInDatabase(col_test));
      insert_statement = col_test.Add(@"{ ""latitude"": 120, ""longitude"": 78 }");
      insert_statement.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.Test_2b_1").Execute().FetchOne()[0];
      Assert.AreEqual(count, col_test.Count());

      //Create collection with json schema and level OFF,ReuseExisting set to true, Try to insert
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Level = ValidationLevel.OFF, Schema = str };
      options.ReuseExisting = true;
      col_test = test.CreateCollection("Test_2b_2", options);
      Assert.True(CollectionExistsInDatabase(col_test));
      insert_statement = col_test.Add(@"{ ""latitude"": 20, ""longitude"": 42 }");
      insert_statement.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.Test_2b_2").Execute().FetchOne()[0];
      Assert.AreEqual(count, col_test.Count());

      //Create collection with only schema option, Try to insert
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Schema = str };
      col_test = test.CreateCollection("Test_2b_3", options);
      Assert.True(CollectionExistsInDatabase(col_test));
      insert_statement = col_test.Add(@"{ ""latitude"": 5, ""longitude"": 10 }");
      insert_statement.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.Test_2b_3").Execute().FetchOne()[0];
      Assert.AreEqual(count, col_test.Count());

      //Create collection with only schema option,ReuseExisting set to true, Try to insert
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Schema = str };
      options.ReuseExisting = true;
      col_test = test.CreateCollection("Test_2b_4", options);
      Assert.True(CollectionExistsInDatabase(col_test));
      insert_statement = col_test.Add(@"{ ""latitude"": 25, ""longitude"": 52 }");
      insert_statement.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.Test_2b_4").Execute().FetchOne()[0];
      Assert.AreEqual(count, col_test.Count());

      //Create collection with only level option
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Level = ValidationLevel.OFF };
      col_test = test.CreateCollection("Test_2b_5", options);
      Assert.True(CollectionExistsInDatabase(col_test));

      //ResuseExisting = false should throw exception for an existing collection
      CreateCollectionOptions testReuseOptions = new CreateCollectionOptions();
      testReuseOptions.ReuseExisting = false;
      testReuseOptions.Validation = new Validation() { Level = ValidationLevel.OFF };
      test.CreateCollection("testReuse");
      var exreuse = Assert.Throws<MySqlException>(() => test.CreateCollection("testReuse", testReuseOptions));
      Assert.AreEqual("Table 'testreuse' already exists", exreuse.Message);

      //Test: Resuse Existing = True should return existing collection
      testReuseOptions.ReuseExisting = true;
      var existing = test.CreateCollection("testReuse", testReuseOptions);
      Assert.True(CollectionExistsInDatabase(existing));

      //Create collection and prepare test data with json schema and level STRICT
      CreateCollectionOptions prepareOptions = new CreateCollectionOptions();
      prepareOptions.Validation = new Validation() { Level = ValidationLevel.STRICT, Schema = str };
      var res_stm = test.CreateCollection("TestCreateInsert", prepareOptions).Add(@"{ ""latitude"": 25, ""longitude"": 52 }");
      res_stm.Execute();
      var num = session.SQL("SELECT COUNT(*) FROM test.TestCreateInsert").Execute().FetchOne()[0];
      var collection_test = test.GetCollection("TestCreateInsert");
      Assert.AreEqual(num, collection_test.Count());

      //Passing invalid Schema
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Level = ValidationLevel.STRICT, Schema = "Not Valid JSON Schema" };
      Exception ex_schema = Assert.Throws<Exception>(() => test.CreateCollection("testInvalidSchema", options));
      StringAssert.Contains(@"The value provided is not a valid JSON document.", ex_schema.Message);

      //Testing an schema with different data types
      str = "{\"id\": \"http://json-schema.org/geo\","
             + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
             + "\"description\": \"A Person example\","
             + "\"type\": \"object\","
             + "\"properties\": {"
             + "\"name\": {"
             + "\"type\": \"string\""
             + " },"
             + "\"age\": {"
             + "\"type\": \"number\""
             + "}"
             + "},"
             + "\"required\": [\"name\", \"age\"]"
             + "}";
      options = new CreateCollectionOptions();
      options.Validation = new Validation() { Level = ValidationLevel.STRICT, Schema = str };
      Collection person_col = test.CreateCollection("testWithPersonSchema", options);
      Assert.True(CollectionExistsInDatabase(person_col));
      person_col.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      var rows = session.SQL("SELECT COUNT(*) FROM test.testWithPersonSchema").Execute().FetchOne()[0];
      Assert.AreEqual(rows, person_col.Count());

      // Create an existing collection with different schema
      options = new CreateCollectionOptions();
      options.ReuseExisting = true;
      options.Validation = new Validation() { Level = ValidationLevel.STRICT, Schema = str };
      Collection col_schema1 = test.CreateCollection("testSchema1", options);
      Assert.True(CollectionExistsInDatabase(col_schema1));

      col_schema1.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      Assert.AreEqual(1, col_schema1.Count());
      var sqlDefinition1 = session.SQL("SHOW CREATE TABLE test.testSchema1").Execute().FetchOne()[1];
      

      var schema2 = "{\"id\": \"http://json-schema.org/geo\","
            + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
            + "\"description\": \"A Movies example\","
            + "\"type\": \"object\","
            + "\"properties\": {"
            + "\"title\": {"
            + "\"type\": \"string\""
            + " },"
            + "\"movie\": {"
            + "\"type\": \"string\""
            + "}"
            + "},"
            + "\"required\": [\"title\", \"movie\"]"
            + "}";
      options.Validation = new Validation() { Level = ValidationLevel.STRICT, Schema = schema2 };
      Collection col_schema2 = test.CreateCollection("testSchema1", options);
      var sqlDefinition2 = session.SQL("SHOW CREATE TABLE test.testSchema1").Execute().FetchOne()[1];
      Assert.AreEqual(sqlDefinition1, sqlDefinition2);

      // Tests for original method CreateCollection
      //Create a collection without sending reuseExisting parameter and insert record
      Collection original_col1 = test.CreateCollection("testOriginal1");
      Assert.True(CollectionExistsInDatabase(original_col1));
      original_col1.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      rows = session.SQL("SELECT COUNT(*) FROM test.testOriginal1").Execute().FetchOne()[0];
      Assert.AreEqual(rows, original_col1.Count());

      //Create a new collection sending reuseExisting as true, insert record
      Collection original_col2 = test.CreateCollection("testOriginal2",true);
      Assert.True(CollectionExistsInDatabase(original_col2));
      original_col2.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      rows = session.SQL("SELECT COUNT(*) FROM test.testOriginal2").Execute().FetchOne()[0];
      Assert.AreEqual(rows, original_col2.Count());

      //Create an existing collection sending reuseExisting as true, insert record
      Collection original_col3 = test.CreateCollection("testOriginal2",true);
      Assert.True(CollectionExistsInDatabase(original_col3));
      original_col3.Add(@"{ ""name"": ""John2"", ""age"": 12 }").Execute();
      Assert.AreEqual(2, original_col3.Count());

      //Create an existing collection sending reuseExisting as false,exception expected
      var ex_existing = Assert.Throws<MySqlException>(() => test.CreateCollection("testOriginal2", false));
      Assert.AreEqual(@"Table 'testoriginal2' already exists", ex_existing.Message);

      //ModifyCollection Test Cases

      //Modify collection with only level option
      ModifyCollectionOptions Test_Options = new ModifyCollectionOptions();
      Test_Options.Validation = new Validation() { Level = ValidationLevel.OFF };
      Collection col_Test_2a_1 = test.ModifyCollection("testWithSchemaValidation", Test_Options);

      // Inser valid and invalid records with level set to Off
      insert_statement = col_Test_2a_1.Add(@"{ ""latitude"": 20, ""longitude"": 30 }")
                                     .Add(@"{ ""OtherField"": ""value"", ""Age"": 30 }");
      insert_statement.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.testWithSchemaValidation").Execute().FetchOne()[0];
      Assert.AreEqual(count, col_Test_2a_1.Count());

      //Modify collection with only schema option
      Test_Options.Validation = new Validation() { Schema = "{ }" };
      test.ModifyCollection("testWithSchemaValidation", Test_Options);
      var sqlCreate = session.SQL("SHOW CREATE TABLE test.testWithSchemaValidation").Execute().FetchOne()[1];
      Assert.True(sqlCreate.ToString().Contains(@"'{\r\n}'") || sqlCreate.ToString().Contains("{}"));

      //Passing null as parameter to ModifyCollection
      var emptyOptions = new ModifyCollectionOptions();
      emptyOptions.Validation = new Validation() { };
      test.CreateCollection("testnull");
      exreuse = Assert.Throws<MySqlException>(() => test.ModifyCollection("testnull", null));
      Assert.AreEqual(@"Arguments value used under ""validation"" must be an object with at least one field", exreuse.Message);
    }

    /// <summary>
    /// Server Bug
    /// Bug #31667405 - INCORRECT PREPARED STATEMENT OUTCOME WITH NUMERIC STRINGS IN JSON
    /// </summary>
    [Test]
    public void PreparedStatementWithNumericStrings()
    {
      Collection coll = CreateCollection("test");
      object[] _docs = new[]
      {
      new {  _id = "1", title = "foo" },
      new {  _id = "2", title = "bar" }
      };

      ExecuteAddStatement(coll.Add(_docs));

      var stmt = coll.Find("_id=:v").Bind("v", "1");
      var res = stmt.Execute();
      var values = res.FetchOne();
      Assert.AreEqual(1, Convert.ToInt32(values.values["_id"]));
      StringAssert.AreEqualIgnoringCase("foo", values.values["title"].ToString());

      res = stmt.Bind("v", "2").Execute();
      values = res.FetchOne();
      Assert.AreEqual(2, Convert.ToInt32(values.values["_id"]));
      StringAssert.AreEqualIgnoringCase("bar", values.values["title"].ToString());
    }
  }
}