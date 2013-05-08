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
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Xunit;


namespace MySql.Parser.Tests.DDL.Show
{
  
  public class Show
  {
    [Fact]		
    public void Simple()
    {
      TestShow("SHOW AUTHORS", ShowStatementType.ShowAuthors);
      TestShow("SHOW CONTRIBUTORS", ShowStatementType.ShowContributors);
      TestShow("SHOW EVENTS", ShowStatementType.ShowEvents);
            //TestShow("SHOW PLUGINS", ShowStatementType.ShowPlugins);
      TestShow("SHOW PROFILES", ShowStatementType.ShowProfiles);
      TestShow("SHOW PRIVILEGES", ShowStatementType.ShowPrivileges);
      TestShow("SHOW SLAVE HOSTS", ShowStatementType.ShowSlaveHosts);
      TestShow("SHOW SLAVE STATUS", ShowStatementType.ShowSlaveStatus);
      TestShow("SHOW INNODB STATUS", ShowStatementType.ShowInnoDBStatus);
      TestShow("SHOW MASTER STATUS", ShowStatementType.ShowMasterStatus);
      TestShow("SHOW SCHEDULER STATUS", ShowStatementType.ShowSchedulerStatus);
      TestShow("SHOW ENGINES", ShowStatementType.ShowEngines);
      /* Illegal or deprecated */
      //TestShow("SHOW FULL ENGINES", ShowStatementType.ShowEngines);
      TestShow("SHOW BINARY LOGS", ShowStatementType.ShowLogs);
      MySQL51Parser.program_return r = Utility.ParseSql("SHOW MASTER LOGS");
      Assert.Equal( "SHOW", (( CommonTree )r.Tree ).GetChild( 0 ).Text );
      Assert.Equal( "MASTER", (( CommonTree )r.Tree ).GetChild( 0 ).GetChild( 0 ).Text );
      Assert.Equal("LOGS", ((CommonTree)r.Tree).GetChild(0).GetChild(0).GetChild(0).Text);
    }

    [Fact]
    public void ShowRoutines()
    {
      TestShow("SHOW FUNCTION CODE `myfunc`", ShowStatementType.ShowFunctionCode);
      /*
      Assert.Equal("`myfunc`", ss.Object);
      */
      TestShow("SHOW PROCEDURE CODE `myproc`", ShowStatementType.ShowProcedureCode);
      /*
      Assert.Equal("`myproc`", ss.Object);
      */
      //TODO: implement tests for these
      //SHOW FUNCTION STATUS [like_or_where]
      //SHOW PROCEDURE STATUS [like_or_where]
    }

    [Fact]
    public void ShowGrants()
    {
      TestShow("SHOW GRANTS FOR 'userx'", ShowStatementType.ShowGrants);
      /*
      Assert.Equal("'userx'", ss.Id);
       * */
      MySQL51Parser.program_return r = Utility.ParseSql("show grants for current_user");
      r = Utility.ParseSql("show grants for current_user()");
    }

    // TODO: implement tests for these
    //SHOW OPEN TABLES [FROM db_name] [like_or_where]
    //SHOW TABLE STATUS [FROM db_name] [like_or_where]
    //SHOW TRIGGERS [FROM db_name] [like_or_where]

    [Fact]
    public void ShowFull()
    {
      TestShow("SHOW PROCESSLIST", ShowStatementType.ShowProcesslist);
      TestShow("SHOW FULL PROCESSLIST", ShowStatementType.ShowProcesslist);
    }

    [Fact]
    public void ShowCharacterSet()
    {
      /*ShowStatement ss = */TestShow("SHOW CHARACTER SET", ShowStatementType.ShowCharacterSet);
      /*
      Assert.Null(ss.Like);
      Assert.Null(ss.Where);*/
      /*ss = */TestShow("SHOW CHARACTER SET LIKE '%utf8%'", ShowStatementType.ShowCharacterSet);
      /*Assert.Null(ss.Where);
      Assert.NotNull(ss.Like);
      Assert.Equal("'%utf8%'", ss.Like.Value);*/
      /*ss = */TestShow("SHOW CHARACTER SET WHERE colname LIKE '%utf8%'", ShowStatementType.ShowCharacterSet);
      /*Assert.NotNull(ss.Where);
      Assert.Null(ss.Like);*/
    }

    [Fact]
    public void ShowCollation()
    {
      /*ShowStatement ss = */TestShow("SHOW COLLATION", ShowStatementType.ShowCollation);
      /*Assert.Null(ss.Like);
      Assert.Null(ss.Where);*/
      /*ss = */TestShow("SHOW COLLATION LIKE '%utf8%'", ShowStatementType.ShowCollation);
      /*Assert.Null(ss.Where);
      Assert.NotNull(ss.Like);
      Assert.Equal("'%utf8%'", ss.Like.Value);
      ss = */TestShow("SHOW COLLATION WHERE colname LIKE '%utf8%'", ShowStatementType.ShowCollation);
      /*
      Assert.NotNull(ss.Where);
      Assert.Null(ss.Like);*/
    }

    [Fact]
    public void ShowDatabase()
    {
      /*ShowStatement ss = */TestShow("SHOW DATABASES", ShowStatementType.ShowDatabases);
      /*Assert.Null(ss.Like);
      Assert.Null(ss.Where);
      ss = */TestShow("SHOW DATABASES LIKE '%utf8%'", ShowStatementType.ShowDatabases);
      /*Assert.Null(ss.Where);
      Assert.NotNull(ss.Like);
      Assert.Equal("'%utf8%'", ss.Like.Value);
      ss = */TestShow("SHOW DATABASES WHERE colname LIKE '%utf8%'", ShowStatementType.ShowDatabases);
      /*Assert.NotNull(ss.Where);
      Assert.Null(ss.Like);*/
    }

    private void TestShow(string sql, ShowStatementType type)
    {
      MySQL51Parser.program_return r = Utility.ParseSql( sql );
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is ShowStatement);
      ShowStatement ds = statements[0] as ShowStatement;
      Assert.Equal(type, ds.Type);
      return ds;
       * */
    }

    [Fact]
    public void BadShow()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("SHOW BAD", true);
    }

    [Fact]
    public void Profile1()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("show profiles", false);
    }

    [Fact]
    public void Profile2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("show profile", false);
    }

    [Fact]
    public void Profile3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("show profile for query 1", false);
    }

    [Fact]
    public void Profile4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("show profiles cpu for query 2", false);
    }

        [Fact]
        public void ShowInnodbStatus()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql("show innodb status", true, out sb, new Version(5, 5));
          Assert.True(sb.ToString().IndexOf("no viable alternative at input 'innodb'") != -1 );
        }

        [Fact]
        public void ShowInnodbStatus2()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql("show innodb status", false, out sb, new Version(5, 0));
        }

        [Fact]
        public void ShowPlugins50()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql("show plugins", true, out sb, new Version(5, 0));
          Assert.True(sb.ToString().IndexOf("no viable alternative at input 'PLUGINS'", StringComparison.OrdinalIgnoreCase) != -1);
        }

        [Fact]
        public void ShowPlugins()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql("show plugins", false, out sb, new Version(5, 1));
        }

    public enum ShowStatementType
    {
      Unknown,
      ShowAuthors,
      ShowContributors,
      ShowEvents,
      ShowPlugins,
      ShowProfiles,
      ShowPrivileges,
      ShowSlaveHosts,
      ShowSlaveStatus,
      ShowInnoDBStatus,
      ShowMasterStatus,
      ShowSchedulerStatus,
      ShowCreateDatabase,
      ShowCreateEvent,
      ShowCreateFunction,
      ShowCreateProcedure,
      ShowCreateTable,
      ShowCreateTrigger,
      ShowCreateView,
      ShowBinaryMasterLogs,
      ShowCollation,
      ShowDatabases,
      ShowEngines,
      ShowProcesslist,
      ShowProcedureCode,
      ShowFunctionCode,
      ShowProcedureStatus,
      ShowFunctionStatus,
      ShowCharacterSet,
      ShowLogs,
      ShowGrants,
      ShowIndex,
      ShowOpenTables,
      ShowTableStatus,
      ShowTriggers
    }
  }
}
