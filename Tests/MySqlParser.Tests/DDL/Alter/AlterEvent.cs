// Copyright © 2012, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace MySql.Parser.Tests
{
  [TestFixture]
  public class AlterEvent
  {
    [Test]
    public void Simple1()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"ALTER EVENT no_such_event 
          ON SCHEDULE 
            EVERY '2:3' DAY_HOUR;", false);
    }

    [Test]
    public void Simple2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT myevent
    ON SCHEDULE
      EVERY 6 HOUR
    COMMENT 'A sample comment.'
    DO
      UPDATE myschema.mytable SET mycol = mycol + 1;", false);
    }

    [Test]
    public void Simple3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"ALTER EVENT myevent
    ON SCHEDULE
      EVERY 12 HOUR
    STARTS CURRENT_TIMESTAMP + INTERVAL 4 HOUR;", false);
    }

    [Test]
    public void Simple4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"ALTER EVENT myevent
    ON SCHEDULE
      AT CURRENT_TIMESTAMP + INTERVAL 1 DAY
    DO
      TRUNCATE TABLE myschema.mytable;", false);
    }

    [Test]
    public void Simple5()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"ALTER EVENT myevent
    DISABLE;", false);
    }

    [Test]
    public void Simple6()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"ALTER EVENT myevent
    RENAME TO yourevent;", false);
    }

    [Test]
    public void Simple7()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"ALTER EVENT olddb.myevent
    RENAME TO newdb.myevent;", false);
    }
  }
}
