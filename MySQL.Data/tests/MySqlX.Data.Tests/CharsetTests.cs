// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class CharsetTests : BaseTest
  {
    /// <summary>
    /// Bug #26163703 SHOW COLLATION FAILS WITH MYSQL SERVER 8.0-WL#10561
    /// </summary>
    [Fact]
    public void IllegalMixCollations()
    {
      using (Session session = MySQLX.GetSession(ConnectionString))
      {
        var result = session.SQL("SHOW COLLATION WHERE `Default` ='Yes';").Execute();
        Assert.True(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=latin1"))
      {
        var result = session.SQL("SHOW COLLATION WHERE `Default` ='Yes';").Execute();
        Assert.True(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=utf8mb4"))
      {
        var result = session.SQL("SHOW COLLATION WHERE `Default` ='Yes';").Execute();
        Assert.True(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=utf-8"))
      {
        var result = session.SQL("SHOW COLLATION WHERE `Default` ='Yes';").Execute();
        Assert.True(result.HasData);
      }
    }
	
	/// <summary>
    /// Bug #26163678 VIEW.SELECT RETURNS TYPE INSTEAD OF THE TABLE-WL#10561
    /// Bug #26163667 COLLECTIONS.NAME RETURNS TYPE INSTEAD OF THE NAME OF THE COLLECTION-WL#10561
    /// </summary>
    [Fact]
    public void NamesAreReturnedAsStrings()
    {
      using (Session mySession = new Session(ConnectionString))
      {
        Schema test = mySession.GetSchema("test");

        ExecuteSQL("CREATE TABLE test1(id1 int,firstname varchar(20))");
        ExecuteSQL("INSERT INTO test1 values ('1','Rob')");
        ExecuteSQL("INSERT INTO test1 values ('2','Steve')");
        ExecuteSQL("CREATE TABLE test2(id2 int,lastname varchar(20))");
        ExecuteSQL("INSERT INTO test2 values ('1','Williams')");
        ExecuteSQL("INSERT INTO test2 values ('2','Waugh')");
        ExecuteSQL("CREATE VIEW view1 AS select * from test.test1");
        ExecuteSQL("SELECT * FROM view1");
        ExecuteSQL("CREATE VIEW view2 AS select * from test.test2");
        ExecuteSQL("SELECT * FROM view2");

        List<Table> tables = test.GetTables();
        Assert.Equal(4,tables.Count);
        Assert.Equal(2,tables.FindAll(i => !i.IsView).Count);
        Assert.Equal(2, tables.FindAll(i => i.IsView).Count);
        tables[0].Select().Execute();
        tables[1].Select().Execute();
        tables[2].Select().Execute();
        tables[3].Select().Execute();
        Assert.Equal("test1", tables[0].Name);
        Assert.Equal("test2", tables[1].Name);
        Assert.Equal("view1", tables[2].Name);
        Assert.Equal("view2", tables[3].Name);

        Table table = test.GetTable("test2");
        Assert.Equal("test2", table.Name);

        Collection c = test.CreateCollection("coll");

        List<Collection> collections = test.GetCollections();
        Assert.Equal(1, collections.Count);
        Assert.Equal("coll", collections[0].Name);

        Collection collection = test.GetCollection("coll");
        Assert.Equal("coll", collection.Name);
      }
    }
  }
}
