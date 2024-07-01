// Copyright © 2017, 2024, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MySqlX.Data.Tests
{
  public class MergePatch : BaseTest
  {
    private static string[] documentsAsJsonStrings;
    private static DbDoc[] documentsAsDbDocs;
    private string documentAsJsonString2 =
                          "{ " +
                          // Document 2.
                          "\"_id\": \"123456789asdferdfghhghjh12334\", " +
                          "\"title\": \"CASTAWAY\", " +
                          "\"description\": \"A FedEx executive must transform himself physically and  " +
                          "emotionally to survive a crash landing on a deserted island\", " +
                          "\"releaseyear\": 2000, " +
                          "\"language\": \"English\", " +
                          "\"duration\": 150, " +
                          "\"rating\": \"PG\", " +
                          "\"genre\": \"Drama\", " +
                          "\"actors\": [ " +
                          "{ \"name\": \"TOM HANKS\", \"country\": \"USA\", \"birthdate\": \"9 Jul 1956\" }, " +
                          "{ \"name\": \"Paul Sanchez\", \"country\": \"Spain\", \"birthdate\": \"26 Oct 1975\" }, " +
                          "{ \"name\": \"Lari White\", \"country\": \"France\", \"birthdate\": \"13 May 1965\" }" +
                          "], " +
                          "\"additionalinfo\": { " +
                          "\"director\": { " +
                          "\"name\": \"Steven Spielberg\", " +
                          "\"birthplace\": " +
                          "{ \"country\": {\"countryActual\": \"USA\", \"city\": \"Florida\"} }, " +
                          "\"age\":67, " +
                          "\"awards\": [ " +
                          "{ \"award\": \"Best Movie\", \"movie\": \"American Beauty\", \"year\": 1999}, " +
                          "{ \"award\": \"Best Director\", \"movie\": \"Saving Private Ryan\", \"year\": 1998 }" +
                          "] " +
                          "}, " +
                          "\"writers\": [\"Tom Sizemore\", \"Barry Pepper\", \"Giovanni Ribisi\"], " +
                          "\"productioncompanies\": [\"Amblin Entertainment\", \"Mutual Film Company\"] " +
                          "} " +
                          "}";

    static MergePatch()
    {
      documentsAsJsonStrings = new string[] {
        "{ " +
          // Document 1.
          "\"_id\": \"a6f4b93e1a264a108393524f29546a8c\", " +
          "\"title\": \"AFRICAN EGG\", " +
          "\"description\": \"A Fast-Paced Documentary of a Pastry Chef And a Dentist who must Pursue a Forensic Psychologist in The Gulf of Mexico\", " +
          "\"releaseyear\": 2006, " +
          "\"language\": \"English\", " +
          "\"duration\": 130, " +
          "\"rating\": \"G\", " +
          "\"genre\": \"Science fiction\", " +
          "\"actors\": [ " +
            "{ \"name\": \"MILLA PECK\", \"country\": \"Mexico\", \"birthdate\": \"12 Jan 1984\" }, " +
            "{ \"name\": \"VAL BOLGER\", \"country\": \"Botswana\", \"birthdate\": \"26 Jul 1975\" }, " +
            "{ \"name\": \"SCARLETT BENING\", \"country\": \"Syria\", \"birthdate\": \"16 Mar 1978\" }" +
          "], " +
          "\"additionalinfo\": { " +
            "\"director\": { " +
              "\"name\": \"Sharice Legaspi\", " +
              "\"age\":57, " +
              "\"birthplace\": " +
                "{ \"country\": \"Italy\", \"city\": \"Rome\" }, " +
              "\"awards\": [ " +
                "{ \"award\": \"Best Movie\", \"movie\": \"THE EGG\", \"year\": 2002}, " +
                "{ \"award\": \"Best Special Effects\", \"movie\": \"AFRICAN EGG\", \"year\": 2006 }" +
              "] " +
            "}, " +
            "\"writers\": [\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"], " +
            "\"productioncompanies\": [\"Qvodrill\", \"Indigoholdings\"] " +
          "} " +
        "}"
      };

      documentsAsDbDocs = new DbDoc[documentsAsJsonStrings.Length];
      for (int i = 0; i < documentsAsJsonStrings.Length; i++)
        documentsAsDbDocs[i] = new DbDoc(documentsAsJsonStrings[i]);
    }

    #region General

    [Test]
    public void PatchExpectedExceptions()
    {
      Collection collection = CreateCollection("test");

      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3))
        Assert.Throws<MySqlException>(() => collection.Modify("true").Patch(null));
      else
      {
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch(null));
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch("  "));
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch(""));
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch(string.Empty));
      }
    }

    [Test]
    public void SimplePatch()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = ExecuteAddStatement(collection.Add("{ \"_id\": \"124\", \"email\": \"jose@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      ExecuteModifyStatement(collection.Modify("email = \"alice@ora.com\"").Patch("{ \"_id\": \"123\", \"email\":\"bob@ora.com\", \"startDate\":null }"));

      DbDoc document = collection.GetOne("123");
      Assert.That(document.Id, Is.EqualTo("123"));
      Assert.That(document.values["email"], Is.EqualTo("bob@ora.com"));
      Assert.That(!document.values.ContainsKey("startDate"));

      document = collection.GetOne("124");
      Assert.That(document.Id, Is.EqualTo("124"));
      Assert.That(document.values["email"], Is.EqualTo("jose@ora.com"));
      Assert.That(document.values.ContainsKey("startDate"));
    }

    [Test]
    public void SimplePatchUsingMySqlExpressionClass()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      var patch = new
      {
        email = new MySqlExpression("UPPER($.email)")
      };

      ExecuteModifyStatement(collection.Modify("true").Patch(patch));
      DbDoc document = collection.GetOne("123");
      Assert.That(document.values["email"], Is.EqualTo("ALICE@ORA.COM"));
    }

    [Test]
    public void CRUD()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      // Add field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": [\"Spanish\"] }").Bind("lang", "English"));
      var document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(document.values.ContainsKey("translations"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": [\"Spanish\", \"Italian\"] }").Bind("lang", "English"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(document.values.ContainsKey("translations"));
      var translations = (object[])document.values["translations"];
      Assert.That((string)translations[0], Is.EqualTo("Spanish"));
      Assert.That((string)translations[1], Is.EqualTo("Italian"));

      // Remove field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": null }").Bind("lang", "English"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(document.values.ContainsKey("translations"), Is.False);

      // Add field.
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"), Is.False);
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": \"Sakila D\" } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"])["musicby"], Is.EqualTo("The Sakila"));

      // Remove field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": null } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("additionalinfo.director.name = :director").Bind("director", "Sharice Legaspi")).FetchOne();
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"), Is.False);

      // Add field.
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"), Is.False);
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"France\" } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"Canada\" } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["country"], Is.EqualTo("Canada"));

      // Remove field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": null } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("additionalinfo.director.name = :director").Bind("director", "Sharice Legaspi")).FetchOne();
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"), Is.False);
    }

    #endregion

    #region Nested operations

    [Test]
    public void ReplaceUpdateInDifferentNestingLevels()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["title"], Is.EqualTo("AFRICAN EGG"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": \"The African Egg\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["title"], Is.EqualTo("The African Egg"));

      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["age"], Is.EqualTo(57));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"age\": 67 } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["age"], Is.EqualTo(67));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"director\": { \"age\": 77 } } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["age"], Is.EqualTo(77));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": { \"movie\": \"The African Egg\"} }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["title"])["movie"], Is.EqualTo("The African Egg"));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": \"No data available\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values["additionalinfo"], Is.EqualTo("No data available"));
    }

    [Test]
    public void AddRemoveFieldInDifferentNestingLevels()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("translations"), Is.False);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"translations\": [\"Spanish\"] }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("translations"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"translations\": null }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("translations"), Is.False);

      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"), Is.False);
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"France\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": null } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"), Is.False);

      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"), Is.False);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": null } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"), Is.False);
    }

    #endregion

    #region Multiple fields

    [Test]
    public void CRUDMultipleFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");

      // Add fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values["field 1"], Is.EqualTo("one"));
      Assert.That(document.values["field 2"], Is.EqualTo("two"));
      Assert.That(document.values["field 3"], Is.EqualTo("three"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 1"], Is.EqualTo("one"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 2"], Is.EqualTo("two"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 3"], Is.EqualTo("three"));

      // Update/Replace fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": \"ONE\", \"field 2\": \"TWO\", \"field 3\": \"THREE\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": \"ONE\", \"field 2\": \"TWO\", \"field 3\": \"THREE\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values["field 1"], Is.EqualTo("ONE"));
      Assert.That(document.values["field 2"], Is.EqualTo("TWO"));
      Assert.That(document.values["field 3"], Is.EqualTo("THREE"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 1"], Is.EqualTo("ONE"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 2"], Is.EqualTo("TWO"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 3"], Is.EqualTo("THREE"));

      // Remove fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": null, \"field 2\": null, \"field 3\": null }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": null, \"field 2\": null, \"field 3\": null } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("field 1"), Is.False);
      Assert.That(document.values.ContainsKey("field 2"), Is.False);
      Assert.That(document.values.ContainsKey("field 3"), Is.False);
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])).ContainsKey("field 1"), Is.False);
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])).ContainsKey("field 2"), Is.False);
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])).ContainsKey("field 3"), Is.False);
    }

    #endregion

    #region Using expressions

    [Test]
    public void AddNewFieldUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(((Dictionary<string, object>)((object[])document.values["actors"])[0]).ContainsKey("age"), Is.False);
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("true").Patch("{ \"actors\": { \"age\": Year(CURDATE()) - CAST(SUBSTRING_INDEX(actors.birthdate, ' ', - 1) AS DECIMAL)) } }")));

      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values.ContainsKey("audio"), Is.False);
      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"audio\": CONCAT($.language, ', no subtitles') }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values["audio"], Is.EqualTo("English, no subtitles"));
    }

    [Test]
    public void ReplaceUpdateUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");

      Assert.That(document.values.ContainsKey("audio"), Is.False);
      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"audio\": CONCAT(UPPER($.language), ', No Subtitles') }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values["audio"], Is.EqualTo("ENGLISH, No Subtitles"));
    }

    [Test]
    public void ReplaceUpdateIdUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      // Changes to the _id field are ignored.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"_id\": replace(UUID(), '-', '') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.Id, Is.EqualTo("a6f4b93e1a264a108393524f29546a8c"));
    }

    [Test]
    public void AddIdToNestedDocumentUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("_id"), Is.False);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": replace(UUID(), '-', '') } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("_id"));
    }

    [Test]
    public void AddNullFieldUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("releasedate"), Is.False);
      Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"releasedate\": DATE_ADD('2006-04-00',INTERVAL 1 DAY) }").Bind("id", "a6f4b93e1a264a108393524f29546a8c")));
      Assert.That(ex.Message, Is.EqualTo("Invalid data for update operation on document collection table"));
    }

    [Test]
    public void ReplaceUpdateNullFieldReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values["title"], Is.EqualTo("AFRICAN EGG"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": concat('my ', NULL, ' title') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("title"), Is.False);
    }

    [Test]
    public void AddNewFieldReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("docfield"), Is.False);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"docfield\": JSON_OBJECT('field 1', 1, 'field 2', 'two') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values.ContainsKey("docfield"));
    }

    [Test]
    public void ReplaceUpdateFieldWithDocumentReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.values["genre"], Is.EqualTo("Science fiction"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"genre\": JSON_OBJECT('name', 'Science Fiction') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document.values["genre"])["name"], Is.EqualTo("Science Fiction"));
    }

    #endregion

    #region Using _id

    [Test]
    public void ReplaceUpdateId()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      // Changes to the _id field are ignored.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"_id\": \"b5f4b93e1a264a108393524f29546a9d\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.Id, Is.EqualTo("a6f4b93e1a264a108393524f29546a8c"));
    }

    [Test]
    public void AddIdToNestedDocument()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      // Add id to nested document is allowed.
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      var field = (Dictionary<string, object>)document.values["additionalinfo"];
      Assert.That(field.Count, Is.EqualTo(3));
      Assert.That(field.ContainsKey("_id"), Is.False);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": \"b5f4b93e1a264a108393524f29546a9d\" } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      field = (Dictionary<string, object>)document.values["additionalinfo"];
      Assert.That(field.Count, Is.EqualTo(4));
      Assert.That(field.ContainsKey("_id"));
      Assert.That(field["_id"], Is.EqualTo("b5f4b93e1a264a108393524f29546a9d"));
    }

    [Test]
    public void SetNullToId()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      // Changes to the _id field are ignored.
      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"_id\": NULL }"));
      Assert.That(ExecuteFindStatement(collection.Find()).FetchOne().Id, Is.EqualTo("a6f4b93e1a264a108393524f29546a8c"));
    }

    [Test]
    public void AddNullFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": NULL }"));
      Assert.That(ExecuteFindStatement(collection.Find()).FetchOne().values.ContainsKey("nullfield"), Is.False);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": [NULL, NULL] }"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values.ContainsKey("nullfield"));
      var nullArray = (object[])document.values["nullfield"];
      Assert.That(nullArray[0], Is.Null);
      Assert.That(nullArray[1], Is.Null);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": NULL } }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(ExecuteFindStatement(collection.Find()).FetchOne().values.ContainsKey("nullfield"));
      Assert.That(((Dictionary<string, object>)document.values["nullfield"]).Count == 0);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": [NULL, NULL] } }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      var nestedNullArray = (object[])((Dictionary<string, object>)document.values["nullfield"])["nested"];
      Assert.That(nestedNullArray[0], Is.Null);
      Assert.That(nestedNullArray[1], Is.Null);
    }

    [Test]
    public void AddNestedNullFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": NULL } }"));
      Assert.That(((Dictionary<string, object>)ExecuteFindStatement(collection.Find()).FetchOne().values["additionalinfo"]).ContainsKey("nullfield"), Is.False);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": [NULL, NULL] } }"));
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedNullArray = (object[])((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"];
      Assert.That(nestedNullArray[0], Is.Null);
      Assert.That(nestedNullArray[1], Is.Null);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": NULL } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"]).ContainsKey("nullfield"), Is.False);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": [NULL, NULL] } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      nestedNullArray = (object[])((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"])["nested"];
      Assert.That(nestedNullArray[0], Is.Null);
      Assert.That(nestedNullArray[1], Is.Null);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": JSON_OBJECT('field', null) } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedObject = (Dictionary<string, object>)((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"])["nested"];
      Assert.That(nestedObject, Is.Empty);
    }

    #endregion

    [Test]
    public void GetDocumentProperties()
    {
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      // Get root string properties.
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.Id, Is.EqualTo("a6f4b93e1a264a108393524f29546a8c"));
      Assert.That(document["title"], Is.EqualTo("AFRICAN EGG"));
      Assert.That(document["rating"], Is.EqualTo("G"));

      // Get root numeric properties.
      Assert.That(document["releaseyear"], Is.EqualTo(2006));
      Assert.That(document["duration"], Is.EqualTo(130));

      // Get root array.
      object[] actors = document["actors"] as object[];
      Assert.That(actors.Length == 3);
      Dictionary<string, object> actor1 = actors[1] as Dictionary<string, object>;
      Assert.That(actor1["name"], Is.EqualTo("VAL BOLGER"));

      // Get nested string properies.
      Assert.That(document["additionalinfo.director.name"], Is.EqualTo("Sharice Legaspi"));
      Assert.That(document["additionalinfo.director.age"], Is.EqualTo(57));
      Assert.That(document["additionalinfo.director.birthplace.country"], Is.EqualTo("Italy"));

      // Get nested array.
      object[] awards = document["additionalinfo.director.awards"] as object[];
      Assert.That(awards.Length == 2);
    }

    [Test]
    public void PatchUsingDateAndTimeFunctions()
    {
      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(t1));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": YEAR('2000-01-01') }").Bind("id", "1"));
      DbDoc document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(2000));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MONTH('2008-02-03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(2));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": WEEK('2008-02-20') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(7));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DAY('2008-02-20') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(20));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": HOUR('10:05:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(10));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MINUTE('2008-02-03 10:05:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(5));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": SECOND('10:05:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(3));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MICROSECOND('12:00:00.123456') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(123456));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": QUARTER('2008-04-01') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(2));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": TIME('2003-12-31 01:02:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo("01:02:03.000000"));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DATE('2003-12-31 01:02:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo("2003-12-31"));
    }

    [Test]
    public void PatchUsingOtherKnownFunctions()
    {
      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(t1));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"otherValue\": CHAR(77, 121, 83, 81, '76') }").Bind("id", "1"));
      DbDoc document = collection.GetOne("1");
      Assert.That(document["otherValue"], Is.EqualTo("base64:type15:TXlTUUw="));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"otherValue\": HEX('abc') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["otherValue"], Is.EqualTo("616263"));

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"otherValue\": BIN(12) }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.That(document["otherValue"], Is.EqualTo("1100"));
    }

    #region WL14389

    [Test, Description("Test valid modify.patch on condition matching multiple records))")]
    public void ModifyPatchMultipleRecords_S1()
    {

      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = "12"},
        new {_id = 2, title = "Book 2", pages = 30,age = "18"},
        new {_id = 3, title = "Book 3", pages = 40,age = "34"},
        new {_id = 4, title = "Book 4", pages = 50,age = "12"}
      };
      Result r = collection.Add(docs).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(4));
      var document = collection.GetOne("1");
      var jsonParams = new { title = "Book 100" };
      r = collection.Modify("age = :age").Patch(jsonParams).
          Bind("age", "12").Execute();//Multiple Records
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));
      string jsonParams1 = "{ \"title\": \"Book 400\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      document = collection.GetOne("1");
      Assert.That(document["title"], Is.EqualTo("Book 100"));
      document = collection.GetOne("4");
      Assert.That(document["title"], Is.EqualTo("Book 100"));
      document = collection.GetOne("2");
      Assert.That(document["title"], Is.EqualTo("Book 400"));

      string splName = "A*b";
      jsonParams1 = "{\"data1\":\"" + splName + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      document = collection.GetOne("2");
      Assert.That(document["data1"], Is.EqualTo(splName));

      splName = "A/b";
      jsonParams1 = "{\"data1\":\"" + splName + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      document = collection.GetOne("2");
      Assert.That(document["data1"], Is.EqualTo(splName));

      splName = "A&b!c@d#e$f%g^h&i(j)k-l+m=0_p~q`r}s{t][.,?/><";
      jsonParams1 = "{\"data1\":\"" + splName + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      document = collection.GetOne("2");
      Assert.That(document["data1"], Is.EqualTo(splName));

      //Large Key Length
      string myString = new string('*', 65535);
      jsonParams1 = "{\"data1\":\"" + myString + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      document = collection.GetOne("2");
      Assert.That(document["data1"], Is.EqualTo(myString));

      collection = CreateCollection("test");
      r = collection.Add(documentsAsJsonStrings).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(documentAsJsonString2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      collection.Modify("_id = :id").Patch("{ \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      collection.Modify("additionalinfo.director.name = :director").
           Patch("{ \"additionalinfo\": { \"director\": { \"field1 11\": " +
                 "\"one\", \"field2 12\": \"two\", \"field3 13\": \"three\" } } }").
           Bind("director", "Sharice Legaspi").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      collection.Modify("_id = :id").Patch("{ \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      collection.Modify("additionalinfo.director.name = :director").
         Patch("{ \"additionalinfo\": { \"director\": { \"field1 11\": " +
                "\"one\", \"field2 12\": \"two\", \"field3 13\": \"three\" } } }").
         Bind("director", "Steven Spielberg").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["field 1"], Is.EqualTo("one"));
      Assert.That(document["field 2"], Is.EqualTo("two"));
      Assert.That(document["field 3"], Is.EqualTo("three"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field1 11"], Is.EqualTo("one"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field2 12"], Is.EqualTo("two"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field3 13"], Is.EqualTo("three"));

      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.That(document["field 1"], Is.EqualTo("one"));
      Assert.That(document["field 2"], Is.EqualTo("two"));
      Assert.That(document["field 3"], Is.EqualTo("three"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field1 11"], Is.EqualTo("one"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field2 12"], Is.EqualTo("two"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field3 13"], Is.EqualTo("three"));

      collection.Modify("language = :language").
          Patch("{ \"additionalinfo\": { \"director\": { \"test1\": " +
                "\"one\", \"test2\": \"two\", \"test3\": \"three\" } } }").
          Bind("language", "English").Execute();
      collection.Modify("language = :language").
          Patch("{ \"field 1\": \"check1\", \"field 2\": \"check2\", \"field 3\": \"check3\" }").
          Bind("language", "English").Execute();
      //Multiple Records
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["field 1"], Is.EqualTo("check1"));
      Assert.That(document["field 2"], Is.EqualTo("check2"));
      Assert.That(document["field 3"], Is.EqualTo("check3"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test1"], Is.EqualTo("one"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test2"], Is.EqualTo("two"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test3"], Is.EqualTo("three"));
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.That(document["field 1"], Is.EqualTo("check1"));
      Assert.That(document["field 2"], Is.EqualTo("check2"));
      Assert.That(document["field 3"], Is.EqualTo("check3"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test1"], Is.EqualTo("one"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test2"], Is.EqualTo("two"));
      Assert.That((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test3"], Is.EqualTo("three"));

      collection = CreateCollection("test");
      var data2 = new DbDoc(@"{ ""_id"": -1, ""pages"": 20,
                          ""books"": [
                            {""_id"" : 10, ""title"" : ""Book 10""},
                            { ""_id"" : 20, ""title"" : ""Book 20"" }
                          ]
                      }");
      r = collection.Add(data2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      document = collection.GetOne("-1");
      Assert.That(document["pages"].ToString(), Is.EqualTo("20"));
      collection.Modify("_id = :id").Patch("{ \"pages\": \"200\" }").
          Bind("id", -1).Execute();
      document = collection.GetOne("-1");
      Assert.That(document["pages"], Is.EqualTo("200"));
      data2 = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,
                          ""books"": [
                            {""_id"" : 10, ""title"" : ""Book 10""},
                            { ""_id"" : 20, ""title"" : ""Book 20"" }
                          ]
                      }");
      r = collection.Add(data2).Execute();
      collection.Modify("_id = :id").
          Patch("{ \"books\": [{ \"_id\": \"11\",\"title\":\"Ganges\"}]}").
          Bind("id", "1").Execute();
      document = collection.GetOne("1");
      object[] books = document["books"] as object[];
      Assert.That(books.Length, Is.EqualTo(1));
      Dictionary<string, object> book1 = books[0] as Dictionary<string, object>;
      Assert.That(book1["_id"], Is.EqualTo("11"));
      Assert.That(book1["title"], Is.EqualTo("Ganges"));
      var t1 =
          "{\"_id\": \"1\", \"name\": \"Alice\", \"address\": [{\"zip\": \"12345\", \"city\": \"Los Angeles\", \"street\": \"32 Main str\"}]}";
      var t2 =
          "{\"_id\": \"2\", \"name\": \"Bob\", \"address\": [{\"zip\": \"325226\", \"city\": \"San Francisco\", \"street\": \"42 2nd str\"}]}";
      var t3 =
          "{\"_id\": \"3\", \"name\": \"Bob\", \"address\": [{\"zip1\": \"325226\", \"city1\": \"San Francisco\", \"street1\": \"42 2nd str\"}," +
          "{\"zip2\": \"325226\", \"city2\": \"San Francisco\", \"street2\": \"42 2nd str\"}]}";

      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t2).Execute();
      //update the name and zip code of match
      collection.Modify("_id = :id").Patch("{\"name\": \"Joe\", \"address\": [{\"zip\":\"91234\"}]}").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Joe"));
      object[] address = document["address"] as object[];
      Assert.That(address.Length, Is.EqualTo(1));
      Dictionary<string, object> address1 = address[0] as Dictionary<string, object>;
      Assert.That(address1["zip"], Is.EqualTo("91234"));

      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :id").Patch("{\"name\": \"Joe\", \"address\": [{\"zip1\":\"91234\"},{\"zip2\":\"10000\"}]}").
          Bind("id", "3").Execute();
      document = collection.GetOne("3");
      Assert.That(document["name"], Is.EqualTo("Joe"));
      address = document["address"] as object[];
      Assert.That(address.Length, Is.EqualTo(2));
      address1 = address[0] as Dictionary<string, object>;
      Assert.That(address1["zip1"], Is.EqualTo("91234"));
      address1 = address[1] as Dictionary<string, object>;
      Assert.That(address1["zip2"], Is.EqualTo("10000"));

      collection = CreateCollection("test");
      var t = "{\"_id\": \"id1004\", \"age\": 1, \"misc\": 1.2, \"name\": { \"last\": \"ABCDEF3\", \"first\": \"ABCDEF1\", \"middle\": { \"middle1\": \"ABCDEF21\", \"middle2\": \"ABCDEF22\"}}}";
      r = collection.Add(t).Execute();
      collection.Modify("_id = :id").Patch("{\"name\":{\"middle\":{\"middle1\": {\"middle11\" : \"ABCDEF211\", \"middle12\" : \"ABCDEF212\", \"middle13\" : \"ABCDEF213\"}}}}").Bind("id", "id1004").Execute();
      document = collection.GetOne("id1004");
      Assert.That(document["name.middle.middle1.middle11"], Is.EqualTo("ABCDEF211"));
      Assert.That(document["name.middle.middle1.middle12"], Is.EqualTo("ABCDEF212"));
      Assert.That(document["name.middle.middle1.middle13"], Is.EqualTo("ABCDEF213"));

      t1 = "{\"_id\": \"1001\", \"ARR\":[1,2,3], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      t2 = "{\"_id\": \"1002\", \"ARR\":[1,1,2], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      t3 = "{\"_id\": \"1003\", \"ARR\":[1,4,5], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :_id").
          Patch("{\"ARR\":[6,8,3],\"ARR1\":[\"changed name1\",\"changed name2\", \"changed name3\"]}").
          Bind("_id", "1001").Execute();
      document = collection.GetOne("1001");

      object[] arr = document["ARR1"] as object[];
      Assert.That(arr.Length, Is.EqualTo(3));
      int j = 1;
      for (int i = 0; i < arr.Length; i++)
      {
        Assert.That(arr[i], Is.EqualTo("changed name" + j));
        j++;
      }

      t1 =
          "{\"_id\": \"1\", \"name\": \"Alice\", \"address\": {\"zip\": \"12345\", \"city\": \"Los Angeles\", \"street\": \"32 Main str\"}}";
      t2 =
          "{\"_id\": \"2\", \"name\": \"Bob\", \"address\": [{\"zip\": \"325226\", \"city\": \"San Francisco\", \"street\": \"42 2nd str\"}]}";
      t3 =
          "{\"_id\": \"3\", \"name\": \"Bob\", \"address\": [{\"zip1\": \"325226\", \"city1\": \"San Francisco\", \"street1\": \"42 2nd str\"}," +
          "{\"zip2\": \"325226\", \"city2\": \"San Francisco\", \"street2\": \"42 2nd str\"}]}";

      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      r = collection.Add(t3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      collection.Modify("_id = :id").
          Patch("{\"address\": null, \"zip\": $.address.zip, \"street\": $.address.street, \"city\": upper($.address.city)}").Bind("id", "1").
          Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Alice"));
      Assert.That(document["street"], Is.EqualTo("32 Main str"));
      Assert.That(document["city"], Is.EqualTo("LOS ANGELES"));
      Assert.That(document["zip"], Is.EqualTo("12345"));

      t1 =
          "{\"_id\": \"1\", \"name\": \"Alice\", \"address\": {\"test\":{\"zip\": \"12345\", " +
          "\"city\": \"Los Angeles\", \"street\": \"32 Main str\"}}}";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :id").
          Patch("{\"address\":{\"test\": null,\"zip\":$.address.test.zip,\"city\": lower($.address.test.city)}}").Bind("id", "1").
          Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Alice"));
      Assert.That(document["address.city"], Is.EqualTo("los angeles"));
      Assert.That(document["address.zip"], Is.EqualTo("12345"));

    }

    [Test, Description("Test valid modify.patch to change element at Depth 5 for multiple arrays))")]
    public void ModifyInNestedObjects()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      string json = "";
      int i = 0, j = 0, k = 0, l = 0, m = 0, n = 0, maxFld = 10;
      var collection = CreateCollection("test");
      int maxdepth = 5;
      json = "{\"_id\":\"1002\",\"XYZ\":1111";
      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }
      json = json + "}";

      collection.Add(json).Execute();

      json = "{\"_id\":\"1003\",\"XYZ\":2222";
      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"DATAX" + j + "\":";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + "{\"D" + i + "\":";
        }
        json = json + maxdepth;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "}";
        }
      }
      json = json + "}";

      collection.Add(json).Execute();

      json = "{\"_id\":\"1001\",\"XYZ\":3333";
      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }

      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"DATAX" + j + "\":";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + "{\"D" + i + "\":";
        }
        json = json + maxdepth;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "}";
        }
      }
      json = json + "}";

      collection.Add(json).Execute();

      //Update each array one by one
      for (n = 1; n < 2; n++)
      {
        collection.Modify("_id = :_id").
        Patch("{\"ARR" + i + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]}").
        Bind("_id", "1002").Execute();
        var document1 = collection.GetOne("1002");
        var test = "ARR" + i;
        object[] arr1 = document1[test] as object[];
        Assert.That(arr1.Length, Is.EqualTo(3));
        i = 0;
        j = 0;
        for (i = 0; i < arr1.Length; i++)
        {

          if (i == 1)
          {
            object[] arr2 = arr1[1] as object[];
            Assert.That(arr2.Length, Is.EqualTo(3));
            for (j = 0; j < arr2.Length; j++)
            {

              if (j == 1)
              {
                object[] arr3 = arr2[1] as object[];
                Assert.That(arr3.Length, Is.EqualTo(3));
                for (k = 0; k < arr3.Length; k++)
                {

                  if (k == 1)
                  {
                    object[] arr4 = arr3[1] as object[];
                    Assert.That(arr4.Length, Is.EqualTo(3));
                    for (l = 0; l < arr4.Length; l++)
                    {

                      if (l == 1)
                      {
                        object[] arr5 = arr4[1] as object[];
                        Assert.That(arr5.Length, Is.EqualTo(3));
                        for (m = 0; m < arr5.Length; m++)
                        {

                          if (m == 1)
                          {
                            object[] arr6 = arr5[1] as object[];
                            Assert.That(arr6.Length, Is.EqualTo(1));
                            if (arr6.Length == 1)
                              Assert.That(arr6[0], Is.EqualTo(4));
                          }
                          else
                            Assert.That(arr5[m], Is.EqualTo(5));
                          if (m == 2)
                            break;
                        }
                      }
                      else
                        Assert.That(arr4[l], Is.EqualTo(6));
                      if (l == 2)
                        break;

                    }
                  }
                  else
                    Assert.That(arr3[k], Is.EqualTo(7));
                  if (k == 2)
                    break;
                }
              }
              if (j == 0 || j == 2)
                Assert.That(arr2[j], Is.EqualTo(8));
              if (j == 2)
                break;
            }
          }
          else
            Assert.That(arr1[i], Is.EqualTo(9));
          if (i == 2)
            break;
        }
      }
      //Update each array at one shot
      string updatedPatch = "{\"ARR" + 0 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 1 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 2 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 3 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 4 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 5 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 6 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 7 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 8 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 9 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]}";

      collection.Modify("_id = :_id").
         Patch(updatedPatch).
         Bind("_id", "1002").Execute();
      var document = collection.GetOne("1002");
      for (n = 0; n < 10; n++)
      {
        var test = "ARR" + i;
        object[] arr1 = document[test] as object[];
        Assert.That(arr1.Length, Is.EqualTo(3));
        i = 0;
        j = 0;
        for (i = 0; i < arr1.Length; i++)
        {

          if (i == 1)
          {
            object[] arr2 = arr1[1] as object[];
            Assert.That(arr2.Length, Is.EqualTo(3));
            for (j = 0; j < arr2.Length; j++)
            {

              if (j == 1)
              {
                object[] arr3 = arr2[1] as object[];
                Assert.That(arr3.Length, Is.EqualTo(3));
                for (k = 0; k < arr3.Length; k++)
                {

                  if (k == 1)
                  {
                    object[] arr4 = arr3[1] as object[];
                    Assert.That(arr4.Length, Is.EqualTo(3));
                    for (l = 0; l < arr4.Length; l++)
                    {

                      if (l == 1)
                      {
                        object[] arr5 = arr4[1] as object[];
                        Assert.That(arr5.Length, Is.EqualTo(3));
                        for (m = 0; m < arr5.Length; m++)
                        {

                          if (m == 1)
                          {
                            object[] arr6 = arr5[1] as object[];
                            Assert.That(arr6.Length, Is.EqualTo(1));
                            if (arr6.Length == 1)
                              Assert.That(arr6[0], Is.EqualTo(4));
                          }
                          else
                            Assert.That(arr5[m], Is.EqualTo(5));
                          if (m == 2)
                            break;
                        }
                      }
                      else
                        Assert.That(arr4[l], Is.EqualTo(6));
                      if (l == 2)
                        break;

                    }
                  }
                  else
                    Assert.That(arr3[k], Is.EqualTo(7));
                  if (k == 2)
                    break;
                }
              }
              if (j == 0 || j == 2)
                Assert.That(arr2[j], Is.EqualTo(8));
              if (j == 2)
                break;
            }
          }
          else
            Assert.That(arr1[i], Is.EqualTo(9));
          if (i == 2)
            break;
        }
      }

      // Replace/Update in different Nesting Depths
      List<DbDoc> listDocs = new List<DbDoc>();
      listDocs.Add(documentsAsDbDocs[0]);
      listDocs.Add(new DbDoc(documentAsJsonString2));
      collection = CreateCollection("test");
      Result r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["title"], Is.EqualTo("AFRICAN EGG"));
      // Modify Using ID and Patch Title
      collection.Modify("_id = :id").Patch("{ \"title\": \"The African Egg\" }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["title"], Is.EqualTo("The African Egg"));

      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"])["age"], Is.EqualTo(57));
      // Modify Using additionalinfo.director.name = :director and Patch Age 67
      collection.Modify("additionalinfo.director.name = :director").
          Patch("{ \"additionalinfo\": { \"director\": { \"age\": 67 } } }").
          Bind("director", "Sharice Legaspi").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"])["age"], Is.EqualTo(67));

      // Modify Using _id=:id and Patch Age 77
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"age\": 77 } } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"])["age"], Is.EqualTo(77));

      // Modify Using _id=:id and Patch contains movie title
      collection.Modify("_id = :id").
          Patch("{ \"title\": { \"movie\": \"The African Egg\"} }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document["title"])["movie"], Is.EqualTo("The African Egg"));

      // Modify Using _id=:id and Patch additionalinfo Birthplace director country countryActual
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"birthplace\": { \"country\": { \"countryActual\": \"India\" } } } } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.That(document["additionalinfo.director.birthplace.country.countryActual"], Is.EqualTo("India"));

      // Modify Using _id=:id and Patch additionalinfo Birthplace director country countryActual"
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"birthplace\": { \"country\": { \"city\": \"NewDelhi\",\"countryActual\": \"India\" } } } } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.That(document["additionalinfo.director.birthplace.country.countryActual"], Is.EqualTo("India"));
      Assert.That(document["additionalinfo.director.birthplace.country.city"], Is.EqualTo("NewDelhi"));

      // Modify Using _id=:id and Patch additionalinfo Birthplace director country to replace an array
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"birthplace\": { \"country\": \"India\" } } } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.That(document["additionalinfo.director.birthplace.country"], Is.EqualTo("India"));

      // Modify Using _id=:id and Patch additionalinfo writers to replace an array
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"writers\": \"Jeoff Archer\" } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.That(document["additionalinfo.writers"], Is.EqualTo("Jeoff Archer"));

      //Reset the docs
      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      // Modify Using _id=:id and Patch contains blank
      collection.Modify("_id = :id").Patch("{ \"additionalinfo\": \"\" }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");


    }

    [Test, Description("Test valid modify.patch to add/remove fields at Depth for multiple arrays.))")]
    public void ModifyPatchAtDepthMultipleArrays_S1()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      Collection collection = CreateCollection("test");
      List<DbDoc> listDocs = new List<DbDoc>();
      listDocs.Add(documentsAsDbDocs[0]);
      listDocs.Add(new DbDoc(documentAsJsonString2));
      Result r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));
      object[] path = null;
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path = document["translations"] as object[]);

      collection.Modify("_id = :id").
          Patch("{ \"translations\": [\"Spanish\"] }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      path = document["translations"] as object[];
      Assert.That(path[0], Is.EqualTo("Spanish"),
          "Verify transalations field is present or not");
      collection.Modify("_id = :id").
          Patch("{ \"translations\": null }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path = document["translations"] as object[]);
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.mobile"]);

      collection.Modify("additionalinfo.director.age = :age").
          Patch("{ \"additionalinfo\": { \"director\": { \"mobile\": \"9876543210\" } } }").
          Bind("age", 57).Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["additionalinfo.director.mobile"], Is.EqualTo("9876543210"),
          "Verify mobile field is present or not");
      collection.Modify("additionalinfo.director.name = :director").
          Patch("{ \"additionalinfo\": { \"director\": { \"mobile\": null } } }").
          Bind("director", "Sharice Legaspi").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.mobile"]);
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.musicby"]);

      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["additionalinfo.musicby"], Is.EqualTo("The Sakila"),
       "Verify musicby field is present or not");
      collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": null } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.musicby"]);
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.awards.genre"]);

      collection.Modify("_id = :id").
          Patch("{\"additionalinfo\": { \"director\": { \"awards\": { \"genre\": \"Action\" } } } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["additionalinfo.director.awards.genre"], Is.EqualTo("Action"),
       "Verify musicby field is present or not");
      collection.Modify("_id = :id").
          Patch("{\"additionalinfo\": { \"director\": { \"awards\": { \"genre\": null } } } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.awards.genre"]);

    }

    [Test, Description("Test valid modify.patch to use expr to change existing keys in document))")]
    public void ModifyPatchAtDepthMultipleArrays_S2()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      DbDoc document = null;
      Collection collection = CreateCollection("test");
      Result r = collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", " +
          "\"startDate\": \"4/1/2017\" }").Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      var patch1 = new
      {
        email = new MySqlExpression("UPPER($.email)")
      };
      var patch2 = new
      {
        email = new MySqlExpression("LOWER($.email)")
      };
      var patch3 = new
      {
        email = new MySqlExpression("CONCAT('No', 'S', 'QL')")
      };
      var patch4 = new
      {
        email = new MySqlExpression("CHAR(77, 121, 83, 81, '76')")
      };
      var patch5 = new
      {
        email = new MySqlExpression("CONCAT('My', NULL, 'QL')")
      };
      var patch6 = new
      {
        email = new MySqlExpression("ELT(4, 'ej', 'Heja', 'hej', 'foo')")
      };
      var patch7 = new
      {
        email = new MySqlExpression("REPEAT('MySQL', 3)")
      };
      var patch8 = new
      {
        email = new MySqlExpression("REVERSE('abc')")
      };
      var patch9 = new
      {
        email = new MySqlExpression("RIGHT('foobarbar', 4)")
      };
      var patch10 = new
      {
        email = new MySqlExpression(" REPLACE('www.mysql.com', 'w', 'Ww')")
      };
      var patch11 = new
      {
        email = new MySqlExpression(" HEX('abc')")
      };
      var patch12 = new
      {
        email = new MySqlExpression(" BIN(12)")
      };

      collection.Modify("true").Patch(patch1).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("ALICE@ORA.COM"));
      collection.Modify("true").Patch(patch2).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("alice@ora.com"));
      collection.Modify("true").Patch(patch3).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("NoSQL"));
      collection.Modify("true").Patch(patch4).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("base64:type15:TXlTUUw="));
      collection.Modify("true").Patch(patch5).Execute();
      document = collection.GetOne("123");
      DbDoc test = null;
      Assert.Throws<InvalidOperationException>(()=> test = (DbDoc)document["email"]);

      collection = CreateCollection("test");
      r = collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", " +
          "\"startDate\": \"4/1/2017\" }").Execute();
      collection.Modify("true").Patch(patch6).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("foo"));
      collection.Modify("true").Patch(patch7).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("MySQLMySQLMySQL"));
      collection.Modify("true").Patch(patch8).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("cba"));
      collection.Modify("true").Patch(patch9).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("rbar"));
      collection.Modify("true").Patch(patch10).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("WwWwWw.mysql.com"));
      collection.Modify("true").Patch(patch11).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("616263"));
      collection.Modify("true").Patch(patch12).Execute();
      document = collection.GetOne("123");
      Assert.That(document["email"], Is.EqualTo("1100"));

      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": YEAR('2000-01-01') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(2000));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MONTH('2008-02-03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(2));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": WEEK('2008-02-20') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(7));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DAY('2008-02-20') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(20));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": HOUR('10:05:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(10));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MINUTE('2008-02-03 10:05:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(5));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": SECOND('10:05:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(3));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MICROSECOND('12:00:00.123456') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(123456));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": QUARTER('2008-04-01') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo(2));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": TIME('2003-12-31 01:02:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo("01:02:03.000000"));

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DATE('2003-12-31 01:02:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.That(document["dateAndTimeValue"], Is.EqualTo("2003-12-31"));

      List<DbDoc> listDocs = new List<DbDoc>();
      listDocs.Add(documentsAsDbDocs[0]);
      listDocs.Add(new DbDoc(documentAsJsonString2));
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      object[] actors = document["actors"] as object[];
      Assert.That(actors.Length, Is.EqualTo(3));
      Dictionary<string, object> actor0 = actors[0] as Dictionary<string, object>;

      Assert.Throws<KeyNotFoundException>(() => test = (DbDoc)actor0["age"]);

      t1 =
     "{\"_id\": \"1\", \"name\": \"Alice\", \"details\": {\"personal\":{\"mobile\": \"9876543210\", " +
     "\"DOB\": \"19 Mar 1982\", \"citizen\": \"USA\"}}}";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"yearofbirth\":" +
          "CAST(SUBSTRING_INDEX($.details.personal.DOB, ' ', -1) AS DECIMAL)}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Alice"));
      Assert.That(document["details.mobile"], Is.EqualTo("9876543210"));
      Assert.That(document["details.yearofbirth"].ToString(), Is.EqualTo("1982"));
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"currentyear\":" +
          "CURDATE()}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Alice"));
      Assert.That(document["details.mobile"], Is.EqualTo("9876543210"));
      Assert.That(document["details.currentyear"].ToString(), Is.EqualTo(document["details.currentyear"].ToString()), "Matching the current date");
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"currentyear\":" +
          "Year(CURDATE())}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Alice"));
      Assert.That(document["details.mobile"], Is.EqualTo("9876543210"));
      Assert.That(document["details.currentyear"].ToString(), Is.EqualTo(document["details.currentyear"].ToString()));
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"currentage\":" +
          "Year(CURDATE()) - CAST(SUBSTRING_INDEX($.details.personal.DOB, ' ', -1) AS DECIMAL)}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.That(document["name"], Is.EqualTo("Alice"));
      Assert.That(document["details.mobile"], Is.EqualTo("9876543210"));
      Assert.That(document["details.currentage"].ToString(), Is.EqualTo(document["details.currentage"].ToString()));

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      // Get root array.
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["audio"]);

      collection.Modify("true").
          Patch("{ \"audio\": CONCAT($.language, ', no subtitles') }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["audio"], Is.EqualTo("English, no subtitles"));

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["audio"]);

      collection.Modify("true").Patch("{ \"audio\": CONCAT(UPPER($.language), ', No Subtitles') }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["audio"], Is.EqualTo("ENGLISH, No Subtitles"));

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      collection.Modify("_id = :id").Patch("{ \"_id\": replace(UUID(), '-', '') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document.Id, Is.EqualTo("a6f4b93e1a264a108393524f29546a8c"));

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["additionalinfo._id"]);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": replace(UUID(), '-', '') } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document["additionalinfo"])["_id"] != null);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["additionalinfo.releasedate"]);

      try
      {
        collection.Modify("_id = :id").
        Patch("{ \"releasedate\": DATE_ADD('2006-04-00',INTERVAL 1 DAY) }").
        Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      }
      catch (MySqlException ex)
      {
        Assert.That(ex.Message, Is.EqualTo("Invalid data for update operation on document collection table"));
      }

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["title"], Is.EqualTo("AFRICAN EGG"));
      collection.Modify("_id = :id").Patch("{ \"title\": concat('my ', NULL, ' title') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["title"]);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["docfield"]);

      collection.Modify("_id = :id").Patch("{ \"docfield\": JSON_OBJECT('field 1', 1, 'field 2', 'two') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      object docField = document["docfield"] as object;

      Assert.That(docField != null);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(document["genre"], Is.EqualTo("Science fiction"));

      collection.Modify("_id = :id").Patch("{ \"genre\": JSON_OBJECT('name', 'Science Fiction') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.That(((Dictionary<string, object>)document["genre"])["name"], Is.EqualTo("Science Fiction"));

    }

    [Test, Description("Test invalid modify.patch to attempt to use invalid string (non JSON string/empty/NULL) as patch string.))")]
    public void ModifyPatchInvalidString()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = 12},
        new {_id = 2, title = "Book 2", pages = 30,age = 18},
        new {_id = 3, title = "Book 3", pages = 40,age = 34},
        new {_id = 4, title = "Book 4", pages = 50,age = 12}
      };
      Result r = collection.Add(docs).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(4));
      var document = collection.GetOne("1");
      var jsonParams = "invalidJsonString";
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("age = :age").Patch(jsonParams).Bind("age", "12")));

      jsonParams = "{invalidJsonString}";
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("age = :age").Patch(jsonParams).Bind("age", "12")));

      jsonParams = "{\"_id\":\"1004\",\"F1\": ] }";
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("age = :age").Patch(jsonParams).Bind("age", "12")));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch(null)));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch("")));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch(" ")));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch(string.Empty)));


      r = collection.Add(documentsAsJsonStrings).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      collection.Modify("true").Patch("{ \"_id\": NULL }").Execute();
      Assert.That(collection.Find().Execute().FetchOne().Id.ToString(), Is.EqualTo("1"));

      collection = CreateCollection("test");
      r = collection.Add(documentsAsJsonStrings).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      collection.Modify("true").Patch("{ \"nullfield\": NULL }").Execute();
      DbDoc test = null;
      Assert.Throws<InvalidOperationException> (() => test = (DbDoc)collection.Find().Execute().FetchOne()["nullfield"]);

      collection.Modify("true").Patch("{ \"nullfield\": [NULL, NULL] }").Execute();
      document = collection.Find().Execute().FetchOne();
      var nullArray = (object[])document["nullfield"];
      Assert.That(nullArray[0], Is.EqualTo(null));
      Assert.That(nullArray[1], Is.EqualTo(null));

      collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": NULL } }").Execute();
      document = collection.Find().Execute().FetchOne();
      Assert.That(((Dictionary<string, object>)document["nullfield"]).Count == 0, Is.EqualTo(true));

      collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": [NULL, NULL] } }").Execute();
      document = collection.Find().Execute().FetchOne();
      var nestedNullArray = (object[])((Dictionary<string, object>)document["nullfield"])["nested"];
      Assert.That(nestedNullArray[0], Is.EqualTo(null));
      Assert.That(nestedNullArray[1], Is.EqualTo(null));

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": NULL } }").Execute();
      Dictionary<string, object> test2 = null;
      Assert.Throws<InvalidOperationException>(() => test2 = ((Dictionary<string, object>)collection.Find().Execute().FetchOne()["additionalinfo.nullfield"]));

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": [NULL, NULL] } }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      nestedNullArray = (object[])((Dictionary<string, object>)document["additionalinfo"])["nullfield"];
      Assert.That(nestedNullArray[0], Is.EqualTo(null));
      Assert.That(nestedNullArray[1], Is.EqualTo(null));

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": [NULL, NULL] } } }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");

      nestedNullArray = (object[])((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["nullfield"]))["nested"]);
      Assert.That(nestedNullArray[0], Is.EqualTo(null));
      Assert.That(nestedNullArray[1], Is.EqualTo(null));

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": JSON_OBJECT('field', null) } } }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedObject = (Dictionary<string, object>)((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo.nullfield.nested"]));
      Assert.That(nestedObject.Count, Is.EqualTo(0));
    }

    #endregion WL14389

  }
}
