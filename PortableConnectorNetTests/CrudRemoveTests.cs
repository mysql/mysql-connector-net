using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.XDevAPI;
using Xunit;

namespace PortableConnectorNetTests
{
  public class CrudRemoveTests : BaseTest
  {
    [Fact]
    public void RemoveSingleDocumentById()
    {
      Collection coll = CreateCollection("test");
      var docs = new { _id = 12, title = "Book 1", pages = 20 };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);

      r = coll.Remove(12).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
    }

    [Fact]
    public void RemoveMultipleDocuments()
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

      r = coll.Remove("pages > 20").Execute();
      Assert.Equal<ulong>(3, r.RecordsAffected);
      coll.Drop();
    }

    [Fact]
    public void RemoveMultipleDocumentsWithLimit()
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

      r = coll.Remove("pages > 20").Limit(1).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      coll.Drop();
    }

    [Fact]
    public void RemoveMultipleDocumentsWithLimitAndOrder()
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

      r = coll.Remove("pages > 20").Limit(1).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      coll.Drop();
    }

    [Fact]
    public void RemovingDocWithNoIdThrowsException()
    {
      Collection coll = CreateCollection("test");
      JsonDoc doc = new JsonDoc();
      Exception ex = Assert.Throws<InvalidOperationException>(() => coll.Remove(doc));
    }

    [Fact]
    public void RemovingItemUsingJsonDoc()
    {
      Collection coll = CreateCollection("test");
      JsonDoc doc = new JsonDoc(new { _id = 1, title = "Book 1", pages = 20 });
      Result r = coll.Add(doc).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);

      r = coll.Remove(doc).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      coll.Drop();
    }
  }
}
