// Copyright © 2011, 2013, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;

namespace MySql.Data.Entity.Tests
{
  public class DatesTypesTests : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

#if CLR4    
    [Fact]
    public void CanCreateDBScriptWithDateTimePrecision()
    {
      if (st.Version < new Version(5, 6, 5)) return;

      MySqlConnection c = new MySqlConnection(st.conn.ConnectionString);
      c.Open();

      var script = new MySqlScript(c);
      using (var ctx = new datesTypesEntities())
      {
        MySqlCommand query = new MySqlCommand("Create database test_types", c);
        query.Connection = c;
        query.ExecuteNonQuery();
        c.ChangeDatabase("test_types");

        script.Query = ctx.CreateDatabaseScript();
        script.Execute();

        query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, DateTime_Precision from information_schema.Columns where table_schema ='" + c.Database + "' and table_name = 'Products' and column_name ='DateTimeWithPrecision'", c);
        query.Connection = c;
        MySqlDataReader reader = query.ExecuteReader();
        while (reader.Read())
        {
          Assert.Equal("DateTimeWithPrecision", reader[0].ToString());
          Assert.Equal("NO", reader[1].ToString());
          Assert.Equal("datetime", reader[2].ToString());
          Assert.Equal("3", reader[3].ToString());
        }
        reader.Close();
        ctx.DeleteDatabase();
        c.Close();
      }
    }
#endif
  }
}
