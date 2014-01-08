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
using MySql.Data.MySqlClient;
using System.Reflection;
using System.IO;

namespace MySql.Debugger.Tests
{
  public class SetUp : IDisposable
  {    
    public SetUp()
    {
      MySqlConnection con = new MySqlConnection(TestUtils.CONNECTION_STRING_WITHOUT_DB);
      MySqlCommand cmd = new MySqlCommand("drop database if exists test", con);
      con.Open();

      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      Stream stream = executingAssembly.GetManifestResourceStream("MySql.Debugger.Tests.Properties.Setup.sql");
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Close();
      MySqlScript s = new MySqlScript(con, sql);      
      try
      {
        cmd.ExecuteNonQuery();
        cmd.CommandText = "create database test;";
        cmd.ExecuteNonQuery();
        s.Execute();
      }
      finally
      {
        con.Close();
      }
    }
  
    public void Dispose()
    {
      MySqlConnection con = new MySqlConnection(TestUtils.CONNECTION_STRING_WITHOUT_DB);
      MySqlCommand cmd = new MySqlCommand("drop database if exists test", con);
      con.Open();
      try
      {
        cmd.ExecuteNonQuery();
      }
      finally
      {
        con.Close();
      }
    }    
  }
}
