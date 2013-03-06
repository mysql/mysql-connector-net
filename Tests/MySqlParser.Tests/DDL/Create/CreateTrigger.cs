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
  public class CreateTrigger
  {
    [Test]
    public void Simple()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE TRIGGER testref BEFORE INSERT ON test1
  FOR EACH ROW BEGIN
    INSERT INTO test2 SET a2 = NEW.a1;
    DELETE FROM test3 WHERE a3 = NEW.a1;
    UPDATE test4 SET b4 = b4 + 1 WHERE a4 = NEW.a1;
  END;");
    }

    [Test]
    public void BeforeInsert()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE TRIGGER sdata_insert BEFORE INSERT ON `sometable`
FOR EACH ROW
BEGIN
SET NEW.guid = UUID();
END");
    }

    [Test]
    public void AfterInsert()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE TRIGGER sdata_insert AFTER INSERT ON `sometable`
FOR EACH ROW
BEGIN
SET NEW.guid = UUID();
END");
    }

    [Test]
    public void BeforeInsert2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CREATE TRIGGER user_insert BEFORE INSERT ON `user` FOR EACH ROW SET NEW.TimeStampCreated = NOW(), 
        NEW.Password = DES_ENCRYPT(NEW.Password);");
    }

    [Test]
    public void BeforeUpdate()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CREATE TRIGGER user_update BEFORE UPDATE ON `user` FOR EACH ROW SET NEW.Password = DES_ENCRYPT(NEW.Password);");
    }

    [Test]
    public void BeforeInsert3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CREATE TRIGGER mytrigger BEFORE INSERT ON TABLE_1 FOR EACH ROW SET NEW.MY_DATETIME_COLUMN = NOW(), 
        NEW.MY_DATE_COLUMN = CURDATE()");
    }

    [Test]
    public void ForEachRow()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE TRIGGER sanityCheck
BEFORE INSERT ON someTable
FOR EACH ROW
BEGIN
CALL doSanityCheck(@resultBool, @resultMessage);
IF @resultBool = 0 THEN
UPDATE ThereWasAnError_Call_privilegeSanityCheck_ToViewTheError SET ThereWas='an error';
END IF;
END;");
    }

    [Test]
    public void ForEachRow2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"CREATE TRIGGER sanityCheck
BEFORE INSERT ON my_table
FOR EACH ROW
BEGIN
IF something THEN
SET NEW.S_ID = 0 ;
END IF;
END;");
    }

//    [Test]
//    public void ForEachRow3()
//    {
//      MySQL51Parser.program_return r = Utility.ParseSql(@"create trigger trg_trigger_test_ins before insert on trigger_test
//for each row 
//begin
//declare msg varchar(255);
//if new.id < 0 then
//set msg = 
//  concat('MyTriggerError: Trying to insert a negative value in trigger_test: ', 
//    cast(new.id as char));
//signal sqlstate '45000' set message_text = msg;
//end if;
//end");
    //}
  }
}
