// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class BlobTests : TestBase
  {
    public BlobTests(TestFixture fixture) : base(fixture) { }

    [Fact]
    public void InsertNullBinary()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, blob1 LONGBLOB, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?b1)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      cmd.Parameters.Add(new MySqlParameter("?b1", null));
      int rows = cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.HasRows == true, "Checking HasRows");

        reader.Read();
        var value = reader.GetValue(1) as string;
        Assert.True(value == null);
      }
    }

    [Fact]
    public void InsertBinary()
    {
      int lenIn = 400000;
      byte[] dataIn = Utils.CreateBlob(lenIn);

      executeSQL("DROP TABLE IF EXISTS InsertBinary");
      executeSQL("CREATE TABLE InsertBinary (id INT NOT NULL, blob1 LONGBLOB, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO InsertBinary VALUES (?id, ?b1)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      cmd.Parameters.Add(new MySqlParameter("?b1", dataIn));
      int rows = cmd.ExecuteNonQuery();

      byte[] dataIn2 = Utils.CreateBlob(lenIn);
      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].Value = dataIn2;
      rows += cmd.ExecuteNonQuery();

      Assert.True(rows == 2, "Checking insert rowcount");

      cmd.CommandText = "SELECT * FROM InsertBinary";
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
      InternalGetChars(true);
    }

    private void InternalGetChars(bool prepare)
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, text1 LONGTEXT, PRIMARY KEY(id))");

      char[] data = new char[20000];
      for (int x = 0; x < data.Length; x++)
        data[x] = (char)(65 + (x % 20));

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, ?text1)", Connection);
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
      InternalInsertText(true);
    }

    private void InternalInsertText(bool prepare)
    {
      executeSQL("DROP TABLE IF EXISTS InsertText");
      executeSQL("CREATE TABLE InsertText (id INT NOT NULL, blob1 LONGBLOB, text1 LONGTEXT, PRIMARY KEY(id))");

      byte[] data = new byte[1024];
      for (int x = 0; x < 1024; x++)
        data[x] = (byte)(65 + (x % 20));

      // Create sample table
      MySqlCommand cmd = new MySqlCommand("INSERT INTO InsertText VALUES (1, ?b1, ?t1)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?t1", data));
      cmd.Parameters.Add(new MySqlParameter("?b1", "This is my blob data"));
      if (prepare) cmd.Prepare();
      int rows = cmd.ExecuteNonQuery();
      Assert.True(rows == 1, "Checking insert rowcount");

      cmd.CommandText = "INSERT INTO InsertText VALUES(2, ?b1, ?t1)";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?t1", DBNull.Value);
      string str = "This is my text value";

      cmd.Parameters.AddWithValue("?b1", str);

      rows = cmd.ExecuteNonQuery();
      Assert.True(rows == 1, "Checking insert rowcount");

      cmd.CommandText = "SELECT * FROM InsertText";
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

    [Fact]
    public void GetCharsOnLongTextColumn()
    {
      executeSQL("CREATE TABLE Test1 (id INT NOT NULL, blob1 LONGBLOB, text1 LONGTEXT, PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test1 (id, text1) VALUES(1, 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT id, text1 FROM Test1", Connection);
      char[] buf = new char[2];

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.GetChars(1, 0, buf, 0, 2);
        Assert.Equal('T', buf[0]);
        Assert.Equal('e', buf[1]);
      }
    }

    [Fact]
    public void MediumIntBlobSize()
    {
      executeSQL("DROP TABLE IF EXISTS Test");

      executeSQL("CREATE TABLE test (id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, " +
         "image MEDIUMBLOB NOT NULL, imageSize MEDIUMINT(8) UNSIGNED NOT NULL DEFAULT 0, " +
         "PRIMARY KEY (id))");

      byte[] image = new byte[2048];
      for (int x = 0; x < image.Length; x++)
        image[x] = (byte)(x % 47);

      MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES(NULL, ?image, ?size)", Connection);
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

    //TODO:  Fix this
    //[Fact]
    //public void BlobBiggerThanMaxPacket()
    //{      
    //  executeAsRoot("SET GLOBAL max_allowed_packet=" + 500 * 1024);

    //  executeSQL("DROP TABLE IF EXISTS Test");
    //  executeSQL("CREATE TABLE test (id INT(10), image BLOB)");

    //  using (var c = GetConnection())
    //  {
    //    c.Open();
    //    byte[] image = Utils.CreateBlob(1000000);

    //    MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES(NULL, ?image)", c);
    //    cmd.Parameters.AddWithValue("?image", image);

    //    Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
    //    Assert.Equal(ex.Message, "Packets larger than max_allowed_packet are not allowed.");
    //  }
    //}

    [Fact]
    public void UpdateDataSet()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, blob1 LONGBLOB, text1 LONGTEXT, PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test VALUES( 1, NULL, 'Text field' )");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

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
  }
}
