// Copyright © 2013, 2015 Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using MySql.Data.Types;
using System.Data.Common;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class DataTypeTests 
  {
    /// <summary>
    /// Bug #10486 MySqlDataAdapter.Update error for decimal column 
    /// </summary>
    [Fact]
    public void UpdateDecimalColumns()
    {
      executeSQL("CREATE TABLE Test (id int not null auto_increment primary key, " +
        "dec1 decimal(10,1))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);
      DataRow row = dt.NewRow();
      row["id"] = DBNull.Value;
      row["dec1"] = 23.4;
      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(1, dt.Rows[0]["id"]);
      Assert.Equal((decimal)23.4, Convert.ToDecimal(dt.Rows[0]["dec1"]));
      cb.Dispose();
    }

    /// <summary>
    /// Bug #17375 CommandBuilder ignores Unsigned flag at Parameter creation 
    /// Bug #15274 Use MySqlDbType.UInt32, throwed exception 'Only byte arrays can be serialize' 
    /// </summary>
    [Fact]
    public void UnsignedTypes()
    {
      executeSQL("CREATE TABLE Test (b TINYINT UNSIGNED PRIMARY KEY)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

      DataTable dt = new DataTable();
      da.Fill(dt);

      DataView dv = new DataView(dt);
      DataRowView row;

      row = dv.AddNew();
      row["b"] = 120;
      row.EndEdit();
      da.Update(dv.Table);

      row = dv.AddNew();
      row["b"] = 135;
      row.EndEdit();
      da.Update(dv.Table);
      cb.Dispose();

      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (b MEDIUMINT UNSIGNED PRIMARY KEY)");
      executeSQL("INSERT INTO Test VALUES(20)");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE (b > ?id)", Connection);
      cmd.Parameters.Add("?id", MySqlDbType.UInt16).Value = 10;
      using (MySqlDataReader dr = cmd.ExecuteReader())
      {
        dr.Read();
        Assert.Equal(20, dr.GetUInt16(0));
      }
    }

    /// <summary>
    /// Bug #48171	MySqlDataReader.GetSchemaTable() returns 0 in "NumericPrecision" for newdecimal
    /// </summary>
    [Fact]
    public void DecimalPrecision()
    {
      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test(a decimal(35,2), b decimal(36,2), c decimal(36,2) unsigned)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        DataRow columnDefinition = dt.Rows[0];
        Assert.Equal(35, columnDefinition[SchemaTableColumn.NumericPrecision]);
        columnDefinition = dt.Rows[1];
        Assert.Equal(36, columnDefinition[SchemaTableColumn.NumericPrecision]);
        columnDefinition = dt.Rows[2];
        Assert.Equal(36, columnDefinition[SchemaTableColumn.NumericPrecision]);
      }
    }
  }
}
