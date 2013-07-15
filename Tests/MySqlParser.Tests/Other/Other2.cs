// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using Antlr.Runtime;
using Antlr.Runtime.Tree;


namespace MySql.Parser.Tests
{

  public class Other2
  {
    /*
    [Fact]
    public void Test0()
    {
      string sql = @"	ALTER TABLE app_userprofile AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }
    */

    [Fact]
    public void Test1()
    {
      string sql = @"	CREATE TABLE auth_group_permissions ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , group_id INTEGER NOT NULL , permission_id INTEGER NOT NULL , UNIQUE ( group_id , permission_id ) ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test2()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_rule_eval_results FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    /*
    [Fact]
    public void Test3()
    {
      string sql = @"	ALTER TABLE app_recipegrain AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }
     * */

    [Fact]
    public void Test4()
    {
      string sql = @"	CREATE TABLE app_recipe ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , author_id INTEGER NOT NULL , name VARCHAR( ? ) NOT NULL , insert_date datetime NOT NULL , batch_size NUMERIC( ? , ? ) NOT NULL , batch_size_units VARCHAR( ? ) NOT NULL , style_id INTEGER NULL , derived_from_recipe_id INTEGER NULL , type VARCHAR( ? ) NOT NULL , source_url VARCHAR( ? ) NULL , notes LONGTEXT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test5()
    {
      string sql = @"	SHOW INDEX FROM inventory_types FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test6()
    {
      string sql = @"	CREATE TABLE auth_permission ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL , content_type_id INTEGER NOT NULL , codename VARCHAR( ? ) NOT NULL , UNIQUE ( content_type_id , codename ) ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test7()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_inventory_attributes FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test8()
    {
      string sql = @"	SHOW INDEX FROM migration_migration_state FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test9()
    {
      string sql = @"	SELECT thresholds0_ . rule_id AS rule5_1_ , thresholds0_ . threshold_id AS threshold1_1_ , thresholds0_ . threshold_id AS threshold1_22_0_ , thresholds0_ . level AS level22_0_ , thresholds0_ . value AS value22_0_ , thresholds0_ . variable AS variable22_0_ FROM rule_thresholds thresholds0_ WHERE thresholds0_ . rule_id = ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test10()
    {
      string sql = @"	ALTER TABLE django_admin_log AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test11()
    {
      string sql = @"	SHOW INDEX FROM inventory_instances FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test12()
    {
      string sql = @"	ALTER TABLE app_hop AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test13()
    {
      string sql = @"	SHOW INDEX FROM rule_variables FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test14()
    {
      string sql = @"	SHOW INDEX FROM resource_bundle FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test15()
    {
      string sql = @"	SHOW INDEX FROM graph_series_v2 FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test16()
    {
      string sql = @"	ALTER TABLE app_adjunct AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test17()
    {
      string sql = @"	SHOW INDEX FROM inventory_instance_tags FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test18()
    {
      string sql = @"	SHOW INDEX FROM migration_status_servers_migration_status_data_collection FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test19()
    {
      string sql = @"	ALTER TABLE auth_user_user_permissions AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test20()
    {
      // group identifier is not valid in this context
      string sql = @"	CREATE TABLE app_grain ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL , extract_min SMALLINT NULL , extract_max SMALLINT NULL , volume_potential_min SMALLINT NULL , volume_potential_max SMALLINT NULL , lovibond_min SMALLINT NULL , lovibond_max SMALLINT NULL , description VARCHAR( ? ) NOT NULL , `group` VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test21()
    {
      string sql = @"	SHOW INDEX FROM rule_alarms FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test22()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_iia FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test23()
    {
      string sql = @"	SHOW INDEX FROM system_maps FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test24()
    {
      string sql = @"	CREATE TABLE auth_user ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , username VARCHAR( ? ) NOT NULL UNIQUE , first_name VARCHAR( ? ) NOT NULL , last_name VARCHAR( ? ) NOT NULL , email VARCHAR( ? ) NOT NULL , password VARCHAR( ? ) NOT NULL , is_staff bool NOT NULL , is_active bool NOT NULL , is_superuser bool NOT NULL , last_login datetime NOT NULL , date_joined datetime NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test25()
    {
      string sql = @"	SHOW INDEX FROM user_form_defaults FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test26()
    {
      string sql = @"	ALTER TABLE app_yeast AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test27()
    {
      string sql = @"	CREATE TABLE app_brew ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , brew_date datetime NULL , brewer_id INTEGER NOT NULL , notes LONGTEXT NULL , recipe_id INTEGER NULL , last_update_date datetime NULL , last_state VARCHAR( ? ) NULL , is_done bool NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test28()
    {
      string sql = @"	CREATE TABLE app_userprofile ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , user_id INTEGER NOT NULL UNIQUE , pref_brew_type VARCHAR( ? ) NOT NULL , pref_make_starter bool NOT NULL , pref_secondary_ferm bool NOT NULL , pref_dispensing_style VARCHAR( ? ) NOT NULL , timezone VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test29()
    {
      string sql = @"	CREATE TABLE auth_user_groups ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , user_id INTEGER NOT NULL , group_id INTEGER NOT NULL , UNIQUE ( user_id , group_id ) ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test30()
    {
      // group identifier is not valid in this context
      string sql = @"	CREATE TABLE app_adjunct ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL , `group` VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test31()
    {
      string sql = @"	SHOW INDEX FROM rule_eval_result_vars FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test32()
    {
      string sql = @"	SHOW INDEX FROM inventory_instance_attributes FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test33()
    {
      string sql = @"	CREATE TABLE django_site ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , domain VARCHAR( ? ) NOT NULL , name VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test34()
    {
      string sql = @"	SHOW INDEX FROM graph_dc_schedules FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test35()
    {
      string sql = @"	SHOW INDEX FROM loghistogram_data FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test36()
    {
      string sql = @"	CREATE TABLE app_yeast ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , manufacturer_id INTEGER NOT NULL , ident VARCHAR( ? ) NOT NULL , name VARCHAR( ? ) NOT NULL , description VARCHAR( ? ) NOT NULL , type VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test37()
    {
      string sql = @"	ALTER TABLE south_migrationhistory AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test38()
    {
      string sql = @"	SELECT hibevalres0_ . result_id AS result1_9_ , hibevalres0_ . alarm_id AS alarm2_9_ , hibevalres0_ . TIME AS time9_ , hibevalres0_ . level AS level9_ , variables1_ . result_id AS result1_0__ , variables1_ . value AS value0__ , variables1_ . variable AS variable0__ FROM rule_eval_results hibevalres0_ LEFT OUTER JOIN rule_eval_result_vars variables1_ ON hibevalres0_ . result_id = variables1_ . result_id WHERE hibevalres0_ . alarm_id = ? AND hibevalres0_ . TIME = ( SELECT MAX( hibevalres2_ . TIME ) FROM rule_eval_results hibevalres2_ WHERE hibevalres2_ . alarm_id = ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test39()
    {
      string sql = @"	CREATE TABLE app_step ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , brew_id INTEGER NOT NULL , type VARCHAR( ? ) NOT NULL , date datetime NOT NULL , entry_date datetime NOT NULL , volume NUMERIC( ? , ? ) NULL , volume_units VARCHAR( ? ) NULL , temp INTEGER NULL , temp_units VARCHAR( ? ) NULL , gravity_read NUMERIC( ? , ? ) NULL , gravity_read_temp INTEGER NULL , gravity_read_temp_units VARCHAR( ? ) NULL , notes VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test40()
    {
      string sql = @"	ALTER TABLE django_content_type AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test41()
    {
      string sql = @"	SHOW INDEX FROM rule_eval_results FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test42()
    {
      string sql = @"	ALTER TABLE app_recipe AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test43()
    {
      string sql = @"	SHOW INDEX FROM graph_schedules FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test44()
    {
      string sql = @"	SHOW INDEX FROM graph_variables_v2 FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test45()
    {
      string sql = @"	SHOW INDEX FROM graph_tags FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test46()
    {
      string sql = @"	ALTER TABLE app_style AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test47()
    {
      string sql = @"	SHOW INDEX FROM user_tags FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test48()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_rule_alarms FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test49()
    {
      string sql = @"	SHOW INDEX FROM statement_data FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test50()
    {
      string sql = @"	ALTER TABLE app_recipehop AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test51()
    {
      string sql = @"	CREATE TABLE app_hop ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL , aau_low NUMERIC( ? , ? ) NOT NULL , aau_high NUMERIC( ? , ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test52()
    {
      string sql = @"	ALTER TABLE auth_group_permissions AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test53()
    {
      string sql = @"	SELECT hibrulesch0_ . schedule_id AS schedule1_24_0_ , hibalarm1_ . alarm_id AS alarm1_11_1_ , hibevalres4_ . result_id AS result1_9_2_ , variables2_ . instance_attribute_id AS instance2_26_3_ , variables2_ . rule_schedule_id AS rule3_26_3_ , variables2_ . variable_id AS variable1_26_3_ , hibrulesch0_ . alarm_id AS alarm7_24_0_ , hibrulesch0_ . autoCloseEnabled AS autoClos2_24_0_ , hibrulesch0_ . autoCloseNote AS autoClos3_24_0_ , hibrulesch0_ . enabled AS enabled24_0_ , hibrulesch0_ . frequency AS frequency24_0_ , hibrulesch0_ . instance_id AS instance8_24_0_ , hibrulesch0_ . rule_id AS rule9_24_0_ , hibrulesch0_ . snmpEnabled AS snmpEnab6_24_0_ , hibalarm1_ . schedule_id AS schedule2_11_1_ , hibalarm1_ . severe_result_id AS severe3_11_1_ , hibalarm1_ . fixed_time AS fixed4_11_1_ , hibalarm1_ . email_targets AS email5_11_1_ , hibalarm1_ . notes AS notes11_1_ , hibalarm1_ . fixed_by_user_id AS fixed7_11_1_ , hibevalres4_ . alarm_id AS alarm2_9_2_ , hibevalres4_ . TIME AS time9_2_ , hibevalres4_ . level AS level9_2_ , variables2_ . rule_schedule_id AS rule3_0__ , variables2_ . instance_attribute_id AS instance2_0__ , variables2_ . variable_id AS variable1_0__ FROM rule_schedules hibrulesch0_ INNER JOIN rule_alarms hibalarm1_ ON hibrulesch0_ . alarm_id = hibalarm1_ . alarm_id INNER JOIN rule_eval_results hibevalres4_ ON hibalarm1_ . severe_result_id = hibevalres4_ . result_id INNER JOIN rule_dc_schedules variables2_ ON hibrulesch0_ . schedule_id = variables2_ . rule_schedule_id";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test54()
    {
      string sql = @"	ALTER TABLE auth_permission AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test55()
    {
      string sql = @"	SHOW FULL TABLES FROM mem LIKE ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test56()
    {
      string sql = @"	ALTER TABLE app_yeastmanufacturer AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test57()
    {
      string sql = @"	ALTER TABLE app_starredrecipe AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test58()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_inventory_namespaces FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test59()
    {
      string sql = @"	CREATE TABLE app_recipehop ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , recipe_id INTEGER NOT NULL , hop_id INTEGER NOT NULL , amount_value NUMERIC( ? , ? ) NOT NULL , amount_units VARCHAR( ? ) NOT NULL , boil_time SMALLINT NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test60()
    {
      string sql = @"	SET autocommit = ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test61()
    {
      string sql = @"	SHOW INDEX FROM migration_status_data_collection FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test62()
    {
      string sql = @"	SHOW INDEX FROM resource_bundle_map FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test63()
    {
      string sql = @"	SHOW INDEX FROM map_entries FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test64()
    {
      string sql = @"	SHOW INDEX FROM statement_summary_data FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test65()
    {
      string sql = @"	SHOW INDEX FROM rule_thresholds FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test66()
    {
      string sql = @"	ALTER TABLE app_brew AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test67()
    {
      string sql = @"	ALTER TABLE app_recipeyeast AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test68()
    {
      string sql = @"	SHOW INDEX FROM group_members_v2 FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test69()
    {
      string sql = @"	SHOW INDEX FROM rule_dc_schedules FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test70()
    {
      string sql = @"	SHOW INDEX FROM migration FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test71()
    {
      string sql = @"	SHOW INDEX FROM tags FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test72()
    {
      string sql = @"	CREATE TABLE django_session ( session_key VARCHAR( ? ) NOT NULL PRIMARY KEY , session_data LONGTEXT NOT NULL , expire_date datetime NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test73()
    {
      string sql = @"	SHOW INDEX FROM migration_migration_status_servers FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test74()
    {
      string sql = @"	CREATE TABLE app_recipeyeast ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , recipe_id INTEGER NOT NULL , yeast_id INTEGER NOT NULL , ideal bool NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test75()
    {
      string sql = @"	ALTER TABLE auth_group AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test76()
    {
      string sql = @"	SHOW INDEX FROM rule_tags FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test77()
    {
      string sql = @"	SHOW INDEX FROM migration_state FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test78()
    {
      string sql = @"	CREATE TABLE app_yeastmanufacturer ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test79()
    {
      string sql = @"	ALTER TABLE app_grain AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test80()
    {
      string sql = @"	SHOW INDEX FROM migration_status_servers FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test81()
    {
      string sql = @"	CREATE TABLE app_recipeadjunct ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , recipe_id INTEGER NOT NULL , adjunct_id INTEGER NOT NULL , amount_value NUMERIC( ? , ? ) NOT NULL , amount_units VARCHAR( ? ) NOT NULL , boil_time SMALLINT NOT NULL , notes VARCHAR( ? ) NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test82()
    {
      string sql = @"	SET autocommit = ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test83()
    {
      string sql = @"	ALTER TABLE auth_user AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test84()
    {
      string sql = @"	CREATE TABLE auth_user_user_permissions ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , user_id INTEGER NOT NULL , permission_id INTEGER NOT NULL , UNIQUE ( user_id , permission_id ) ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test85()
    {
      string sql = @"	SHOW INDEX FROM rule_schedules FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test86()
    {
      string sql = @"	ALTER TABLE django_site AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test87()
    {
      string sql = @"	SHOW INDEX FROM statement_examples FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test88()
    {
      string sql = @"	CREATE TABLE app_recipegrain ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , recipe_id INTEGER NOT NULL , grain_id INTEGER NOT NULL , amount_value NUMERIC( ? , ? ) NOT NULL , amount_units VARCHAR( ? ) NOT NULL , by_weight_extract_override SMALLINT NULL , by_volume_extract_override SMALLINT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test89()
    {
      string sql = @"	SHOW INDEX FROM users FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test90()
    {
      string sql = @"	SHOW INDEX FROM whats_new_entries FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test91()
    {
      string sql = @"	SHOW INDEX FROM user_preferences FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test92()
    {
      string sql = @"	SHOW INDEX FROM group_names FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test93()
    {
      string sql = @"	SET autocommit = ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test94()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_inventory_types FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test95()
    {
      string sql = @"	SHOW INDEX FROM graphs FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test96()
    {
      string sql = @"	CREATE TABLE app_style ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL , bjcp_code VARCHAR( ? ) NOT NULL , parent_id INTEGER NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test97()
    {
      string sql = @"	SHOW INDEX FROM schema_version_v2 FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test98()
    {
      string sql = @"	SHOW INDEX FROM inventory_attributes FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test99()
    {
      string sql = @"	SHOW INDEX FROM inventory_namespaces FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test100()
    {
      string sql = @"	CREATE TABLE auth_message ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , user_id INTEGER NOT NULL , message LONGTEXT NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test101()
    {
      string sql = @"	SHOW INDEX FROM support_portal_issues FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test102()
    {
      string sql = @"	SHOW INDEX FROM target_email FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test103()
    {
      string sql = @"	SHOW INDEX FROM rule_schedule_email_targets FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test104()
    {
      string sql = @"	SELECT @@session . auto_increment_increment";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test105()
    {
      /* when is not valid identifier in this context */
      string sql = @"	CREATE TABLE app_starredrecipe ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , recipe_id INTEGER NOT NULL , user_id INTEGER NOT NULL , `when` datetime NOT NULL , notes VARCHAR( ? ) NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test106()
    {
      string sql = @"	SHOW INDEX FROM hilo_sequence_inventory_instances FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test107()
    {
      string sql = @"	SHOW INDEX FROM explain_data FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test108()
    {
      string sql = @"	ALTER TABLE app_step AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test109()
    {
      string sql = @"	ALTER TABLE app_recipeadjunct AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test110()
    {
      string sql = @"	ALTER TABLE auth_message AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test111()
    {
      string sql = @"	CREATE TABLE django_content_type ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL , app_label VARCHAR( ? ) NOT NULL , model VARCHAR( ? ) NOT NULL , UNIQUE ( app_label , model ) ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test112()
    {
      string sql = @"	SHOW INDEX FROM migration_status_servers_migration_state FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test113()
    {
      string sql = @"	ALTER TABLE auth_user_groups AUTO_INCREMENT = ? ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test114()
    {
      string sql = @"	SHOW INDEX FROM rules FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test115()
    {
      string sql = @"	CREATE TABLE south_migrationhistory ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , app_name VARCHAR( ? ) NOT NULL , migration VARCHAR( ? ) NOT NULL , applied datetime NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test116()
    {
      string sql = @"	SHOW INDEX FROM statement_summaries FROM mem";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test117()
    {
      string sql = @"	CREATE TABLE django_admin_log ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , action_time datetime NOT NULL , user_id INTEGER NOT NULL , content_type_id INTEGER NULL , object_id LONGTEXT NULL , object_repr VARCHAR( ? ) NOT NULL , action_flag SMALLINT UNSIGNED NOT NULL , change_message LONGTEXT NOT NULL ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test118()
    {
      string sql = @"	CREATE TABLE auth_group ( id INTEGER AUTO_INCREMENT NOT NULL PRIMARY KEY , name VARCHAR( ? ) NOT NULL UNIQUE ) ;";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test119()
    {
      // group is not valid identifier in this context
      string sql = @"	SELECT app_grain . id , app_grain . name , app_grain . extract_min , app_grain . extract_max , app_grain . volume_potential_min , app_grain . volume_potential_max , app_grain . lovibond_min , app_grain . lovibond_max , app_grain . description , app_grain . `group` FROM app_grain";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test120()
    {
      // group is not valid identifier in this context
      string sql = @"	INSERT INTO app_grain ( name , extract_min , extract_max , volume_potential_min , volume_potential_max , lovibond_min , lovibond_max , description , `group` ) VALUES ( ? , ? , ? , ? , ? , ? , ? , ? , ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test121()
    {
      // when is not valid identifier in this context
      string sql = @"	SELECT app_starredrecipe . id , app_starredrecipe . recipe_id , app_starredrecipe . user_id , app_starredrecipe . `when` , app_starredrecipe . notes FROM app_starredrecipe WHERE app_starredrecipe . user_id = ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test122()
    {
      // there was an extra ? at the end.
      string sql = @"	INSERT IGNORE INTO dc_p_long ( instance_attribute_id , end_time , value ) VALUES ( ? , ? , ? ) /* , ... */ /*? */";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test123()
    {
      // there was an extra ? at the end.
      string sql = @"	INSERT IGNORE INTO dc_p_long ( instance_attribute_id , end_time , value ) VALUES ( ? , ? , ? ) /* , ... */ /* ? ? ? */";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test124()
    {
      // group is not valid identifier in this context
      string sql = @"	SELECT app_adjunct . id , app_adjunct . name , app_adjunct . `group` FROM app_adjunct";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test125()
    {
      string sql = @"	SELECT id , millis_stamp , function_name , table_name , log_msg FROM log_db_actions ORDER BY id ASC LIMIT ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test126()
    {
      string sql = @"	SELECT this_ . id AS id36_0_ , this_ . alias AS alias36_0_ , this_ . database_name AS database3_36_0_ , this_ . query_type AS query4_36_0_ , this_ . TEXT AS text36_0_ , this_ . text_hash AS text6_36_0_ FROM statement_data this_ WHERE ( this_ . database_name = ? AND this_ . text_hash = ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test127()
    {
      // group is not valid identifier in this context
      string sql = @"	SELECT app_grain . id , app_grain . name , app_grain . extract_min , app_grain . extract_max , app_grain . volume_potential_min , app_grain . volume_potential_max , app_grain . lovibond_min , app_grain . lovibond_max , app_grain . description , app_grain . `group` FROM app_grain WHERE app_grain . name LIKE BINARY ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test128()
    {
      string sql = @"	INSERT INTO statement_data ( alias , database_name , query_type , TEXT , text_hash ) VALUES ( ? , ? , ? , ? , ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    // TODO: are placeholder valid instead of expressions.
    [Fact]
    public void Test129()
    {
      // max_rows, min_rows are not valid identifier in this context
      string sql = @"	INSERT IGNORE INTO statement_summary_data ( bytes , count , errors , exec_time , max_bytes , max_exec_time , `max_rows` , min_bytes , min_exec_time , `min_rows` , no_good_index_used , no_index_used , rows , warnings , statement_summary_id , TIMESTAMP ) VALUES ( ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test130()
    {
      // group are not valid identifier in this context
      string sql = @"	SELECT app_grain . id , app_grain . name , app_grain . extract_min , app_grain . extract_max , app_grain . volume_potential_min , app_grain . volume_potential_max , app_grain . lovibond_min , app_grain . lovibond_max , app_grain . description , app_grain . `group` FROM app_grain WHERE ( app_grain . name = ? AND app_grain . `group` = ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);

    }

    [Fact]
    public void Test131()
    {
      string sql = @"	INSERT INTO rule_eval_results ( alarm_id , TIME , level , result_id ) VALUES ( ? , ? , ? , ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test132()
    {
      // group are not valid identifier in this context
      string sql = @"	SELECT app_grain . id , app_grain . name , app_grain . extract_min , app_grain . extract_max , app_grain . volume_potential_min , app_grain . volume_potential_max , app_grain . lovibond_min , app_grain . lovibond_max , app_grain . description , app_grain . `group` FROM app_grain WHERE app_grain . name = ?";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test133()
    {
      string sql = @"	INSERT INTO loghistogram_data ( base , nul , bin_0 , bin_1 , bin_2 , bin_3 , bin_4 , bin_5 , bin_6 , bin_7 , bin_8 , bin_9 , bin_10 , bin_11 , bin_12 , bin_13 , bin_14 , bin_15 , bin_16 , bin_17 , bin_18 , bin_19 , bin_20 , bin_21 , bin_22 , bin_23 , bin_24 , bin_25 , bin_26 , bin_27 , bin_28 , bin_29 , bin_30 , bin_31 , bin_32 , bin_33 , bin_34 , bin_35 , bin_36 , bin_37 , bin_38 , bin_39 , bin_40 , bin_41 , bin_42 , bin_43 , bin_44 , bin_45 , bin_46 , bin_47 , bin_48 , bin_49 , bin_50 , bin_51 , bin_52 , bin_53 , bin_54 , bin_55 , bin_56 , bin_57 , bin_58 , bin_59 , bin_60 , bin_61 , bin_62 , instance_id , TIMESTAMP ) VALUES ( ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test134()
    {
      // a long sequence of ifnull calls was syntactically wrong, commented out the original query.
      //string sql = @"	INSERT INTO statement_examples ( bytes , comment , connection_id , data_base , errors , exec_time , explain_plan , host_from , host_to , no_good_index_used , no_index_used , query_type , rows , source_location , TEXT , user , warnings , instance_id , TIMESTAMP ) VALUES ( ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? ) ON duplicate KEY UPDATE bytes = IFNULL( `VALUES`( bytes ) , bytes ) /* , ... */ = IFNULL( `VALUES`( comment ) , comment ) /* , ... */ = IFNULL( `VALUES`( connection_id ) , connection_id ) /* , ... */ = IFNULL( `VALUES`( data_base ) , data_base ) /* , ... */ = IFNULL( `VALUES`( errors ) , errors ) /* , ... */ = IFNULL( `VALUES`( exec_time ) , exec_time ) /* , ... */ = IFNULL(  `VALUES`( explain_plan ) , explain_plan ) /* , ... */ = IFNULL( `VALUES`( host_from ) , host_from ) /* , ... */ = IFNULL( `VALUES`( host_to ) , host_to ) /* , ... */ = IFNULL( `VALUES`( no_good_index_used ) , no_good_index_used ) /* , ... */ = IFNULL( `VALUES`( no_index_used ) , no_index_used ) /* , ... */ = IFNULL( `VALUES`( query_type ) , query_type ) /* , ... */ = IFNULL( `VALUES`( rows ) , rows ) /* , ... */ = IFNULL( `VALUES`( source_location ) , source_location ) /* , ... */ TEXT = IFNULL( `VALUES`( TEXT ) , TEXT ) /* , ... */ = IFNULL( `VALUES`( user ) , user ) /* , ... */ = IFNULL( `VALUES`( warnings ) , warnings )";
      string sql = @"	INSERT INTO statement_examples ( bytes , comment , connection_id , data_base , errors , exec_time , explain_plan , host_from , host_to , no_good_index_used , no_index_used , query_type , rows , source_location , TEXT , user , warnings , instance_id , TIMESTAMP ) VALUES ( ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? , ? ) ON duplicate KEY UPDATE bytes = IFNULL( `VALUES`( bytes ) , bytes )";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test135()
    {
      // Missing semicolon
      string sql = @"	SHOW CREATE PROCEDURE barC518;	SHOW CREATE TRIGGER a";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test136()
    {
      string sql = "	SELECT GROUP_CONCAT('\\\\\n* ', t.table_schema, '.', t.table_name, ' (', ROUND(t.pct_used, 1) , '%)' ORDER BY pct_used DESC) AS table_list    FROM ( SELECT t.table_schema, t.table_name,                  100 * (t.auto_increment / (                 CASE                    WHEN ((LOCATE('tinyint',   c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) = 0)) THEN 127                    WHEN ((LOCATE('tinyint',   c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) > 0)) THEN 255                    WHEN ((LOCATE('smallint',  c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) = 0)) THEN 32767                    WHEN ((LOCATE('smallint',  c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) > 0)) THEN 65535                    WHEN ((LOCATE('mediumint', c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) = 0)) THEN 8388607                    WHEN ((LOCATE('mediumint', c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) > 0)) THEN 16777215                    WHEN ((LOCATE('bigint',    c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) = 0)) THEN 9223372036854775807                    WHEN ((LOCATE('bigint',    c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) > 0)) THEN 18446744073709551615                    WHEN ((LOCATE('int',       c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) = 0)) THEN 2147483647                    WHEN ((LOCATE('int',       c.column_type) > 0) AND (LOCATE('unsigned', c.column_type) > 0)) THEN 4294967295                    ELSE 0                  END)) AS pct_used             FROM information_schema.tables t, information_schema.columns c            WHERE t.table_schema = c.table_schema              AND t.table_name = c.table_name              AND LOCATE('auto_increment', c.extra) > 0        ) AS t   WHERE t.pct_used > 75";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test137()
    {
      string sql = @"	SELECT ftable.id AS ftable_id, ftable.tid AS ftable_tid, ftable.fid AS ftable_fid, ftable.tablename AS ftable_tablename, ftable.ref AS ftable_ref FROM ftable INNER JOIN ttable ON ttable.id = ftable.tid WHERE ttable.state = 'TEST' AND ftable.fid = 'TEST' AND (ftable.tablename = 'testa' AND ftable.ref = 981675 OR ftable.tablename = 'testa' AND ftable.ref = 981663 OR ftable.tablename = 'testa' AND ftable.ref = 981668 OR ftable.tablename = 'testa' AND ftable.ref = 981694 OR ftable.tablename = 'testa' AND ftable.ref = 981699 OR ftable.tablename = 'testa' AND ftable.ref = 981687 OR ftable.tablename = 'testa' AND ftable.ref = 981659 OR ftable.tablename = 'testa' AND ftable.ref = 981664 OR ftable.tablename = 'testa' AND ftable.ref = 981690 OR ftable.tablename = 'testa' AND ftable.ref = 981652 OR ftable.tablename = 'testa' AND ftable.ref = 981683 OR ftable.tablename = 'testa' AND ftable.ref = 981676 OR ftable.tablename = 'testa' AND ftable.ref = 981669 OR ftable.tablename = 'testa' AND ftable.ref = 981695 OR ftable.tablename = 'testa' AND ftable.ref = 981672 OR ftable.tablename = 'testa' AND ftable.ref = 981660 OR ftable.tablename = 'testa' AND ftable.ref = 981665 OR ftable.tablename = 'testa' AND ftable.ref = 981691 OR ftable.tablename = 'testa' AND ftable.ref = 981696 OR ftable.tablename = 'testa' AND ftable.ref = 981653 OR ftable.tablename = 'testa' AND ftable.ref = 981684 OR ftable.tablename = 'testa' AND ftable.ref = 981656 OR ftable.tablename = 'testa' AND ftable.ref = 981677 OR ftable.tablename = 'testa' AND ftable.ref = 981670 OR ftable.tablename = 'testa' AND ftable.ref = 981680 OR ftable.tablename = 'testa' AND ftable.ref = 981673 OR ftable.tablename = 'testa' AND ftable.ref = 981661 OR ftable.tablename = 'testa' AND ftable.ref = 981666 OR ftable.tablename = 'testa' AND ftable.ref = 981692 OR ftable.tablename = 'testa' AND ftable.ref = 981697 OR ftable.tablename = 'testa' AND ftable.ref = 981654 OR ftable.tablename = 'testa' AND ftable.ref = 981685 OR ftable.tablename = 'testa' AND ftable.ref = 981657 OR ftable.tablename = 'testa' AND ftable.ref = 981678 OR ftable.tablename = 'testa' AND ftable.ref = 981688 OR ftable.tablename = 'testa' AND ftable.ref = 981650 OR ftable.tablename = 'testa' AND ftable.ref = 981671 OR ftable.tablename = 'testa' AND ftable.ref = 981681 OR ftable.tablename = 'testa' AND ftable.ref = 981674 OR ftable.tablename = 'testa' AND ftable.ref = 981662 OR ftable.tablename = 'testa' AND ftable.ref = 981667 OR ftable.tablename = 'testa' AND ftable.ref = 981693 OR ftable.tablename = 'testa' AND ftable.ref = 981698 OR ftable.tablename = 'testa' AND ftable.ref = 981655 OR ftable.tablename = 'testa' AND ftable.ref = 981686 OR ftable.tablename = 'testa' AND ftable.ref = 981658 OR ftable.tablename = 'testa' AND ftable.ref = 981679 OR ftable.tablename = 'testa' AND ftable.ref = 981689 OR ftable.tablename = 'testa' AND ftable.ref = 981651 OR ftable.tablename = 'testa' AND ftable.ref = 981682) ORDER BY ttable.datetime DESC";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public void Test138()
    {
      // release is not a valid identifier in this context
      string sql = @"SELECT SQL_NO_CACHE room_id , rate_id , price1ok , `release` * ? ,
max_advance_res * ? , starts , ends , default_policy_group_id FROM
B_Rate_Room_Directory WHERE room_id IN ( ? /* , ... */ ) AND active";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test139()
    {
      string sql = @"REPLACE INTO support_issue_bug ( issue_id , bug_id , bug_category ,
bug_status , bug_reported_date , is_currently_associated ) SELECT si .
issue_id , bdb . id , bdb . bug_type , bdb . status , bdb . ts1 , ? FROM
support_issue si JOIN bugs . bugdb bdb ON ( bdb . affectedissues IS NOT NULL
AND bdb . affectedissues != ? AND FIND_IN_SET( REPLACE( bdb . affectedissues ,
 ? , ? ) , si . issue_id ) )
";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test140()
    {
      string sql = " SELECT GROUP_CONCAT('\\\\\n* ', t.table_schema, '.', t.table_name, ' (', " +
@"ROUND(t.pct_used, 1) , '%)' ORDER BY pct_used DESC) AS table_list
   FROM ( SELECT t.table_schema, t.table_name,
                 100 * (t.auto_increment / (
                 CASE
                   WHEN ((LOCATE('tinyint',   c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) = 0)) THEN 127
                   WHEN ((LOCATE('tinyint',   c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) > 0)) THEN 255
                   WHEN ((LOCATE('smallint',  c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) = 0)) THEN 32767
                   WHEN ((LOCATE('smallint',  c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) > 0)) THEN 65535
                   WHEN ((LOCATE('mediumint', c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) = 0)) THEN 8388607
                   WHEN ((LOCATE('mediumint', c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) > 0)) THEN 16777215
                   WHEN ((LOCATE('bigint',    c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) = 0)) THEN 9223372036854775807
                   WHEN ((LOCATE('bigint',    c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) > 0)) THEN 18446744073709551615
                   WHEN ((LOCATE('int',       c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) = 0)) THEN 2147483647
                   WHEN ((LOCATE('int',       c.column_type) > 0) AND
 (LOCATE('unsigned', c.column_type) > 0)) THEN 4294967295
                   ELSE 0
                 END)) AS pct_used
            FROM information_schema.tables t, information_schema.columns c
           WHERE t.table_schema = c.table_schema
             AND t.table_name = c.table_name
             AND LOCATE('auto_increment', c.extra) > 0
        ) AS t
  WHERE t.pct_used > 75";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test141()
    {
      string sql = "SELECT GROUP_CONCAT('\\\\\n* ', '\'', u.user, '\'@\'', u.host ,'\' requires the ', u.plugin, ' plugin (', IF(p.PLUGIN_STATUS = 'DISABLED', 'which is disabled)', 'which is not installed)')) AS requirement_list FROM mysql.user u LEFT JOIN INFORMATION_SCHEMA.PLUGINS p ON (u.plugin = p.PLUGIN_NAME) WHERE (p.PLUGIN_NAME IS NULL OR p.PLUGIN_STATUS = 'DISABLED') AND TRIM(u.plugin) <> ''";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test142()
    {
      string sql = @"SELECT IF(end_time >= (NOW() - INTERVAL 60 SECOND), TIMESTAMPDIFF(SECOND, start_time, end_time), 0) AS total_time, IF((end_time >= (NOW() - INTERVAL 60 SECOND)), lock_time, 0) AS lock_time FROM mysql.backup_history ORDER BY end_time DESC LIMIT 1";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test143()
    {
      // current_time is not valid identifier in this context
      string sql = "SELECT start_time AS start_time_raw, UNIX_TIMESTAMP(start_time) AS start_time_ts, end_time AS end_time_raw, UNIX_TIMESTAMP(end_time) AS end_time_ts, IFNULL(TIMESTAMPDIFF(SECOND, start_time, end_time), 0) AS total_time, lock_time, exit_state, last_error, last_error_code, (SELECT GROUP_CONCAT('\\\\\n* ', backup_progress.`current_time`, ': ', IF((error_message != 'NO_ERROR' OR current_state = ''), CONCAT(error_message, ' (errcode: ', error_code, ') ', current_state), current_state)) progress_log FROM mysql.backup_progress WHERE backup_progress.backup_id = backup_history.backup_id GROUP BY backup_id) AS progress_log, UNIX_TIMESTAMP() AS collected_ts, UNIX_TIMESTAMP() AS collected_ts_counter, mysql_data_dir, backup_destination FROM mysql.backup_history ORDER BY backup_id DESC LIMIT 1";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test144()
    {
      string sql = @"SELECT GROUP_CONCAT(plugin_name ORDER BY plugin_name SEPARATOR ""!"") AS innodb_plugin_list FROM information_schema.plugins WHERE plugin_name IN ('InnoDB', 'INNODB_TRX', 'INNODB_LOCKS', 'INNODB_LOCK_WAITS', 'INNODB_CMP', 'INNODB_CMPMEM') AND plugin_status = ""ACTIVE""";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test145()
    {
      string sql = "SELECT DISTINCT GROUP_CONCAT('\\\\\n* ', s.table_schema, '.', s.table_name, '.', s.index_name) AS table_list "  +
@"FROM information_schema.statistics s
JOIN information_schema.tables t
  ON (s.table_schema = t.table_schema
  AND s.table_name = t.table_name)
WHERE s.table_schema NOT IN ('mysql', 'information_schema', 'performance_schema')
  AND t.engine = 'MyISAM'
  AND s.cardinality IS NULL";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }

    [Fact]
    public void Test146()
    {
      string sql = @"SELECT COUNT(*) AS num_long_running, @@long_query_time AS long_running_time FROM information_schema.processlist WHERE time > @@long_query_time AND state NOT IN ('Sleep', 'Waiting for master to send event', 'Has read all relay log; waiting for the slave I/O thread t', 'Waiting on empty queue')";
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(sql, false, out sb);
    }
  }
}
