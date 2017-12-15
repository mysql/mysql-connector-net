// Copyright © 2013, 2017, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.MySqlClient;
using System.Data;
using System.Configuration;
using System.Reflection;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Resources;
using System.Xml;
using System.IO;
using System.Text;
using Xunit;
using MySql.Data.EntityFramework.Tests;
using System.Data.Entity.Core.EntityClient;


namespace MySql.Data.EntityFramework.CodeFirst.Tests
{
  public class SetUpCodeFirstTests : SetUpClass, IDisposable
  {
    // A trace listener to use during testing.
    private AssertFailTraceListener asertFailListener = new AssertFailTraceListener();

    public override void  Initialize()
    {
      database0 = database1 = "test";
      MySqlConnection.ClearAllPools();
    }
    
    public SetUpCodeFirstTests():base()
    {      

      // Override sql_mode so it converts automatically from varchar(65535) to text
      MySqlCommand cmd = new MySqlCommand("SET GLOBAL SQL_MODE=``", rootConn);
      cmd.ExecuteNonQuery();

      // Replace existing listeners with listener for testing.
      Trace.Listeners.Clear();
      Trace.Listeners.Add(this.asertFailListener);

      DataSet dataSet = ConfigurationManager.GetSection("system.data") as System.Data.DataSet;
      DataView vi = dataSet.Tables[0].DefaultView;
      vi.Sort = "Name";
      int idx = -1;
      if ( ((idx = vi.Find("MySql")) != -1 ) || (( idx = vi.Find("MySQL Data Provider")) != -1 ) )
      {
        DataRow row = vi[idx].Row;
        dataSet.Tables[0].Rows.Remove(row);
      }
      dataSet.Tables[0].Rows.Add("MySql"
        , "MySql.Data.MySqlClient"
        , "MySql.Data.MySqlClient"
        ,
        typeof(MySql.Data.MySqlClient.MySqlClientFactory).AssemblyQualifiedName);


      cmd = new MySqlCommand("SELECT COUNT(SCHEMA_NAME) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'sakila'", rootConn);

      if (Convert.ToInt32(cmd.ExecuteScalar() ?? 0) == 0)
      {
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        using (var stream = executingAssembly.GetManifestResourceStream("MySql.Data.EntityFramework.CodeFirst.Tests.Properties.SetupSakilaDb.sql"))
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                string sql = sr.ReadToEnd();
                MySqlScript s = new MySqlScript(rootConn, sql);
                s.Execute();
            }
        }

        using (var stream = executingAssembly.GetManifestResourceStream("MySql.Data.EntityFramework.CodeFirst.Tests.Properties.SakilaDbDataScript.sql"))
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                string sql = sr.ReadToEnd();
                MySqlScript s = new MySqlScript(rootConn, sql);
                s.Execute();
            }
        }
      }
    }
    
    public override void Dispose()
    {
      base.Dispose();      
    }

    private EntityConnection GetEntityConnection()
    {
      return null;
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
        Assert.True(message == String.Empty, "Failure: " + message);       
      }

      public override void Fail(string message, string detailMessage)
      {
        Assert.True(message == String.Empty, "Failure: " + message);        
      }
    }

  }
}
