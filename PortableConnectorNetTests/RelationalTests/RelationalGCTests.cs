using MySql.XDevAPI;
using MySql.XDevAPI.Common;
using MySql.XDevAPI.CRUD;
using System;
using Xunit;

namespace PortableConnectorNetTests.ResultTests
{
  public class RelationalGCTests : BaseTest
  {
    [Fact]
    public void FetchAllNoReference()
    {
      Collection testColl = CreateCollection("test");
      var stmt = testColl.Add(@"{ ""_id"": 1, ""foo"": 1 }");
      stmt.Add(@"{ ""_id"": 2, ""foo"": 2 }");
      stmt.Add(@"{ ""_id"": 3, ""foo"": 3 }");
      stmt.Add(@"{ ""_id"": 4, ""foo"": 4 }");
      Result result = stmt.Execute();
      Assert.Equal(4, (int)result.RecordsAffected);

      var docResult = testColl.Find().Execute();
      var docs = docResult.FetchAll();
      WeakReference wr = new WeakReference(docResult);
      docResult = null;
      GC.Collect();
      Assert.False(wr.IsAlive);
      Assert.Equal(4, docs.Count);
    }
  }
}
