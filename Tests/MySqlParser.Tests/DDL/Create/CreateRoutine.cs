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
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Xunit;


namespace MySql.Parser.Tests
{
  
  public class CreateRoutine
  {
    [Fact]
    public void SimpleProc()
    {
      string sql = @"
CREATE PROCEDURE simpleproc (OUT param1 INT)
    BEGIN
      SELECT COUNT(*) INTO param1 FROM t;
    END;";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void SimpleProc2()
    {
      string sql = @"
CREATE PROCEDURE dorepeat(p1 INT)
BEGIN
  SET @x = 0;
  REPEAT SET @x = @x + 1; UNTIL @x > p1 END REPEAT;
END;
";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void CreateProcWithSec()
    {
      string sql = @"CREATE DEFINER = 'admin'@'localhost' PROCEDURE account_count()
SQL SECURITY INVOKER
BEGIN
  SELECT 'Number of accounts:', COUNT(*) FROM mysql.user;
END;";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void ComplexProc()
    {
      string sql = @"
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_error_log`(
`error_level` int(11),
`error_level_name` varchar(512),
`error_message` longtext,
`error_file` text,
`error_line` int(11),
`error_context` longtext,
`error_query_string` longtext,
`error_time` text ,
`user_id` int(11),
`post_data` longtext,
`user_msg` text)
BEGIN
INSERT INTO tbl_error_log(error_level, error_level_name, error_message, error_file,error_line, error_context,error_query_string,error_time, user_id, post_data, user_msg)
values(error_level, error_level_name, error_message, error_file,error_line, error_context,error_query_string, error_time, user_id, post_data, user_msg);
END;";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void CreateProcWithSec2()
    {
      string sql = @"create DEFINER=`root`@`localhost` PROCEDURE `spTest2`()
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

end;";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }
    

    [Fact]
    public void SimpleFunc()
    {
      string sql = @"CREATE FUNCTION hello (s CHAR(20))
    RETURNS CHAR(50) DETERMINISTIC
    RETURN CONCAT('Hello, ',s,'!');";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void ComplexFunc()
    {
      string sql = @"CREATE FUNCTION fnGetXMLinfoVraag4 (xmlTag varchar(30),message text) returns varchar(255)
DETERMINISTIC
READS SQL DATA
begin
declare lenField int;
declare xmlTagBegin varchar(30);
declare xmlTagEnd varchar(30);
declare fieldresult varchar(255);
set xmlTagBegin = concat('<', xmlTag, '>');
set xmlTagEnd = concat('</', xmlTag, '>');
set lenField = length(xmlTag) + 2;
set fieldresult = case when locate(xmlTagBegin,message) = 0 then ''
else substring(message,locate(xmlTagBegin,message) + lenField,locate(xmlTagEnd,message) - (locate(xmlTagBegin,message) + lenField)) end;
return fieldresult;
end";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void ComplexFunc2()
    {
      string sql = @"
CREATE FUNCTION db.fnfullname ( id smallint(5) unsigned ) RETURNS varchar(160) CHARACTER SET utf8
COMMENT 'Returns the full name of person in db.people table referenced by id where FirstName and FamilyName are not null but MiddleName may be null'
DETERMINISTIC
READS SQL DATA
BEGIN
DECLARE fulname varchar(160) CHARACTER SET utf8;
SELECT CONCAT_WS(' ', db.people.FirstName, db.people.MiddleName, db.people.FamilyName) into fulname from db.people where db.people.id=id;
RETURN fulname;
END;
";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void SimpleCompare()
    {
      string sql = @"CREATE FUNCTION SimpleCompare(n INT, m INT)
  RETURNS VARCHAR(20)

  BEGIN
    DECLARE s VARCHAR(20);

    IF n > m THEN SET s = '>';
    ELSEIF n = m THEN SET s = '=';
    ELSE SET s = '<';
    END IF;

    SET s = CONCAT(n, ' ', s, ' ', m);

    RETURN s;
  END";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void VerboseCompare()
    {
      string sql = @"CREATE FUNCTION VerboseCompare (n INT, m INT)
  RETURNS VARCHAR(50)

  BEGIN
    DECLARE s VARCHAR(50);

    IF n = m THEN SET s = 'equals';
    ELSE
      IF n > m THEN SET s = 'greater';
      ELSE SET s = 'less';
      END IF;

      SET s = CONCAT('is ', s, ' than');
    END IF;

    SET s = CONCAT(n, ' ', s, ' ', m, '.');

    RETURN s;
  END";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void WithoutName()
    {
      string sql = @"create procedure ( id int, name varchar( 10 ))
begin
  create table test3( id2 int );
  insert into test3 (1), (2), (3);
  # insert into test3 values (1), (2), (3);
end";
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, true, out sb);
    }

    [Fact]
    public void NameIsKeyword()
    {
      string sql = @"
CREATE DEFINER=`root`@`localhost` PROCEDURE `count`() 
BEGIN 
  DECLARE y varchar(50); 
  INSERT INTO world.d_table (`name`) VALUES (""Armando""); 
  INSERT INTO world.d_table (`name`) VALUES (""Elisa""); 
  select row_count() into y; 
  select found_rows() into y; 
  select last_insert_id() into y; 
END";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void DifferentDeclareOrders()
    {
      string sql = @"
CREATE DEFINER=`root`@`localhost` PROCEDURE `dohandler`() 
BEGIN 
  DECLARE dup_keys CONDITION FOR SQLSTATE '23000'; 
  DECLARE y varchar(50); 
  DECLARE CONTINUE HANDLER FOR dup_keys SET @GARBAGE = 1; 
  SET @x = 1; 
  INSERT INTO world.d_table (`name`) VALUES (""Armando""); 
  SET @x = 2; 
  INSERT INTO world.d_table (`name`) VALUES (""Elisa""); 
  set @x = 3; 
  select row_count() into y; 
  select found_rows() into y; 
  select last_insert_id() into y; 
END";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void SetSession()
    {
      string sql = @"
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

end;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void RoutineWithRowcount51()
    {
      string sql = @"CREATE DEFINER=`root`@`localhost` PROCEDURE `count`() 
BEGIN 
  DECLARE y varchar(50); 
  INSERT INTO d_table (`name`) VALUES (""y""); 
  INSERT INTO d_table (`name`) VALUES (""x""); 
  select row_count() into y; 
  select found_rows() into y; 
  select last_insert_id() into y; 
END;
";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb, new Version(5, 1));
    }

    [Fact]
    public void RoutineWithRowcount56()
    {
      string sql = @"CREATE DEFINER=`root`@`localhost` PROCEDURE `count`() 
BEGIN 
  DECLARE y varchar(50); 
  INSERT INTO d_table (`name`) VALUES (""y""); 
  INSERT INTO d_table (`name`) VALUES (""x""); 
  select row_count() into y; 
  select found_rows() into y; 
  select last_insert_id() into y; 
END;
";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb, new Version( 5, 6 ));
    }

    [Fact]
    public void WithBunchOfDeclares()
    {
      string sql = @"create procedure sp()
  begin
     DECLARE temp_timestamp datetime DEFAULT CURRENT_TIMESTAMP;     
     DECLARE temp_news_type varchar(3);
     DECLARE temp_news_id decimal(3);        
     DECLARE temp_news_line varchar(80) character set big5; 
     
     DECLARE temp_start_time ,temp_end_time varchar(19);     
          
     DECLARE turnover_str varchar(30) character set big5 DEFAULT 'Short Sell Turnover';      
     DECLARE shortsellz INT;
     
     DECLARE stock_code varchar(8);
     DECLARE stock_code_is_num decimal(1);     
     DECLARE stock_code_int decimal(5) DEFAULT 0;      
     DECLARE shortsell_share_char,shortsell_turnover_char  varchar(15); 
       
     DECLARE shortsell_share_pre,shortsell_turnover_pre decimal(15);         
     DECLARE temp_timestamp_out varchar(10);       
     DECLARE temp_sub_line1 ,temp_sub_line2 varchar(70);      
     DECLARE turnover_char_length,temp_sub_line1_length,temp_sub_length_a ,stock_code_include_x INT;         
     DECLARE stock_code_include_v varchar(5); 
     DECLARE non_designated_char varchar(1) DEFAULT 'N';
     DECLARE stock_code_include_y INT;     
     DECLARE stock_code_include_yv varchar(5); 
     DECLARE non_HKD_char varchar(1) DEFAULT 'N';
  end;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void RewardsReport()
    {
      string sql = @"CREATE DEFINER=`root`@`localhost` PROCEDURE `rewards_report`(
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
END;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb, new Version(5, 1));
    }
  }
}
