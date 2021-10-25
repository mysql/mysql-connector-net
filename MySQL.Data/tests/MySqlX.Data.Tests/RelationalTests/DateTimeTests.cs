// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class DateTimeTests : BaseTest
  {
    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");
    [Test]
    public void DateTime()
    {
      ExecuteSQL("CREATE TABLE test.test(DT DATETIME)");
      ExecuteSQL("INSERT INTO test.test VALUES('2001-02-03 04:05:06')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("dt"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(DateTime), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.DateTime, r.Columns[0].Type);
      Assert.That(rows, Has.One.Items);
      DateTime dt = (DateTime)rows[0]["dt"];
      DateTime test = new DateTime(2001, 2, 3, 4, 5, 6);
      Assert.AreEqual(test, dt);
    }

    [Test]
    public void Date()
    {
      ExecuteSQL("CREATE TABLE test.test(DT DATE)");
      ExecuteSQL("INSERT INTO test.test VALUES('2001-02-03')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("dt"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(DateTime), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Date, r.Columns[0].Type);
      Assert.That(rows, Has.One.Items);
      DateTime dt = (DateTime)rows[0]["dt"];
      DateTime test = new DateTime(2001, 2, 3);
      Assert.AreEqual(test, dt);
    }

    [Test]
    public void Timestamp()
    {
      ExecuteSQL("CREATE TABLE test.test(DT TIMESTAMP)");
      ExecuteSQL("INSERT INTO test.test VALUES('2001-02-03')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("dt"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(DateTime), r.Columns[0].ClrType);
      //TODO:  this should support timestamp
      Assert.AreEqual(ColumnType.Timestamp, r.Columns[0].Type);
      Assert.That(rows, Has.One.Items);
      DateTime dt = (DateTime)rows[0]["dt"];
      DateTime test = new DateTime(2001, 2, 3);
      Assert.AreEqual(test, dt);
    }

    [Test]
    public void Time()
    {
      ExecuteSQL("CREATE TABLE test.test(DT TIME)");
      ExecuteSQL("INSERT INTO test.test VALUES('01:02:03')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("dt"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(TimeSpan), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Time, r.Columns[0].Type);
      Assert.That(rows, Has.One.Items);
      TimeSpan t = (TimeSpan)rows[0]["dt"];
      TimeSpan test = new TimeSpan(1, 2, 3);
      Assert.AreEqual(test, t);
    }

    [Test]
    public void NegativeTime()
    {
      ExecuteSQL("CREATE TABLE test.test(DT TIME)");
      ExecuteSQL("INSERT INTO test.test VALUES('-01:02:03')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("dt"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(TimeSpan), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Time, r.Columns[0].Type);
      Assert.That(rows, Has.One.Items);
      TimeSpan t = (TimeSpan)rows[0]["dt"];
      TimeSpan test = new TimeSpan(-1, 2, 3);
      Assert.AreEqual(test, t);
    }
  }
}
