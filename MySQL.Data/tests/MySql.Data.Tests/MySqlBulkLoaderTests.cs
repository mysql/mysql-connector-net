// Copyright © 2013, 2019 Oracle and/or its affiliates. All rights reserved.
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
using System.IO;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlBulkLoaderTests : TestBase
  {
    public MySqlBulkLoaderTests(TestFixture fixture) : base(fixture)
    {
      if (fixture.Version >= new Version(8,0,2)) executeSQL("SET GLOBAL local_infile = 1");
    }

    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.AllowLoadLocalInfile = true;
    }

    [Fact]
    public void BulkLoadSimple()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(200, count);

      TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.Equal(200, dt.Rows.Count);
      Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadReadOnlyFile()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Dispose();

      FileInfo fi = new FileInfo(path);
      FileAttributes oldAttr = fi.Attributes;
      fi.Attributes = fi.Attributes | FileAttributes.ReadOnly;
      try
      {
        MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
        loader.TableName = "Test";
        loader.FileName = path;
        loader.Timeout = 0;
        loader.Local = true;
        int count = loader.Load();
        Assert.Equal(200, count);

        TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
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
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.Write(i + ",'Test' xxx");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.Equal(200, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void BulkLoadSimple3()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.Write(i + ",'Test' xxx");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.NumberOfLinesToSkip = 50;
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(150, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.Equal(150, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void BulkLoadSimple4()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 100; i++)
        sw.Write("aaa" + i + ",'Test' xxx");
      for (int i = 100; i < 200; i++)
        sw.Write("bbb" + i + ",'Test' xxx");
      for (int i = 200; i < 300; i++)
        sw.Write("aaa" + i + ",'Test' xxx");
      for (int i = 300; i < 400; i++)
        sw.Write("bbb" + i + ",'Test' xxx");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.LinePrefix = "bbb";
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(200, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.Equal(200, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void BulkLoadFieldQuoting()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t`col1`\tcol2");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldQuotationCharacter = '`';
      loader.FieldQuotationOptional = true;
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(200, count);

      TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.Equal(200, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Fact]
    public void BulkLoadEscaping()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + ",col1\tstill col1,col2");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.EscapeCharacter = '\t';
      loader.FieldTerminator = ",";
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(200, count);

      TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.Equal(200, dt.Rows.Count);
      Assert.Equal("col1still col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionReplace()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(20, count);

      path = Path.GetTempFileName();
      sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Dispose();

      loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Replace;
      loader.Local = true;
      loader.Load();

      TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col2", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionIgnore()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(20, count);

      path = Path.GetTempFileName();
      sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Dispose();

      loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Ignore;
      loader.Local = true;
      loader.Load();

      TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1].ToString().Trim());
    }

    #region AsyncTests

    [Fact]
    public void BulkLoadSimpleAsync()
    {
      executeSQL("CREATE TABLE BulkLoadSimpleAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadSimpleAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.Local = true;

      loader.LoadAsync().ContinueWith(loadResult =>
      {
        int dataLoaded = loadResult.Result;
        TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadSimpleAsyncTest", Connection);

        Assert.Equal(dataLoaded, dt.Rows.Count);
        Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
      }).Wait();
    }

    [Fact]
    public void BulkLoadReadOnlyFileAsync()
    {
      executeSQL("CREATE TABLE BulkLoadReadOnlyFileAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Dispose();

      FileInfo fi = new FileInfo(path);
      FileAttributes oldAttr = fi.Attributes;
      fi.Attributes = fi.Attributes | FileAttributes.ReadOnly;
      try
      {
        MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
        loader.TableName = "BulkLoadReadOnlyFileAsyncTest";
        loader.FileName = path;
        loader.Timeout = 0;
        loader.Local = true;

        loader.LoadAsync().ContinueWith(loadResult =>
        {
          int dataLoaded = loadResult.Result;

          TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadReadOnlyFileAsyncTest", Connection);
          Assert.Equal(dataLoaded, dt.Rows.Count);
          Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
        }).Wait();
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
      executeSQL("CREATE TABLE BulkLoadFieldQuotingAsyncTest (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t`col1`\tcol2");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadFieldQuotingAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldQuotationCharacter = '`';
      loader.FieldQuotationOptional = true;
      loader.Local = true;

      loader.LoadAsync().ContinueWith(loadResult =>
      {
        int dataLoaded = loadResult.Result;
        TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadFieldQuotingAsyncTest", Connection);

        Assert.Equal(dataLoaded, dt.Rows.Count);
        Assert.Equal("col1", dt.Rows[0][1]);
        Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
      }).Wait();
    }

    [Fact]
    public void BulkLoadEscapingAsync()
    {
      executeSQL("CREATE TABLE BulkLoadEscapingAsyncTest (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + ",col1\tstill col1,col2");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadEscapingAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.EscapeCharacter = '\t';
      loader.FieldTerminator = ",";
      loader.Local = true;

      loader.LoadAsync().ContinueWith(loadResult =>
      {
        int dataLoaded = loadResult.Result;
        TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadEscapingAsyncTest", Connection);

        Assert.Equal(dataLoaded, dt.Rows.Count);
        Assert.Equal("col1still col1", dt.Rows[0][1]);
        Assert.Equal("col2", dt.Rows[0][2].ToString().Trim());
      }).Wait();
    }

    [Fact]
    public void BulkLoadConflictOptionReplaceAsync()
    {
      executeSQL("CREATE TABLE BulkLoadConflictOptionReplaceAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadConflictOptionReplaceAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.Local = true;

      loader.LoadAsync().Wait();

      path = Path.GetTempFileName();
      sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col2");
      sw.Flush();
      sw.Dispose();

      loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadConflictOptionReplaceAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Replace;
      loader.Local = true;

      loader.LoadAsync().Wait();
      TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadConflictOptionReplaceAsyncTest", Connection);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col2", dt.Rows[0][1].ToString().Trim());
    }

    [Fact]
    public void BulkLoadConflictOptionIgnoreAsync()
    {
      executeSQL("CREATE TABLE BulkLoadConflictOptionIgnoreAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col1");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadConflictOptionIgnoreAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.Local = true;

      loader.LoadAsync().ContinueWith(loadResult =>
      {
        int dataLoaded = loadResult.Result;
        path = Path.GetTempFileName();
        sw = new StreamWriter(new FileStream(path, FileMode.Create));
        for (int i = 0; i < dataLoaded; i++)
          sw.WriteLine(i + ",col2");
        sw.Flush();
        sw.Dispose();
      }).Wait();

      loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadConflictOptionIgnoreAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.ConflictOption = MySqlBulkLoaderConflictOption.Ignore;
      loader.Local = true;

      loader.LoadAsync().ContinueWith(loadResult =>
      {
        int dataLoaded = loadResult.Result;
        TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadConflictOptionIgnoreAsyncTest", Connection);
        Assert.Equal(20, dt.Rows.Count);
        Assert.Equal("col1", dt.Rows[0][1].ToString().Trim());
      }).Wait();
    }

    [Fact]
    public void BulkLoadColumnOrderAsync()
    {
      executeSQL(@"CREATE TABLE BulkLoadColumnOrderAsyncTest (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), n3 VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col3,col2,col1");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "BulkLoadColumnOrderAsyncTest";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = Environment.NewLine;
      loader.Columns.Add("id");
      loader.Columns.Add("n3");
      loader.Columns.Add("n2");
      loader.Columns.Add("n1");
      loader.Local = true;

      loader.LoadAsync().ContinueWith(loadResult =>
      {
        int dataLoaded = loadResult.Result;
        TestDataTable dt = Utils.FillTable("SELECT * FROM BulkLoadColumnOrderAsyncTest", Connection);
        Assert.Equal(20, dt.Rows.Count);
        Assert.Equal("col1", dt.Rows[0][1]);
        Assert.Equal("col2", dt.Rows[0][2]);
        Assert.Equal("col3", dt.Rows[0][3].ToString().Trim());
      }).Wait();
    }
    #endregion

    private string GetElapsedTime(System.Diagnostics.Stopwatch Timer)
    {
      TimeSpan timeElapsed = Timer.Elapsed;
      return string.Format("{0}:{1}:{2}.{3} (HH:MM:SS.MS)", timeElapsed.Hours, timeElapsed.Minutes, timeElapsed.Seconds, timeElapsed.Milliseconds);
    }

    [Fact]
    public void BulkLoadColumnOrder()
    {
      executeSQL(@"CREATE TABLE Test (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), 
            n3 VARCHAR(250), PRIMARY KEY(id))");

      // first create the external file
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col3,col2,col1");
      sw.Flush();
      sw.Dispose();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.FileName = path;
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = Environment.NewLine;
      loader.Columns.Add("id");
      loader.Columns.Add("n3");
      loader.Columns.Add("n2");
      loader.Columns.Add("n1");
      loader.Local = true;
      int count = loader.Load();
      Assert.Equal(20, count);

      TestDataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.Equal(20, dt.Rows.Count);
      Assert.Equal("col1", dt.Rows[0][1]);
      Assert.Equal("col2", dt.Rows[0][2]);
      Assert.Equal("col3", dt.Rows[0][3].ToString().Trim());
    }
  }
}
