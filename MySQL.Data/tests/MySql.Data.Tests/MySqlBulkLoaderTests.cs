// Copyright © 2013, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlBulkLoaderTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database), true);
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
      if (Version >= new Version(8, 0, 2)) ExecuteSQL("SET GLOBAL local_infile = 1", true);
    }

    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.AllowLoadLocalInfile = true;
    }

    [Test]
    public void BulkLoadSimple()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(200));

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(200));
      Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("'Test'"));
    }

    [Test]
    public void BulkLoadReadOnlyFile()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
        Assert.That(count, Is.EqualTo(200));

        DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
        Assert.That(dt.Rows.Count, Is.EqualTo(200));
        Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("'Test'"));
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
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(200));

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.That(Convert.ToInt32(cmd.ExecuteScalar()), Is.EqualTo(200));
    }

    [Test]
    public void BulkLoadSimple3()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(150));

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.That(Convert.ToInt32(cmd.ExecuteScalar()), Is.EqualTo(150));
    }

    [Test]
    public void BulkLoadSimple4()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(200));

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.That(Convert.ToInt32(cmd.ExecuteScalar()), Is.EqualTo(200));
    }

    [Test]
    public void BulkLoadFieldQuoting()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(200));

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(200));
      Assert.That(dt.Rows[0][1], Is.EqualTo("col1"));
      Assert.That(dt.Rows[0][2].ToString().Trim(), Is.EqualTo("col2"));
    }

    [Test]
    public void BulkLoadEscaping()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(200));

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(200));
      Assert.That(dt.Rows[0][1], Is.EqualTo("col1still col1"));
      Assert.That(dt.Rows[0][2].ToString().Trim(), Is.EqualTo("col2"));
    }

    [Test]
    public void BulkLoadConflictOptionReplace()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(20));

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

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(20));
      Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("col2"));
    }

    [Test]
    public void BulkLoadConflictOptionIgnore()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      Assert.That(count, Is.EqualTo(20));

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

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(20));
      Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("col1"));
    }

    #region AsyncTests

    [Test]
    public void BulkLoadSimpleAsync()
    {
      ExecuteSQL("CREATE TABLE BulkLoadSimpleAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
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
        DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadSimpleAsyncTest", Connection);

        Assert.That(dt.Rows.Count, Is.EqualTo(dataLoaded));
        Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("'Test'"));
      }).Wait();
    }

    [Test]
    public void BulkLoadReadOnlyFileAsync()
    {
      ExecuteSQL("CREATE TABLE BulkLoadReadOnlyFileAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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

          DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadReadOnlyFileAsyncTest", Connection);
          Assert.That(dt.Rows.Count, Is.EqualTo(dataLoaded));
          Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("'Test'"));
        }).Wait();
      }
      finally
      {
        fi.Attributes = oldAttr;
        fi.Delete();
      }
    }

    [Test]
    public void BulkLoadFieldQuotingAsync()
    {
      ExecuteSQL("CREATE TABLE BulkLoadFieldQuotingAsyncTest (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

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
        DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadFieldQuotingAsyncTest", Connection);

        Assert.That(dt.Rows.Count, Is.EqualTo(dataLoaded));
        Assert.That(dt.Rows[0][1], Is.EqualTo("col1"));
        Assert.That(dt.Rows[0][2].ToString().Trim(), Is.EqualTo("col2"));
      }).Wait();
    }

    [Test]
    public void BulkLoadEscapingAsync()
    {
      ExecuteSQL("CREATE TABLE BulkLoadEscapingAsyncTest (id INT NOT NULL, name VARCHAR(250), name2 VARCHAR(250), PRIMARY KEY(id))");

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
        DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadEscapingAsyncTest", Connection);

        Assert.That(dt.Rows.Count, Is.EqualTo(dataLoaded));
        Assert.That(dt.Rows[0][1], Is.EqualTo("col1still col1"));
        Assert.That(dt.Rows[0][2].ToString().Trim(), Is.EqualTo("col2"));
      }).Wait();
    }

    [Test]
    public void BulkLoadConflictOptionReplaceAsync()
    {
      ExecuteSQL("CREATE TABLE BulkLoadConflictOptionReplaceAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
      DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadConflictOptionReplaceAsyncTest", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(20));
      Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("col2"));
    }

    [Test]
    public void BulkLoadConflictOptionIgnoreAsync()
    {
      ExecuteSQL("CREATE TABLE BulkLoadConflictOptionIgnoreAsyncTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

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
        DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadConflictOptionIgnoreAsyncTest", Connection);
        Assert.That(dt.Rows.Count, Is.EqualTo(20));
        Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("col1"));
      }).Wait();
    }

    [Test]
    public void BulkLoadColumnOrderAsync()
    {
      ExecuteSQL(@"CREATE TABLE BulkLoadColumnOrderAsyncTest (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), n3 VARCHAR(250), PRIMARY KEY(id))");

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
        DataTable dt = Utils.FillTable("SELECT * FROM BulkLoadColumnOrderAsyncTest", Connection);
        Assert.That(dt.Rows.Count, Is.EqualTo(20));
        Assert.That(dt.Rows[0][1], Is.EqualTo("col1"));
        Assert.That(dt.Rows[0][2], Is.EqualTo("col2"));
        Assert.That(dt.Rows[0][3].ToString().Trim(), Is.EqualTo("col3"));
      }).Wait();
    }
    #endregion

    private string GetElapsedTime(System.Diagnostics.Stopwatch Timer)
    {
      TimeSpan timeElapsed = Timer.Elapsed;
      return string.Format("{0}:{1}:{2}.{3} (HH:MM:SS.MS)", timeElapsed.Hours, timeElapsed.Minutes, timeElapsed.Seconds, timeElapsed.Milliseconds);
    }

    [Test]
    public void BulkLoadColumnOrder()
    {
      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), 
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
      Assert.That(count, Is.EqualTo(20));

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(20));
      Assert.That(dt.Rows[0][1], Is.EqualTo("col1"));
      Assert.That(dt.Rows[0][2], Is.EqualTo("col2"));
      Assert.That(dt.Rows[0][3].ToString().Trim(), Is.EqualTo("col3"));
    }

    /// <summary>
    /// WL14093 - Add option to specify LOAD DATA LOCAL white list folder
    /// </summary>
    [TestCase(true, "", true)]
    [TestCase(true, " ", true)]
    [TestCase(true, null, true)]
    [TestCase(true, "tmp/data/", true)]
    [TestCase(false, "", false)]
    [TestCase(false, " ", false)]
    [TestCase(false, null, false)]
    [TestCase(false, "otherPath/", false)]
    [TestCase(false, "tmp/", true)]
    [TestCase(false, "tmp/data/", true)]
    [TestCase(false, "c:/SymLink/", false)] // symbolic link
    public void BulkLoadUsingSafePath(bool allowLoadLocalInfile, string allowLoadLocalInfileInPath, bool shouldPass)
    {
      DirectoryInfo info;
      bool isSymLink = false;

      if (!string.IsNullOrWhiteSpace(allowLoadLocalInfileInPath))
      {
        info = new DirectoryInfo(allowLoadLocalInfileInPath);
        isSymLink = info.Attributes.HasFlag(FileAttributes.ReparsePoint);
      }

      Connection.Settings.AllowLoadLocalInfile = allowLoadLocalInfile;
      Connection.Settings.AllowLoadLocalInfileInPath = allowLoadLocalInfileInPath;

      ExecuteSQL(string.Format("CREATE TABLE `{0}`.Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))", Connection.Database), true);

      // create the external file to be uploaded
      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Dispose();

      // create another path to test against unsafe directory
      Directory.CreateDirectory("otherPath");

      // copy the external file to our safe path
      Directory.CreateDirectory("tmp/data");
      if (!File.Exists("tmp/data/file.tmp"))
        File.Copy(path, "tmp/data/file.tmp");

      string filePath = allowLoadLocalInfile ? path : Path.GetFullPath("tmp/data/file.tmp");

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection)
      {
        TableName = "Test",
        FileName = filePath,
        Timeout = 0,
        Local = true
      };

      if (shouldPass)
      {
        int count = loader.Load();
        Assert.That(count, Is.EqualTo(200));

        DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
        Assert.That(dt.Rows.Count, Is.EqualTo(200));
        Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("'Test'"));
      }
      else if (isSymLink && !Directory.Exists(allowLoadLocalInfileInPath))
        Assert.Ignore("For the symbolic link test to run, it should be manually created before executing it.");
      else
      {
        var ex = Assert.Throws<MySqlException>(() => loader.Load());
        if (allowLoadLocalInfileInPath == " " || allowLoadLocalInfileInPath is null)
          if (Version > new Version(8, 0))
            Assert.That(ex.Message, Is.EqualTo("Loading local data is disabled; this must be enabled on both the client and server sides"));
          else
            Assert.That(ex.Message, Is.EqualTo("The used command is not allowed with this MySQL version"));
        else
          Assert.That(ex.Message, Does.Contain("allowloadlocalinfileinpath"));
      }

      File.Delete(path);
      Directory.Delete("tmp", true);
      Directory.Delete("otherPath");
    }

    /// <summary>
    /// Bug#39261524 [Fix safe-path validation for AllowLoadLocalInfileInPath.]
    /// Verifies that sibling and relative paths resolving outside the safe directory are rejected.
    /// </summary>
    /// <param name="useRelativePath">Indicates whether to use a relative path that resolves outside the safe directory.</param>
    [TestCase(false)]
    [TestCase(true)]
    public void BulkLoadUsingSafePathRejectsPathOutsideSafeDirectory(bool useRelativePath)
    {
      string testDirectory = Path.Combine(
        Path.GetTempPath(),
        string.Format("{0}_{1}", nameof(BulkLoadUsingSafePathRejectsPathOutsideSafeDirectory), Guid.NewGuid()));
      string safeDirectory = Path.Combine(testDirectory, "data");
      string outsideDirectory = Path.Combine(testDirectory, "data_backup");

      try
      {
        Directory.CreateDirectory(safeDirectory);
        Directory.CreateDirectory(outsideDirectory);
        File.WriteAllText(Path.Combine(outsideDirectory, "file.tmp"), "1\tTest");

        using MySqlConnection connection = GetConnection();
        connection.Settings.AllowLoadLocalInfile = false;
        connection.Settings.AllowLoadLocalInfileInPath = safeDirectory;
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = string.Format("CREATE TABLE `{0}`.Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))", connection.Database);
        command.ExecuteNonQuery();
        string filePath = useRelativePath
          ? Path.Combine(safeDirectory, "..", "data_backup", "file.tmp")
          : Path.Combine(outsideDirectory, "file.tmp");

        MySqlBulkLoader loader = new MySqlBulkLoader(connection)
        {
          TableName = "Test",
          FileName = filePath,
          Timeout = 0,
          Local = true
        };

        var ex = Assert.Throws<MySqlException>(() => loader.Load());
        Assert.That(ex.Message, Does.Contain("allowloadlocalinfileinpath"));
      }
      finally
      {
        if (Directory.Exists(testDirectory))
          Directory.Delete(testDirectory, true);
      }
    }

    /// <summary>
    /// Bug#39261524 [Fix safe-path validation for AllowLoadLocalInfileInPath.]
    /// Verifies that links resolving outside the safe directory are rejected and legacy .NET Framework targets reject all reparse points.
    /// </summary>
    /// <param name="linkOutsideSafeDirectory">Indicates whether the symbolic link resolves outside the safe directory.</param>
    [TestCase(false)]
    [TestCase(true)]
    public void BulkLoadUsingSafePathHandlesSymbolicLinks(bool linkOutsideSafeDirectory)
    {
      string testDirectory = Path.Combine(
        Path.GetTempPath(),
        string.Format("{0}_{1}", nameof(BulkLoadUsingSafePathHandlesSymbolicLinks), Guid.NewGuid()));
      string safeDirectory = Path.Combine(testDirectory, "safe");
      string insideDirectory = Path.Combine(safeDirectory, "inside");
      string outsideDirectory = Path.Combine(testDirectory, "outside");
      string linkPath = Path.Combine(safeDirectory, linkOutsideSafeDirectory ? "outside-link" : "inside-link");
      string targetDirectory = linkOutsideSafeDirectory ? outsideDirectory : insideDirectory;

      try
      {
        Directory.CreateDirectory(insideDirectory);
        Directory.CreateDirectory(outsideDirectory);
        File.WriteAllText(Path.Combine(insideDirectory, "file.tmp"), "1\tTest");
        File.WriteAllText(Path.Combine(outsideDirectory, "file.tmp"), "1\tTest");

        CreateDirectoryLink(linkPath, targetDirectory);

        using MySqlConnection connection = GetConnection();
        connection.Settings.AllowLoadLocalInfile = false;
        connection.Settings.AllowLoadLocalInfileInPath = safeDirectory;
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = string.Format("CREATE TABLE `{0}`.Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))", connection.Database);
        command.ExecuteNonQuery();

        MySqlBulkLoader loader = new MySqlBulkLoader(connection)
        {
          TableName = "Test",
          FileName = Path.Combine(linkPath, "file.tmp"),
          Timeout = 0,
          Local = true
        };

        bool shouldPass = !linkOutsideSafeDirectory;
#if NETFRAMEWORK
        shouldPass = false;
#endif
        if (!shouldPass)
        {
          var ex = Assert.Throws<MySqlException>(() => loader.Load());
          Assert.That(ex.Message, Does.Contain("allowloadlocalinfileinpath"));
        }
        else
        {
          int count = loader.Load();
          Assert.That(count, Is.EqualTo(1));
        }
      }
      finally
      {
        if (Directory.Exists(linkPath))
          Directory.Delete(linkPath);

        if (Directory.Exists(testDirectory))
          Directory.Delete(testDirectory, true);
      }
    }

    /// <summary>
    /// Creates a directory symbolic link or, when symbolic links are unavailable on Windows, a directory junction.
    /// </summary>
    /// <param name="linkPath">Path of the directory link to create.</param>
    /// <param name="targetDirectory">Existing directory targeted by the link.</param>
    private static void CreateDirectoryLink(string linkPath, string targetDirectory)
    {
      MethodInfo createSymbolicLink = typeof(Directory).GetMethod("CreateSymbolicLink", new[] { typeof(string), typeof(string) });
      if (createSymbolicLink != null)
      {
        try
        {
          createSymbolicLink.Invoke(null, new object[] { linkPath, targetDirectory });
          return;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is UnauthorizedAccessException || ex.InnerException is IOException)
        {
        }
      }

      using Process junctionProcess = Process.Start(new ProcessStartInfo
      {
        FileName = "cmd.exe",
        Arguments = string.Format("/c mklink /J \"{0}\" \"{1}\"", linkPath, targetDirectory),
        CreateNoWindow = true,
        RedirectStandardError = true,
        UseShellExecute = false
      });
      junctionProcess.WaitForExit();

      if (junctionProcess.ExitCode != 0)
      {
        string error = junctionProcess.StandardError.ReadToEnd();
        Assert.Fail(string.Format("Unable to create a directory link. {0}", error));
      }
    }

    [Test, Description("MySQL Bulk Loader ran with Automation")]
    public void InsertFilesInDatabase()
    {
      var cPath = Assembly.GetExecutingAssembly().Location.Replace(String.Format("{0}.dll",
             Assembly.GetExecutingAssembly().GetName().Name), string.Empty);
      string imageFile = cPath + "image1.jpg";
      var fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read);
      long fileSize = fs.Length;
      byte[] rawData = new byte[fs.Length];
#if NET9_0_OR_GREATER
      fs.ReadExactly(rawData, 0, (int)fs.Length);
#else
      fs.Read(rawData, 0, (int)fs.Length);
#endif

      fs.Close();

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        var cmd = new MySqlCommand();
        conn.Open();
        cmd.Connection = conn;
        cmd.CommandText = "DROP TABLE IF EXISTS file";
        cmd.ExecuteNonQuery();
        cmd.CommandText = " CREATE TABLE file(file_id SMALLINT UNSIGNED AUTO_INCREMENT NOT NULL PRIMARY KEY," +
                          "file_name VARCHAR(64) NOT NULL, file_size MEDIUMINT UNSIGNED NOT NULL,file MEDIUMBLOB NOT NULL);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO file VALUES(NULL, @FileName, @FileSize, @File)";
        cmd.Parameters.AddWithValue("@FileName", "image1.jpg");
        cmd.Parameters.AddWithValue("@FileSize", fileSize);
        cmd.Parameters.AddWithValue("@File", rawData);
        var result = cmd.ExecuteNonQuery();
        Assert.That(result, Is.Not.Null);
        cmd.CommandText = "select count(*) from file;";
        var count = cmd.ExecuteScalar();
        Assert.That(count, Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Bug21049228 [SUPPORT TO USE (MEMORY-)STREAM FOR BULK LOADING DATA]
    /// </summary>
    [Test]
    public void BulkLoadStream()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // create the sream to be loaded
      using MemoryStream stream = new MemoryStream();
      using StreamWriter sw = new StreamWriter(stream);

      for (int i = 0; i < 200; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.Timeout = 0;
      loader.Local = true;
      int count = loader.Load(stream);
      Assert.That(count, Is.EqualTo(200));

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(200));
      Assert.That(dt.Rows[0][1].ToString().Trim(), Is.EqualTo("'Test'"));
    }

    [Test]
    public void BulkLoadStream2()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      // create the sream to be loaded
      using MemoryStream stream = new MemoryStream();
      using StreamWriter sw = new StreamWriter(stream);

      for (int i = 0; i < 100; i++)
        sw.Write("aaa" + i + ",'Test' xxx");
      for (int i = 100; i < 200; i++)
        sw.Write("bbb" + i + ",'Test' xxx");
      for (int i = 200; i < 300; i++)
        sw.Write("aaa" + i + ",'Test' xxx");
      for (int i = 300; i < 400; i++)
        sw.Write("bbb" + i + ",'Test' xxx");
      sw.Flush();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = "xxx";
      loader.LinePrefix = "bbb";
      loader.Local = true;
      int count = loader.Load(stream);
      Assert.That(count, Is.EqualTo(200));

      using MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.That(Convert.ToInt32(cmd.ExecuteScalar()), Is.EqualTo(200));
    }

    [Test]
    public void BulkLoadStreamColumnOrder()
    {
      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL, n1 VARCHAR(250), n2 VARCHAR(250), 
            n3 VARCHAR(250), PRIMARY KEY(id))");

      // create the sream to be loaded
      using MemoryStream stream = new MemoryStream();
      using StreamWriter sw = new StreamWriter(stream);
      for (int i = 0; i < 20; i++)
        sw.WriteLine(i + ",col3,col2,col1");
      sw.Flush();

      MySqlBulkLoader loader = new MySqlBulkLoader(Connection);
      loader.TableName = "Test";
      loader.Timeout = 0;
      loader.FieldTerminator = ",";
      loader.LineTerminator = Environment.NewLine;
      loader.Columns.Add("id");
      loader.Columns.Add("n3");
      loader.Columns.Add("n2");
      loader.Columns.Add("n1");
      loader.Local = true;
      int count = loader.Load(stream);
      Assert.That(count, Is.EqualTo(20));

      DataTable dt = Utils.FillTable("SELECT * FROM Test", Connection);
      Assert.That(dt.Rows.Count, Is.EqualTo(20));
      Assert.That(dt.Rows[0][1], Is.EqualTo("col1"));
      Assert.That(dt.Rows[0][2], Is.EqualTo("col2"));
      Assert.That(dt.Rows[0][3].ToString().Trim(), Is.EqualTo("col3"));
    }
  }
}
