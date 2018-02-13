// Copyright (c) 2014, 2017, Oracle and/or its affiliates. All rights reserved.
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

using System.Linq;
using Xunit;

namespace MySql.Data.EntityFramework.Tests
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
