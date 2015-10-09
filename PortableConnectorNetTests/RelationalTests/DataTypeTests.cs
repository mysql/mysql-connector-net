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

using MySql.Data;
using MySql.XDevAPI.Relational;
using Xunit;

namespace PortableConnectorNetTests.RelationalTests
{
  public class DataTypeTests : BaseTest
  {
    [Fact]
    public void Float()
    {
      ExecuteSQL("CREATE TABLE test(rvalue FLOAT(14,8))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(float), r.Columns[0].ClrType);
      ///TODO:  file this bug report.  length is wrong I think
      Assert.Equal(14, (int)r.Columns[0].Length);
      Assert.Equal(8, (int)r.Columns[0].FractionalDigits);
      Assert.Equal(MySQLDbType.Float, r.Columns[0].DbType);
      Assert.Equal(3, rows.Count);
      Assert.Equal(23.4f, (float)rows[0][0]);
      Assert.Equal(14.8f, rows[1][0]);
      Assert.Equal(11.9f, rows[2][0]);
    }

    [Fact]
    public void Double()
    {
      ExecuteSQL("CREATE TABLE test(rvalue DOUBLE(12,4))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(double), r.Columns[0].ClrType);
      Assert.Equal(MySQLDbType.Double, r.Columns[0].DbType);
      ///TODO:  file this bug report.  length is wrong I think
      Assert.Equal(12, (int)r.Columns[0].Length);
      Assert.Equal(4, (int)r.Columns[0].FractionalDigits);
      Assert.Equal(3, rows.Count);
      Assert.Equal(23.4, rows[0][0]);
      Assert.Equal(14.8, rows[1][0]);
      Assert.Equal(11.9, rows[2][0]);
    }

    [Fact]
    public void Set()
    {
      ExecuteSQL("CREATE TABLE test(rvalue SET('A','B','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('A'), ('B,A'), ('B')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(string), r.Columns[0].ClrType);
      Assert.Equal(MySQLDbType.Set, r.Columns[0].DbType);
      Assert.Equal(3, rows.Count);
      Assert.Equal("A", rows[0][0]);
      Assert.Equal("A,B", rows[1][0]);
      Assert.Equal("B", rows[2][0]);
    }

    [Fact]
    public void Enum()
    {
      ExecuteSQL("CREATE TABLE test(rvalue Enum('Alpha','Beta','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('Alpha'), ('Beta'), ('C')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(string), r.Columns[0].ClrType);
      Assert.Equal(MySQLDbType.Enum, r.Columns[0].DbType);
      Assert.Equal(3, rows.Count);
      Assert.Equal("Alpha", rows[0][0]);
      Assert.Equal("Beta", rows[1][0]);
      Assert.Equal("C", rows[2][0]);
    }
  }
}
