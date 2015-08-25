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
    public void RemoveSingleDocument()
    {

    }

    [Fact]
    public void RemoveMultipleDocuments()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema("test");
      Collection testCollection = schema.CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = testCollection.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);

      r = testCollection.Remove("pages > 20").Execute();
      Assert.Equal<ulong>(3, r.RecordsAffected);
    }
  }
}
