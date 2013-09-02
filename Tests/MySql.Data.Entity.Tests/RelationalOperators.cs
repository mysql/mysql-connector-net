// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
#if !EF6
using System.Data.EntityClient;
using System.Data.Objects; 
#else
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
#endif
using System.Data.Common;
using MySql.Data.Entity.Tests.Properties;
using System.Linq;
using Xunit;
using MySql.Data.Entity.Tests;

namespace MySql.Data.Entity.Tests
{
  public class RelationalOperators : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

    [Fact]
    public void Except()
    {
      /*            using (TestDB.TestDB db = new TestDB.TestDB())
                  {
                      var q = from c in db.Companies where 
                      var query = from o in db.Orders
                                  where o.StoreId = 3
                                  select o;

                      var result = query.First();
                  }*/
    }

    [Fact]
    public void Intersect()
    {
    }

    [Fact]
    public void CrossJoin()
    {
    }

    [Fact]
    public void Union()
    {
    }

    [Fact]
    public void UnionAll()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter(
            "SELECT t.Id FROM Toys t UNION ALL SELECT c.Id FROM Companies c", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        string entitySQL = @"(SELECT t.Id, t.Name FROM Toys AS t) 
                UNION ALL (SELECT c.Id, c.Name FROM Companies AS c)";
        ObjectQuery<DbDataRecord> query = context.CreateQuery<DbDataRecord>(entitySQL);

        string sql = query.ToTraceString();
        st.CheckSql(sql, SQLSyntax.UnionAll);

        int i = 0;
        foreach (DbDataRecord r in query)
        {
          i++;
        }
        Assert.Equal(dt.Rows.Count, i);
      }
    }

    /// <summary>
    /// Bug #60652	Query returns BLOB type but no BLOBs are in the database.        
    /// </summary>
    [Fact]
    public void UnionAllWithBitColumnsDoesNotThrow()
    {
      using (testEntities entities = new testEntities())
      {
        // Here, Computer is the base type of DesktopComputer, LaptopComputer and TabletComputer. 
        // LaptopComputer and TabletComputer include the bit fields that would provoke
        // an InvalidCastException (byte[] to bool) when participating in a UNION 
        // created internally by the Connector/Net entity framework provider.
        var computers = from c in entities.Computers
                        select c;

        foreach (Computer computer in computers)
        {
          Assert.NotNull(computer);
          Assert.True(computer.Id > 0);
        }
      }
    }
  }
}