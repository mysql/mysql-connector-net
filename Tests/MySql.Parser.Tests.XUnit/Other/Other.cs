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
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Xunit;


namespace MySql.Parser.Tests
{
  
  public class Other
  {
  [Fact]
  public void Purge()
  {
      MySQL51Parser.program_return r = Utility.ParseSql("PURGE BINARY LOGS TO 'mysql-bin.010';", false);
  }

    [Fact]
    public void Purge2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("PURGE BINARY LOGS BEFORE '2008-04-02 22:46:26';", false);
    }

    [Fact]
    public void ResetMaster()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("reset master;", false);
    }

    [Fact]
    public void ChangeMaster()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"STOP SLAVE; -- if replication was running
CHANGE MASTER TO MASTER_PASSWORD='new3cret';
START SLAVE; -- if you want to restart replication
", false);
    }

    [Fact]
    public void ChangeMaster2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CHANGE MASTER TO IGNORE_SERVER_IDS = ();;", false);
    }

    [Fact]
    public void ChangeMaster3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CHANGE MASTER TO IGNORE_SERVER_IDS = ( 1, 3 );", false);
    }

    [Fact]
    public void ChangeMaster4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CHANGE MASTER TO
  MASTER_HOST='master2.mycompany.com',
  MASTER_USER='replication',
  MASTER_PASSWORD='bigs3cret',
  MASTER_PORT=3306,
  MASTER_LOG_FILE='master2-bin.001',
  MASTER_LOG_POS=4,
  MASTER_CONNECT_RETRY=10;
;", false);
    }

    [Fact]
    public void ChangeMaster5()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CHANGE MASTER TO
  RELAY_LOG_FILE='slave-relay-bin.006',
  RELAY_LOG_POS=4025;", false);
    }

    [Fact]
    public void ChangeMaster6()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CHANGE MASTER TO
  RELAY_LOG_FILE='myhost-bin.153',
  RELAY_LOG_POS=410,
  MASTER_HOST='some_dummy_string';
", false);
    }

    [Fact]
    public void StartSlave()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("START SLAVE SQL_THREAD;", false);
    }

    [Fact]
    public void LoadData51()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD DATA FROM MASTER", false, new Version( 5, 1 ));
    }

    [Fact]
    public void LoadData55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD DATA FROM MASTER", true, out sb, new Version( 5, 5 ));
      Assert.True(sb.ToString().IndexOf("no viable alternative at input 'FROM'") != -1);
    }

    [Fact]
    public void LoadTable51()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD TABLE tbl_name FROM MASTER", false, new Version( 5, 1 ));
    }

    [Fact]
    public void LoadTable55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD TABLE tbl_name FROM MASTER", true, out sb, new Version( 5, 5 ));
      Assert.True(sb.ToString().IndexOf("no viable alternative at input 'TABLE'") != -1);
    }

    [Fact]
    public void ResetSlave()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("reset slave;", false);
    }

    [Fact]
    public void SqlSlaveSkipCounter()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("SET GLOBAL sql_slave_skip_counter = 200", false);
    }

    [Fact]
    public void StartSlave2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("START SLAVE UNTIL MASTER_LOG_FILE='/tmp/log', MASTER_LOG_POS=101;", false);
    }

    [Fact]
    public void StopSlave()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("stop slave;", false);
    }

    [Fact]
    public void StopSlave2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("stop slave IO_THREAD;", false);
    }

    [Fact]
    public void AnalyzeLocal()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("analyze local table tbl1;", false);
    }

    [Fact]
    public void AnalyzeNoWrite()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("analyze no_write_to_binlog table `tab1`, `tab2`, tab3;", false);
    }

    [Fact]
    public void BackupTable51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("BACKUP TABLE tbl_name, tbl_name TO '/path/to/backup/directory';", false, out sb, new Version( 5, 1 ));
    }

    [Fact]
    public void BackupTable55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("BACKUP TABLE tbl_name, tbl_name TO '/path/to/backup/directory';", true, out sb, new Version( 5, 5 ));
      Assert.True(sb.ToString().IndexOf("no viable alternative at input 'BACKUP'") != -1);
    }

    [Fact]
    public void RestoreTable51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("RESTORE TABLE tbl_name, tbl_name FROM '/path/to/backup/directory';", false, out sb, new Version( 5, 1 ));
    }

    [Fact]
    public void RestoreTable55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("RESTORE TABLE tbl_name, tbl_name FROM '/path/to/backup/directory';", true, out sb, new Version( 5, 5 ));
      Assert.True(sb.ToString().IndexOf("no viable alternative at input 'RESTORE'") != -1);
    }

    [Fact]
    public void CheckTable()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CHECK TABLE test_table FAST QUICK;", false);
    }

    [Fact]
    public void Checksum()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("checksum table tab1 quick;", false);
    }

    [Fact]
    public void Checksum2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("checksum table tab1, tab3 extended", false);
    }

    [Fact]
    public void Optimize()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("OPTIMIZE TABLE foo;", false);
    }

    [Fact]
    public void Binlog()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("binlog 'x';", false);
    }

    [Fact]
    public void CacheIndex()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX t1, t2, t3 IN hot_cache;", false);
    }

    [Fact]
    public void CacheIndex2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX t1 IN non_existent_cache;", false);
    }

    [Fact]
    public void CacheIndexPartition51_1()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (p0) IN kc_fast;", true, out sb, new Version( 5, 1 ));
      Assert.True( sb.ToString().IndexOf( "'partition'", StringComparison.OrdinalIgnoreCase ) != -1 );
    }

    [Fact]
    public void CacheIndexPartition55_1()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (p0) IN kc_fast;", false, out sb, new Version( 5, 5 ));
    }

    [Fact]
    public void CacheIndexPartition55_2()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (p1, p3) IN kc_slow;", false, out sb, new Version(5, 5));
    }

    [Fact]
    public void CacheIndexPartition55_3()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (ALL) IN kc_all;", false, out sb, new Version(5, 5));
    }

    [Fact]
    public void Flush()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("flush logs;", false);
    }

    [Fact]
    public void Flush2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("flush tables;", false);
    }

    [Fact]
    public void Kill()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("kill connection 20;", false);
    }

    [Fact]
    public void Load()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE t1, t2 IGNORE LEAVES;", false);
    }

    [Fact]
    public void LoadPartition_1_51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (p0);", true, out sb, new Version(5, 1));
      Assert.True(sb.ToString().IndexOf("partition", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Fact]
    public void LoadPartition_1_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (p0);", false, out sb, new Version(5, 5));
    }

    [Fact]
    public void LoadPartition_2_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (p1, p3);", false, out sb, new Version(5, 5));
    }

    [Fact]
    public void LoadPartition_3_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (ALL);", false, out sb, new Version(5, 5));
    }

    [Fact]
    public void Reset()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("RESET QUERY CACHE;", false);
    }

    [Fact]
    public void CreateUdf()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("create aggregate function HyperbolicSum returns real soname 'libso';", false);
    }

    [Fact]
    public void InstallPlugin()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("install plugin myplugin soname 'libmyplugin.so';", false);
    }

    [Fact]
    public void UninstallPlugin()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("uninstall plugin myplugin;", false);
    }

    [Fact]
    public void LoadXml_51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"
LOAD XML LOCAL INFILE 'person.xml'
       INTO TABLE person
       ROWS IDENTIFIED BY '<person>';", true, out sb, new Version(5, 1));
      Assert.True(sb.ToString().IndexOf("xml", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Fact]
    public void LoadXml_1_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"
LOAD XML LOCAL INFILE 'person.xml'
       INTO TABLE person
       ROWS IDENTIFIED BY '<person>';", false, out sb, new Version(5, 5));
    }

    [Fact]
    public void LoadXml_2_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"
LOAD XML LOCAL INFILE 'person-dump.xml'
    INTO TABLE person2;", false, out sb, new Version(5, 5));
    }
  }
}
