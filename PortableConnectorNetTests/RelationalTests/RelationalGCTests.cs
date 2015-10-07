using MySql.XDevAPI;
using MySql.XDevAPI.Common;
using MySql.XDevAPI.CRUD;
using MySql.XDevAPI.Relational;
using System;
using Xunit;

namespace PortableConnectorNetTests.ResultTests
{
  public class RelationalGCTests : BaseTest
  {
    [Fact]
    public void FetchAllNoReference()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      table.Insert("name", "age").Values("Henry", "22").Values("Patric", 30).Execute();
      var result = table.Select().Execute();
      var rows = result.FetchAll();
      WeakReference wr = new WeakReference(result);
      result = null;
      GC.Collect();
      Assert.False(wr.IsAlive);
      Assert.Equal(2, rows.Count);
      Assert.Equal(22, rows[0]["age"]);
      Assert.Equal("Patric", rows[1]["name"]);
    }
  }
}
