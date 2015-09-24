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

using MySql.XDevAPI.Common;
using MySql.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests
{
  public class TableAsyncTests : IClassFixture<TableFixture>
  {
    TableFixture fixture;

    public TableAsyncTests(TableFixture fixture)
    {
      this.fixture = fixture;

      fixture.GetNodeSession().SQL("DELETE FROM " + fixture.TableInsert).Execute();
    }

    [Fact]
    public void MultipleTableInsertAsync()
    {
      var table = fixture.GetTableInsert();
      List<Task<UpdateResult>> tasksList = new List<Task<UpdateResult>>();

      for (int i = 1; i <= 200; i++)
      {
        tasksList.Add(table.Insert().Values(i, i, i%250).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      foreach(Task<UpdateResult> task in tasksList)
      {
        Assert.True(task.Result.Succeeded);
      }
    }

    [Fact]
    public void MultipleTableSelectAsync()
    {
      var table = fixture.GetTable();

      List<Task<TableResult>> tasksList = new List<Task<TableResult>>();

      for(int i = 0; i < 20; i++)
      {
        tasksList.Add(table.Select().Limit(3, i).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      foreach (Task<TableResult> task in tasksList)
      {
        Assert.True(task.Result.Succeeded);
      }
    }
  }
}
