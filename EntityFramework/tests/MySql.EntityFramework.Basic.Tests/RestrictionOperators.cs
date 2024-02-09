// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using System.Data.Entity.Core.Objects;
using NUnit.Framework;

namespace MySql.Data.EntityFramework.Tests
{
  public class RestrictionOperators : DefaultFixture
  {
    [Test]
    public void SimpleSelectWithParam()
    {
      TestESql<Book>(
        "SELECT VALUE b FROM Books AS b WHERE b.Pages > @pages",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
          `Extent1`.`Author_Id` FROM `Books` AS `Extent1` WHERE `Extent1`.`Pages` > @pages",
        new ObjectParameter("pages", 200));
    }

    [Test]
    public void WhereLiteralOnRelation()
    {
      TestESql<Author>(
        "SELECT VALUE a FROM Authors AS a WHERE a.Address.City = 'Dallas'",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, 
          `Extent1`.`Address_Street`, `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode`
          FROM `Authors` AS `Extent1` WHERE `Extent1`.`Address_City` = @gp1");
    }

    [Test]
    public void WhereWithRelatedEntities1()
    {
      TestESql<Book>(
        "SELECT VALUE b FROM Books AS b WHERE b.Author.Address.State = 'TX'",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
          `Extent1`.`Author_Id` FROM `Books` AS `Extent1` INNER JOIN `Authors` AS `Extent2` 
          ON `Extent1`.`Author_Id` = `Extent2`.`Id` WHERE `Extent2`.`Address_State` = @gp1");
    }

    [Test]
    public void Exists()
    {
      TestESql<Book>(
        @"SELECT VALUE a FROM Authors AS a WHERE EXISTS(
                    SELECT b FROM a.Books AS b WHERE b.Pages > 200)",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, 
         `Extent1`.`Address_Street`, `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode`
          FROM `Authors` AS `Extent1` WHERE EXISTS(SELECT 1 AS `C1` FROM `Books` AS `Extent2`
          WHERE (`Extent1`.`Id` = `Extent2`.`Author_Id`) AND (`Extent2`.`Pages` > 200))");
    }
  }
}
