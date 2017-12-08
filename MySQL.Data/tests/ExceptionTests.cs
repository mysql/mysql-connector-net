﻿// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class ExceptionTests : TestBase
  {
    public ExceptionTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact (Skip =  "Having an issue")]
    public void Timeout()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100))");
      for (int i = 1; i < 10; i++)
        executeSQL("INSERT INTO Test VALUES (" + i + ", 'This is a long text string that I am inserting')");

      // we create a new connection so our base one is not closed
      var connection = Fixture.GetConnection(false);
      KillConnection(connection);
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", connection);    
       
      Exception ex = Assert.Throws<MySqlException>(() =>  cmd.ExecuteReader());
      Assert.Equal(ex.Message, "Connection must be valid and open.");     
      Assert.Equal(ConnectionState.Closed, connection.State);
      connection.Close();
      
    }
    /// <summary>
    /// Bug #27436 Add the MySqlException.Number property value to the Exception.Data Dictionary  
    /// </summary>
    [Fact]
    public void ErrorData()
    {
      MySqlCommand cmd = new MySqlCommand("SELEDT 1", Connection);
      try
      {
        cmd.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        Assert.Equal(1064, ex.Data["Server Error Code"]);
      }
    }
  }
}
