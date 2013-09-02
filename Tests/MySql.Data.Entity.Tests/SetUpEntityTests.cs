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
using System.Text;
using MySql.Data.MySqlClient.Tests;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Resources;
using Xunit;
using System.Linq;
#if EF6
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
#else
using System.Data.EntityClient;
using System.Data.Objects;
#endif


namespace MySql.Data.Entity.Tests
{
  public class SetUpEntityTests : SetUpClass, IDisposable
  {
    // A trace listener to use during testing.
    private AssertFailTraceListener asertFailListener = new AssertFailTraceListener();

    internal protected override void Initialize()
    {
      database0 = database1 = "test";
      MySqlConnection.ClearAllPools();    
    }

    public SetUpEntityTests()
      : base()
    {
      // Replace existing listeners with listener for testing.
      Trace.Listeners.Clear();
      Trace.Listeners.Add(this.asertFailListener);

      ResourceManager r = new ResourceManager("MySql.Data.Entity.Tests.Properties.Resources", typeof(SetUpEntityTests).Assembly);
      string schema = r.GetString("schema");
      MySqlScript script = new MySqlScript(conn);
      script.Query = schema;
      script.Execute();

      // now create our procs
      schema = r.GetString("procs");
      script = new MySqlScript(conn);
      script.Delimiter = "$$";
      script.Query = schema;
      script.Execute();

      //ModelFirstModel1
      schema = r.GetString("ModelFirstModel1");
      script = new MySqlScript(conn);
      script.Query = schema;
      script.Execute();

      MySqlCommand cmd = new MySqlCommand("DROP DATABASE IF EXISTS `modeldb`", rootConn);
      cmd.ExecuteNonQuery();
    }

    public override void Dispose()
    {      
      if (rootConn.State != System.Data.ConnectionState.Open )
        rootConn.Open();
      
      MySqlCommand cmd = new MySqlCommand("DROP DATABASE IF EXISTS `modeldb`", rootConn);
      cmd.ExecuteNonQuery();
      
      base.Dispose();
    }

    private EntityConnection GetEntityConnection()
    {
      string connectionString = String.Format(
          "metadata=TestDB.csdl|TestDB.msl|TestDB.ssdl;provider=MySql.Data.MySqlClient; provider connection string=\"{0}\"", GetConnectionString(true));
      EntityConnection connection = new EntityConnection(connectionString);
      return connection;
    }

    protected internal void CheckSql(string sql, string refSql)
    {
      StringBuilder str1 = new StringBuilder();
      StringBuilder str2 = new StringBuilder();
      foreach (char c in sql)
        if (!Char.IsWhiteSpace(c))
          str1.Append(c);
      foreach (char c in refSql)
        if (!Char.IsWhiteSpace(c))
          str2.Append(c);
      Assert.Equal(0, String.Compare(str1.ToString(), str2.ToString(), true));
    }


    private class AssertFailTraceListener : DefaultTraceListener
    {
      public override void Fail(string message)
      {
        //Assert.Fail("Assertion failure: " + message);
      }

      public override void Fail(string message, string detailMessage)
      {
        //Assert.Fail("Assertion failure: " + detailMessage);
      }
    }
  }

  public static class ExtensionMethods
  {
    public static string ToTraceString<T>(this IQueryable<T> t)
    {
      // try to cast to ObjectQuery<T>
      ObjectQuery<T> oqt = t as ObjectQuery<T>;
      if (oqt != null)
        return oqt.ToTraceString();
      return "";
    }
  }
}
