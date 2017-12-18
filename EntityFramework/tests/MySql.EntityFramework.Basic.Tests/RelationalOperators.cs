// Copyright (c) 2013, 2017, Oracle and/or its affiliates. All rights reserved.
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


using System.Data;
using MySql.Data.MySqlClient;
using System.Data.Entity.Core.Objects;
using System.Data.Common;
using System.Linq;
using Xunit;
using System.Data.Entity.Infrastructure;

namespace MySql.Data.EntityFramework.Tests
{
  public class RelationalOperators : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public RelationalOperators(DefaultFixture fixture)
    {
      st = fixture;
      st.Setup(this.GetType());
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
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        st.TestESql<DbDataRecord>(
          @"(SELECT b.Id, b.Name FROM Books AS b) 
                UNION ALL (SELECT c.Id, c.Name FROM Companies AS c)",
          @"SELECT `UnionAll1`.`Id` AS `C1`, `UnionAll1`.`Id1` AS `C2`, `UnionAll1`.`Name` AS `C3`
            FROM ((SELECT `Extent1`.`Id`, `Extent1`.`Id` AS `Id1`, `Extent1`.`Name` FROM `Books` AS `Extent1`) 
            UNION ALL (SELECT `Extent2`.`Id`, `Extent2`.`Id` AS `Id1`, `Extent2`.`Name` 
            FROM `Companies` AS `Extent2`)) AS `UnionAll1`");
      }
    }

  }
}