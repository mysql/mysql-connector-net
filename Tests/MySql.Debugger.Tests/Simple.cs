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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Debugger;
using MySql.Parser;
using MySql.Data.MySqlClient;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Xunit;

namespace MySql.Debugger.Tests
{
  public class Simple : IUseFixture<SetUp>
  {
    private SetUp st;

    public void SetFixture(SetUp data)
    {
      st = data;
    }


    [Fact]
    public void VerySimpleTest()
    {
      string sql = 
@"create procedure spTest()
begin
    declare n int;
    set n = 1;
    while n < 5 do
    begin
    
        set n = n + 1;
    
    end;
    end while;

end;
";
      
      Debugger dbg = new Debugger();
      try
      {
        dbg.SqlInput = sql;
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, string.Format( "delimiter // drop procedure if exists spTest; {0} //", sql ));
        script.Execute();
        Watch w = dbg.SetWatch("n");
        dbg.SetBreakpoint( sql, 8);
        dbg.SetBreakpoint( sql, 13);
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        bool bpHit = false;
        int i = 0;
        dbg.OnBreakpoint += (bp) =>
        {
          bpHit = true;
          int val = 0;
          if (bp.Line == 8 || bp.Line == 13)
          {
            val = Convert.ToInt32(w.Eval());
          }
          if (bp.Line == 8)
          {
            Assert.Equal(++i, val);
            Debug.Write(val);
            Debug.WriteLine(" within simpleproc");
          }
          else if (bp.Line == 13)
          {
            Assert.Equal( 5, val );
            Debug.Write(val);
            Debug.WriteLine(" within simpleproc");
          }
        };
        dbg.Run(new string[0], null);
        Assert.True(bpHit);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void NonScalarFunction()
    {
      string sql =
        @"delimiter //
drop procedure if exists `SimpleNonScalar` //
CREATE PROCEDURE `SimpleNonScalar`()
begin
 
    update calcdata set z = DoSum( x, y );

end //
drop function if exists `DoSum`
//
CREATE FUNCTION `DoSum`( a int, b int ) RETURNS int(11)
begin

    declare a1 int;
    declare b1 int;
    
    set a1 = a;
    set b1 = b;
    return a1 + b1;

end
//
drop table if exists `calcdata`;
//
CREATE TABLE `calcdata` (
  `x` int(11) DEFAULT NULL,
  `y` int(11) DEFAULT NULL,
  `z` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //
insert into `calcdata`( x, y, z ) values ( 5, 10, 0 ) //
insert into `calcdata`( x, y, z ) values ( 8, 4, 0 ) //
insert into `calcdata`( x, y, z ) values ( 6, 7, 0 ) //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE `SimpleNonScalar`()
begin
 
    update calcdata set z = DoSum( x, y );

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void ScalarFunctionCall2()
    {
      string sql =
        @"delimiter //
drop procedure if exists `SimpleScalar` //
CREATE PROCEDURE `SimpleScalar`()
begin
 
    update calcdata set z = DoSum( x, y ) where x = 5;

end //
drop function if exists `DoSum`
//
CREATE FUNCTION `DoSum`( a int, b int ) RETURNS int(11)
begin

    declare a1 int;
    declare b1 int;
    
    set a1 = a;
    set b1 = b;
    return a1 + b1;

end
//
drop table if exists `calcdata`;
//
CREATE TABLE `calcdata` (
  `x` int(11) DEFAULT NULL,
  `y` int(11) DEFAULT NULL,
  `z` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //
insert into `calcdata`( x, y, z ) values ( 5, 10, 0 ) //
insert into `calcdata`( x, y, z ) values ( 8, 4, 0 ) //
insert into `calcdata`( x, y, z ) values ( 6, 7, 0 ) //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE `SimpleScalar`()
begin
 
    update calcdata set z = DoSum( x, y ) where x = 5;

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) => {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
          if ( (bp.RoutineName == "test6.DoSum") && ( bp.Line == 9 ) )
          {
            dbg.CurrentScope.Variables["a1"].Value = 100;
            dbg.CommitLocals(); 
          }
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void NestedCall()
    {
      string sql =
        @"delimiter //
drop procedure if exists `NestedCall` //
CREATE PROCEDURE `NestedCall`()
begin
 
    call dummyCall();

end //
drop procedure if exists `DummyCall`
//
create procedure DummyCall()
begin

    update calcdata set z = -1;

end
//
drop table if exists `calcdata`;
//
CREATE TABLE `calcdata` (
  `x` int(11) DEFAULT NULL,
  `y` int(11) DEFAULT NULL,
  `z` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //
insert into `calcdata`( x, y, z ) values ( 5, 10, 0 ) //
insert into `calcdata`( x, y, z ) values ( 8, 4, 0 ) //
insert into `calcdata`( x, y, z ) values ( 6, 7, 0 ) //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE `NestedCall`()
begin
 
    call dummyCall();

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) => {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void NestedCallWithVars()
    {
      string sql =
        @"delimiter //
drop procedure if exists `NestedCall` //
CREATE PROCEDURE `NestedCall`()
begin
 
    declare val int;
    call dummyCall( val );

end //
drop procedure if exists `DummyCall`
//
create procedure DummyCall()
begin

    update calcdata set z = -1;

end
//
drop table if exists `calcdata`;
//
CREATE TABLE `calcdata` (
  `x` int(11) DEFAULT NULL,
  `y` int(11) DEFAULT NULL,
  `z` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 //
insert into `calcdata`( x, y, z ) values ( 5, 10, 0 ) //
insert into `calcdata`( x, y, z ) values ( 8, 4, 0 ) //
insert into `calcdata`( x, y, z ) values ( 6, 7, 0 ) //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE `NestedCall`()
begin
 
    call dummyCall();

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void ScalarFunctionCall()
    {
      string sql =
        @"delimiter //

drop procedure if exists NestedFunction //

create procedure NestedFunction()
begin

    declare val int;    
    set val = DoSum( 1, 2 );
    set val = val + 2;

end
   //
drop function if exists `DoSum`
//
CREATE FUNCTION `DoSum`( a int, b int ) RETURNS int(11)
not deterministic modifies sql data
begin

declare a1 int;
   declare b1 int;
    
    set a1 = a;
    set b1 = b;
    return a1 + b1;
#return a + b;

end
//
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure NestedFunction()
begin

    declare val1 int;
    set val1 = DoSum( 1, 2 );
    set val1 = val1 + 2;

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void CommaSeparatedDeclare()
    {
      string sql =
        @"delimiter //
drop procedure if exists spTest2 //

create DEFINER=`root`@`localhost` PROCEDURE `spTest2`()
begin
    declare n,x,y,z int;
  declare str varchar(1100);
    set n = 1;
  set str = 'Armando';

    while n < 1000 do
    begin
    
        set n = n + 1;
    set x = n * 2;
    set y = n * 5;
    set z = n * 10;
    set str = CONCAT(str, 'o');
    
    end;
    end while;

end
//
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create DEFINER=`root`@`localhost` PROCEDURE `spTest2`()
begin
    declare n,x,y,z int;
  declare str varchar(1100);
    set n = 1;
  set str = 'Armando';

    while n < 10 do
    begin
    
        set n = n + 1;
    set x = n * 2;
    set y = n * 5;
    set z = n * 10;
    set str = CONCAT(str, 'o');
    
    end;
    end while;

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) => {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void LoopWithIfs()
    {
      string sql =
        @"delimiter //
drop procedure if exists doloopif //

DELIMITER //
CREATE PROCEDURE doloopif (p1 INT)
BEGIN
  DECLARE var_x INT;
  SET var_x=0;
  loop_test: LOOP
    IF var_x < p1 THEN
      SET var_x = var_x+1;
    ELSE
      LEAVE loop_test;
    END IF;
  END LOOP loop_test;
  SELECT CONCAT ('The final LOOP and IF number is: ', var_x) AS Results;
END
//
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE doloopif (p1 INT)
BEGIN
  DECLARE var_x INT;
  SET var_x=0;
  loop_test: LOOP
    IF var_x < p1 THEN
      SET var_x = var_x+1;
    ELSE
      LEAVE loop_test;
    END IF;
  END LOOP loop_test;
  SELECT CONCAT ('The final LOOP and IF number is: ', var_x) AS Results;
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[1] { "3" }, null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void DoHandler()
    {
      string sql =
        @"
delimiter //

drop table if exists d_table2 //

CREATE TABLE d_table2 (s1 int, primary key (s1)) //

drop procedure if exists dohandler //

DELIMITER //
CREATE PROCEDURE dohandler()
BEGIN
  DECLARE dup_keys CONDITION FOR  SQLSTATE '23000';
  DECLARE CONTINUE HANDLER FOR dup_keys SET @GARBAGE = 1;
  SET @x = 1;
  INSERT INTO d_table2 VALUES (1);
  SET @x = 2;
  INSERT INTO d_table2 VALUES (1);
  set @x = 3;

END //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE dohandler()
BEGIN
  DECLARE dup_keys CONDITION FOR  SQLSTATE '23000';
  DECLARE CONTINUE HANDLER FOR dup_keys SET @GARBAGE = 1;
  SET @x = 1;
  INSERT INTO d_table2 VALUES (1);
  SET @x = 2;
  INSERT INTO d_table2 VALUES (1);
  set @x = 3;

END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void DoRepeat()
    {
      string sql =
        @"
delimiter //

drop procedure if exists DoRepeat //

DELIMITER //
CREATE PROCEDURE DoRepeat()
BEGIN
  DECLARE i INT default 3;
  DECLARE done1 INT default 0;  
  
  retry: REPEAT
  begin
  DECLARE CONTINUE HANDLER FOR SQLWARNING
          BEGIN
            SET done1 = TRUE;
          END;  
        IF done1 OR i < 0 THEN
          LEAVE retry;
        END IF;
        SET i = i - 1;      
        end;
    UNTIL FALSE END REPEAT;
END  //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE DoRepeat()
BEGIN
  DECLARE i INT default 3;
  DECLARE done1 INT default 0;  
  
  retry: REPEAT
  begin
  DECLARE CONTINUE HANDLER FOR SQLWARNING
          BEGIN
            SET done1 = TRUE;
          END;  
        IF done1 OR i < 0 THEN
          LEAVE retry;
        END IF;
        SET i = i - 1;      
        end;
    UNTIL FALSE END REPEAT;
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void MutipleInsert()
    {
      string sql =
        @"
delimiter //

drop procedure if exists MultipleInsert //

drop table if exists test3 //

DELIMITER //
create procedure MultipleInsert( id int, name varchar( 10 ))
begin
  create table test3( id2 int );
  insert into test3 values (1);
end //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure MultipleInsert( id int, name varchar( 10 ))
begin
  create table test3( id2 int );
  insert into test3 values (1);
end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[] { "3", "'a'" }, null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void SteppingIntoTriggers()
    {
      string sql =
        @"
delimiter //

drop table if exists triggertable //

create table triggertable ( 
  myid int,
  myname varchar( 30 )
) //

create trigger trTriggerTable before insert on triggertable for each row
begin

    set new.myid = new.myid + 1;

end //

drop procedure if exists DoInsertTriggerTable //

create procedure DoInsertTriggerTable()
begin

  insert into triggertable( myid, myname ) values ( 1, 'val' );

end //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure DoInsertTriggerTable()
begin

  insert into triggertable( myid, myname ) values ( 1, 'val' );

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void SteppingIntoTriggers2()
    {
      string sql =
        @"
delimiter //

drop table if exists triggertable //

create table triggertable ( 
  myid int,
  myname varchar( 30 )
) //

create trigger trTriggerTable before insert on triggertable for each row
begin

    set new.myid = new.myid + 1;

end //

drop procedure if exists DoInsertTriggerTable //

create procedure DoInsertTriggerTable()
begin

replace into triggertable( myid, myname ) values ( 1, 'val' );

end //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure DoInsertTriggerTable()
begin

  replace into triggertable( myid, myname ) values ( 1, 'val' );

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("NonScalarFunction breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void RoutineWithoutBeginEndBlock()
    {
      string sql =
        @"
delimiter //

drop table if exists triggertable //

create table triggertable ( 
  myid int,
  myname varchar( 30 )
) //

create trigger trTriggerTable before insert on triggertable for each row
    set new.myid = new.myid + 1;
//

drop procedure if exists DoInsertTriggerTable //

create procedure DoInsertTriggerTable()
  replace into triggertable( myid, myname ) values ( 1, 'val' );
//
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure DoInsertTriggerTable()
  replace into triggertable( myid, myname ) values ( 1, 'val' );
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        Watch w = dbg.SetWatch("new.myid");
        Watch w2 = dbg.SetWatch("new.myname");
        //Watch w3 = dbg.SetWatch("old.myid");
        //Watch w4 = dbg.SetWatch("old.myname");
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
          if ( bp.RoutineName == "test6.trTriggerTable")
          {
            if (bp.Line == 3)
            {
              Debug.WriteLine("Checking new & old object in trigger scope");
              Assert.Equal(1, Convert.ToInt32(w.Eval()));
              //Assert.AreEqual(1, Convert.ToInt32(w3.Eval()));
              Assert.Equal("val", w2.Eval());
              //Assert.AreEqual("Val", w4.Eval());
            }
          }
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
        dbg.Stop();
      }
    }

    [Fact]
    public void InformationFunctions()
    {
      string sql =
        @"
delimiter //

drop table if exists informationtable //

create table informationtable ( 
  myid int auto_increment,
  myname varchar( 30 ),
  primary key ( myid )
) //

drop procedure if exists DoTestInformationFunctions //

create procedure DoTestInformationFunctions()
begin

  declare my_found_rows int;
  declare my_last_insert_id int;
  declare my_row_count int;
  declare flag int;

  insert into informationtable( myname ) values ( 'val' );
  insert into informationtable( myname ) values ( 'val2' );
  insert into informationtable( myname ) values ( 'val3' );
  set my_last_insert_id = last_insert_id();
  select * from informationtable limit 10;  
  set my_found_rows = found_rows();
  update informationtable set myname = concat( myname, 'x' );
  set my_row_count = row_count();
 
  if ( my_last_insert_id = 3 ) and ( my_row_count = 3 ) and ( my_found_rows = 3 ) then
    set flag = 1;
  else
    set flag = 0;
  end if;
  select flag;

end //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure DoTestInformationFunctions()
begin

  declare my_found_rows int;
  declare my_last_insert_id int;
  declare my_row_count int;
  declare flag int;

  insert into informationtable( myname ) values ( 'val' );
  insert into informationtable( myname ) values ( 'val2' );
  insert into informationtable( myname ) values ( 'val3' );
  set my_last_insert_id = last_insert_id();
  select * from informationtable limit 10;  
  set my_found_rows = found_rows();
  update informationtable set myname = concat( myname, 'x' );
  set my_row_count = row_count();
 
  if ( my_last_insert_id = 3 ) and ( my_row_count = 3 ) and ( my_found_rows = 3 ) then
    set flag = 1;
  else
    set flag = 0;
  end if;
  select flag;

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        Watch w = dbg.SetWatch( "flag" );
        Watch w2 = dbg.SetWatch( "my_last_insert_id" );
        Watch w3 = dbg.SetWatch( "my_row_count" );
        Watch w4 = dbg.SetWatch( "my_found_rows" );
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
          if (bp.Line == 23)
          {
            Debug.WriteLine("At line 23, checking locals values");
            Assert.Equal(1, Convert.ToInt32( w.Eval() ) );
            Assert.Equal(3, Convert.ToInt32(w2.Eval()));
            Assert.Equal(3, Convert.ToInt32(w3.Eval()));
            Assert.Equal(3, Convert.ToInt32(w4.Eval()));
            Debug.WriteLine("Locals values just right");
          }
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    /// <summary>
    /// These test checks that debugger fix works for evaluating & changing session variables
    /// in the debugger.
    /// </summary>
    [Fact]
    public void EvaluatingAndChangingSessionVariables()
    {
      string sql =
        @"
delimiter //

drop procedure if exists PlayWithSessionVars //

create procedure PlayWithSessionVars()
begin

  set @x = 1;
  set @y = 2;
  set @y = @y + @x;
  select @y;

end //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure PlayWithSessionVars()
begin

  set @x1 = 1;
  set @y = 2;
  set @y = @y + @x1;
  select @y;

end ;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        Watch w = dbg.SetWatch("@x1");
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
          if (bp.Line == 6)
          { 
            Assert.Equal(1, Convert.ToInt32(w.Eval()));
            dbg.CurrentScope.Variables["@x1"].Value = 5;
            dbg.CommitLocals();
          }
          else if (bp.Line == 7)
          {
            Assert.Equal(5, Convert.ToInt32(w.Eval()));
          }
        };
        dbg.Run(new string[0], null);
      }
      finally
      {        
        dbg.RestoreRoutinesBackup();
      }
    }

    /// <summary>
    /// This test assumes existence of sakila db.
    /// </summary>
    [Fact]
    public void DataIsNull()
    {
      string sql =
        @"
delimiter //

CREATE TABLE customer
(
  `customer_id` smallint(5) unsigned NOT NULL AUTO_INCREMENT,  
  `store_id` int(11) DEFAULT NULL,  
  `first_name` varchar(45) NOT NULL,
  `last_name` varchar(45) NOT NULL,
  `email` varchar(50) DEFAULT NULL,
  `address_id` smallint(5) unsigned NOT NULL,
  `active` tinyint(1) NOT NULL DEFAULT '1',
  `create_date` datetime NOT NULL,
  `last_update` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`customer_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 //
drop procedure if exists `new_customer` //

CREATE DEFINER=`root`@`localhost` PROCEDURE `new_customer`() 
BEGIN 
  INSERT INTO `customer` (`store_id`, `first_name`, `last_name`, `email`, `address_id`, `create_date` ) 
  VALUES ( 1, ""Armando"", ""Lopez"", ""armand2099@gmail.com"", 1, NOW() ); 
END
//
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE DEFINER=`root`@`localhost` PROCEDURE `new_customer`() 
BEGIN 
  INSERT INTO `customer` (`store_id`, `first_name`, `last_name`, `email`, `address_id`, `create_date` ) 
  VALUES ( 1, ""Armando"", ""Lopez"", ""armand2099@gmail.com"", 1, NOW() ); 
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }


    [Fact]
    public void CharsetIssue()
    {
      string sql =
        @"
delimiter //

drop table if exists `city` //

drop procedure if exists `create_proc` //

CREATE DEFINER=`root`@`localhost` PROCEDURE `create_proc`() 
BEGIN 
  CREATE TABLE `city` ( `Name` char(35) NOT NULL DEFAULT '', `CountryCode` char(3) NOT NULL DEFAULT '', 
  `District` char(20) NOT NULL DEFAULT '', `Population` int(11) NOT NULL DEFAULT '0', `ID` int(11) NOT NULL AUTO_INCREMENT, 
  PRIMARY KEY (`ID`) ) ENGINE=MyISAM AUTO_INCREMENT=4080 DEFAULT CHARSET=latin1; 
END //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE DEFINER=`root`@`localhost` PROCEDURE `create_proc`() 
BEGIN 
  CREATE TABLE `city` ( `Name` char(35) NOT NULL DEFAULT '', `CountryCode` char(3) NOT NULL DEFAULT '', 
  `District` char(20) NOT NULL DEFAULT '', `Population` int(11) NOT NULL DEFAULT '0', `ID` int(11) NOT NULL AUTO_INCREMENT, 
  PRIMARY KEY (`ID`) ) ENGINE=MyISAM AUTO_INCREMENT=4080 DEFAULT CHARSET=latin1; 
END ;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void ColumnNumber()
    {
      string sql =
        @"
delimiter //

drop procedure if exists `sp_testMultiline` //

CREATE PROCEDURE `sp_testMultiline`()
BEGIN
 declare n,x,y,z int;
 declare str varchar(1100);
 set n = 1;
 set str = 'Rafa';
 while
 n < 10
 do
 begin
 set n = n + 1; set x = n * 2;
 set y = n * 5;
 set z = n * 10;
   set str = CONCAT(str, 'o'); end;
 end while;
END //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE `sp_testMultiline`()
BEGIN
 declare n,x,y,z int;
 declare str varchar(1100);
 set n = 1;
 set str = 'Rafa';
 while
 n < 10
 do
 begin
 set n = n + 1; set x = n * 2;
 set y = n * 5;
 set z = n * 10;
   set str = CONCAT(str, 'o'); end;
 end while;
END ;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void NameIsKeyword()
    {
      string sql =
        @"
delimiter //

drop table if exists d_table //

create table d_table( id int auto_increment PRIMARY KEY, `name` varchar( 20 ) ) //

drop procedure if exists `count` //

CREATE DEFINER=`root`@`localhost` PROCEDURE `count`() 
BEGIN 
  DECLARE y varchar(50); 
  INSERT INTO d_table (`name`) VALUES (""Armando""); 
  INSERT INTO d_table (`name`) VALUES (""Elisa""); 
  select row_count() into y; 
  select found_rows() into y; 
  select last_insert_id() into y; 
END //
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE DEFINER=`root`@`localhost` PROCEDURE `count`() 
BEGIN 
  DECLARE y varchar(50); 
  INSERT INTO d_table (`name`) VALUES (""Armando""); 
  INSERT INTO d_table (`name`) VALUES (""Elisa""); 
  select row_count() into y; 
  select found_rows() into y; 
  select last_insert_id() into y; 
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1}", bp.RoutineName, bp.Line));
        };
        dbg.Run(new string[0], null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    [Fact]
    public void FibonacciGeneration()
    {
      string sql =
        @"
delimiter //

drop procedure if exists spFiboGen //

delimiter //
create procedure spFiboGen( n int, out myresult int )
begin

    declare result1 int;
    declare result2 int;
    
    if ( n = 1 ) or ( n = 2 ) then
        set myresult = 1;
    elseif ( n = 0 ) then
        set myresult = 0;
    else    
        call spFiboGen( n - 1, result1 );
        call spFiboGen( n - 2, result2 );
        set myresult = result1 + result2;
    end if;

end
//
drop procedure if exists spClientFiboGen //

delimiter //
create procedure spClientFiboGen( nMax int )
begin

    declare i int;
    declare myresult int;
    
    SET @@GLOBAL.max_sp_recursion_depth = 20;
    SET @@session.max_sp_recursion_depth = 20; 
    set i = 0;
    
    drop table if exists tblFibo;
    create table tblFibo( n int, fibo int );
    
    while i < nMax do    
        call spFiboGen( i, myresult );
        insert into tblFibo( n, fibo ) values ( i, myresult );
        set i = i + 1;
    end while;

end
//
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"create procedure spClientFiboGen( nMax int )
begin

    declare i int;
    declare myresult int;
    
    SET @@GLOBAL.max_sp_recursion_depth = 20;
    SET @@session.max_sp_recursion_depth = 20; 
    set i = 0;
    
    drop table if exists tblFibo;
    create table tblFibo( n int, fibo int );
    
    while i < nMax do    
        call spFiboGen( i, myresult );
        insert into tblFibo( n, fibo ) values ( i, myresult );
        set i = i + 1;
    end while;

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run( new string[] { "3" }, new string[ 0 ] );
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    private void DumpConnectionThreads(Debugger dbg)
    {
      dbg.Connection.Open();
      dbg.UtilityConnection.Open();
      dbg.LockingConnection.Open();
      //dbg.LockingConnection2.Open();
      Debug.WriteLine(string.Format("Debugger thread id: {0}", dbg.UtilityConnection.ServerThread));
      Debug.WriteLine(string.Format("Debuggee thread id: {0}", dbg.Connection.ServerThread));
      Debug.WriteLine(string.Format("Locking thread id: {0}", dbg.LockingConnection.ServerThread));
      //Debug.WriteLine(string.Format("Locking2 thread id: {0}", dbg.LockingConnection2.ServerThread));
    }

    /// <summary>
    /// Test for In, Out and InOut Parameters
    /// </summary>
    [Fact]
    public void ArgumentsTest()
    {
      string fullSql = @"DELIMITER //
DROP PROCEDURE IF EXISTS pr_ArgumentsTest //
";
      string procedureSql = @"
CREATE PROCEDURE pr_ArgumentsTest(param1 tinyint unsigned, out param2 varchar(5), inout param3 int, inout param4 varchar(5))
BEGIN
  SET param2 = param1;
  SET param3 = param3 + param1;
  IF param4 = 'abc' THEN
    SET param4 = 'xyz';
  ELSE
    SET param4 = NULL;
  END IF;
END
";

      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, fullSql + procedureSql + @"//");
        script.Execute();

        dbg.SqlInput = procedureSql;
        dbg.Run(new string[] { "1", "@dbg_var1", "@dbg_var2", "@dbg_var3" }, new string[] { "@dbg_var2 = '3'", "@dbg_var3 = 'abc'" });
        Assert.Equal("1", dbg.ScopeVariables["param1"].Value);
        Assert.Equal("1", dbg.ScopeVariables["param2"].Value);
        Assert.Equal("4", dbg.ScopeVariables["param3"].Value);
        Assert.Equal("xyz", dbg.ScopeVariables["param4"].Value);
        dbg.RestoreRoutinesBackup();

        dbg.Run(new string[] { "1", "@dbg_var1", "@dbg_var2", "@dbg_var3" }, new string[] { "@dbg_var2 = '3'", "@dbg_var3 = 'mysql'" });
        Assert.Equal("1", dbg.ScopeVariables["param1"].Value);
        Assert.Equal("1", dbg.ScopeVariables["param2"].Value);
        Assert.Equal("4", dbg.ScopeVariables["param3"].Value);
        Assert.Equal(DBNull.Value, dbg.ScopeVariables["param4"].Value);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
        dbg.Stop();
      }
    }

    /// <summary>
    /// This fixes BUG 16002371 - MYSQL.DEBUGGER MODULE PARSES INCORRECTLY CAUSING SYNTAX ERROR.
    /// </summary>
    [Fact]
    public void BrokenInstrumentation()
    {
      string sql =
        @"
DELIMITER // 

CREATE PROCEDURE rewards_report1 ( 
    IN min_monthly_purchases TINYINT UNSIGNED 
) 
    READS SQL DATA 
    COMMENT 'Provides a customizable report on best customers' 
proc: BEGIN 

    DECLARE something int; 
    SELECT min_monthly_purchase;   
END // 
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE PROCEDURE rewards_report1 ( 
    IN min_monthly_purchases TINYINT UNSIGNED 
) 
    READS SQL DATA 
    COMMENT 'Provides a customizable report on best customers' 
proc: BEGIN 

    DECLARE something int; 
    SELECT min_monthly_purchases;
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run(new string[] { "3" }, new string[0]);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    /// <summary>
    /// This fixes BUG 17284598 - Error raised when using Begin-End inside another Begin-End.
    /// </summary>
    [Fact]
    public void BrokenInstrumentationInCase()
    {
      string sql =
        @"
DELIMITER // 

CREATE DEFINER=`root`@`localhost` PROCEDURE `proceso`()

BEGIN
    DECLARE var1 INT;
    DECLARE var2 INT;

    SET var1 = 10;
    SET var2 = 20;

    CASE var1
        WHEN 1000 THEN SELECT var1;
        WHEN 3000 THEN SELECT var1;
        ELSE
       BEGIN
        -- multiple statements; statement list right below
        -- performing multiple variable value setting
            SET var1 = var1 + 10;
            SET var2 = var2 + 10;
           SELECT var1, var2;
        END;
    END CASE;
END // 
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE DEFINER=`root`@`localhost` PROCEDURE `proceso`()

BEGIN
    DECLARE var1 INT;
    DECLARE var2 INT;

    SET var1 = 10;
    SET var2 = 20;

    CASE var1
        WHEN 1000 THEN SELECT var1;
        WHEN 3000 THEN SELECT var1;
        ELSE
       BEGIN
        -- multiple statements; statement list right below
        -- performing multiple variable value setting
            SET var1 = var1 + 10;
            SET var2 = var2 + 10;
           SELECT var1, var2;
        END;
    END CASE;
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run(null, null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    /// <summary>
    /// Fix for bug Data too long for column 'pVarName' at row 1
    /// </summary>
    [Fact]
    public void ReallyBigLocalVarIdentifier()
    {
      string sql =
        @"
DELIMITER // 

CREATE DEFINER=`root`@`localhost` PROCEDURE `A_big_name_for_stored_procedure_if_you_ask_me_must_smaller_end1`()
begin    
    declare n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 int;
    set n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 = 1;
    while n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 < 5 do
    begin
    
        set n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 = 
          n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 + 1;
    
    end;
    end while;

end // 
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE DEFINER=`root`@`localhost` PROCEDURE `A_big_name_for_stored_procedure_if_you_ask_me_must_smaller_end1`()
begin    
    declare n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 int;
    set n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 = 1;
    while n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 < 5 do
    begin
    
        set n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 = 
          n_012345678901234567890123456789012345678901234567890123456789001234567890123456789012345678901234567890 + 1;
    
    end;
    end while;

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run(null, null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    /// <summary>
    /// Fix for debugger fails to debug routine with two functions expression (Oracle bug #17865915).
    /// </summary>
    [Fact]
    public void ExpressionWithTwoFunctions()
    {
      string sql =
        @"
DELIMITER // 

CREATE DEFINER=`root`@`localhost` FUNCTION `GetDiff`( x int, y int ) RETURNS 
int(11) 
begin 
return x - y; 
end //

CREATE DEFINER=`root`@`localhost` FUNCTION `GetSum`( a int, b int ) RETURNS 
int(11) 
begin 
return a + b; 
end //

CREATE DEFINER=`root`@`localhost` PROCEDURE `TestingFunctions`() 
begin

declare result int; 
set result = GetSum( 5, 4 ) + GetDiff( 5, 4 ); 

end // 
";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"CREATE DEFINER=`root`@`localhost` PROCEDURE `TestingFunctions`() 
begin

declare result int; 
set result = GetSum( 5, 4 ) + GetDiff( 5, 4 ); 

end;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run(null, null);
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }

    /// <summary>
    /// Fix for debugger fails with certain stored procedure (Oracle bug #17924210).
    /// </summary>
    [Fact]
    public void SakilaRewardsReport()
    {
      string sql =
        @" /* nothing, already exists in sakila */";
      Debugger dbg = new Debugger();
      try
      {
        dbg.Connection = new MySqlConnection(TestUtils.CONNECTION_STRING_SAKILA );
        dbg.UtilityConnection = new MySqlConnection(TestUtils.CONNECTION_STRING_SAKILA);
        dbg.LockingConnection = new MySqlConnection(TestUtils.CONNECTION_STRING_SAKILA);
        DumpConnectionThreads(dbg);
        MySqlScript script = new MySqlScript(dbg.Connection, sql);
        script.Execute();
        sql =
@"
CREATE DEFINER=`root`@`localhost` PROCEDURE `rewards_report`(
    IN min_monthly_purchases TINYINT UNSIGNED
    , IN min_dollar_amount_purchased DECIMAL(10,2) UNSIGNED
    , OUT count_rewardees INT
)
    READS SQL DATA
    COMMENT 'Provides a customizable report on best customers'
proc: BEGIN

    DECLARE last_month_start DATE;
    DECLARE last_month_end DATE;

    /* Some sanity checks... */
    IF min_monthly_purchases = 0 THEN
        SELECT 'Minimum monthly purchases parameter must be > 0';
        LEAVE proc;
    END IF;
    IF min_dollar_amount_purchased = 0.00 THEN
        SELECT 'Minimum monthly dollar amount purchased parameter must be > $0.00';
        LEAVE proc;
    END IF;

    /* Determine start and end time periods */
    SET last_month_start = DATE_SUB(CURRENT_DATE(), INTERVAL 1 MONTH);
    SET last_month_start = STR_TO_DATE(CONCAT(YEAR(last_month_start),'-',MONTH(last_month_start),'-01'),'%Y-%m-%d');
    SET last_month_end = LAST_DAY(last_month_start);

    /*
        Create a temporary storage area for
        Customer IDs.
    */
    CREATE TEMPORARY TABLE tmpCustomer (customer_id SMALLINT UNSIGNED NOT NULL PRIMARY KEY);

    /*
        Find all customers meeting the
        monthly purchase requirements
    */
    INSERT INTO tmpCustomer (customer_id)
    SELECT p.customer_id
    FROM payment AS p
    WHERE DATE(p.payment_date) BETWEEN last_month_start AND last_month_end
    GROUP BY customer_id
    HAVING SUM(p.amount) > min_dollar_amount_purchased
    AND COUNT(customer_id) > min_monthly_purchases;

    /* Populate OUT parameter with count of found customers */
    SELECT COUNT(*) FROM tmpCustomer INTO count_rewardees;

    /*
        Output ALL customer information of matching rewardees.
        Customize output as needed.
    */
    SELECT c.*
    FROM tmpCustomer AS t
    INNER JOIN customer AS c ON t.customer_id = c.customer_id;

    /* Clean up */
    DROP TABLE tmpCustomer;
END;
";
        dbg.SqlInput = sql;
        dbg.SteppingType = SteppingTypeEnum.StepInto;
        dbg.OnBreakpoint += (bp) =>
        {
          Debug.WriteLine(string.Format("breakpoint at line {0}:{1},{2}", bp.RoutineName, bp.Line, bp.StartColumn));
        };
        dbg.Run( new string[] { "10", "1", "@x" }, new string[] { "@x=1" });
      }
      finally
      {
        dbg.RestoreRoutinesBackup();
      }
    }
  }
}