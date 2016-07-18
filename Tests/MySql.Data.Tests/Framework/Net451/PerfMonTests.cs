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
using Xunit;
using System.Diagnostics;
using System.Data;

#if !MONO
namespace MySql.Data.MySqlClient.Tests
{
  public class PerfMonTests : IUseFixture<SetUpClass>, IDisposable
  {

    protected SetUpClass st;
    private string _connString = string.Empty;

    public string conn
    {
      get
      {
        _connString = st.conn.ConnectionString;
        _connString += st.csAdditions;
        return _connString;
      }
    }

    public void SetFixture(SetUpClass data)
    {
      st = data;
      st.csAdditions = ";use performance monitor=true;";
      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(100))");
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
    }
    /// <summary>
    /// This test doesn't work from the CI setup currently
    /// </summary>
    [Fact]
    public void ProcedureFromCache()
    {
      //TODO: Check this test
      return;
      
      if (st.Version < new Version(5, 0)) return;      

      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
      st.execSQL("CREATE PROCEDURE spTest(id int) BEGIN END");

      PerformanceCounter hardQuery = new PerformanceCounter(
         ".NET Data Provider for MySQL", "HardProcedureQueries", true);
      PerformanceCounter softQuery = new PerformanceCounter(
         ".NET Data Provider for MySQL", "SoftProcedureQueries", true);
      long hardCount = hardQuery.RawValue;
      long softCount = softQuery.RawValue;

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.ExecuteScalar();

      Assert.Equal(hardCount + 1, hardQuery.RawValue);
      Assert.Equal(softCount, softQuery.RawValue);
      hardCount = hardQuery.RawValue;

      MySqlCommand cmd2 = new MySqlCommand("spTest", st.conn);
      cmd2.CommandType = CommandType.StoredProcedure;
      cmd2.Parameters.AddWithValue("?id", 1);
      cmd2.ExecuteScalar();

      Assert.Equal(hardCount, hardQuery.RawValue);
      Assert.Equal(softCount + 1, softQuery.RawValue);
    }
  }
}
#endif