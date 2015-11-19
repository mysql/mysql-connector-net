// Copyright © 2013, 2014 Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Diagnostics;
using MySql.Data.MySqlClient.Tests;

namespace MySql.Data.Tests.Stress
{
  public class StressTests : SpecialFixtureWithCustomConnectionString
  {
    public override void SetFixture(SetUpClassPerTestInit data)
    {
      base.SetFixture(data);
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name varchar(100), blob1 LONGBLOB, text1 TEXT, " +
        "PRIMARY KEY(id))");
    }

    protected override void Dispose(bool disposing)
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      base.Dispose(disposing);
    }

    [Fact]
    public void TestMultiPacket()
    {
      int len = 20000000;

      st.suExecSQL("SET GLOBAL max_allowed_packet=64000000");

      // currently do not test this with compression
      if (st.conn.UseCompression) return;

      using (MySqlConnection c = new MySqlConnection(st.GetConnectionString(true)))
      {
        c.Open();
        byte[] dataIn = Utils.CreateBlob(len);
        byte[] dataIn2 = Utils.CreateBlob(len);

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, NULL, ?blob, NULL )", c);
        cmd.CommandTimeout = 0;
        cmd.Parameters.Add(new MySqlParameter("?id", 1));
        cmd.Parameters.Add(new MySqlParameter("?blob", dataIn));
        cmd.ExecuteNonQuery();

        cmd.Parameters[0].Value = 2;
        cmd.Parameters[1].Value = dataIn2;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          byte[] dataOut = new byte[len];
          long count = reader.GetBytes(2, 0, dataOut, 0, len);
          Assert.Equal(len, count);
          int i = 0;
          try
          {
            for (; i < len; i++)
              Assert.Equal(dataIn[i], dataOut[i]);
          }
          catch (Exception)
          {
            int z = i;
          }

          reader.Read();
          count = reader.GetBytes(2, 0, dataOut, 0, len);
          Assert.Equal(len, count);

          for (int x = 0; x < len; x++)
            Assert.Equal(dataIn2[x], dataOut[x]);
        }
      }
    }

    [Fact]
    public void TestSequence()
    {
      MySqlCommand cmd = new MySqlCommand("insert into Test (id, name) values (?id, 'test')", st.conn);
      cmd.Parameters.Add(new MySqlParameter("?id", 1));

      for (int i = 1; i <= 8000; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      int i2 = 0;
      cmd = new MySqlCommand("select * from Test", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          Assert.True(i2 + 1 == reader.GetInt32(0), "Sequence out of order");
          i2++;
        }
        reader.Close();

        Assert.Equal(8000, i2);
        cmd = new MySqlCommand("delete from Test where id >= 100", st.conn);
        cmd.ExecuteNonQuery();
      }
    }
  }
}
