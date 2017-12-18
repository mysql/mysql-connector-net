// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Threading;
using System.Web.SessionState;
using System.IO;
using System.Collections.Specialized;
using System.Net;
using System.Diagnostics;
using System.Configuration;
using Xunit;
using MySql.Data.MySqlClient;
using MySql.Web.SessionState;

namespace MySql.Web.Tests
{
  public class SessionTests : WebTestBase 
  {
    private string strSessionID { get; set; }
    private string calledId { get; set; }
    private AutoResetEvent _evt { get; set; }

    private byte[] Serialize(SessionStateItemCollection items)
    {
      MemoryStream ms = new MemoryStream();
      BinaryWriter writer = new BinaryWriter(ms);
      if (items != null)
      {
        items.Serialize(writer);
      }
      writer.Close();
      return ms.ToArray();
    }


    private void CreateSessionData(int AppId, DateTime timeCreated)
    {
      MySqlCommand cmd = new MySqlCommand();
      strSessionID = System.Guid.NewGuid().ToString();

      //DateTime now = DateTime.Now;
      //DateTime lastHour = now.Subtract(new TimeSpan(1, 0, 0));

      SessionStateItemCollection collection = new SessionStateItemCollection();
      collection["FirstName"] = "Some";
      collection["LastName"] = "Name";
      byte[] items = Serialize(collection);

      string sql = @"INSERT INTO my_aspnet_sessions VALUES (
            @sessionId, @appId, @created, @expires, @lockdate, @lockid, @timeout,
            @locked, @items, @flags)";

      cmd = new MySqlCommand(sql, Connection);
      cmd.Parameters.AddWithValue("@sessionId", strSessionID);
      cmd.Parameters.AddWithValue("@appId", AppId);
      cmd.Parameters.AddWithValue("@created", timeCreated);
      cmd.Parameters.AddWithValue("@expires", timeCreated);
      cmd.Parameters.AddWithValue("@lockdate", timeCreated);
      cmd.Parameters.AddWithValue("@lockid", 1);
      cmd.Parameters.AddWithValue("@timeout", 1);
      cmd.Parameters.AddWithValue("@locked", 0);
      cmd.Parameters.AddWithValue("@items", items);
      cmd.Parameters.AddWithValue("@flags", 0);
      cmd.ExecuteNonQuery();

      //create new row on sessioncleanup table
      cmd.CommandText = "INSERT IGNORE INTO my_aspnet_sessioncleanup SET" +
              " ApplicationId = @ApplicationId, " +
              " LastRun = NOW(), " +
              " IntervalMinutes = 10";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("@ApplicationId", AppId);
      cmd.ExecuteNonQuery();

      // set our last run table to 1 hour ago
      cmd.CommandText = "UPDATE my_aspnet_sessioncleanup SET LastRun=@lastHour WHERE ApplicationId = @ApplicationId";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("@lastHour", DateTime.Now.Subtract(new TimeSpan(1, 0, 0)));
      cmd.Parameters.AddWithValue("@ApplicationId", AppId);
      cmd.ExecuteNonQuery();
    }


    private void SetSessionItemExpiredCallback(bool includeCallback)
    {
      _evt = new AutoResetEvent(false);
      calledId = null;

      CreateSessionData(1, DateTime.Now.Subtract(new TimeSpan(1, 0, 0)));

      MySqlSessionStateStore session = new MySqlSessionStateStore();

      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("enableExpireCallback", includeCallback ? "true" : "false");
      session.Initialize("SessionProvTest", config);
      if (includeCallback) session.SetItemExpireCallback(expireCallback);
      Thread.Sleep(1000);
      session.Dispose();
    }

    private long CountSessions()
    {
      return (long)MySqlHelper.ExecuteScalar(Connection, "SELECT COUNT(*) FROM my_aspnet_sessions");
    }

    public void expireCallback(string id, SessionStateStoreData item)
    {
      calledId = id;
      _evt.Set();
    }


    [Fact]
    public void SessionItemWithExpireCallback()
    {
      SetSessionItemExpiredCallback(true);
      _evt.WaitOne();

      Assert.Equal(strSessionID, calledId);

      int i = 0;
      while (((long)MySqlHelper.ExecuteScalar(Connection, "SELECT Count(*) FROM my_aspnet_sessions;") != 0) && (i < 10))
      {
        Thread.Sleep(500);
        i++;
      }

      Assert.Equal(0, CountSessions());
    }


    [Fact]
    public void SessionItemWithoutExpireCallback()
    {
      SetSessionItemExpiredCallback(false);
      Assert.NotEqual(strSessionID, calledId);

      int i = 0;
      while (((long)MySqlHelper.ExecuteScalar(Connection, "SELECT Count(*) FROM my_aspnet_sessions;") != 0) && (i < 10))
      {
        Thread.Sleep(500);
        i++;
      }

      Assert.Equal(0, CountSessions());
    }

    [Fact]
    public void DeleteSessionAppSpecific()
    {
      // create two sessions of different appId
      // it should delete only 1
      CreateSessionData(1, DateTime.Now.Subtract(new TimeSpan(1, 10, 0)));
      CreateSessionData(2, DateTime.Now.Subtract(new TimeSpan(1, 0, 0)));

      MySqlSessionStateStore session = new MySqlSessionStateStore();

      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("enableExpireCallback", "false");
      session.Initialize("SessionTests", config);

      int i = 0;
      while (CountSessions() == 2 && (i < 10))
      {
        Thread.Sleep(500);
        i++;
      }

      session.Dispose();
      Assert.Equal(1, CountSessions());

    }

    public class ThreadRequestData
    {
      public string pageName;
      public ManualResetEvent signal;
      public bool FirstDateToUpdate;
    }

    delegate WebResponse GetResponse();
    delegate void ThreadRequest(ThreadRequestData data);

    [Fact(Skip ="Fix Me")]
    public void SessionLocking()
    {
      // Copy updated configuration file for web server process 
      Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      ConnectionStringSettings css = config.ConnectionStrings.ConnectionStrings["LocalMySqlServer"];
      string curDir = Directory.GetCurrentDirectory();
      string webconfigPath = string.Format(@"{0}\SessionLocking\{1}", Directory.GetCurrentDirectory(), @"web.config");
      string webconfigPathSrc = string.Format(@"{0}\SessionLocking\{1}", Directory.GetCurrentDirectory(), @"web_config_src.txt");

      string text = File.ReadAllText(webconfigPathSrc);
      text = text.Replace("connection_string_here", css.ConnectionString);
      Version ver = System.Environment.Version;
      if (ver.Major != 4)
      {
        text = text.Replace("<compilation targetFramework=\"4.0\" />", "");
      }

      File.WriteAllText(webconfigPath, text);

      int port = 12224;

      string webserverPath;
      if (ver.Major == 4)
      {
        webserverPath = @"C:\Program Files (x86)\Common Files\microsoft shared\DevServer\10.0\WebDev.WebServer40.exe";
      }
      else
      {
        webserverPath = @"C:\Program Files (x86)\Common Files\microsoft shared\DevServer\9.0\WebDev.WebServer.exe";
      }
      string webserverArgs = string.Format(" /port:{0} /path:{1}\\SessionLocking", port,
        Path.GetFullPath(@"."));

      DirectoryInfo di = new DirectoryInfo(Path.GetFullPath(curDir));
      Directory.CreateDirectory(Path.GetFullPath(@".\SessionLocking\bin"));
      foreach (FileInfo fi in di.GetFiles("*.dll"))
      {
        File.Copy(fi.FullName, Path.Combine(Path.GetFullPath(@".\SessionLocking\bin\"), fi.Name), true);
      }

      Process webserver = Process.Start(webserverPath, webserverArgs);
      System.Threading.Thread.Sleep(2000);

      // This dummy request is just to get the ASP.NET sessionid to reuse.
      HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://localhost:12224/InitSessionLocking.aspx");
      HttpWebResponse res = (HttpWebResponse)req.GetResponse();
      WebHeaderCollection headers = new WebHeaderCollection();

      string url = res.ResponseUri.ToString().Replace("InitSessionLocking.aspx", "");
      Debug.Write(url);

      try
      {
        DateTime? firstDt = null;
        DateTime? secondDt = null;

        ManualResetEvent[] re = new ManualResetEvent[2];
        re[0] = new ManualResetEvent(false);
        re[1] = new ManualResetEvent(false);
        ParameterizedThreadStart ts =
          (object data1) =>
          {
            ThreadRequestData data = (ThreadRequestData)data1;
            Debug.WriteLine(string.Format("Requesting {0}", data.pageName));
            try
            {
              HttpWebRequest req1 =
                (HttpWebRequest)WebRequest.Create(string.Format(@"{0}{1}", url, data.pageName));
              req1.Timeout = 2000000;
              WebResponse res1 = req1.GetResponse();
              Debug.WriteLine(string.Format("Response from {0}", data.pageName));
              Stream s = res1.GetResponseStream();
              while (s.ReadByte() != -1)
                ;
              res1.Close();
              if (data.FirstDateToUpdate)
              {
                firstDt = DateTime.Now;
              }
              else
              {
                secondDt = DateTime.Now;
              }
            }
            catch (Exception e)
            {
              Debug.WriteLine(string.Format("Server error: {0}", e.ToString()));
              throw;
            }
            finally
            {
              data.signal.Set();
            }
          };

        Thread t = new Thread(ts);
        Thread t2 = new Thread(ts);
        t.Start(new ThreadRequestData()
        {
          pageName = "write.aspx",
          FirstDateToUpdate = true,
          signal = re[0]
        });
        t2.Start(new ThreadRequestData()
        {
          pageName = "read.aspx",
          FirstDateToUpdate = false,
          signal = re[1]
        });
        WaitHandle.WaitAll(re);
        re[0].Reset();
        Thread t3 = new Thread(ts);
        t3.Start(new ThreadRequestData()
        {
          pageName = "write2.aspx",
          FirstDateToUpdate = false,
          signal = re[0]
        });
        WaitHandle.WaitAll(re);
        double totalMillisecs = Math.Abs((secondDt.Value - firstDt.Value).TotalMilliseconds);
        // OK if wait is less than session timeout
        Debug.WriteLine(string.Empty);
        Debug.WriteLine(totalMillisecs);
        Assert.True(totalMillisecs < 30000);
      }
      finally
      {
        webserver.Kill();
      }
    }

    public volatile static ManualResetEvent mtxReader = null;
    public volatile static ManualResetEvent mtxWriter = null;

    public static void WaitSyncCreation(bool writer)
    {
      if (writer)
      {
        while (true)
        {
          if (mtxWriter == null)
            Thread.Sleep(100);
          else
            break;
        }
        mtxWriter.WaitOne();
      }
      else
      {
        while (true)
        {
          if (mtxReader == null)
            Thread.Sleep(100);
          else
            break;
        }
        mtxReader.WaitOne();
      }
    }


  }
}
