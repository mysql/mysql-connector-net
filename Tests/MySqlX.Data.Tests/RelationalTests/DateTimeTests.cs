// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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

using System;
using MySqlX.XDevAPI.Relational;
using Xunit;
using MySqlX.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class DateTimeTests : BaseTest
  {
    [Fact]
    public void DateTime()
    {
      ExecuteSQL("CREATE TABLE test.test(DT DATETIME)");
      ExecuteSQL("INSERT INTO test.test VALUES('2001-02-03 04:05:06')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(DateTime), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.DateTime, r.Columns[0].Type);
      Assert.Equal(1, rows.Count);
      DateTime dt = (DateTime)rows[0]["dt"];
      DateTime test = new DateTime(2001, 2, 3, 4, 5, 6);
      Assert.Equal(test, dt);
    }

    [Fact]
    public void Date()
    {
      ExecuteSQL("CREATE TABLE test.test(DT DATE)");
      ExecuteSQL("INSERT INTO test.test VALUES('2001-02-03')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(DateTime), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Date, r.Columns[0].Type);
      Assert.Equal(1, rows.Count);
      DateTime dt = (DateTime)rows[0]["dt"];
      DateTime test = new DateTime(2001, 2, 3);
      Assert.Equal(test, dt);
    }

    [Fact]
    public void Timestamp()
    {
      ExecuteSQL("CREATE TABLE test.test(DT TIMESTAMP)");
      ExecuteSQL("INSERT INTO test.test VALUES('2001-02-03')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(DateTime), r.Columns[0].ClrType);
      ///TODO:  this should support timestamp
      Assert.Equal(ColumnType.Timestamp, r.Columns[0].Type);
      Assert.Equal(1, rows.Count);
      DateTime dt = (DateTime)rows[0]["dt"];
      DateTime test = new DateTime(2001, 2, 3);
      Assert.Equal(test, dt);
    }

    [Fact]
    public void Time()
    {
      ExecuteSQL("CREATE TABLE test.test(DT TIME)");
      ExecuteSQL("INSERT INTO test.test VALUES('01:02:03')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(TimeSpan), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Time, r.Columns[0].Type);
      Assert.Equal(1, rows.Count);
      TimeSpan t = (TimeSpan)rows[0]["dt"];
      TimeSpan test = new TimeSpan(1, 2, 3);
      Assert.Equal(test, t);
    }

    [Fact]
    public void NegativeTime()
    {
      ExecuteSQL("CREATE TABLE test.test(DT TIME)");
      ExecuteSQL("INSERT INTO test.test VALUES('-01:02:03')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(TimeSpan), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Time, r.Columns[0].Type);
      Assert.Equal(1, rows.Count);
      TimeSpan t = (TimeSpan)rows[0]["dt"];
      TimeSpan test = new TimeSpan(-1, 2, 3);
      Assert.Equal(test, t);
    }
  }
}
