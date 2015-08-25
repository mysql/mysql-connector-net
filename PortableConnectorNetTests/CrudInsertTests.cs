using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.XDevAPI;
using Xunit;

namespace PortableConnectorNetTests
{
  public class CrudInsertTests : BaseTest
  {
    [Fact]
    public void InsertSingleJSONDocWithId()
    {
      Collection coll = CreateCollection("test");
      Result r = coll.Add("{ \"_id\": 1, \"foo\": 1 }").Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      coll.Drop();
    }

    [Fact]
    public void InsertSingleJSONDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      Result r = coll.Add("{ \"foo\": 1 }").Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      /// TODO:  retrieve doc and complete foo column
      coll.Drop();
    }

    [Fact]
    public void InsertAnonymousObjectWithId()
    {
      var obj = new { _id = "5", name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = coll.Add(obj).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      ///TODO:  pull object and verify data
      coll.Drop();
    }

    [Fact]
    public void InsertAnonymousObjectWithNoId()
    {
      var obj = new { name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = coll.Add(obj).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      ///TODO:  pull object and verify data
      coll.Drop();
    }

    [Fact]
    public void InsertMultipleAnonymousObjectsWithId()
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
      coll.Drop();
    }
  }
}
