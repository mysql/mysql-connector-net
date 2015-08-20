using MySql.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests
{
  public class CollectionTests : BaseTest
  {
    [Fact]
    public void GetAllCollections()
    {
      Session s = GetSession();
      Schema schema = s.GetSchema("sakila");
      List<Collection> collections = schema.GetCollections();
    }

    [Fact]
    public void CreateAndDeleteCollection()
    {
      Session s = GetSession();
      Schema test = s.GetSchema("sakila");
      Collection books = test.CreateCollection("books");
      ///TODO:  use old c/net to confirm creation
//      test.DeleteCollection(books);
      ///TODO:  use old c/net to confirm remove
    }

  }
}
