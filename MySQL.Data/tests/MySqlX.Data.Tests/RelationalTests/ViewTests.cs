// Copyright (c) 2016, 2020, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class ViewTests : BaseTest
  {
    [Test]
    public void TryUpdatingView()
    {
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select *, 1 from test");

      List<Table> tables = testSchema.GetTables();
      Assert.AreEqual(2, tables.Count);

      Table view = tables.First(i => i.IsView);
      Assert.AreEqual("view1", view.Name);
      MySqlException ex = Assert.Throws<MySqlException>(() => ExecuteInsertStatement(view.Insert().Values(1)));
      Assert.AreEqual("Column '1' is not updatable", ex.Message);
    }

    [Test]
    public void GetView()
    {
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select *, 1 from test");

      Table table = testSchema.GetTable("test");
      Table view = testSchema.GetTable("view1");
      Assert.True(view.IsView);
      Assert.False(table.IsView);
    }

    //private bool IsView() => testSchema.GetTable("no_exists").IsView;

    [Test]
    public void NonExistingView()
    {
      bool isView;
      void IsView() { isView = testSchema.GetTable("no_exists").IsView; };
      Assert.Throws<MySqlException>(() => IsView());
    }
  }
}
