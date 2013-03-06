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
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NUnit.Framework;


namespace MySql.Parser.Tests
{
  [TestFixture]
  public class CreateEvent
  {
    [Test]
    public void Simple()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT myevent
    ON SCHEDULE AT CURRENT_TIMESTAMP + INTERVAL 1 HOUR
    DO
      UPDATE myschema.mytable SET mycol = mycol + 1;", false);
    }

    [Test]
    public void Simple2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT e_totals
         ON SCHEDULE AT '2006-02-10 23:59:00'
         DO INSERT INTO test.totals VALUES (NOW())", false);
    }

    [Test]
    public void Simple3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT e_hourly
    ON SCHEDULE
      EVERY 1 HOUR
    COMMENT 'Clears out sessions table each hour.'
    DO
      DELETE FROM site_activity.sessions;", false);
    }

    [Test]
    public void Simple4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT e_daily
    ON SCHEDULE
      EVERY 1 DAY
    COMMENT 'Saves total number of sessions then clears the table each day'
    DO
      BEGIN
        INSERT INTO site_activity.totals (time, total)
          SELECT CURRENT_TIMESTAMP, COUNT(*)
            FROM site_activity.sessions;
        DELETE FROM site_activity.sessions;
      END", false);
    }

    [Test]
    public void Simple4a()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT e
    ON SCHEDULE
      EVERY 5 SECOND
    DO
      BEGIN
        DECLARE v INTEGER;
        DECLARE CONTINUE HANDLER FOR SQLEXCEPTION BEGIN END;

        SET v = 0;

        WHILE v < 5 DO
          INSERT INTO t1 VALUES (0);
          UPDATE t2 SET s1 = s1 + 1;
          SET v = v + 1;
        END WHILE;
    END", false);
    }

    [Test]
    public void Simple5()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT e_call_myproc
    ON SCHEDULE
      AT CURRENT_TIMESTAMP + INTERVAL 1 DAY
    DO CALL myproc(5, 27);", false);
    }

    [Test]
    public void Simple5In50()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE EVENT e_call_myproc
    ON SCHEDULE
      AT CURRENT_TIMESTAMP + INTERVAL 1 DAY
    DO CALL myproc(5, 27);", true, out sb, new Version( 5, 0 ));
      Assert.IsTrue(sb.ToString().IndexOf( "no viable alternative at input 'EVENT'" ) != -1 );
    }
  }
}
