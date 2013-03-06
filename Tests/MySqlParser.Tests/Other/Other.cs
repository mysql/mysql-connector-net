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
  public class Other
  {
	[Test]
	public void Purge()
	{
      MySQL51Parser.program_return r = Utility.ParseSql("PURGE BINARY LOGS TO 'mysql-bin.010';", false);
	}

    [Test]
    public void Purge2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("PURGE BINARY LOGS BEFORE '2008-04-02 22:46:26';", false);
    }

    [Test]
    public void ResetMaster()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("reset master;", false);
    }

    [Test]
    public void ChangeMaster()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(@"STOP SLAVE; -- if replication was running
CHANGE MASTER TO MASTER_PASSWORD='new3cret';
START SLAVE; -- if you want to restart replication
", false);
    }

    [Test]
    public void ChangeMaster2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CHANGE MASTER TO IGNORE_SERVER_IDS = ();;", false);
    }

    [Test]
    public void ChangeMaster3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CHANGE MASTER TO IGNORE_SERVER_IDS = ( 1, 3 );", false);
    }

    [Test]
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

    [Test]
    public void ChangeMaster5()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CHANGE MASTER TO
  RELAY_LOG_FILE='slave-relay-bin.006',
  RELAY_LOG_POS=4025;", false);
    }

    [Test]
    public void ChangeMaster6()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"CHANGE MASTER TO
  RELAY_LOG_FILE='myhost-bin.153',
  RELAY_LOG_POS=410,
  MASTER_HOST='some_dummy_string';
", false);
    }

    [Test]
    public void StartSlave()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("START SLAVE SQL_THREAD;", false);
    }

    [Test]
    public void LoadData51()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD DATA FROM MASTER", false, new Version( 5, 1 ));
    }

    [Test]
    public void LoadData55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD DATA FROM MASTER", true, out sb, new Version( 5, 5 ));
      Assert.IsTrue(sb.ToString().IndexOf("no viable alternative at input 'FROM'") != -1);
    }

    [Test]
    public void LoadTable51()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD TABLE tbl_name FROM MASTER", false, new Version( 5, 1 ));
    }

    [Test]
    public void LoadTable55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD TABLE tbl_name FROM MASTER", true, out sb, new Version( 5, 5 ));
      Assert.IsTrue(sb.ToString().IndexOf("no viable alternative at input 'TABLE'") != -1);
    }

    [Test]
    public void ResetSlave()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("reset slave;", false);
    }

    [Test]
    public void SqlSlaveSkipCounter()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("SET GLOBAL sql_slave_skip_counter = 200", false);
    }

    [Test]
    public void StartSlave2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("START SLAVE UNTIL MASTER_LOG_FILE='/tmp/log', MASTER_LOG_POS=101;", false);
    }

    [Test]
    public void StopSlave()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("stop slave;", false);
    }

    [Test]
    public void StopSlave2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("stop slave IO_THREAD;", false);
    }

    [Test]
    public void AnalyzeLocal()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("analyze local table tbl1;", false);
    }

    [Test]
    public void AnalyzeNoWrite()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("analyze no_write_to_binlog table `tab1`, `tab2`, tab3;", false);
    }

    [Test]
    public void BackupTable51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("BACKUP TABLE tbl_name, tbl_name TO '/path/to/backup/directory';", false, out sb, new Version( 5, 1 ));
    }

    [Test]
    public void BackupTable55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("BACKUP TABLE tbl_name, tbl_name TO '/path/to/backup/directory';", true, out sb, new Version( 5, 5 ));
      Assert.IsTrue(sb.ToString().IndexOf("no viable alternative at input 'BACKUP'") != -1);
    }

    [Test]
    public void RestoreTable51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("RESTORE TABLE tbl_name, tbl_name FROM '/path/to/backup/directory';", false, out sb, new Version( 5, 1 ));
    }

    [Test]
    public void RestoreTable55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("RESTORE TABLE tbl_name, tbl_name FROM '/path/to/backup/directory';", true, out sb, new Version( 5, 5 ));
      Assert.IsTrue(sb.ToString().IndexOf("no viable alternative at input 'RESTORE'") != -1);
    }

    [Test]
    public void CheckTable()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CHECK TABLE test_table FAST QUICK;", false);
    }

    [Test]
    public void Checksum()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("checksum table tab1 quick;", false);
    }

    [Test]
    public void Checksum2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("checksum table tab1, tab3 extended", false);
    }

    [Test]
    public void Optimize()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("OPTIMIZE TABLE foo;", false);
    }

    [Test]
    public void Binlog()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("binlog 'x';", false);
    }

    [Test]
    public void CacheIndex()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX t1, t2, t3 IN hot_cache;", false);
    }

    [Test]
    public void CacheIndex2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX t1 IN non_existent_cache;", false);
    }

    [Test]
    public void CacheIndexPartition51_1()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (p0) IN kc_fast;", true, out sb, new Version( 5, 1 ));
      Assert.IsTrue( sb.ToString().IndexOf( "'partition'", StringComparison.OrdinalIgnoreCase ) != -1 );
    }

    [Test]
    public void CacheIndexPartition55_1()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (p0) IN kc_fast;", false, out sb, new Version( 5, 5 ));
    }

    [Test]
    public void CacheIndexPartition55_2()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (p1, p3) IN kc_slow;", false, out sb, new Version(5, 5));
    }

    [Test]
    public void CacheIndexPartition55_3()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("CACHE INDEX pt PARTITION (ALL) IN kc_all;", false, out sb, new Version(5, 5));
    }

    [Test]
    public void Flush()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("flush logs;", false);
    }

    [Test]
    public void Flush2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("flush tables;", false);
    }

    [Test]
    public void Kill()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("kill connection 20;", false);
    }

    [Test]
    public void Load()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE t1, t2 IGNORE LEAVES;", false);
    }

    [Test]
    public void LoadPartition_1_51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (p0);", true, out sb, new Version(5, 1));
      Assert.IsTrue(sb.ToString().IndexOf("partition", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Test]
    public void LoadPartition_1_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (p0);", false, out sb, new Version(5, 5));
    }

    [Test]
    public void LoadPartition_2_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (p1, p3);", false, out sb, new Version(5, 5));
    }

    [Test]
    public void LoadPartition_3_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql("LOAD INDEX INTO CACHE pt PARTITION (ALL);", false, out sb, new Version(5, 5));
    }

    [Test]
    public void Reset()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("RESET QUERY CACHE;", false);
    }

    [Test]
    public void CreateUdf()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("create aggregate function HyperbolicSum returns real soname 'libso';", false);
    }

    [Test]
    public void InstallPlugin()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("install plugin myplugin soname 'libmyplugin.so';", false);
    }

    [Test]
    public void UninstallPlugin()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("uninstall plugin myplugin;", false);
    }

    [Test]
    public void LoadXml_51()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"
LOAD XML LOCAL INFILE 'person.xml'
       INTO TABLE person
       ROWS IDENTIFIED BY '<person>';", true, out sb, new Version(5, 1));
      Assert.IsTrue(sb.ToString().IndexOf("xml", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Test]
    public void LoadXml_1_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"
LOAD XML LOCAL INFILE 'person.xml'
       INTO TABLE person
       ROWS IDENTIFIED BY '<person>';", false, out sb, new Version(5, 5));
    }

    [Test]
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
