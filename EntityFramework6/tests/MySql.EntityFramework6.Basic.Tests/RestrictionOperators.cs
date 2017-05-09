// Copyright � 2013 Oracle and/or its affiliates. All rights reserved.
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

using System.Data;
using MySql.Data.MySqlClient;
using System.Data.Entity.Core.Objects;
using System.Data.Common;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class RestrictionOperators : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public RestrictionOperators(DefaultFixture data)
    {
      st = data;
      st.Setup(this.GetType());
    }

    [Fact]
    public void SimpleSelectWithParam()
    {
      st.TestESql<Book>(
        "SELECT VALUE b FROM Books AS b WHERE b.Pages > @pages",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
          `Extent1`.`Author_Id` FROM `Books` AS `Extent1` WHERE `Extent1`.`Pages` > @pages",
        new ObjectParameter("pages", 200));
    }

    [Fact]
    public void WhereLiteralOnRelation()
    {
      st.TestESql<Author>(
        "SELECT VALUE a FROM Authors AS a WHERE a.Address.City = 'Dallas'",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, 
          `Extent1`.`Address_Street`, `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode`
          FROM `Authors` AS `Extent1` WHERE `Extent1`.`Address_City` = @gp1");
    }

    [Fact]
    public void WhereWithRelatedEntities1()
    {
      st.TestESql<Book>(
        "SELECT VALUE b FROM Books AS b WHERE b.Author.Address.State = 'TX'",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
          `Extent1`.`Author_Id` FROM `Books` AS `Extent1` INNER JOIN `Authors` AS `Extent2` 
          ON `Extent1`.`Author_Id` = `Extent2`.`Id` WHERE `Extent2`.`Address_State` = @gp1");
    }

    [Fact]
    public void Exists()
    {
      st.TestESql<Book>(
        @"SELECT VALUE a FROM Authors AS a WHERE EXISTS(
                    SELECT b FROM a.Books AS b WHERE b.Pages > 200)",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, 
         `Extent1`.`Address_Street`, `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode`
          FROM `Authors` AS `Extent1` WHERE EXISTS(SELECT 1 AS `C1` FROM `Books` AS `Extent2`
          WHERE (`Extent1`.`Id` = `Extent2`.`Author_Id`) AND (`Extent2`.`Pages` > 200))");
    }
  }
}