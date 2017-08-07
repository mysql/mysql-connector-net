// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using Mysqlx.Expr;
using MySqlX.Protocol.X;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using System.Net.Sockets;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class BasicFindTests : BaseTest
  {
    [Fact]
    public void SimpleFind()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      DocResult foundDocs = coll.Find("pages > 20").Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 2");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 3");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 4");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void SimpleFindWithSort()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      DocResult foundDocs = coll.Find("pages > 20").OrderBy("pages DESC").Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 4");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 3");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 2");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void SimpleFindWithLimit()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      DocResult foundDocs = coll.Find("pages > 20").Limit(1).Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 2");
      Assert.False(foundDocs.Next());

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => coll.Find().Limit(0).Execute());
      Assert.Throws<ArgumentOutOfRangeException>(() => coll.Find().Limit(-1).Execute());
    }

    [Fact]
    public void FindConditional()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      DocResult foundDocs = coll.Find("pages = :Pages").Bind("pAges", 40).Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 3");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void BindDbDoc()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      //var s = MySql.Data.ResourcesX.TestingResources;

      DbDoc docParams = new DbDoc(new { pages1 = 30, pages2 = 40 });
      DocResult foundDocs = coll.Find("pages = :Pages1 || pages = :Pages2").Bind(docParams).Execute();
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void BindJsonAsAnonymous()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      var jsonParams = new { pages1 = 30, pages2 = 40 };
      DocResult foundDocs = coll.Find("pages = :Pages1 || pages = :Pages2").Bind(jsonParams).Execute();
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void BindJsonAsString()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      var jsonParams = "{ \"pages1\" : 30, \"pages2\" : 40 }";
      DocResult foundDocs = coll.Find("pages = :Pages1 || pages = :Pages2").Bind(jsonParams).Execute();
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void InOperatorWithListOfValues()
    {
      // Validates the IN operator allows expressions of the type
      // ( compExpr ["NOT"] "IN" "(" argsList ")" ) | ( compExpr ["NOT"] "IN" "[" argsList "]" )
      Collection coll = CreateCollection("test");
      coll.Add(new DbDoc("{ \"a\": 1, \"b\": [ 1, \"value\" ] }")).Execute();

      Assert.Equal(1, coll.Find("a IN (1,2,3)").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("a not in (0,2,3)").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("b[0] in (1,2,3)").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("b[1] in (\"a\", \"b\", \"value\")").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("b[0] NOT IN (0,2,3)").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("b[1] not in (\"a\", \"b\", \"c\")").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("a in [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("a in [2,3,4]").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("a NOT in [0,2,3]").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("b not IN [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("b[0] not IN [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("c NOT IN [1,2,3]").Execute().FetchAll().Count);

      Collection movies = CreateCollection("movies");
      var docString = "{ \"_id\" : \"a6f4b93e1a264a108393524f29546a8c\", \"title\" : \"AFRICAN EGG\", \"description\" : \"A Fast-Paced Documentary of a Pastry Chef And a Dentist who must Pursue a Forensic Psychologist in The Gulf of Mexico\", \"releaseyear\" : 2006, \"language\" : \"English\", \"duration\" : 130, \"rating\" : \"G\", \"genre\" : \"Science fiction\", \"actors\" : [{ \"name\" : \"MILLA PECK\", \"country\" : \"Mexico\", \"birthdate\": \"12 Jan 1984\"}, { \"name\" : \"VAL BOLGER\", \"country\" : \"Botswana\", \"birthdate\": \"26 Jul 1975\" }, { \"name\" : \"SCARLETT BENING\", \"country\" : \"Syria\", \"birthdate\": \"16 Mar 1978\" }], \"additionalinfo\" : { \"director\" : \"Sharice Legaspi\", \"writers\" : [\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"], \"productioncompanies\" : [\"Qvodrill\", \"Indigoholdings\"] } }";
      movies.Add(new DbDoc(docString)).Execute();

      Assert.Equal(1, movies.Find("(1>5) in (true, false)").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("(1+5) in (1, 2, 3, 4, 5)").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("('a'>'b') in (true, false)").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("(1>5) in [true, false]").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("(1+5) in [1, 2, 3, 4, 5]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("('a'>'b') in [true, false]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("true IN [(1>5), !(false), (true || false), (false && true)]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("true IN ((1>5), !(false), (true || false), (false && true))").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("{\"field\":true} IN (\"mystring\", 124, myvar, othervar.jsonobj)").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("actor.name IN ['a name', null, (1<5-4), myvar.jsonobj.name]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("!false && true IN [true]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("1-5/2*2 > 3-2/1*2 IN [true, false]").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("true IN [1-5/2*2 > 3-2/1*2]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find(" 'African Egg' IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find(" 1 IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find(" [0,1,2] IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find(" { 'title' : 'Atomic Firefighter' } IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("title IN ('African Egg', 'The Witcher', 'Jurassic Perk')").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("releaseyear IN (2006, 2010, 2017)").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("1 IN [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("0 IN [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("0 NOT IN [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("1 NOT IN [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("releaseyear IN [2006, 2007, 2008]").Execute().FetchAll().Count);
    }

    [Fact]
    public void InOperatorWithCompExpr()
    {
      // Validates the IN operator allows expressions of the type: compExpr ["NOT"] "IN" compExpr
      Collection coll = CreateCollection("test");
      var docString = "{ \"a\": 1, \"b\": \"foo\", \"c\": { \"d\": true, \"e\": [1,2,3] }, \"f\": [ {\"x\":5}, {\"x\":7 } ] }";
      coll.Add(new DbDoc(docString)).Execute();

      Assert.Equal(1, coll.Find("a in [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("c.e[0] in [1,2,3]").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("5 in f[*].x").Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("3 in c.e").Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("5 in c.e").Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("\"foo\" in " + docString).Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("\"a\" in " + docString).Execute().FetchAll().Count);
      Assert.Equal(0, coll.Find("a in " + docString).Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("{\"a\":1} in " + docString).Execute().FetchAll().Count);
      Assert.Equal(1, coll.Find("\"foo\" in b").Execute().FetchAll().Count);

      Collection movies = CreateCollection("movies");
      docString = "{ \"_id\" : \"a6f4b93e1a264a108393524f29546a8c\", \"title\" : \"AFRICAN EGG\", \"description\" : \"A Fast-Paced Documentary of a Pastry Chef And a Dentist who must Pursue a Forensic Psychologist in The Gulf of Mexico\", \"releaseyear\" : 2006, \"language\" : \"English\", \"duration\" : 130, \"rating\" : \"G\", \"genre\" : \"Science fiction\", \"actors\" : [{ \"name\" : \"MILLA PECK\", \"country\" : \"Mexico\", \"birthdate\": \"12 Jan 1984\"}, { \"name\" : \"VAL BOLGER\", \"country\" : \"Botswana\", \"birthdate\": \"26 Jul 1975\" }, { \"name\" : \"SCARLETT BENING\", \"country\" : \"Syria\", \"birthdate\": \"16 Mar 1978\" }], \"additionalinfo\" : { \"director\" : \"Sharice Legaspi\", \"writers\" : [\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"], \"productioncompanies\" : [\"Qvodrill\", \"Indigoholdings\"] } }";
      movies.Add(new DbDoc(docString)).Execute();

      Assert.Equal(1, movies.Find("{ \"name\" : \"MILLA PECK\" } IN actors").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("'African Egg' in movietitle").Execute().FetchAll().Count);
      Assert.Throws<MySqlException>(() => movies.Find("(1 = NULL) IN title").Execute().FetchAll().Count);
      Assert.Throws<MySqlException>(() => movies.Find("NOT NULL IN title").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("[\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"] IN additionalinfo.writers").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("{ \"name\" : \"MILLA PECK\", \"country\" : \"Mexico\", \"birthdate\": \"12 Jan 1984\"} IN actors").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("true IN title").Execute().FetchAll().Count);
      Assert.Equal(0, movies.Find("false IN genre").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("'Sharice Legaspi' IN additionalinfo.director").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("'Mexico' IN actors[*].country").Execute().FetchAll().Count);
      Assert.Equal(1, movies.Find("'Angelic Orduno' IN additionalinfo.writers").Execute().FetchAll().Count);
    }
  }
}

