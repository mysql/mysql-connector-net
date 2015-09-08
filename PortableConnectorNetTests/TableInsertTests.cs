using MySql.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests
{
  public class TableInsertTests : IClassFixture<TableFixture>
  {
    TableFixture fixture;

    public TableInsertTests(TableFixture fixture)
    {
      this.fixture = fixture;

      fixture.GetNodeSession().ExecuteSql("DELETE FROM " + fixture.TableInsert);
    }

    [Fact]
    public void InsertMultipleValues()
    {
      var table = fixture.GetTableInsert();

      var result = table.Insert("name", "age")
        .Values("Henry", "22")
        .Values("Patric", 30)
        .Execute();
      Assert.Equal<ulong>(2, result.RecordsAffected);

      var selectResult = table.Select().Execute();
      while (selectResult.Next()) ;
      Assert.Equal(2, selectResult.Rows.Count);
      Assert.Equal("Henry", selectResult.Rows.ToArray()[0][1]);
      Assert.Equal(22, (byte)selectResult.Rows.ToArray()[0][2]);
      Assert.Equal("Patric", selectResult.Rows.ToArray()[1][1]);
      Assert.Equal(30, (byte)selectResult.Rows.ToArray()[1][2]);
    }

    [Fact]
    public void InsertExpressions()
    {
      var table = fixture.GetTableInsert();

      var result = table.Insert("name", "age")
        .Values("upper('mark')", "50-16")
        .Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      var selectResult = table.Select().Execute();
      while (selectResult.Next()) ;
      Assert.Equal(1, selectResult.Rows.Count);
      Assert.Equal("MARK", (string)selectResult.Rows.ToArray()[0][1]);
      Assert.Equal(34, (byte)selectResult.Rows.ToArray()[0][2]);
    }
  }
}
