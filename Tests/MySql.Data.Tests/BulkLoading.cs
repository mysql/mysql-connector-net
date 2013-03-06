// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class BulkLoading : BaseTest
  {
    [Test]
    public void BulkLoadSimple()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      int count = loader.Load();
      Assert.AreEqual(200, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(200, dt.Rows.Count);
      Assert.AreEqual("'Test'", dt.Rows[0][1].ToString().Trim());
    }

    [Test]
    public void BulkLoadReadOnlyFile()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Close();

      FileInfo fi = new FileInfo(path);
      FileAttributes oldAttr = fi.Attributes;
      fi.Attributes = fi.Attributes | FileAttributes.ReadOnly;
      try
      {
        MySqlBulkLoader loader = new MySqlBulkLoader(conn);
        loader.TableName = "Test";
        loader.FileName = path;
        loader.Timeout = 0;
        int count = loader.Load();
        Assert.AreEqual(200, count);

        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.AreEqual(200, dt.Rows.Count);
        Assert.AreEqual("'Test'", dt.Rows[0][1].ToString().Trim());
      }
      finally
      {
        fi.Attributes = oldAttr;
        fi.Delete();
      }
    }

    [Test]
    public void BulkLoadSimple2()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.Write(i + ",'Test' xxx");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      int count = loader.Load();
      Assert.AreEqual(200, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", conn);
      Assert.AreEqual(200, cmd.ExecuteScalar());
    }

    [Test]
    public void BulkLoadSimple3()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.Write(i + ",'Test' xxx");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.NumberOfLinesToSkip = 50;
      int count = loader.Load();
      Assert.AreEqual(150, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", conn);
      Assert.AreEqual(150, cmd.ExecuteScalar());
    }

    [Test]
    public void BulkLoadSimple4()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 100; i++)
        sw.Write("aaa" + i + ",'Test' xxx");
      for (int i = 100; i < 200; i++)
        sw.Write("bbb" + i + ",'Test' xxx");
      for (int i = 200; i < 300; i++)
        sw.Write("aaa" + i + ",'Test' xxx");
      for (int i = 300; i < 400; i++)
        sw.Write("bbb" + i + ",'Test' xxx");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.LinePrefix = "bbb";
      int count = loader.Load();
      Assert.AreEqual(200, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", conn);
      Assert.AreEqual(200, cmd.ExecuteScalar());
    }

    [Test]
    public void BulkLoadFieldQuoting()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t`col1`\tcol2");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldQuotationCharacter = '`';
      loader.FieldQuotationOptional = true;
      int count = loader.Load();
      Assert.AreEqual(200, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(200, dt.Rows.Count);
      Assert.AreEqual("col1", dt.Rows[0][1]);
      Assert.AreEqual("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Test]
    public void BulkLoadEscaping()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + ",col1\tstill col1,col2");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.EscapeCharacter = '\t';
      loader.FieldTerminator = ",";
      int count = loader.Load();
      Assert.AreEqual(200, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(200, dt.Rows.Count);
      Assert.AreEqual("col1still col1", dt.Rows[0][1]);
      Assert.AreEqual("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Test]
    public void BulkLoadConflictOptionReplace()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      int count = loader.Load();
      Assert.AreEqual(20, count);

      path = Path.GetTempFileName();
      sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Close();

      loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Replace;
      loader.Load();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(20, dt.Rows.Count);
      Assert.AreEqual("col2", dt.Rows[0][1].ToString().Trim());
    }

    [Test]
    public void BulkLoadConflictOptionIgnore()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      int count = loader.Load();
      Assert.AreEqual(20, count);

      path = Path.GetTempFileName();
      sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Close();

      loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Ignore;
      loader.Load();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(20, dt.Rows.Count);
      Assert.AreEqual("col1", dt.Rows[0][1].ToString().Trim());
    }

    [Test]
    public void BulkLoadColumnOrder()
    {
      execSQL(@"CREATE TABLE Test (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), 
            n3 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col3,col2,col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = Environment.NewLine;
      loader.Columns.Add("id");
      loader.Columns.Add("n3");
      loader.Columns.Add("n2");
      loader.Columns.Add("n1");
      int count = loader.Load();
      Assert.AreEqual(20, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(20, dt.Rows.Count);
      Assert.AreEqual("col1", dt.Rows[0][1]);
      Assert.AreEqual("col2", dt.Rows[0][2]);
      Assert.AreEqual("col3", dt.Rows[0][3].ToString().Trim());
    }

  }
}
