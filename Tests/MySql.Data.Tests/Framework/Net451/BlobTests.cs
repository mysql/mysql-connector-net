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
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;
using Xunit;
using System.Data;
using System.ComponentModel;


namespace MySql.Data.MySqlClient.Tests
{
  public class BlobTests : SpecialFixtureWithCustomConnectionString
  {
    [Fact]    
    public void InsertBinary()
    {
      int lenIn = 400000;
      byte[] dataIn = Utils.CreateBlob(lenIn);

      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, blob1 LONGBLOB, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?b1)", st.conn);
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      cmd.Parameters.Add(new MySqlParameter("?b1", dataIn));
      int rows = cmd.ExecuteNonQuery();

      byte[] dataIn2 = Utils.CreateBlob(lenIn);
      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].Value = dataIn2;
      rows += cmd.ExecuteNonQuery();

      Assert.True(rows == 2, "Checking insert rowcount");

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.HasRows == true, "Checking HasRows");

        reader.Read();

        byte[] dataOut = new byte[lenIn];
        long lenOut = reader.GetBytes(1, 0, dataOut, 0, lenIn);

        Assert.True(lenIn == lenOut, "Checking length of binary data (row 1)");

        // now see if the buffer is intact
        for (int x = 0; x < dataIn.Length; x++)
          Assert.True(dataIn[x] == dataOut[x], "Checking first binary array at " + x);

        // now we test chunking
        int pos = 0;
        int lenToRead = dataIn.Length;
        while (lenToRead > 0)
        {
          int size = Math.Min(lenToRead, 1024);
          int read = (int)reader.GetBytes(1, pos, dataOut, pos, size);
          lenToRead -= read;
          pos += read;
        }
        // now see if the buffer is intact
        for (int x = 0; x < dataIn.Length; x++)
          Assert.True(dataIn[x] == dataOut[x], "Checking first binary array at " + x);

        reader.Read();
        lenOut = reader.GetBytes(1, 0, dataOut, 0, lenIn);
        Assert.True(lenIn == lenOut, "Checking length of binary data (row 2)");

        // now see if the buffer is intact
        for (int x = 0; x < dataIn2.Length; x++)
          Assert.True(dataIn2[x] == dataOut[x], "Checking second binary array at " + x);
      }
    }

    [Fact]
    public void GetChars()
    {
      InternalGetChars(false);
    }

    [Fact]
    public void GetCharsPrepared()
    {
      if (st.Version < new Version(4, 1)) return;

      InternalGetChars(true);
    }

    private void InternalGetChars(bool prepare)
    {    
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, text1 LONGTEXT, PRIMARY KEY(id))");

      char[] data = new char[20000];
      for (int x = 0; x < data.Length; x++)
        data[x] = (char)(65 + (x % 20));

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, ?text1)", st.conn);
      cmd.Parameters.AddWithValue("?text1", data);
      if (prepare)
        cmd.Prepare();
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Parameters.Clear();
      if (prepare)
        cmd.Prepare();

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();

        // now we test chunking
        char[] dataOut = new char[data.Length];
        int pos = 0;
        int lenToRead = data.Length;
        while (lenToRead > 0)
        {
          int size = Math.Min(lenToRead, 1024);
          int read = (int)reader.GetChars(1, pos, dataOut, pos, size);
          lenToRead -= read;
          pos += read;
        }
        // now see if the buffer is intact
        for (int x = 0; x < data.Length; x++)
          Assert.True(data[x] == dataOut[x], "Checking first text array at " + x);
      }
    }

    [Fact]
    public void InsertText()
    {
      InternalInsertText(false);
    }

    [Fact]
    public void InsertTextPrepared()
    {
      if (st.Version < new Version(4, 1)) return;

      InternalInsertText(true);
    }

    private void InternalInsertText(bool prepare)
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, blob1 LONGBLOB, text1 LONGTEXT, PRIMARY KEY(id))");

      byte[] data = new byte[1024];
      for (int x = 0; x < 1024; x++)
        data[x] = (byte)(65 + (x % 20));

      // Create sample table
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, ?b1, ?t1)", st.conn);
      cmd.Parameters.Add(new MySqlParameter("?t1", data));
      cmd.Parameters.Add(new MySqlParameter("?b1", "This is my blob data"));
      if (prepare) cmd.Prepare();
      int rows = cmd.ExecuteNonQuery();
      Assert.True(rows == 1, "Checking insert rowcount");

      cmd.CommandText = "INSERT INTO Test VALUES(2, ?b1, ?t1)";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?t1", DBNull.Value);
      string str = "This is my text value";

#if RT
      cmd.Parameters.Add(new MySqlParameter("?b1", MySqlDbType.LongBlob, str.Length)
      {
          Direction = ParameterDirection.Input,
          IsNullable = true,
          Precision = 0,
          Scale = 0,
          Value = str
      });
#else
      cmd.Parameters.Add(new MySqlParameter("?b1", MySqlDbType.LongBlob, str.Length,
      ParameterDirection.Input, true, 0, 0, "b1", DataRowVersion.Current, str));
#endif

      rows = cmd.ExecuteNonQuery();
      Assert.True(rows == 1, "Checking insert rowcount");

      cmd.CommandText = "SELECT * FROM Test";
      if (prepare) cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.HasRows, "Checking HasRows");

        Assert.True(reader.Read());

        Assert.Equal("This is my blob data", reader.GetString(1));
        string s = reader.GetString(2);
        Assert.True(s.Length == 1024, "Checking length returned ");
        Assert.True(s.Substring(0, 9) == "ABCDEFGHI", "Checking first few chars of string");

        Assert.True(reader.Read());
        Assert.Equal(DBNull.Value, reader.GetValue(2));
        Assert.Equal("This is my text value", reader.GetString(1));
      }
    }

#if !RT
    [Fact]
    public void UpdateDataSet()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, blob1 LONGBLOB, text1 LONGTEXT, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test VALUES( 1, NULL, 'Text field' )");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      string s = (string)dt.Rows[0][2];
      Assert.Equal("Text field", s);

      byte[] inBuf = Utils.CreateBlob(512);
      dt.Rows[0].BeginEdit();
      dt.Rows[0]["blob1"] = inBuf;
      dt.Rows[0].EndEdit();
      DataTable changes = dt.GetChanges();
      da.Update(changes);
      dt.AcceptChanges();

      dt.Clear();
      da.Fill(dt);
      cb.Dispose();

      byte[] outBuf = (byte[])dt.Rows[0]["blob1"];
      Assert.True(inBuf.Length == outBuf.Length, "checking length of updated buffer");
     
      for (int y = 0; y < inBuf.Length; y++)
        Assert.True(inBuf[y] == outBuf[y], "checking array data");
    }
#endif

    [Fact]
    public void GetCharsOnLongTextColumn()
    {
      //if (st.conn.connectionState != ConnectionState.Open)
      //  st.conn.Open();
      
      st.execSQL("CREATE TABLE Test1 (id INT NOT NULL, blob1 LONGBLOB, text1 LONGTEXT, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test1 (id, text1) VALUES(1, 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT id, text1 FROM Test1", st.conn);
      char[] buf = new char[2];

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.GetChars(1, 0, buf, 0, 2);
        Assert.Equal(buf[0], 'T');
        Assert.Equal(buf[1], 'e');
      }
    }

    [Fact]
    public void MediumIntBlobSize()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");      
      
      st.execSQL("CREATE TABLE test (id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, " +
         "image MEDIUMBLOB NOT NULL, imageSize MEDIUMINT(8) UNSIGNED NOT NULL DEFAULT 0, " +
         "PRIMARY KEY (id))");

      byte[] image = new byte[2048];
      for (int x = 0; x < image.Length; x++)
        image[x] = (byte)(x % 47);

      MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES(NULL, ?image, ?size)", st.conn);
      cmd.Parameters.AddWithValue("?image", image);
      cmd.Parameters.AddWithValue("?size", image.Length);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT imageSize, length(image), image FROM test WHERE id=?id";
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Prepare();

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        uint actualsize = reader.GetUInt32(1);
        Assert.Equal((uint)image.Length, actualsize);

        uint size = reader.GetUInt32(0);
        byte[] outImage = new byte[size];
        long len = reader.GetBytes(reader.GetOrdinal("image"), 0, outImage, 0, (int)size);
        Assert.Equal((uint)image.Length, size);
        Assert.Equal((uint)image.Length, len);
      }
    }

    [Fact]
    public void BlobBiggerThanMaxPacket()
    {      
      st.suExecSQL("SET GLOBAL max_allowed_packet=" + 500 * 1024);

      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE test (id INT(10), image BLOB)");

      using (MySqlConnection c = new MySqlConnection(st.GetConnectionString(true)))
      {
        c.Open();
        byte[] image = Utils.CreateBlob(1000000);

        MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES(NULL, ?image)", c);
        cmd.Parameters.AddWithValue("?image", image);
          
        Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
        Assert.Equal(ex.Message, "Packets larger than max_allowed_packet are not allowed.");
      }
    }
  }
}
