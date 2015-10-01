// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySql.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests.RelationalTests
{
  public class TableInsertTests : IClassFixture<TableFixture>
  {
    TableFixture fixture;

    public TableInsertTests(TableFixture fixture)
    {
      this.fixture = fixture;

      fixture.GetNodeSession().SQL("DELETE FROM " + fixture.TableInsert).Execute();
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

      Assert.Equal(2, table.Count());
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
