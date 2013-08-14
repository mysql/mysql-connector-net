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
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlBulkLoaderTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    [Fact]
    public void BulkLoadSimple()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(200, dt.Rows.Count);
      Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadReadOnlyFile()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
        MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
        loader.TableName = "Test";
        loader.FileName = path;
        loader.Timeout = 0;
        int count = loader.Load();
        Assert.Equal(200, count);

        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.Equal(200, dt.Rows.Count);
        Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
      }
      finally
      {
        fi.Attributes = oldAttr;
        fi.Delete();
      }
    }

    [Fact]
    public void BulkLoadSimple2()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.Write(i + ",'Test' xxx");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
      Assert.Equal(200, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void BulkLoadSimple3()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.Write(i + ",'Test' xxx");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.NumberOfLinesToSkip = 50;
      int count = loader.Load();
      Assert.Equal(150, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
      Assert.Equal(150, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void BulkLoadSimple4()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.LinePrefix = "bbb";
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
      Assert.Equal(200, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void BulkLoadFieldQuoting()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t`col1`\tcol2");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldQuotationCharacter = '`';
      loader.FieldQuotationOptional = true;
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(200, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Fact]
    public void BulkLoadEscaping()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + ",col1\tstill col1,col2");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.EscapeCharacter = '\t';
      loader.FieldTerminator = ",";
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(200, dt.Rows.Count);
      Assert.Equal("col1still col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionReplace()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      int count = loader.Load();
      Assert.Equal(20, count);

      path = Path.GetTempFileName();
      sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Close();

      loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Replace;
      loader.Load();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col2", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionIgnore()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      int count = loader.Load();
      Assert.Equal(20, count);

      path = Path.GetTempFileName();
      sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Close();

      loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Ignore;
      loader.Load();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1].ToString().Trim());
    }

#if NET_40_OR_GREATER
    #region AsyncTests
    [Fact]
    public void BulkLoadSimpleAsync()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
      System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
      timer.Start();
      loader.LoadAsync();
      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      Console.WriteLine("Trying to get data.");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      while (dt.Rows.Count < 500)
      {
        Console.WriteLine(string.Format("Asynchronous task is still running, processed records at this time:{0}", dt.Rows.Count));
        da.Fill(dt);
      }
      timer.Stop();
      Console.WriteLine(string.Format("Asynchronous task finished in:{0}", GetElapsedTime(timer)));
      Assert.Equal(500, dt.Rows.Count);
      Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadReadOnlyFileAsync()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Close();

      FileInfo fi = new FileInfo(path);
      FileAttributes oldAttr = fi.Attributes;
      fi.Attributes = fi.Attributes | FileAttributes.ReadOnly;
      try
      {
        MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
        loader.TableName = "Test";
        loader.FileName = path;
        loader.Timeout = 0;

        Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        loader.LoadAsync();
        Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
        System.Threading.Thread.Sleep(1000);

        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        while (dt.Rows.Count < 500)
        {
          Console.WriteLine(string.Format("Asynchronous task is still running, processed records at this time:{0}", dt.Rows.Count));
          da.Fill(dt);
        }
        timer.Stop();
        Console.WriteLine(string.Format("Asynchronous task finished in:{0}", GetElapsedTime(timer)));

        Assert.Equal(500, dt.Rows.Count);
        Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
      }
      finally
      {
        fi.Attributes = oldAttr;
        fi.Delete();
      }
    }

    [Fact]
    public void BulkLoadFieldQuotingAsync()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t`col1`\tcol2");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldQuotationCharacter = '`';
      loader.FieldQuotationOptional = true;

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
      System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
      timer.Start();
      loader.LoadAsync();
      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      while (dt.Rows.Count < 500)
      {
        Console.WriteLine(string.Format("Asynchronous task is still running, processed records at this time:{0}", dt.Rows.Count));
        da.Fill(dt);
      }
      timer.Stop();
      Console.WriteLine(string.Format("Asynchronous task finished in:{0}", GetElapsedTime(timer)));

      Assert.Equal(500, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Fact]
    public void BulkLoadEscapingAsync()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + ",col1\tstill col1,col2");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.EscapeCharacter = '\t';
      loader.FieldTerminator = ",";

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
      System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
      timer.Start();
      loader.LoadAsync();
      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      while (dt.Rows.Count < 500)
      {
        Console.WriteLine(string.Format("Asynchronous task is still running, processed records at this time:{0}", dt.Rows.Count));
        da.Fill(dt);
      }
      timer.Stop();
      Console.WriteLine(string.Format("Asynchronous task finished in:{0}", GetElapsedTime(timer)));

      Assert.Equal(500, dt.Rows.Count);
      Assert.Equal("col1still col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionReplaceAsync()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
      loader.LoadAsync();

      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      path = Path.GetTempFileName();
      sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Close();

      loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Replace;

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync) with duplicated keys.");
      loader.LoadAsync();
      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col2", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionIgnoreAsync()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
      loader.LoadAsync();

      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      path = Path.GetTempFileName();
      sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Close();

      loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Ignore;

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)  with duplicated keys.");
      loader.LoadAsync();

      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadColumnOrderAsync()
    {
      st.execSQL(@"CREATE TABLE Test (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), 
            n3 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col3,col2,col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = Environment.NewLine;
      loader.Columns.Add("id");
      loader.Columns.Add("n3");
      loader.Columns.Add("n2");
      loader.Columns.Add("n1");

      Console.WriteLine("Calling Asynchronous version of MySqlBulkLoader.Load (LoadAsync)");
      loader.LoadAsync();

      Console.WriteLine("Wait 1 seconds to give a chance to Asynchronous method to finish.");
      System.Threading.Thread.Sleep(1000);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2]);
      Assert.Equal("col3", dt.Rows[0][3].ToString().Trim());
    }
    #endregion

    private string GetElapsedTime(System.Diagnostics.Stopwatch Timer)
    {
      TimeSpan timeElapsed = Timer.Elapsed;
      return string.Format("{0}:{1}:{2}.{3} (HH:MM:SS.MS)", timeElapsed.Hours, timeElapsed.Minutes, timeElapsed.Seconds, timeElapsed.Milliseconds);
    }
#endif

    [Fact]
    public void BulkLoadColumnOrder()
    {
      st.execSQL(@"CREATE TABLE Test (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), 
            n3 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col3,col2,col1");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(st.conn);
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
      Assert.Equal(20, count);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2]);
      Assert.Equal("col3", dt.Rows[0][3].ToString().Trim());
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
    }
  }
}
