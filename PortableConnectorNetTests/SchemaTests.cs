using MySql.XDevAPI;
using System.Collections.Generic;
using Xunit;

namespace PortableConnectorNetTests
{
  public class SchemaTests : BaseTest
  {
    [Fact]
    public void GetSchemas()
    {
      Session s = GetSession();
      List<Schema> schemas = s.GetSchemas();

      Assert.Equal(5, schemas.Count);
    }

    [Fact]
    public void GetInvalidSchema()
    {
      ///TODO:  what should this do?
      Session s = GetSession();
      Schema schema = s.GetSchema("test-schema");

    }
  }
}
