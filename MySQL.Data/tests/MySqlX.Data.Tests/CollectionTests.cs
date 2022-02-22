// Copyright (c) 2015, 2022, Oracle and/or its affiliates.
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
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class CollectionTests : BaseTest
  {
    [TearDown]
    public void tearDown()
    {
      session.Schema.DropCollection("test");
      session.Schema.DropCollection("test123");
      session.Schema.DropCollection("testcount");
      session.Schema.DropCollection("coll");
      session.Schema.DropCollection("col20");
      session.Schema.DropCollection("col21");
      session.Schema.DropCollection("col22");
    }

    [Test]
    public void GetAllCollections()
    {
      session.DropSchema("test");
      session.CreateSchema("test");
      Collection book = CreateCollection("book");
      List<Collection> collections = book.Schema.GetCollections();
      Assert.That(collections, Has.One.Items);
      Assert.True(collections[0].Name == "book");
      book.Schema.DropCollection("book");
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

      //dropCollection when the object to drop contains invalid characters
      test.DropCollection("%^&!@~*(&*(*&:><?}{:");

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
      Schema test = session.GetSchema(schemaName);
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
      Schema test = session.GetSchema(schemaName);
      Collection testColl = CreateCollection("test");
      testColl.CreateIndex("testIndex", "{ \"fields\": [ { \"field\":$.myId, \"type\":\"INT\", \"required\":true } ] }");

      // Drop existing index.
      testColl.DropIndex("testIndex");

      // Drop non-existing index.
      testColl.DropIndex("testIndex");

      //dropIndex contains invalid characters
      testColl.DropIndex("%^&!@~*(&*(*&:><?}{:");

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(string.Empty));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(" "));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex("  "));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(null));

    }

    [Test]
    public void DropSchemaTests()
    {
      session.DropSchema("validSchema");
      session.CreateSchema("validSchema");
      session.DropSchema("validSchema");
      session.DropSchema("%^&!@~*(&*(*&:><?}{:");

      Assert.Throws<ArgumentNullException>(() => session.DropSchema(string.Empty));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(" "));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema("  "));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(null));
    }


    [Test]
    public void ValidateExistence()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema(schemaName);
      var ex = Assert.Throws<MySqlException>(() => schema.GetCollection("nonExistentCollection", true));
      Assert.AreEqual("Collection 'nonExistentCollection' does not exist.", ex.Message);
    }

    [Test]
    public void CountCollection()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema(schemaName);
      CreateCollection("testCount");
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
      StringAssert.AreEqualIgnoringCase("Table 'testReuse' already exists", exreuse.Message);

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

      //Create a collection without sending reuseExisting parameter and insert record
      Collection original_col1 = test.CreateCollection("testOriginal1");
      Assert.True(CollectionExistsInDatabase(original_col1));
      original_col1.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      rows = session.SQL("SELECT COUNT(*) FROM test.testOriginal1").Execute().FetchOne()[0];
      Assert.AreEqual(rows, original_col1.Count());

      //Create a new collection sending reuseExisting as true, insert record
      Collection original_col2 = test.CreateCollection("testOriginal2", true);
      Assert.True(CollectionExistsInDatabase(original_col2));
      original_col2.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      rows = session.SQL("SELECT COUNT(*) FROM test.testOriginal2").Execute().FetchOne()[0];
      Assert.AreEqual(rows, original_col2.Count());

      //Create an existing collection sending reuseExisting as true, insert record
      Collection original_col3 = test.CreateCollection("testOriginal2", true);
      Assert.True(CollectionExistsInDatabase(original_col3));
      original_col3.Add(@"{ ""name"": ""John2"", ""age"": 12 }").Execute();
      Assert.AreEqual(2, original_col3.Count());

      //Create an existing collection sending reuseExisting as false,exception expected
      var ex_existing = Assert.Throws<MySqlException>(() => test.CreateCollection("testOriginal2", false));
      StringAssert.AreEqualIgnoringCase(@"Table 'testOriginal2' already exists", ex_existing.Message);

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

      test.DropCollection("testWithSchemaValidation");
      test.DropCollection("bug_0962");
      test.DropCollection("bug_0962b");
      test.DropCollection("testReuseExisting_1");
      test.DropCollection("Test_2b_1");
      test.DropCollection("Test_2b_2");
      test.DropCollection("Test_2b_3");
      test.DropCollection("Test_2b_4");
      test.DropCollection("Test_2b_5");
      test.DropCollection("testReuse");
      test.DropCollection("TestCreateInsert");
      test.DropCollection("testWithPersonSchema");
      test.DropCollection("testSchema1");
      test.DropCollection("testOriginal1");
      test.DropCollection("testOriginal2");
      test.DropCollection("testnull");
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

      res = coll.Find("_id=:v").Bind("v", "2").Execute();
      values = res.FetchOne();
      Assert.AreEqual(2, Convert.ToInt32(values.values["_id"]));
      StringAssert.AreEqualIgnoringCase("bar", values.values["title"].ToString());
    }

    [Test, Description("Verify Count method for Tables,Collections,Collection As Table,Views with different combinations")]
    public void AdditionalCountTests()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema(schemaName);
      CreateCollection("testCount");
      var count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      var collection = schema.GetCollection("testCount");
      var collectionAsTable = schema.GetCollectionAsTable("testCount");
      Assert.AreEqual(count, collection.Count());
      Assert.AreEqual(count, collectionAsTable.Count());

      session.SQL($"USE {schemaName}").Execute();
      session.SQL("create table test1(name VARCHAR(40), age INT)").Execute();
      count = session.SQL($"SELECT COUNT(*) FROM {schemaName}.test1").Execute().FetchOne()[0];
      Table table = session.GetSchema(schemaName).GetTable("test1");
      Assert.AreEqual(count, collectionAsTable.Count());

      var result = table.Insert("name", "age")
        .Values("MARK", "34")
        .Values("richie", "16")
        .Values("TEST", "50")
       .Execute();

      Assert.AreEqual((ulong)3, result.AffectedItemsCount);
      var selectResult = table.Select().Execute();
      while (selectResult.Next()) ;
      Assert.AreEqual(3, selectResult.Rows.Count);
      Assert.AreEqual("MARK", selectResult.Rows.ToArray()[0][0].ToString());
      count = session.SQL($"SELECT COUNT(*) FROM {schemaName}.test1").Execute().FetchOne()[0];
      Assert.AreEqual(count, table.Count());

      // Insert some records
      var stm = collection.Add(@"{ ""_id"": 1, ""foo"": 1 }")
        .Add(@"{ ""_id"": 2, ""foo"": 2 }")
        .Add(@"{ ""_id"": 3, ""foo"": 3 }");
      stm.Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
      collectionAsTable = schema.GetCollectionAsTable("testCount");
      Assert.AreEqual(count, collectionAsTable.Count());

      table = schema.GetTable("testCount");
      table.Insert("doc").Values(@"{ ""_id"": 4, ""foo"": 4 }").Execute();
      count = session.SQL($"SELECT COUNT(*) FROM {schemaName}.testCount").Execute().FetchOne()[0];
      Assert.AreEqual(count, table.Count());

      collection.RemoveOne(2);
      count = session.SQL($"SELECT COUNT(*) FROM {schemaName}.testCount").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
      Assert.AreEqual(count, table.Count());

      // Collection/Table does not exist
      Assert.Throws<MySqlException>(() => schema.GetCollection("testCount_").Count());
      Assert.Throws<MySqlException>(() => schema.GetTable("testCount_").Count());

      session.SQL("DROP TABLE IF EXISTS test1").Execute();
      session.SQL("CREATE TABLE test1(id1 int,firstname varchar(20))").Execute();
      session.SQL("INSERT INTO test1 values ('1','Rob')").Execute();
      session.SQL("INSERT INTO test1 values ('2','Steve')").Execute();
      session.SQL("CREATE TABLE test2(id2 int,lastname varchar(20))").Execute();
      session.SQL("INSERT INTO test2 values ('1','Williams')").Execute();
      session.SQL("INSERT INTO test2 values ('2','Waugh')").Execute();
      session.SQL("CREATE VIEW view1 AS select * from test1").Execute();
      session.SQL("SELECT * FROM view1").Execute();
      session.SQL("CREATE VIEW view2 AS select * from test2").Execute();
      session.SQL("SELECT * FROM view2").Execute();
      count = session.SQL("SELECT COUNT(*) FROM view1").Execute().FetchOne()[0];
      Assert.AreEqual(count, schema.GetTable("view1").Count());
      schema.DropCollection("testCount");
      session.SQL("DROP TABLE IF EXISTS test1").Execute();
      session.SQL("DROP TABLE IF EXISTS test2").Execute();
    }

    [Test, Description("Verify Expected exceptions in Count")]
    public void ExceptionsInCount()
    {
      if (!session.Version.isAtLeast(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      var coll = CreateCollection("testCount");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20},
        new {_id = 2, title = "Book 2", pages = 30},
        new {_id = 3, title = "Book 3", pages = 40},
        new {_id = 4, title = "Book 4", pages = 50}
      };
      var r = coll.Add(docs).Execute();
      var count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      Schema schema = session.GetSchema(schemaName);
      var collection = schema.GetCollection("testCount");
      Assert.AreEqual(4, collection.Count());

      coll.Add(new { _id = 5, title = "Book 5", pages = 60 }).Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      schema = session.GetSchema(schemaName);
      collection = schema.GetCollection("testCount");
      Assert.AreEqual(5, collection.Count());

      Table table = session.GetSchema("test").GetTable("testCount");
      Assert.AreEqual(5, table.Count());

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => coll.RemoveOne(null));
      Assert.Throws<ArgumentNullException>(() => coll.RemoveOne(""));
      Assert.Throws<ArgumentNullException>(() => coll.RemoveOne(string.Empty));
      Assert.Throws<ArgumentNullException>(() => coll.RemoveOne(" "));

      // Remove sending numeric parameter.
      Assert.AreEqual(1, coll.RemoveOne(1).AffectedItemsCount);
      Assert.AreEqual(4, collection.Count());
      Assert.AreEqual(4, table.Count());

      // Remove sending string parameter.
      Assert.AreEqual(1, coll.RemoveOne("3").AffectedItemsCount);
      Assert.AreEqual(3, collection.Count());
      Assert.AreEqual(3, table.Count());

      // Remove an auto-generated id.
      var document = coll.Find("pages = 60").Execute().FetchOne();
      Assert.AreEqual(1, coll.RemoveOne(document.Id).AffectedItemsCount);
      Assert.AreEqual(2, collection.Count());
      Assert.AreEqual(2, table.Count());

      // Remove a non-existing document.
      Assert.AreEqual(0, coll.RemoveOne(5).AffectedItemsCount);
      Assert.AreEqual(2, collection.Count());
      Assert.AreEqual(2, table.Count());

      // Add or ReplaceOne
      Assert.AreEqual(1, coll.AddOrReplaceOne(5, new { _id = 5, title = "Book 5", pages = 60 }).
          AffectedItemsCount);
      Assert.AreEqual(3, collection.Count());
      Assert.AreEqual(3, table.Count());

      // Add or ReplaceOne
      Assert.AreEqual(2, coll.AddOrReplaceOne(2, new { title = "Book 50", pages = 60 }).
          AffectedItemsCount);
      Assert.AreEqual(3, collection.Count());
      Assert.AreEqual(3, table.Count());

      // Add or ReplaceOne
      Assert.AreEqual(1, coll.AddOrReplaceOne(6, new { _id = 6, title = "Book 6", pages = 70 }).
          AffectedItemsCount);
      Assert.AreEqual(4, collection.Count());
      Assert.AreEqual(4, table.Count());

      var result = coll.Modify("_id = 5").Set("title", "Book 5").Execute();
      Assert.AreEqual(4, collection.Count());
      Assert.AreEqual(4, table.Count());

      coll = CreateCollection("testCount");

      DbDoc[] jsonlist = new DbDoc[1000];
      DbDoc[] jsonlist1 = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 1000));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }

      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 10000));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist1[i] = newDoc2;
        newDoc2 = null;
      }
      Result res = coll.Add(jsonlist).Add(jsonlist1).Execute();
      count = session.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
      schema = session.GetSchema(schemaName);
      collection = schema.GetCollection("testCount");
      Assert.AreEqual(2000, collection.Count());

      r = coll.Remove("_id = :_id").Bind("_id", 1000).Execute();
      Assert.AreEqual(1999, collection.Count());
    }

    [Test, Description("Verify MultiThreading with count")]
    public async Task MultithreadCount()
    {
      _ = await SubProcessA();
      _ = await SubProcessB();
    }

    private Task<int> SubProcessA()
    {
      using (var sessionA = MySQLX.GetSession(ConnectionString))
      {
        Schema schema = sessionA.GetSchema("test");
        var coll = schema.CreateCollection("testCount");
        DbDoc[] jsonlist = new DbDoc[1000];
        for (int i = 0; i < 1000; i++)
        {
          DbDoc newDoc2 = new DbDoc();
          newDoc2.SetValue("_id", (i + 1000));
          newDoc2.SetValue("F1", ("Field-1-Data-" + i));
          newDoc2.SetValue("F2", ("Field-2-Data-" + i));
          newDoc2.SetValue("F3", (300 + i).ToString());
          jsonlist[i] = newDoc2;
          newDoc2 = null;
        }
        var res = coll.Add(jsonlist).Execute();
        var count = sessionA.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
        var collection = schema.GetCollection("testCount");
        Assert.AreEqual(1000, collection.Count());

        var r = coll.Remove("_id = :_id").Bind("_id", 1001).Execute();
        Assert.AreEqual(999, collection.Count());
      }
      return Task.FromResult(0);
    }

    private Task<int> SubProcessB()
    {
      Thread.Sleep(8000);
      using (var sessionB = MySQLX.GetSession(ConnectionString))
      {
        Schema schema = session.GetSchema(schemaName);
        var coll = schema.GetCollection("testCount");

        var count = sessionB.SQL("SELECT COUNT(*) FROM test.testCount").Execute().FetchOne()[0];
        schema = sessionB.GetSchema("test");
        var collection = schema.GetCollection("testCount");
        do
        {
          if (collection.Count() > 5)
          {
            Assert.AreEqual(999, collection.Count());
            break;
          }
        }
        while (true);

        var r = coll.Remove("_id = :_id").Bind("_id", 1100).Execute();
        Assert.AreEqual(998, collection.Count());
      }
      return Task.FromResult(0);
    }

    [Test, Description("Verify the behaviour of the dropX method for dropCollection under stressed conditions")]
    public void DropUnderStressedConditions()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      Schema schema = session.GetSchema(schemaName);

      for (var i = 0; i < 10; i++)
      {
        schema.CreateCollection("my_collection_123456789");
        schema.DropCollection("my_collection_123456789");
      }

    }

    [Test, Description("Verify that dropX method for dropSchema, dropIndex succeeds in stress conditions")]
    public void DropObjectsUnderStress()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var schema = session.GetSchema(schemaName);
      var testColl = CreateCollection("test123");
      for (var i = 0; i < 150; i++)
      {
        testColl.Add(new { myId = 1 }).Execute();
        testColl.DropIndex("testIndex");
        testColl.Add(new { myId = 1 }).Execute();
        testColl.DropIndex("testIndex");
      }

      for (var i = 0; i < 1000; i++)
      {
        session.CreateSchema("validSchema");
        session.DropSchema("validSchema");
        Assert.False(session.GetSchema("validSchema").ExistsInDatabase());
      }

    }

    [Test, Description("Verify that dropX method for dropIndex succeeds when deleted and created again with various combinations")]
    public void DropDocuments()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var schema = session.GetSchema(schemaName);
      schema.DropCollection("test123");
      var testColl = schema.CreateCollection("test123");

      var result = testColl.Add(new { myId = 1 }).Add(new { myId = 2 }).Execute();
      result = testColl.Add(new { myId = 1 }).Execute();

      testColl.DropIndex("testIndex");
      result = testColl.Add(new { myId = 1 }).Execute();

      testColl.DropIndex("testIndex");

      testColl.DropIndex("testIndex");
      testColl.DropIndex("testIndex");
      result = testColl.Remove("myId = :myId").Bind("myId", 1).Execute();
      result = testColl.Add(new { myId = 1 }).Execute();
      result = testColl.Remove("myId = :myId").Bind("myId", 1).Execute();

    }

    [Test, Description("Verify ModifyCollection with level OFF and JSON schema")]
    public void SchemaValidation_S1()
    {
      if (!session.Version.isAtLeast(8, 0, 19)) Assert.Ignore("This test is for MySql 8.0.19 or higher.");
      var schema = session.GetSchema(schemaName);
      var options = new CreateCollectionOptions();
      var options1 = new ModifyCollectionOptions();
      var val = new Validation();
      var doc1 = "{\"id\": \"http://json-schema.org/geo\","
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

      session.SQL($"use {schemaName}").Execute();
      val.Level = ValidationLevel.OFF;
      val.Schema = doc1;
      options.Validation = val;
      options.ReuseExisting = false;
      var collection = schema.CreateCollection("coll1", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      var count = session.SQL("select count(*) from coll1").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      val.Level = ValidationLevel.OFF;
      val.Schema = doc1;
      options.Validation = val;
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll1", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      count = session.SQL("select count(*) from coll1").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      val.Level = ValidationLevel.OFF;
      val.Schema = doc1;
      options.Validation = val;
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll2", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      count = session.SQL("select count(*) from coll2").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation() { Level = ValidationLevel.OFF, Schema = doc1 };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll3", options);
      collection.Add(@"{ ""name"": ""Ram"" , ""age"": 22 }").Execute();
      count = session.SQL("select count(*) from coll3").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation() { Level = ValidationLevel.OFF, Schema = doc1 };
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll3", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      count = session.SQL("select count(*) from coll3").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      var doc3 = "{\"id\": \"http://json-schema.org/geo\","
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

      val.Level = ValidationLevel.OFF;
      val.Schema = doc3;
      options1.Validation = val;
      collection = schema.ModifyCollection("coll1", options1);
      collection.Add(@"{ ""latitude"": 20, ""longitude"": 30 }");
      count = session.SQL("select count(*) from coll1").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      var doc4 = "{\"id\": \"http://json-schema.org/geo\","
                           + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
                           + "\"description\": \"A Person example\","
                           + "\"type\": \"object\","
                           + "\"properties\": {"
                           + "\"name\": {"
                           + "\"type\": \"string\""
                           + " }"
                           + "},"
                           + "\"required\": [\"name\"]"
                           + "}";

      val.Level = ValidationLevel.STRICT;
      val.Schema = doc4;
      options1.Validation = val;
      collection = schema.ModifyCollection("coll3", options1);
      collection.Add(@"{ ""name"": ""Samar"" }");
      count = session.SQL("select count(*) from coll3").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation() { Level = ValidationLevel.OFF, Schema = doc1 };
      collection = schema.ModifyCollection("coll2", options1);
      collection.Add(@"{ ""name"": ""Ram"" , ""age"": 22 }").Execute();
      count = session.SQL("select count(*) from coll2").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      val.Level = ValidationLevel.STRICT;
      val.Schema = doc1;
      options1.Validation = new Validation() { Level = ValidationLevel.STRICT, Schema = doc1 };
      collection = schema.ModifyCollection("coll2", options1);
      collection.Add(@"{ ""name"": ""Ram"" , ""age"": 22 }").Execute();
      count = session.SQL("select count(*) from coll2").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
    }

    [Test, Description("Checking the createcollection() and ModifyCollection() with either the level or the schema")]
    public void SchemaValidation_S2()
    {
      if (!session.Version.isAtLeast(8, 0, 19)) Assert.Ignore("This test is for MySql 8.0.19 or higher.");
      var schema = session.GetSchema(schemaName);
      session.SQL($"use {schemaName}").Execute();
      var options = new CreateCollectionOptions();
      var options1 = new ModifyCollectionOptions();
      var doc1 = "{\"id\": \"http://json-schema.org/geo\","
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

      options.Validation = new Validation { Schema = doc1 };
      options.ReuseExisting = false;
      var collection = schema.CreateCollection("coll4", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      var result = session.SQL("select * from coll4").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      var count = session.SQL("select count(*) from coll4").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Schema = doc1 };
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll4", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      result = session.SQL("select * from coll4").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll4").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Schema = doc1 };
      options.ReuseExisting = true;
      collection = schema.CreateCollection("colltesting", options);
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(collection.Add(@"{ ""name"": ""John"", ""age"": ""52"" }")));
      count = session.SQL("select count(*) from colltesting").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      var doc2 = "{\"id\":\"http://json-schema.org/geo\","
                     + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
                     + "\"description\": \"A Person example\","
                     + "\"type\": \"object\","
                     + "\"properties\": {"
                     + "\"name\": {\"type\": \"string\"},"
                     + "\"number\": {\"type\": \"number\"},"
                     + "\"street_name\": {\"type\": \"string\"} ,"
                     + "\"street_type\": {\"type\": \"string\"},"
                     + "\"colors\": "
                     + "{\"type\": \"array\" ,"
                     + "\"description\": \"different colors\","
                     + "\"items\": ["
                     + "{\"type\": \"string\","
                     + "\"enum\": [\"red\", \"amber\", \"green\"]"
                     + "},"
                     + "{\"type\": \"number\"},"
                     + "{\"type\": \"boolean\"}"//,"
                     + "{\"type\": \"null\"},"
                     + "]"
                     + "}"
                     + "},"
                     + "\"required\": [\"name\", \"number\"]"
                     + "}";

      options.Validation = new Validation { Schema = doc2 };
      options.ReuseExisting = true;
      Assert.Throws<Exception>(() => schema.CreateCollection("colltesting2", options));

      options.Validation = new Validation { Level = ValidationLevel.OFF }; ;
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll5", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      result = session.SQL("select * from coll5").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll5").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Level = ValidationLevel.STRICT };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll6", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      result = session.SQL("select * from coll6").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll6").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation() { Schema = doc1 };
      collection = schema.ModifyCollection("coll6", options1);
      collection.Add(@"{ ""name"": ""Ram"" , ""age"": 22 }").Execute();
      result = session.SQL("select * from coll6").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll6").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation() { Schema = doc1 };
      collection = schema.ModifyCollection("colltesting", options1);
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(collection.Add(@"{ ""name"": ""Ram"" , ""age"": ""22"" }")));

      options1.Validation = new Validation() { Schema = doc2 };
      Assert.Throws<Exception>(() => collection = schema.ModifyCollection("colltesting2", options1));

      options1.Validation = new Validation() { Level = ValidationLevel.OFF };
      collection = schema.ModifyCollection("coll4", options1);
      collection.Add(@"{ ""name"": ""Ram"" , ""age"": 22 }").Execute();
      result = session.SQL("select * from coll4").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll4").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation() { Level = ValidationLevel.STRICT };
      collection = schema.ModifyCollection("coll5", options1);
      collection.Add(@"{ ""name"": ""Ram"" , ""age"": 22 }").Execute();
      result = session.SQL("select * from coll5").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll5").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation() { };
      Assert.Throws<MySqlException>(() => schema.ModifyCollection("coll2", options1));

      string docEnum = "{\"id\":\"http://json-schema.org/draft-06/schema#\",\"$schema\":\"http://json-schema.org/draft-06/schema#\","
      + "\"description\": \"A Person example\",\"type\":\"object\","
      + "\"properties\":{"
      + "\"name\":{"
       + "\"type\":\"string\""
       + "},"
        + "\"number\":{"
            + "\"type\":\"number\""
        + "},"
        + "\"street_name\":{"
         + "\"type\":\"string\""
          + "},"
          + "\"street_type\": {"
           + "\"type\": \"string\""
         + "},"
         + "\"colors\":{"
         + "\"type\": \"array\","
           + "\"items\": {"
            + "\"type\":\"string\""
            + "}"
         + " },"
          + "\"consistent\":{"
          + "\"type\": \"boolean\""
         + " },"
          + "\"Favourite colors\":{"
          + "\"enum\": ["
             + "\"red\","
             + "\"amber\","
             + "\"green\""
              + "]"
          + "}"
      + "}"
       + "}";

      options.Validation = new Validation { Schema = docEnum, Level = ValidationLevel.STRICT };
      options.ReuseExisting = true;
      collection = schema.CreateCollection("collEnum", options);
      collection.Add(@"{ ""name"": ""John"" }").Execute();
      result = session.SQL("select * from collEnum").Execute().FetchAll();
      foreach (Row res in result)
        Console.WriteLine("test with enum: " + res[0]);
      count = session.SQL("select count(*) from collEnum").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
    }

    [Test, Description("Checking the error messages with different level")]
    public void SchemaValidation_S3()
    {
      if (!session.Version.isAtLeast(8, 0, 19)) Assert.Ignore("This test is for MySql 8.0.19 or higher.");
      session.SQL($"use {schemaName}").Execute();
      var options = new CreateCollectionOptions();
      var options1 = new ModifyCollectionOptions();
      var doc1 = "{\"id\": \"http://json-schema.org/geo\","
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

      var schema = session.GetSchema(schemaName);
      options.Validation = new Validation { Schema = doc1, Level = ValidationLevel.STRICT };
      options.ReuseExisting = false;
      var collection = schema.CreateCollection("coll7", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      var result = session.SQL("select * from coll7").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      var count = session.SQL("select count(*) from coll7").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Schema = doc1, Level = ValidationLevel.STRICT };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("collext", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      result = session.SQL("select * from collext").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]); ;
      count = session.SQL("select count(*) from collext").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      var doc2 = "{\"id\":\"http://json-schema.org/geo\","
                + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
                + "\"description\": \"A Person example\","
                + "\"type\": \"object\","
                + "\"properties\": {"
                + "\"name\": {\"type\": \"string\"},"
                + "\"number\": {\"type\": \"number\"},"
                + "\"street_name\": {\"type\": \"string\"} ,"
                + "\"street_type\": {\"type\": \"string\"},"
                + "\"colors\": "
                + "{\"type\": \"array\" ,"
                + "\"description\": \"different colors\","
                + "\"items\": ["
                + "{\"type\": \"string\","
                + "\"enum\": [\"red\", \"amber\", \"green\"]"
                + "},"
                + "{\"type\": \"number\"},"
                + "{\"type\": \"boolean\"}"//,"
                + "{\"type\": \"null\"},"
                + "]"
                + "}"
                + "},"
                + "\"required\": [\"name\", \"number\"]"
                + "}";

      options.Validation = new Validation { Schema = doc2, Level = ValidationLevel.STRICT };
      options.ReuseExisting = false;
      Assert.Throws<Exception>(() => schema.CreateCollection("coll8", options));

      options.Validation = new Validation { Schema = doc1, Level = ValidationLevel.STRICT };
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll8", options);
      collection.Add(@"{ ""name"": ""John"", ""age"": 52 }").Execute();
      result = session.SQL("select * from coll8").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]); ;
      count = session.SQL("select count(*) from coll8").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Schema = doc1, Level = ValidationLevel.STRICT };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll9", options);
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(collection.Add(@"{""longitude"":""99""}")));

      options1.Validation = new Validation { Schema = doc1, Level = ValidationLevel.STRICT };
      collection = schema.ModifyCollection("coll7", options1);
      Assert.Throws<MySqlException>(() => ExecuteAddStatement(collection.Add(@"{ ""number"": ""56"" }")));

      options.Validation = new Validation { Schema = doc1, Level = ValidationLevel.OFF };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll10", options);
      collection.Add(@"{""longitude"":""99""}").Execute();
      result = session.SQL("select * from coll10").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]); ;
      count = session.SQL("select count(*) from coll10").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation { Schema = doc1, Level = ValidationLevel.OFF };
      collection = schema.ModifyCollection("coll10", options1);
      collection.Add(@"{ ""name"": 67  }").Execute();
      result = session.SQL("select * from coll10").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]); ;
      count = session.SQL("select count(*) from coll10").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Schema = doc1, Level = ValidationLevel.STRICT };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll", options);
      collection.Add(@"{""_id"":1,""name"": ""John"", ""age"": 52}").Execute();
      result = session.SQL("select * from coll").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]); ;
      count = session.SQL("select count(*) from coll").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
    }

    /// <summary>
    ///  Bug 30693969
    /// </summary>
    [Test, Description("Verify ModifyCollection with level OFF and JSON schema with Json schema")]
    public void ModifyCollectionSchemaValidation()
    {
      if (!session.Version.isAtLeast(8, 0, 19)) Assert.Ignore("This test is for MySql 8.0.19 or higher.");
      session.SQL($"use {schemaName}").Execute();
      var schema = session.GetSchema(schemaName);
      var options = new CreateCollectionOptions();
      var val = new Validation();
      string doc1 = "{"
      + " \"id\":\"https://example.com/arrays.schema.json\","
      + " \"$schema\": \"http://json-schema.org/draft-07/schema#\","
      + " \"description\": \"A representation of a person, company, organization, or place\","
      + " \"type\": \"object\","
       + " \"properties\": {"
      + " \"fruits\": {"
      + " \"type\": \"array\","
      + " \"items\": {"
      + "\"type\": \"string\""
      + " }"
      + "},"
       + "\"vegetables\": {"
       + " \"type\": \"array\","
      + "\"items\": { \"$ref\": \"#/definitions/veggie\" }"
      + " }"
      + "},"
      + "\"definitions\": {"
      + " \"veggie\": {"
       + " \"type\": \"object\","
      + " \"required\": [ \"veggieName\", \"veggieLike\" ],"
      + "\"properties\": {"
      + " \"veggieName\": {"
      + " \"type\": \"string\","
      + " \"description\": \"The name of the vegetable.\""
      + "},"
      + "\"veggieLike\": {"
      + "\"type\": \"boolean\","
      + "\"description\": \"Do I like this vegetable?\""
      + "}"
      + "}"
      + "}"
      + "}"
      + "}";

      val.Level = ValidationLevel.STRICT;
      val.Schema = doc1;
      options.Validation = val;
      options.ReuseExisting = false;
      var collection = schema.CreateCollection("coll20", options);
      collection.Add(@"{""fruits"": [ ""apple"", ""orange"", ""pear"" ],""vegetables"": [{""veggieName"": ""potato"",""veggieLike"": true},{""veggieName"": ""broccoli"",""veggieLike"": false}]}").Execute();
      var result = session.SQL("select * from coll20").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      var count = session.SQL("select count(*) from coll20").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Level = ValidationLevel.STRICT, Schema = doc1 };
      options.ReuseExisting = false;
      collection = schema.CreateCollection("coll21", options);
      collection.Add(@"{""fruits"": [ ""apple"", ""orange"", ""pear"" ],""vegetables"": [{""veggieName"": ""potato"",""veggieLike"": true},{""veggieName"": ""broccoli"",""veggieLike"": false}]}").Execute();
      result = session.SQL("select * from coll21").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll21").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      val.Level = ValidationLevel.STRICT;
      val.Schema = doc1;
      options.Validation = val;
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll20", options);
      collection.Add(@"{""fruits"": [ ""apple"", ""orange"", ""pear"" ],""vegetables"": [{""veggieName"": ""potato"",""veggieLike"": true},{""veggieName"": ""broccoli"",""veggieLike"": false}]}").Execute();
      result = session.SQL("select * from coll20").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll20").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options.Validation = new Validation { Level = ValidationLevel.STRICT, Schema = doc1 };
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll21", options);
      collection.Add(@"{""fruits"": [ ""apple"", ""orange"", ""pear"" ],""vegetables"": [{""veggieName"": ""potato"",""veggieLike"": true},{""veggieName"": ""broccoli"",""veggieLike"": false}]}").Execute();
      result = session.SQL("select * from coll21").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll21").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      val.Level = ValidationLevel.OFF;
      val.Schema = doc1;
      options.Validation = val;
      options.ReuseExisting = true;
      collection = schema.CreateCollection("coll22", options);
      collection.Add(@"{""fruits"": [ 78, ""orange"", ""pear"" ],""vegetables"": [{""veggieName"": ""potato"",""veggieLike"": true},{""veggieName"": ""broccoli"",""veggieLike"": false}]}").Execute();
      result = session.SQL("select * from coll22").Execute().FetchAll();
      foreach (Row res in result)
        Assert.IsNotNull(res[0]);
      count = session.SQL("select count(*) from coll22").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());
    }

    [Test, Description("Verify ModifyCollection with level OFF and JSON schema")]
    public void SchemaValidationDeleteRecords()
    {
      // Bug30748283
      if (!session.Version.isAtLeast(8, 0, 19)) Assert.Ignore("This test is for MySql 8.0.19 or higher.");
      string doc5 = "{\"id\": \"http://json-schema.org/geo\","
           + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
           + "\"description\": \"A Person example\","
           + "\"type\": \"object\","
           + "\"properties\": {"
           + "\"name\": {"
           + "\"type\": \"number\""
           + " }"
           + "},"
           + "\"required\": [\"name\"]"
           + "}";

      string doc1 = "{\"id\": \"http://json-schema.org/geo\","
          + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
          + "\"description\": \"A Person example\","
          + "\"type\": \"object\","
          + "\"properties\": {"
          + "\"name\": {"
          + "\"type\": \"string\""
          + " }"
          + ",\"age\": {"
          + "\"type\": \"number\""
          + "}"
          + "},"
          + "\"required\": [\"name\",\"age\"]"
          + "}";
      string doc3 = "{\"id\": \"http://json-schema.org/geo\","
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

      string doc4 = "{\"id\": \"http://json-schema.org/geo\","
           + "\"$schema\": \"http://json-schema.org/draft-06/schema#\","
           + "\"description\": \"A Person example\","
           + "\"type\": \"object\","
           + "\"properties\": {"
           + "\"name\": {"
           + "\"type\": \"string\""
           + " }"
           + "},"
           + "\"required\": [\"name\"]"
           + "}";

      session.SQL($"use {schemaName}").Execute();
      var schema = session.GetSchema(schemaName);
      var options = new CreateCollectionOptions();
      var options1 = new ModifyCollectionOptions();

      options.Validation = new Validation { Level = ValidationLevel.STRICT, Schema = doc5 };
      options.ReuseExisting = true;
      var collection = schema.CreateCollection("collectiontest", options);
      collection.Add(@"{ ""name"": 52 }").Execute();
      var result = session.SQL("select * from collectiontest").Execute().FetchAll();
      foreach (Row res1 in result)
        Assert.IsNotNull(res1[0]);
      var count = session.SQL("select count(*) from collectiontest").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation { Level = ValidationLevel.STRICT, Schema = doc1 };
      session.SQL("delete from collectiontest").Execute();

      collection = schema.ModifyCollection("collectiontest", options1);
      collection.Add(@"{ ""name"": ""sammeer"",""age"":8 }").Execute();
      var result2 = session.SQL("select * from collectiontest").Execute().FetchAll();
      foreach (Row res2 in result2)
        Console.WriteLine(res2[0]);
      count = session.SQL("select count(*) from collectiontest").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation { Level = ValidationLevel.OFF, Schema = doc1 };
      collection = schema.ModifyCollection("collectiontest", options1);
      collection.Add(@"{ ""name"": 78 }").Execute();
      result = session.SQL("select * from collectiontest").Execute().FetchAll();
      foreach (Row res2 in result)
        Assert.IsNotNull(res2[0]);
      count = session.SQL("select count(*) from collectiontest").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation { Level = ValidationLevel.STRICT, Schema = doc3 };
      session.SQL("delete from collectiontest").Execute();
      collection = schema.ModifyCollection("collectiontest", options1);
      collection.Add(@"{""latitude"": 253, ""longitude"": 525}").Execute();
      result = session.SQL("select * from collectiontest").Execute().FetchAll();
      foreach (Row res2 in result)
        Assert.IsNotNull(res2[0]);
      count = session.SQL("select count(*) from collectiontest").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

      options1.Validation = new Validation { Level = ValidationLevel.STRICT, Schema = doc4 };
      session.SQL("delete from collectiontest").Execute();
      result = session.SQL("select * from collectiontest").Execute().FetchAll();
      collection = schema.ModifyCollection("collectiontest", options1);
      collection.Add(@"{ ""name"": ""Johnny"" }").Execute();
      result = session.SQL("select * from collectiontest").Execute().FetchAll();
      foreach (Row res2 in result)
        Assert.IsNotNull(res2[0]);
      count = session.SQL("select count(*) from collectiontest").Execute().FetchOne()[0];
      Assert.AreEqual(count, collection.Count());

    }

    [Test, Description("Test MySQLX plugin Remove Bind Stress")]
    public void RemoveBindStress()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      Collection coll = CreateCollection("test");
      DbDoc[] jsonlist = new DbDoc[10];
      DbDoc[] jsonlist1 = new DbDoc[10];
      for (int i = 0; i < 10; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 1000));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }

      for (int i = 0; i < 10; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 10000));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist1[i] = newDoc2;
        newDoc2 = null;
      }
      Result r = coll.Add(jsonlist).Add(jsonlist1).Execute();

      Assert.AreEqual(20, r.AffectedItemsCount, "Matching");

      r = coll.Remove("_id = :_id").Bind("_id", 1000).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount, "Matching");
    }

    [Test, Description("Test MySQLX plugin Get Collection as Table")]
    public void GetCollectionAsTableStress()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      Collection testCollection = CreateCollection("test");

      DbDoc[] jsonlist = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 1000));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      Result r = testCollection.Add(jsonlist).Execute();
      Assert.AreEqual(1000, r.AffectedItemsCount, "Matching");

      Table test = testSchema.GetCollectionAsTable("test");
      Assert.IsTrue(test.ExistsInDatabase());
      var rows = test.Select("_id").Execute().FetchAll();

      for (int j = 0; j < rows.Count; j++)
      {
        var doc = testCollection.Find("_id like :param").Bind("param", (j + 1000)).Execute();
        var docs = doc.FetchAll().Count();
        Assert.AreEqual(1, docs, "Matches");
      }
    }

    [Test, Description("Test MySQLX plugin GetCollection Exception Scenario")]
    public void GetCollectionException()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        Schema db = sessionPlain.GetSchema(schemaName);
        Collection col = db.GetCollection("my_collection_123456789");
        if (col.ExistsInDatabase())
        {
          db.DropCollection("my_collection_123456789");
          col = db.CreateCollection("my_collection_123456789");
        }
        else { col = db.CreateCollection("my_collection_123456789"); }

        Collection col1 = db.GetCollection("my_collection_123456789", true);
        if (col.ExistsInDatabase())
        {
          db.DropCollection("my_collection_123456789");
          col1 = db.CreateCollection("my_collection_123456789");
        }
        else { col1 = db.CreateCollection("my_collection_123456789"); }

        var col2 = db.GetTable("my_collection_1234567891");
        Assert.Throws<MySqlException>(() => db.GetCollection("my_collection_test", true));
        db.DropCollection("my_collection_123456789");
      }
    }

    [Test, Description("Collection GetDocumentIDS Stress(1000 records)")]
    public void GetDocumentIDSStress()
    {
      Collection coll = CreateCollection("test");
      DbDoc[] jsonlist = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (3 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      Result r = coll.Add(jsonlist).Execute();
      Assert.AreEqual(1000, r.AffectedItemsCount, "Matching");
      var documentIds = r.GeneratedIds;
      Assert.False(documentIds != null && documentIds.Count > 0);
    }

    [Test, Description("Session Performance Test")]
    public void SessionPerformanceTest()
    {
      string json = "";
      int i = 0, j = 0, maxField = 100;
      var collection = CreateCollection("test");
      int maxDepth = 97;
      json = "{\"_id\":\"1002\",\"XYZ\":1111";
      for (j = 0; j < maxField; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxDepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxDepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }
      json = json + "}";

      collection.Add(json).Execute();
      json = "{\"_id\":\"1003\",\"XYZ\":2222";
      for (j = 0; j < maxField; j++)
      {
        json = json + ",\"DATAX" + j + "\":";
        for (i = 0; i < maxDepth; i++)
        {
          json = json + "{\"D" + i + "\":";
        }
        json = json + maxDepth;
        for (i = maxDepth - 1; i >= 0; i--)
        {
          json = json + "}";
        }
      }
      json = json + "}";

      collection.Add(json).Execute();
      json = "{\"_id\":\"1001\",\"XYZ\":3333";
      for (j = 0; j < maxField; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxDepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxDepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }

      for (j = 0; j < maxField; j++)
      {
        json = json + ",\"DATAX" + j + "\":";
        for (i = 0; i < maxDepth; i++)
        {
          json = json + "{\"D" + i + "\":";
        }
        json = json + maxDepth;
        for (i = maxDepth - 1; i >= 0; i--)
        {
          json = json + "}";
        }
      }
      json = json + "}";
      collection.Add(json).Execute();

      // select
      string query = "$.ARR" + (maxField - 1);
      for (i = 0; i < maxDepth; i++)
      {
        query = query + "[1]";
      }
      query = query + "[0]";
      json = "CAST(" + query + " as SIGNED)= " + maxDepth;
      var docs = collection.Find(json).Fields("$._id as _id," + query + " as Arr").Execute();
      var res = docs.FetchAll();
      Assert.AreEqual("1001", res[0]["_id"].ToString(), "Matching the id");
      Assert.AreEqual("1002", res[1]["_id"].ToString(), "Matching the id");

      query = "$.DATAX" + (maxField - 1);
      for (i = 0; i < maxDepth; i++)
      {
        query = query + ".D" + i;
      }
      json = "CAST(" + query + " as SIGNED)";
      docs = collection.Find(json + " =" + maxDepth).Fields("$._id as _id ").Execute();
      res = docs.FetchAll();
      Assert.AreEqual("1001", res[0]["_id"].ToString(), "Matching the id");
      Assert.AreEqual("1003", res[1]["_id"].ToString(), "Matching the id");
    }
  }
}