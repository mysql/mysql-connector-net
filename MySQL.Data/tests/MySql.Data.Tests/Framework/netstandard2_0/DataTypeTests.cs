// Copyright (c) 2013, 2020, Oracle and/or its affiliates. All rights reserved.
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

using System;
using NUnit.Framework;
using System.Data;
using System.Data.Common;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class DataTypeTests
  {
    /// <summary>
    /// Bug #10486 MySqlDataAdapter.Update error for decimal column 
    /// </summary>
    [Test]
    public void UpdateDecimalColumns()
    {
      ExecuteSQL("CREATE TABLE Test (id int not null auto_increment primary key, " +
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
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual(1, dt.Rows[0]["id"]);
      Assert.AreEqual((decimal)23.4, Convert.ToDecimal(dt.Rows[0]["dec1"]));
      cb.Dispose();
    }

    /// <summary>
    /// Bug #17375 CommandBuilder ignores Unsigned flag at Parameter creation 
    /// Bug #15274 Use MySqlDbType.UInt32, throwed exception 'Only byte arrays can be serialize' 
    /// </summary>
    [Test]
    public void UnsignedTypes()
    {
      ExecuteSQL("CREATE TABLE Test (b TINYINT UNSIGNED PRIMARY KEY)");

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

      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (b MEDIUMINT UNSIGNED PRIMARY KEY)");
      ExecuteSQL("INSERT INTO Test VALUES(20)");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE (b > ?id)", Connection);
      cmd.Parameters.Add("?id", MySqlDbType.UInt16).Value = 10;
      using (MySqlDataReader dr = cmd.ExecuteReader())
      {
        dr.Read();
        Assert.AreEqual(20, dr.GetUInt16(0));
      }
    }

    /// <summary>
    /// Bug #48171	MySqlDataReader.GetSchemaTable() returns 0 in "NumericPrecision" for newdecimal
    /// </summary>
    [Test]
    public void DecimalPrecision()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("CREATE TABLE test(a decimal(35,2), b decimal(36,2), c decimal(36,2) unsigned)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        DataRow columnDefinition = dt.Rows[0];
        Assert.AreEqual(35, columnDefinition[SchemaTableColumn.NumericPrecision]);
        columnDefinition = dt.Rows[1];
        Assert.AreEqual(36, columnDefinition[SchemaTableColumn.NumericPrecision]);
        columnDefinition = dt.Rows[2];
        Assert.AreEqual(36, columnDefinition[SchemaTableColumn.NumericPrecision]);
      }
    }
  }
}
