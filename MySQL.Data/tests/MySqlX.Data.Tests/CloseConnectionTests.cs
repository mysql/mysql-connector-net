// Copyright (c) 2020, 2021, Oracle and/or its affiliates.
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

using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading;

namespace MySqlX.Data.Tests
{
  public class CloseConnectionTests : BaseTest
  {
    /// <summary>
    /// WL14166 - XProtocol: Support connection close notification
    /// This feature was implemented since MySQL Server 8.0.23
    /// </summary>

    /************************************/
    /* Tests for not pooling connections*/
    /************************************/
    // Kill Notice, exception expected
    [Test]
    public void NotificationKill()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();
      using (Session session1 = MySQLX.GetSession(ConnectionString))
      {
        Schema test = session1.GetSchema("test");
        Assert.AreEqual(SessionState.Open, session1.XSession.SessionState);
        var threadId1 = (UInt64)session1.ThreadId;

        // Kill connection
        ExecuteSqlAsRoot($"KILL {threadId1};");
        Thread.Sleep(5000);

        Exception ex = Assert.Throws<MySqlException>(() => session1.SQL("select 1").Execute());
        StringAssert.Contains(ResourcesX.NoticeKilledConnection, ex.Message);

        Assert.AreEqual(SessionState.Closed, session1.XSession.SessionState);
      }
    }

    //Idle Notice, exception expected
    [Test]
    public void NotificationIdle()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();

      ExecuteSqlAsRoot("SET GLOBAL mysqlx_read_timeout = 5");
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_wait_timeout = 5");
      using (Session localsession = MySQLX.GetSession(ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, localsession.XSession.SessionState);
        //wait for server to kill idle connection
        Thread.Sleep(7000);

        Exception ex = Assert.Throws<MySqlException>(() => localsession.SQL("select 1").Execute());
        StringAssert.Contains(ResourcesX.NoticeIdleConnection, ex.Message);

        Assert.AreEqual(SessionState.Closed, localsession.XSession.SessionState);
      }
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_read_timeout = 28800");
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_wait_timeout = 28800");
    }

    [DatapointSource]
    private readonly CloseData[] closeData = new CloseData[]
    {
      new CloseData { Action = new Action<Session>(s => { s.Schema.CreateCollection("collection1"); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetCollections(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetCollection("collection1",true); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetCollections()[0].Find().Execute(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetTables(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetTables()[0].Select().Execute(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetTable("test").Count(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetCollection("collection1",true).ExistsInDatabase(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.ExistsInDatabase(); }) },
    };

    [Theory]
    public void CloseWarningsWithCollections(CloseData closeData)
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();
      Session sessionCol = null;
      sessionCol = MySQLX.GetSession(ConnectionString);

      sessionCol.DropSchema(schemaName);
      sessionCol.CreateSchema(schemaName);
      var threadId1 = (ulong)sessionCol.ThreadId;

      // Kill connection
      ExecuteSqlAsRoot($"KILL {threadId1};");
      Thread.Sleep(5000);

      MySqlException ex = Assert.Throws<MySqlException>(() => { closeData.Action.Invoke(sessionCol); });
      Assert.AreEqual(ResourcesX.NoticeKilledConnection, ex.Message);
      Assert.AreEqual(SessionState.Closed, sessionCol.XSession.SessionState);
    }

    //Shutdown server Notice, exception expected
    [Test]
    [Ignore("This test is marked as Ignore because it shutdown the local MySQL Server, comment this line to run this test manually")]
    public void NotificationShutdown()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();

      using (Session localsession = MySQLX.GetSession(BaseTest.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, localsession.XSession.SessionState);

        // Shutdown local MySQL Server
        ShutdownServer();
        Thread.Sleep(2000);

        Exception ex = Assert.Throws<MySqlException>(() => localsession.SQL("select 1").Execute());
        StringAssert.Contains(ResourcesX.NoticeServerShutdown, ex.Message);

        Assert.AreEqual(SessionState.Closed, localsession.XSession.SessionState);
      }
    }

    /*********************************/
    /* Tests for pooling connections */
    /*********************************/
    // Open multiple connection and one receive close notification
    [Test]
    public void PoolTestCloseOneConnection()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();
      int size = 3;
      int timeout = 3000;
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { maxSize = size, queueTimeout = timeout } }))
      {
        //Creeate 3 sessions
        Session session1 = client.GetSession();
        Session session2 = client.GetSession();
        Session session3 = client.GetSession();
        int threadId1 = session1.ThreadId;
        int threadId2 = session2.ThreadId;
        int threadId3 = session3.ThreadId;
        //Attempt 4th session
        var overflow = Assert.Throws<TimeoutException>(() => client.GetSession());

        //kill session 2, exception expected
        ExecuteSqlAsRoot($"KILL {threadId2};");
        Thread.Sleep(5000);

        //verify session 1 open
        var result1 = session1.SQL("select sqrt(9) as raiz;").Execute().FetchOne().GetString("raiz");
        StringAssert.Contains("3", result1);

        //try to use session 2
        Exception ex1 = Assert.Throws<MySqlException>(() => session2.SQL("select sqrt(9) as raiz;").Execute());
        StringAssert.Contains(ResourcesX.NoticeKilledConnection, ex1.Message);

        //verify session 3 open
        var result2 = session3.SQL("select sqrt(9) as raiz;").Execute().FetchOne().GetString("raiz");
        StringAssert.Contains("3", result2);

        //try to use session 2 once it is Invalid
        Exception ex2 = Assert.Throws<MySqlException>(() => session2.SQL("SELECT 5").Execute());
        Assert.AreEqual(ResourcesX.InvalidSession, ex2.Message);

        //reuse session2
        session2 = client.GetSession();
        var result3 = session2.SQL("select sqrt(9) as raiz;").Execute().FetchOne().GetString("raiz");
        StringAssert.Contains("3", result3);
      }
    }

    // Open multiple connection and shutdown server
    [Test]
    [Ignore("This test is marked as Ignore because it shutdown the local MySQL Server, comment this line to run this test manually")]
    public void PoolTestShutdown()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();
      int size = 3;
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { maxSize = size } }))
      {
        Session session1 = client.GetSession();
        Session session2 = client.GetSession();
        Session session3 = client.GetSession();

        // Shutdown MySQL server
        ShutdownServer();
        Thread.Sleep(5000);

        // All connections should receive a close notification and become closed and invalid
        Exception ex1 = Assert.Throws<MySqlException>(() => session1.SQL("select sqrt(9) as raiz;").Execute());
        StringAssert.Contains(ResourcesX.NoticeServerShutdown, ex1.Message);

        Exception ex2 = Assert.Throws<MySqlException>(() => session2.SQL("select sqrt(9) as raiz;").Execute());
        StringAssert.Contains(ResourcesX.NoticeServerShutdown, ex2.Message);

        Exception ex3 = Assert.Throws<MySqlException>(() => session3.SQL("select sqrt(9) as raiz;").Execute());
        StringAssert.Contains(ResourcesX.NoticeServerShutdown, ex3.Message);
      }
    }

    // Open multiple connection and wait o be killed by server
    [Test]
    public void PoolWithIdleConnections()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();
      int size = 2;
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_read_timeout = 5");
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_wait_timeout = 5");
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { maxSize = size } }))
      {
        Session session1 = client.GetSession();
        Session session2 = client.GetSession();

        // wait for server to kill connections
        Thread.Sleep(8000);

        Exception ex1 = Assert.Throws<MySqlException>(() => session1.SQL("select sqrt(9) as raiz;").Execute());
        StringAssert.Contains(ResourcesX.NoticeIdleConnection, ex1.Message);

        Exception ex2 = Assert.Throws<MySqlException>(() => session2.SQL("select sqrt(9) as raiz;").Execute());
        StringAssert.Contains(ResourcesX.NoticeIdleConnection, ex2.Message);

      }
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_read_timeout = 28800");
      ExecuteSqlAsRoot("SET GLOBAL mysqlx_wait_timeout = 28800");
    }

    public void ShutdownServer()
    {
      var basedir = session.SQL("SELECT @@BASEDIR as basedir;").Execute().FetchOne().GetString("basedir");
      basedir += @"bin\";
      var externalProc = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = basedir + "mysqladmin.exe",
          Arguments = " -uroot shutdown",
          UseShellExecute = true,
          RedirectStandardOutput = false,
          CreateNoWindow = true,
          WorkingDirectory = basedir
        }
      };

      externalProc.Start();
      ServerIsDown = true;
    }
    public struct CloseData
    {
      public Action<Session> Action { get; set; }
    }
  }
}
