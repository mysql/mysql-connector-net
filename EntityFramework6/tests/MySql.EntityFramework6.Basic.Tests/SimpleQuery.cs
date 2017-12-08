// Copyright (c) 2014, 2017 Oracle and/or its affiliates. All rights reserved.
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

using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{

  public class SimpleQuery : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public SimpleQuery(DefaultFixture fixture)
    {
      st = fixture;
      st.Setup(this.GetType());
    }

    [Fact]
    public void SimpleFindAll()
    {

      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var q = from b in ctx.Books select b;
        string sql = q.ToString();

        var expected = @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
                        `Extent1`.`Author_Id` FROM `Books` AS `Extent1`";
        st.CheckSql(sql, expected);
      }
    }

    [Fact]
    public void SimpleFindAllWithCondition()
    {

      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var q = from b in ctx.Books where b.Id == 1 select b;
        string sql = q.ToString();

        var expected = @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
                        `Extent1`.`Author_Id` FROM `Books` AS `Extent1` WHERE 1 = `Extent1`.`Id`";
        st.CheckSql(sql, expected);
      }
    }

  }
}
