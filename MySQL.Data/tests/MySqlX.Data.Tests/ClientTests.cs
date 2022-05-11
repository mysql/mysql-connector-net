// Copyright (c) 2018, 2022, Oracle and/or its affiliates.
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
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class ClientTests : BaseTest
  {
    private static string localServerIpv4 = "localServerIpv4";

    public struct ClientOptions
    {
      public object ConnectionOptions { get; set; }
      public object Options { get; set; }
    }

    [DatapointSource]
    private readonly ClientOptions[] clientOptions = new ClientOptions[]
    {
      new ClientOptions {
        ConnectionOptions = new { pooling = new { enabled = false } },
        Options = new Func<Client.ConnectionOptions>(() => { var c = new Client.ConnectionOptions(); c.Pooling.Enabled = false; return c; }).Invoke()
      },
      new ClientOptions {
        ConnectionOptions = new { pooling = new { maxsIzE = 100, MAXidleTime = 60000 } },
        Options = new Func<Client.ConnectionOptions>(() => { var c = new Client.ConnectionOptions(); c.Pooling.MaxSize = 100; c.Pooling.MaxIdleTime = 60000; return c; }).Invoke()
      },
      new ClientOptions {
        ConnectionOptions = new { pooling = new { queuetimeout = 45000 } },
        Options = new Func<Client.ConnectionOptions>(() => { var c = new Client.ConnectionOptions(); c.Pooling.QueueTimeout = 45000; return c; }).Invoke()
      },
      new ClientOptions {
        ConnectionOptions = "{ \"pooling\": { \"enabled\": false } }",
        Options = new Func<Client.ConnectionOptions>(() => { var c = new Client.ConnectionOptions(); c.Pooling.Enabled = false; return c; }).Invoke()
      },
      new ClientOptions {
        ConnectionOptions = "{ \"pooling\": { \"maxidleTIME\": 55000, \"QUEUEtimeout\": 120000 } }",
        Options = new Func<Client.ConnectionOptions>(() => { var c = new Client.ConnectionOptions(); c.Pooling.MaxIdleTime = 55000; c.Pooling.QueueTimeout = 120000; return c; }).Invoke()
      },
    };

    [Theory]
    public void ParseConnectionOptionsTest(ClientOptions clientOptions)
    {
      Client.ConnectionOptions poolingOptions = (Client.ConnectionOptions)clientOptions.Options;
      Client.ConnectionOptions connectionOptionsResult = Client.ParseConnectionOptions(clientOptions.ConnectionOptions);
      Assert.True(poolingOptions.Equals(connectionOptionsResult));
    }

    public struct InvalidOptions
    {
      public object ConnectionOptions { get; set; }
      public string OptionName { get; set; }
    }

    [DatapointSource]
    private readonly InvalidOptions[] invalidOptions = new InvalidOptions[]
    {
      new InvalidOptions {
        ConnectionOptions = new { pooling = new { isenabled = false } },
        OptionName = "pooling.isenabled"
      },
      new InvalidOptions {
        ConnectionOptions = new { pooling = new { maxsIzE = 80, MAX_idle_Time = 60000 } },
        OptionName = "pooling.MAX_idle_Time"
      },
      new InvalidOptions {
        ConnectionOptions = new { enabled = true },
        OptionName = "enabled"
      },
      new InvalidOptions {
        ConnectionOptions = new { pooling = new { enabled = true }, maxIdleTime = 30000 },
        OptionName = "maxIdleTime"
      },
      new InvalidOptions {
        ConnectionOptions = "{ \"pooling\": { \"is_enabled\": false } }",
        OptionName = "pooling.is_enabled"
      },
      new InvalidOptions {
        ConnectionOptions = "{ \"queueTimeout\": 50000 }",
        OptionName = "queueTimeout"
      },
      new InvalidOptions {
        ConnectionOptions = "{ \"pooling\": { \"idleTIME\": 55000, \"queuetimeout\": 120000 } }",
        OptionName = "pooling.idleTIME"
      },
      new InvalidOptions {
        ConnectionOptions = "{ \"pooling\": { \"MaxidleTIME\": 55000 }, \"queuetimeout\": 120000 }",
        OptionName = "queuetimeout"
      },
      new InvalidOptions {
        ConnectionOptions = "{ pooling: { MaxidleTIME: 55000 } }",
        OptionName = "JSON"
      },
      new InvalidOptions {
        ConnectionOptions = DateTime.Today,
        OptionName = DateTime.Today.ToString()
      },
      new InvalidOptions {
        ConnectionOptions = 25,
        OptionName = "25"
      },
    };

    [Theory]
    public void ParseConnectionOptionsInvalidOptions(InvalidOptions invalidOptions)
    {
      ArgumentException ex = Assert.Throws<ArgumentException>(() => Client.ParseConnectionOptions(invalidOptions.ConnectionOptions));
      Assert.That(string.Format(ResourcesX.ClientOptionNotValid, invalidOptions.OptionName), Is.EqualTo(ex.Message).IgnoreCase);
    }

    public struct InvalidValues
    {
      public object ConnectionOptions { get; set; }
      public string OptionName { get; set; }
      public object Value { get; set; }
    }

    [DatapointSource]
    private readonly InvalidValues[] invalidValues = new InvalidValues[]
    {
      new InvalidValues {
        ConnectionOptions = new { pooling = true },
        OptionName = "pooling",
        Value = true
      },
      new InvalidValues {
        ConnectionOptions = new { pooling = new { maxsIzE = 0 } },
        OptionName = "pooling.maxsIzE",
        Value = 0
      },
      new InvalidValues {
        ConnectionOptions = new { pooling = new { maxsIzE = -25, MAXidleTime = 60000 } },
        OptionName = "pooling.maxsIzE",
        Value = -25
      },
      new InvalidValues {
        ConnectionOptions = new { pooling = new { maxsIzE = 90L } },
        OptionName = "pooling.maxsIzE",
        Value = 90L
      },
      new InvalidValues {
        ConnectionOptions = "{ \"pooling\": { \"enabled\": yes } }",
        OptionName = "pooling.enabled",
        Value = "yes"
      },
      new InvalidValues {
        ConnectionOptions = "{ \"pooling\": { \"MaxidleTIME\": -22, \"queueTimeout\": 120000 } }",
        OptionName = "pooling.MaxidleTIME",
        Value = -22
      }
    };

    [Theory]
    public void ParseConnectionOptionsInvalidValues(InvalidValues invalidValues)
    {
      ArgumentException ex = Assert.Throws<ArgumentException>(() => Client.ParseConnectionOptions(invalidValues.ConnectionOptions));
      Assert.That(string.Format(ResourcesX.ClientOptionInvalidValue, invalidValues.OptionName, invalidValues.Value), Is.EqualTo(ex.Message).IgnoreCase);
    }

    public struct PoolingTestData
    {
      public object ConnectionOptions { get; set; }
      public int Size { get; set; }
      public int Iterations { get; set; }
    }

    [DatapointSource]
    private readonly PoolingTestData[] poolingTestData = new PoolingTestData[] {
      new PoolingTestData
      {
        ConnectionOptions = new { pooling = new { maxSize = 5, queueTimeout = 5000 } },
        Size = 5,
        Iterations = 10
      },
      new PoolingTestData
      {
        ConnectionOptions = "{ \"pooling\": { \"maxSize\": 5, \"queueTimeout\": 5000 } }",
        Size = 5,
        Iterations = 10
      }
    };

    [Theory]
    [Property("Category", "Security")]
    public void PoolingTest(PoolingTestData poolingTestData)
    {
      using (Client client = MySQLX.GetClient(ConnectionString, poolingTestData.ConnectionOptions))
      {
        List<string> hosts = new List<string>(poolingTestData.Size);
        List<Session> sessions = new List<Session>(poolingTestData.Size);
        for (int i = 0; i < poolingTestData.Size; i++)
        {
          Session session = client.GetSession();
          hosts.Add(session.SQL("SELECT host FROM information_schema.PROCESSLIST where id=CONNECTION_ID()").Execute().FetchOne().GetString("host"));
          sessions.Add(session);
        }

        Action closeSessions = () =>
        {
          foreach (Session session in sessions)
          {
            session.Close();
          }
          sessions.Clear();
        };
        closeSessions.Invoke();

        for (int x = 0; x < poolingTestData.Iterations; x++)
        {
          for (int i = 0; i < poolingTestData.Size; i++)
          {
            Session session = client.GetSession();
            string host = session.SQL("SELECT host FROM information_schema.PROCESSLIST where id=CONNECTION_ID()").Execute().FetchOne().GetString("host");
            sessions.Add(session);
            Assert.Contains(host, hosts);
          }
          closeSessions.Invoke();
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void QueueTimeoutTest()
    {
      if (Platform.IsWindows()) Assert.Ignore("Fix this for Windows OS");
      int timeout = 3000;
      using (Client client = MySQLX.GetClient(ConnectionString, new { pooling = new { maxSize = 1, queueTimeout = timeout } }))
      {
        using (Session session1 = client.GetSession())
        {
          Stopwatch stopwatch = Stopwatch.StartNew();
          TimeoutException ex = Assert.Throws<TimeoutException>(() => { Session session2 = client.GetSession(); });
          stopwatch.Stop();
          Assert.AreEqual(ResourcesX.PoolingQueueTimeout, ex.Message);
          Assert.True(stopwatch.ElapsedMilliseconds >= timeout);
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ReuseSessions()
    {
      int size = 3;
      int timeout = 3000;
      using (Client client = MySQLX.GetClient(ConnectionString, new { pooling = new { maxSize = size, queueTimeout = timeout } }))
      {
        Session session = client.GetSession();
        Assert.AreEqual((sbyte)5, session.SQL("SELECT 5").Execute().FetchOne()[0]);
        session.Close();
        MySqlException ex = Assert.Throws<MySqlException>(() => { session.SQL("SELECT 5").Execute(); });
        Assert.AreEqual(ResourcesX.InvalidSession, ex.Message);
      }
    }

    private const int _connectionTimeout = 1000;

    public struct MultiHostData
    {
      public object ConnectionData { get; set; }
    }

    [DatapointSource]
    private readonly MultiHostData[] multiHostData = new MultiHostData[]
    {
      new MultiHostData { ConnectionData = $"server=10.10.10.10,{Host};port={XPort};user=root;connecttimeout={_connectionTimeout};" },
      new MultiHostData { ConnectionData = $"server=unknown,{Host};port={XPort};user=root;connecttimeout={_connectionTimeout};" },
      new MultiHostData { ConnectionData = $"server=(address=10.10.10.10,priority=20),(address={Host},priority=100);port={XPort};user=root;connecttimeout={_connectionTimeout};" },
      new MultiHostData { ConnectionData = $"mysqlx://root@[10.10.10.10,{Host}:{XPort}]?connecttimeout={_connectionTimeout}" },
      new MultiHostData { ConnectionData = $"mysqlx://root@[unknown,{Host}:{XPort}]?connecttimeout={_connectionTimeout}" },
      new MultiHostData { ConnectionData = $"mysqlx://root@[(address=10.10.10.10,priority=20),(address={Host}:{XPort},priority=100)]?connecttimeout={_connectionTimeout}" },
      new MultiHostData { ConnectionData = new { server = $"10.10.10.10,{Host}", user = "root", port = XPort, connecttimeout = _connectionTimeout } },
      new MultiHostData { ConnectionData = new { server = $"unknown,{Host}", user = "root", port = XPort, connecttimeout = _connectionTimeout } },
      new MultiHostData { ConnectionData = new { server = $"(address=10.10.10.10,priority=100), (address=20.20.20.20,priority=90), (address={Host},priority=20)", user = "root", port = XPort, connecttimeout = _connectionTimeout } }
    };

    [Theory]
    public void MultiHostTest(MultiHostData multiHostData)
    {
      using (Client client = MySQLX.GetClient(multiHostData.ConnectionData, "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session = client.GetSession())
        {
          Assert.AreEqual((sbyte)8, session.SQL("SELECT 8").Execute().FetchOne()[0]);
        }
      }
    }

    public struct CloseData
    {
      public Action<Session> Action { get; set; }
    }

    [DatapointSource]
    private readonly CloseData[] closeData = new CloseData[]
    {
      new CloseData { Action = new Action<Session>(s => { s.SQL("SELECT 9").Execute(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetCollections(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetCollections()[0].Find().Execute(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetTables(); }) },
      new CloseData { Action = new Action<Session>(s => { s.Schema.GetTables()[0].Select().Execute(); }) },
    };

    [Theory]
    [Property("Category", "Security")]
    public void CloseTests(CloseData closeData)
    {
      using (Client client = MySQLX.GetClient(ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session = client.GetSession())
        {
          session.DropSchema(schemaName);
          session.CreateSchema(schemaName);
          client.Close();
          Assert.AreEqual(SessionState.Closed, session.XSession.SessionState);
          MySqlException ex = Assert.Throws<MySqlException>(() => { closeData.Action.Invoke(session); });
          Assert.AreEqual(ResourcesX.InvalidSession, ex.Message);
        }
      }
    }

    /// <summary>
    /// WL12515 - DevAPI: Support new session reset functionality
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void ResetSessionTest()
    {
      // This feature was implemented since MySQL Server 8.0.16
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 16))) return;

      int size = 2;
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { maxSize = size } }))
      {
        Session session1 = client.GetSession();
        Session session2 = client.GetSession();

        int threadId1 = session1.ThreadId;
        int threadId2 = session2.ThreadId;

        ResetTestBeforeClose(session1, 1);
        ResetTestBeforeClose(session2, 2);

        session1.Close();
        session2.Close();

        Session session1_1 = client.GetSession();
        Session session2_1 = client.GetSession();

        ResetTestAfterClose(session1_1, threadId1, 1);
        ResetTestAfterClose(session2_1, threadId2, 2);

        session1_1.Close();
      }
    }

    private void ResetTestBeforeClose(Session session, int id)
    {
      session.SQL(string.Format("CREATE TEMPORARY TABLE testResetSession{0} (id int)", id)).Execute();
      session.SQL(string.Format("SET @a='session{0}'", id)).Execute();

      SqlResult res = session.SQL("SELECT @a AS a").Execute();
      Assert.AreEqual("session" + id, res.FetchAll()[0][0]);
      res = session.SQL("SHOW CREATE TABLE testResetSession" + id).Execute();
      Assert.AreEqual("testResetSession" + id, res.FetchAll()[0][0]);
    }

    private void ResetTestAfterClose(Session session, int threadId, int id)
    {
      Assert.AreEqual(threadId, session.ThreadId);
      SqlResult res = session.SQL("SELECT @a IS NULL").Execute();
      Assert.AreEqual((sbyte)1, res.FetchOne()[0]);
      var ex = Assert.Throws<MySqlException>(() => session.SQL("SHOW CREATE TABLE testResetSession" + id).Execute());
      StringAssert.AreEqualIgnoringCase(string.Format("Table 'test.testresetsession{0}' doesn't exist", id), ex.Message);

      session.SQL(string.Format("SET @a='session{0}'", id)).Execute();
      res = session.SQL("SELECT @a AS a").Execute();
      Assert.AreEqual("session" + id, res.FetchAll()[0][0]);
    }

    /// <summary>
    /// WL12514 - DevAPI: Support session-connect-attributes
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void ConnectionAttributes()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");

      // Validate that MySQLX.GetSession() supports a new 'connection-attributes' query parameter
      // with default values and all the client attributes starts with a '_'.
      TestConnectionAttributes(ConnectionString + ";connection-attributes=true;");

      // Validate that no attributes, client or user defined, are sent to server when the value is "false".
      TestConnectionAttributes(ConnectionString + ";connection-attributes=false;");

      // Validate default behavior with different scenarios.
      TestConnectionAttributes(ConnectionString + ";connection-attributes;");
      TestConnectionAttributes(ConnectionString + ";connection-attributes=true;");


      // Validate user-defined attributes to be sent to server.
      Dictionary<string, object> userAttrs = new Dictionary<string, object>
      {
        { "foo", "bar" },
        { "quua", "qux" },
        { "key", null }
      };
      TestConnectionAttributes(ConnectionString + ";connection-attributes=[foo=bar,quua=qux,key]", userAttrs);
      TestConnectionAttributes(ConnectionStringUri + "?connectionattributes=[foo=bar,quua=qux,key=]", userAttrs);

      // Errors
      var ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[_key=value]"));
      Assert.AreEqual(ResourcesX.InvalidUserDefinedAttribute, ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=123"));
      Assert.AreEqual(ResourcesX.InvalidConnectionAttributes, ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[key=value,key=value2]"));
      Assert.AreEqual(string.Format(ResourcesX.DuplicateUserDefinedAttribute, "key"), ex.Message);

      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder();
      builder.Server = Host;
      builder.Port = UInt32.Parse(XPort);
      builder.UserID = RootUser;
      builder.ConnectionAttributes = ";";
      ex = Assert.Throws<MySqlException>(() => MySQLX.GetClient(builder.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"));
      Assert.AreEqual("The requested value ';' is invalid for the given keyword 'connection-attributes'.", ex.Message);
    }

    private void TestConnectionAttributes(string connString, Dictionary<string, object> userAttrs = null)
    {
      string sql = "SELECT * FROM performance_schema.session_account_connect_attrs WHERE PROCESSLIST_ID = connection_id()";

      using (Client client = MySQLX.GetClient(connString, "{ \"pooling\": { \"enabled\": true } }"))
      using (Session session = client.GetSession())
      {
        Assert.AreEqual(SessionState.Open, session.XSession.SessionState);
        var result = session.SQL(sql).Execute().FetchAll();

        if (session.Settings.ConnectionAttributes == "false")
          Assert.IsEmpty(result);
        else
        {
          Assert.IsNotEmpty(result);
          MySqlConnectAttrs clientAttrs = new MySqlConnectAttrs();

          if (userAttrs == null)
          {
            Assert.AreEqual(8, result.Count);

            foreach (Row row in result)
              StringAssert.StartsWith("_", row[1].ToString());
          }
          else
          {
            Assert.AreEqual(11, result.Count);

            for (int i = 0; i < userAttrs.Count; i++)
            {
              Assert.True(userAttrs.ContainsKey(result.ElementAt(i)[1].ToString()));
              Assert.True(userAttrs.ContainsValue(result.ElementAt(i)[2]));
            }
          }
        }
      }
    }

    #region WL14389

    public static object connObject = new { server = Host, port = XPort, user = "test", password = "test" };    

    // Pooling Tests
    [Test, Description("Xprotocol: Reset connection state")]
    public void ResetConnectionPooling()
    {
      string connectionID1 = null;
      Session session1, session2 = null;
      Client client1 = null;
      using (client1 = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { maxSize = 1, queueTimeout = 2000 } }))
      {
        session1 = client1.GetSession();
        session1.DropSchema("newtest");
        session1.CreateSchema("newtest");
        var s = session1.GetSchema("newtest");
        var col = s.CreateCollection("test");
        object[] data = new object[]
        {
          new {  _id = 1, title = "Book 1", pages = 20 },
          new {  _id = 2, title = "Book 2", pages = 30 },
          new {  _id = 3, title = "Book 3", pages = 40 },
          new {  _id = 4, title = "Book 4", pages = 50 },
        };

        Result result = col.Add(data).Execute();
        var res0 = session1.SQL("SELECT CONNECTION_ID()").Execute();
        if (res0.HasData)
        {
          var row = res0.FetchOne();
          Assert.NotNull(row);
          connectionID1 = row[0].ToString();
          Assert.NotNull(connectionID1);
        }
        session1.Close();
        session2 = client1.GetSession();
        res0 = session2.SQL("SELECT CONNECTION_ID()").Execute();
        if (res0.HasData)
        {
          var row = res0.FetchOne();
          Assert.NotNull(row);
          connectionID1 = row[0].ToString();
          Assert.NotNull(connectionID1);
        }
        s = session2.GetSchema("newtest");
        col = s.GetCollection("test");
        var data1 = col.Remove("_id = 1").Execute();
        client1.Close();
        Assert.Catch(() => client1.GetSession());
      }
      session.DropSchema("newtest");
    }

    [Test, Description("Xplugin crash for large number of session open/close operations")]
    public void LargeOpenCloseOperations()
    {
      Thread tid1 = new Thread(new ThreadStart(SubProcessA));
      Thread tid2 = new Thread(new ThreadStart(SubProcessB));
      tid1.Start();
      tid2.Start();
      tid1.Join();
      tid2.Join();
    }

    private void SubProcessA()
    {
      Session session0 = MySQLX.GetSession(ConnectionStringUri);
      session0.SQL("SET @@global.mysqlx_max_connections=3000").Execute();
      session0.SQL("SET @@global.max_connections=3000").Execute();
      session0.Close();
      using (Client client = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { enabled = true, maxSize = 1000, queueTimeout = 1, maxIdleTime = 1 } }))
      {
        for (int i = 0; i < 1; i++)
        {
          using (Session session1 = client.GetSession())
          {
            session1.DropSchema("thread1");
            session1.CreateSchema("thread1");
            session1.DropSchema("thread1");
          }
        }
      }
    }

    private void SubProcessB()
    {
      Session session0 = MySQLX.GetSession(ConnectionStringUri);
      session0.SQL("SET @@global.mysqlx_max_connections=3000").Execute();
      session0.SQL("SET @@global.max_connections=3000").Execute();
      session0.Close();
      using (Client client = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { enabled = true, maxSize = 1000, queueTimeout = 1, maxIdleTime = 1 } }))
      {
        for (int i = 0; i < 1; i++)
        {
          using (Session session1 = client.GetSession())
          {
            session1.DropSchema("thread2");
            session1.CreateSchema("thread2");
            session1.DropSchema("thread2");
          }
        }
      }
    }

    [Test, Description("Get Session hangs when Maxsize is set to zero")]
    public void MaxsizeSetToZero()
    {
      string connectionString = ConnectionString + ";protocol=Socket;database=test;characterset=utf8mb4;sslmode=VerifyCA;ssl-ca=" + sslCa + ";certificatepassword=pass;keepalive=10;auth=PLAIN;";
      Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connectionString, new { pooling = new { maxSize = 0, queueTimeout = 2000 } }));
    }

    [Test, Description("Invalid exception while getsession when mysqlx max connection is reached")]
    [Ignore("This test needs to be executed individually.")]
    public void InvalidExceptionMaxConnection()
    {
      ExecuteSQL("SET @@global.mysqlx_max_connections=3");

      Client client1 = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { maxSize = 2 } });
      Session session1 = client1.GetSession();
      Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);

      session1.Close();
      session1 = client1.GetSession();
      Session session2 = client1.GetSession();
      Assert.AreEqual(SessionState.Open, session2.InternalSession.SessionState);

      Client client2 = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { maxSize = 2 } });
      Assert.Catch(() => client2.GetSession());

      client1.Close();
      client2.Close();
      ExecuteSQL("SET @@global.mysqlx_max_connections=151");
    }

    [TestCase(3, "select  @@port;")]
    [TestCase(4, "SET @@global.mysqlx_max_connections=2000;")]
    [Ignore("This test needs to be executed individually.")]
    public void ClientAlreadyClosed(int maxSize, string closeQuery)
    {
      ExecuteSQL($"SET @@global.mysqlx_max_connections={maxSize}");
      Session session01, session02;

      using (var client1 = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { maxSize = maxSize, queueTimeout = 2000 } }))
      {
        session01 = client1.GetSession();
        session01.DropSchema("newtest");
        session01.CreateSchema("newtest");
        session01.Close();
        session01 = client1.GetSession();
        session02 = client1.GetSession();
        session02.DropSchema("newtest");
        session02.CreateSchema("newtest");
        client1.Close();
      }

      Assert.Throws<MySqlException>(() => session01.SQL(closeQuery).Execute());
      ExecuteSQL("SET @@global.mysqlx_max_connections=100");
    }

    [TestCase(26, 1)]
    [TestCase(31, 2)]
    [Test, Description("INVALID EXCEPTION WHILE GETSESSION WHEN MYSQLX MAX CONNECTION IS REACHD")]
    public void MaxConnectionReached(int iterations, int objectType)
    {
      Session session1 = null;
      Client client1 = null;
      object connectionpoolingObject = null;
      switch (objectType)
      {
        case 1:
          connectionpoolingObject = new { pooling = new { enabled = true, queueTimeout = 20 } };
          break;
        case 2:
          connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 30, queueTimeout = 20 } };
          break;
      }

      using (client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject))
      {
        for (int i = 0; i < iterations; i++)
        {
          if (i == iterations - 1)
          {
            Assert.Throws<System.TimeoutException>(() => client1.GetSession());
          }
          else
          {
            session1 = client1.GetSession();
          }
        }
      }
    }

    [Test, Description("Test that a new static method GetClient() has been added to MySQLX class and takes two parameters as input.")]
    public void GetClientTest()
    {
      var connectionpoolingObject = new { pooling = new { enabled = true } };
      var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject);
      VerifyClient(client1);
      client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject);
      VerifyClient(client1);
      client1 = MySQLX.GetClient(connObject, connectionpoolingObject);
      VerifyClient(client1);
    }

    [Test, Description("Test that a new static method GetClient() has been added to MySQLX class and takes two parameters as input.Async test")]
    public void GetClientTestAsync()
    {
      var connectionpoolingObject = new { pooling = new { enabled = true } };
      var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject);
      Task.Run(() => VerifyClientAsync(client1)).Wait();
      client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject);
      Task.Run(() => VerifyClientAsync(client1)).Wait();
      client1 = MySQLX.GetClient(connObject, connectionpoolingObject);
      Task.Run(() => VerifyClientAsync(client1)).Wait();
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test the option 'maxSize' to configure the max number of sessions allowed in the connection pool.")]
    public void MaxSizeOptionTests(string inputType)
    {
      string[] connectionpooling = { "{ \"pooling\": { \"maxSize\": 15} }", "{ \"pooling\": { \"maxSize\": true} }", "{ \"pooling\": { \"maxSize\": -1} }", "{ \"pooling\": { \"maxSize\": 84584759345 } }", "{ \"pooling\": { \"maxSize\": 0} }", "{ \"pooling\": { \"maxSize\": } }" };
      object[] connectionpoolingObject = { new { pooling = new { maxSize = 15 } }, new { pooling = new { maxSize = true } }, new { pooling = new { maxSize = -1 } }, new { pooling = new { maxSize = 84584759345 } }, new { pooling = new { maxSize = 0 } }, new { pooling = new { maxSize = "" } } };

      for (int i = 0; i < connectionpoolingObject.Length; i++)
      {

        if (i == 0)
        {
          //Connection String
          using (var client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            var session1 = client1.GetSession();
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
          //Uri
          using (var client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            var session1 = client1.GetSession();
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
          //Connectin Object
          using (var client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            var session1 = client1.GetSession();
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
        }
        else
        {
          Exception ex = Assert.Catch(() => MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.True(ex.Message.Contains("'pooling.maxSize' does not support value"), "Expected Exception");

          ex = Assert.Catch(() => MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.True(ex.Message.Contains("'pooling.maxSize' does not support value"), "Expected Exception");

          ex = Assert.Catch(() => MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.True(ex.Message.Contains("'pooling.maxSize' does not support value"), "Expected Exception");
        }
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test maxIdle Timeout with different values")]
    public void maxIdleTimeOptionTests(string inputType)
    {
      int timeout = 5000;
      string[] connectionpooling = { "{ \"pooling\": { \"maxSize\": 1, \"maxIdleTime\": 2000 } }", "{ \"pooling\": { \"maxIdleTime\": true} }", "{ \"pooling\": { \"maxIdleTime\": 'true'} }", "{ \"pooling\": { \"maxIdleTime\": -1} }", "{ \"pooling\": { \"maxIdleTime\": 84584759345 } }", "{ \"pooling\": { \"maxIdleTime\": } }" };
      object[] connectionpoolingObject = { new { pooling = new { maxSize = 1, maxIdleTime = 2000 } }, new { pooling = new { maxIdleTime = true } }, new { pooling = new { maxIdleTime = "true" } }, new { pooling = new { maxIdleTime = -1 } }, new { pooling = new { maxIdleTime = 84584759345 } }, new { pooling = new { maxIdleTime = "" } } };

      for (int i = 0; i < (inputType == "string" ? connectionpooling : connectionpoolingObject).Length; i++)
      {
        if (i == 0)
        {
          //Connection string
          using (var client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            CompareConnectionIDs(client1, timeout, true);
          }

          //Uri
          using (var client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            CompareConnectionIDs(client1, timeout, true);
          }

          //Anonymous Object
          using (var client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            CompareConnectionIDs(client1, timeout, true);
          }
        }
        else
        {
          Assert.Catch(() => MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.Catch(() => MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.Catch(() => MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        }
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test queueTimeout with different values")]
    public void QueueTimeoutOptionTests(string inputType)
    {
      if (Platform.IsWindows()) Assert.Ignore("Fix this for Windows OS");

      int timeoutMS = 3000;
      string[] connectionpooling = { "{ \"pooling\": { \"maxSize\": 2,\"queueTimeout\": " + timeoutMS + " } }", "{ \"pooling\": { \"queueTimeout\": true} }", "{ \"pooling\": { \"queueTimeout\": 'true'} }", "{ \"pooling\": { \"queueTimeout\": -1} }", "{ \"pooling\": { \"queueTimeout\": 84584759345 } }", "{ \"pooling\": { \"queueTimeout\": } }" };
      object[] connectionpoolingObject = { new { pooling = new { maxSize = 2, queueTimeout = timeoutMS } }, new { pooling = new { queueTimeout = true } }, new { pooling = new { queueTimeout = "true" } }, new { pooling = new { queueTimeout = -1 } }, new { pooling = new { queueTimeout = 84584759345 } }, new { pooling = new { queueTimeout = "" } } };
      for (int i = 0; i < (inputType == "string" ? connectionpooling : connectionpoolingObject).Length; i++)
      {
        if (i == 0)
        {
          //Connection String
          using (var client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            var session1 = client1.GetSession();
            var session2 = client1.GetSession();
            Stopwatch stopwatch = Stopwatch.StartNew();
            Assert.Catch(() => client1.GetSession());
            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds >= timeoutMS);
          }

          //Uri
          using (var client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            var session1 = client1.GetSession();
            var session2 = client1.GetSession();
            Stopwatch stopwatch = Stopwatch.StartNew();
            Assert.Catch(() => client1.GetSession());
            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds >= timeoutMS);
          }

          //Anonymous Object
          using (var client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]))
          {
            var session1 = client1.GetSession();
            var session2 = client1.GetSession();
            Stopwatch stopwatch = Stopwatch.StartNew();
            Assert.Catch(() => client1.GetSession());
            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds >= timeoutMS);
          }
        }
        else
        {
          Assert.Catch(() => MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.Catch(() => MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
          Assert.Catch(() => MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        }
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Try to set a pooling option other than `enable`, `maxSize`, `maxIdleTime`, `queueTimeOut`. Expected to throw error.")]
    public void InvalidPoolingOption(string inputType)
    {
      string[] connectionpooling = { "{ \"pooling\": { \"minSize\": 1} }" };
      object[] connectionpoolingObject = { new { pooling = new { minSize = 1 } } };

      for (int i = 0; i < (inputType == "string" ? connectionpooling : connectionpoolingObject).Length; i++)
      {
        //Connection String
        Assert.Throws<ArgumentException>(() => MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        //Uri
        Assert.Throws<ArgumentException>(() => MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        //Anonymous object
        Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Try to set a client option other than `pooling`. Expected to throw error.")]
    public void InvalidConnectionOptions(string inputType)
    {
      string[] connectionpooling = { null, "", " ", "ten", "10000", "&" };
      object[] connectionpoolingObject = { null, "", " ", "ten", "10000", "&" };

      for (int i = 0; i < (inputType == "string" ? connectionpooling : connectionpoolingObject).Length; i++)
      {
        //Connection string
        Exception ex = Assert.Catch(() => MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        Assert.True(ex is ArgumentNullException || ex is ArgumentException);
        //Uri
        ex = Assert.Catch(() => MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        Assert.True(ex is ArgumentNullException || ex is ArgumentException);
        //Anonymous object
        ex = Assert.Catch(() => MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)[i]));
        Assert.True(ex is ArgumentNullException || ex is ArgumentException);
      }
    }

    [TestCase("string", 1)]
    [TestCase("object", 1)]
    [TestCase("string", 2)]
    [TestCase("object", 2)]
    [Test, Description("Verify that the pool is managed per host in case connection string includes multiple hosts having priority set and pool enabled")]
    public void MultipleHostsAndPriorityTests(string inputType, int hostNameOrIP)
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 5, \"queueTimeout\": 5000 } }";
      object connectionpoolingObject = new { pooling = new { maxSize = 5, queueTimeout = 20 } };
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      //Connection String
      StringBuilder hostList = new StringBuilder();
      var connString = string.Empty;
      var priority = 100;
      if (hostNameOrIP == 1) //Test with Host Names
      {
        for (var i = 1; i < 101; i++)
        {
          hostList.Append("(address=server" + i + ".example,priority=" + (priority != 0 ? priority-- : 0) + "),");
          if (i == 100) hostList.Append($"(address={sb.Server},priority=0)");
        }
        connString = "server=" + hostList + ";port=" + XPort + ";uid=" + sb.UserID + ";password=" + sb.Password;
      }
      else //Test with IP's
      {
        connString = "server=10.10.10.10," + sb.Server + ";port=" + XPort + ";uid=" + sb.UserID + ";password=" + sb.Password;
      }

      using (var client1 = MySQLX.GetClient(connString, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        using (var localSession = client1.GetSession())
        {
          var schema = localSession.GetSchema("test");
          Assert.IsNotNull(schema);
          schema.DropCollection("test123");
          var testColl = schema.CreateCollection("test123");
          Assert.IsNotNull(testColl);
          schema.DropCollection("test123");
        }
      }

      //Object
      Object connectionObject = null;
      if (hostNameOrIP == 1)
      {
        connectionObject = new
        {
          server = hostList.ToString(),
          port = XPort,
          user = sb.UserID,
          password = sb.Password,
          sslmode = MySqlSslMode.Required
        };

      }
      else
      {
        connectionObject = new { server = "10.10.10.10," + sb.Server, port = XPort, uid = sb.UserID, password = sb.Password };
      }
      using (var client1 = MySQLX.GetClient(connectionObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        using (var localSession = client1.GetSession())
        {
          var schema = localSession.GetSchema("test");
          Assert.IsNotNull(schema);
          schema.DropCollection("test123");
          var testColl = schema.CreateCollection("test123");
          Assert.IsNotNull(testColl);
          schema.DropCollection("test123");
        }
      }

      //Uri
      var connStringURI = string.Empty;
      if (hostNameOrIP == 1)
      {
        hostList.Replace("localhost", "localhost:" + XPort.ToString());
        connStringURI = "mysqlx://" + sb.UserID + ":" + sb.Password + "@[" + hostList + "]";
      }
      else
      {
        connStringURI = "mysqlx://" + sb.UserID + ":" + sb.Password + "@[192.1.10.10," + sb.Server + ":" + XPort + "]";
      }

      using (var client1 = MySQLX.GetClient(connStringURI, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        using (var localSession = client1.GetSession())
        {
          var schema = localSession.GetSchema("test");
          Assert.IsNotNull(schema);
          schema.DropCollection("test123");
          var testColl = schema.CreateCollection("test123");
          Assert.IsNotNull(testColl);
          schema.DropCollection("test123");
        }
      }
    }

    [Test, Description("Test that the connection taken from pool is clean and all the user variables, temp tables etc are cleaned up")]
    public void ValidateCleanUpVariables()
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 3, \"queueTimeout\": 2000 , \"maxIdleTime\":2000, \"enabled\": true} }";

      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpooling))
      {
        VerifyDataCleanupAfterClientClose(client1, 3, ConnectionString, connectionpooling);
      }
      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpooling))
      {
        VerifyDataCleanupAfterClientClose(client1, 3, ConnectionStringUri, connectionpooling);
      }
      using (var client1 = MySQLX.GetClient(connObject, connectionpooling))
      {
        VerifyDataCleanupAfterClientClose(client1, 3, ConnectionString, connectionpooling);
      }

    }

    [Test, Description("Test that if the pool doesn't have the sessions available and if tried to get the session then the client creates one.")]
    public void SessionCreatedByClient()
    {
      var connectionpooling = "{ \"pooling\": { \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true } };

      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpoolingObject))
      {
        var rs1 = client1.GetSession();
        Assert.AreEqual(SessionState.Open, rs1.InternalSession.SessionState);
      }

      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject))
      {
        client1.GetSession();
        var rs1 = client1.GetSession();
        Assert.AreEqual(SessionState.Open, rs1.InternalSession.SessionState);
      }

      using (var client1 = MySQLX.GetClient(connObject, connectionpoolingObject))
      {
        client1.GetSession();
        var rs1 = client1.GetSession();
        Assert.AreEqual(SessionState.Open, rs1.InternalSession.SessionState);
      }

      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpooling))
      {
        var rs1 = client1.GetSession();
        Assert.AreEqual(SessionState.Open, rs1.InternalSession.SessionState);
      }

      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpooling))
      {
        client1.GetSession();
        var rs1 = client1.GetSession();
        Assert.AreEqual(SessionState.Open, rs1.InternalSession.SessionState);
      }

      using (var client1 = MySQLX.GetClient(connObject, connectionpooling))
      {
        client1.GetSession();
        var rs1 = client1.GetSession();
        Assert.AreEqual(SessionState.Open, rs1.InternalSession.SessionState);
      }

    }

    [Test, Description("Test that the sessions in the pool increases from 0 to maxSize")]
    public void VerifyMaxSizeIsReached()
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"queueTimeout\": 2000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 1, queueTimeout = 2000, maxIdleTime = 1000 } };

      var client1 = MySQLX.GetClient(ConnectionString, connectionpoolingObject);
      client1.GetSession();
      TestClientQueueTimeout(client1, 1000, 3000);

      client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject);
      client1.GetSession();
      TestClientQueueTimeout(client1, 1000, 3000);

      client1 = MySQLX.GetClient(connObject, connectionpoolingObject);
      client1.GetSession();
      TestClientQueueTimeout(client1, 1000, 3000);

      client1 = MySQLX.GetClient(ConnectionString, connectionpooling);
      client1.GetSession();
      TestClientQueueTimeout(client1, 1000, 3000);
      client1 = MySQLX.GetClient(ConnectionStringUri, connectionpooling);
      client1.GetSession();
      TestClientQueueTimeout(client1, 1000, 3000);

      client1 = MySQLX.GetClient(connObject, connectionpooling);
      client1.GetSession();
      TestClientQueueTimeout(client1, 1000, 3000);

    }

    [Test, Description("Test that when the session is closed, it resets the session, removes all temporary variables etc, and adds it to the pool 1")]
    public void ResetSessionsAfterClose()
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 3, \"queueTimeout\": 1000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 3, queueTimeout = 1000, maxIdleTime = 1000 } };

      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpoolingObject))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
        TestClientQueueTimeout(client1, 0, 2000);
      }
      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
        TestClientQueueTimeout(client1, 0, 2000);
      }
      using (var client1 = MySQLX.GetClient(connObject, connectionpoolingObject))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
        TestClientQueueTimeout(client1, 0, 2000);
      }
      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpooling))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
        TestClientQueueTimeout(client1, 0, 2000);
      }
      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpooling))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
        TestClientQueueTimeout(client1, 0, 2000);
      }
      using (var client1 = MySQLX.GetClient(connObject, connectionpooling))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
        TestClientQueueTimeout(client1, 0, 2000);
      }

    }

    [Test, Description("Test that when the session is closed, it resets the session, removes all temporary variables etc, and adds it to the pool 2")]
    public void ResetSessionVariablesAfterClose()
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 3, \"queueTimeout\": 2000 , \"maxIdleTime\":2000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 3, queueTimeout = 2000, maxIdleTime = 2000 } };

      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpoolingObject))
      {
        VerifyDataCleanupOneSession(client1, 3);
      }
      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpoolingObject))
      {
        VerifyDataCleanupOneSession(client1, 3);
      }
      using (var client1 = MySQLX.GetClient(connObject, connectionpoolingObject))
      {
        VerifyDataCleanupOneSession(client1, 3);
      }
      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpooling))
      {
        VerifyDataCleanupOneSession(client1, 3);
      }
      using (var client1 = MySQLX.GetClient(ConnectionStringUri, connectionpooling))
      {
        VerifyDataCleanupOneSession(client1, 3);
      }
      using (var client1 = MySQLX.GetClient(connObject, connectionpooling))
      {
        VerifyDataCleanupOneSession(client1, 3);
      }

    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test that when the session is closed, it resets the session, removes all temporary variables etc, and adds it to the pool 3")]
    public void ValidateConnectionIDs(string inputType)
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 16))) return;
      Client client1 = null, client2 = null, client3 = null;
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"queueTimeout\": 2000 , \"maxIdleTime\":2000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 1, queueTimeout = 2000, maxIdleTime = 2000 } };
      //Connection string
      client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      client2 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      client3 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      CompareConnectionIDs(client1, client2, client3);
      //Uri
      client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      client2 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      client3 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      CompareConnectionIDs(client1, client2, client3);
      //Anonymous object
      client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      client2 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      client3 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject));
      CompareConnectionIDs(client1, client2, client3);
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test that when the session is closed, it resets the session, removes all temporary variables etc, and adds it to the pool 4")]
    public void ValidateConnectionIDsAndTimeouts(string inputType)
    {
      Client client1 = null;
      int timeout = 6000;
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"maxIdleTime\": 2000 } }";
      var connectionpoolingObject = new { pooling = new { maxSize = 1, maxIdleTime = 2000 } };
      //Connection String
      using (client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        CompareConnectionIDs(client1, timeout, true);
      }

      //Uri
      using (client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        CompareConnectionIDs(client1, timeout, true);
      }

      //Anoymous Object
      using (client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        CompareConnectionIDs(client1, timeout, true);
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test that when the session is closed, it resets the session, removes all temporary variables etc, and adds it to the pool 5")]
    public void CompareConnectionIDMaxTimeoutAndSleep(string inputType)
    {
      Client client1 = null;
      int timeout = 3000;
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"maxIdleTime\": 3000 } }";
      var connectionpoolingObject = new { pooling = new { maxSize = 1, maxIdleTime = timeout } };
      //Connection String
      using (client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        CompareConnectionIDs(client1, timeout, false, timeout);
      }
      //Uri
      using (client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        CompareConnectionIDs(client1, timeout, false, timeout);
      }
      //Anonymous object
      using (client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        CompareConnectionIDs(client1, timeout, false, timeout);
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Test that when the session is closed, it resets the session, removes all temporary variables etc, and adds it to the pool 6")]
    public void UpdatePoolingObjectAfterCreateSessions(string inputType)
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 3, \"queueTimeout\": 1000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      var connectionpoolingObject = new
      {
        pooling = new
        {
          enabled = true,
          maxSize = 3,
          queueTimeout = 1000,
          maxIdleTime = 1000
        }
      };

      using (var client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
      }

      connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"queueTimeout\": 2000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 1, queueTimeout = 2000, maxIdleTime = 1000 } };
      using (var client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        TestClientQueueTimeout(client1, 1000, 3000);
      }

      using (var client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        TestClientQueueTimeout(client1, 1000, 3000);
      }

      using (var client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        TestClientQueueTimeout(client1, 1000, 3000);
      }
    }

    [TestCase("string")]
    [TestCase("object")]
    [Test, Description("Try to set the pooling from True to False while the sessions are being opened by the client")]
    public void UpdateEnabledOptionWhileOpenSessions(string inputType)
    {
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 3, \"queueTimeout\": 1000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 3, queueTimeout = 1000, maxIdleTime = 1000 } };
      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpoolingObject))
      {
        client1.GetSession();
        client1.GetSession();
        client1.GetSession();
      }
      connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"queueTimeout\": 2000 , \"maxIdleTime\":1000, \"enabled\": false} }";
      connectionpoolingObject = new { pooling = new { enabled = false, maxSize = 1, queueTimeout = 2000, maxIdleTime = 1000 } };
      using (var client1 = MySQLX.GetClient(ConnectionString, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        client1.GetSession();
      }
      using (var client1 = MySQLX.GetClient(ConnectionStringUri, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        client1.GetSession();
      }
      using (var client1 = MySQLX.GetClient(connObject, (inputType == "string" ? connectionpooling : connectionpoolingObject)))
      {
        client1.GetSession();
        client1.GetSession();
      }
    }

    [Test, Description("Test MySqlX plugin reset connection state")]
    public void SessionResetConnectionState()
    {

      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 3, queueTimeout = 2000, maxIdleTime = 1000 } };
      using (var client1 = MySQLX.GetClient(ConnectionString, connectionpoolingObject))
      {
        var session1 = client1.GetSession();
        var session2 = client1.GetSession();
        var db1 = session1.GetSchema("test1");
        if (db1.ExistsInDatabase())
        {
          session1.DropSchema("test1");
          db1 = session1.CreateSchema("test1");
        }
        else { db1 = session1.CreateSchema("test1"); }

        var col1 = db1.GetCollection("my_collection1");
        if (col1.ExistsInDatabase())
        {
          db1.DropCollection("my_collection1");
          col1 = db1.CreateCollection("my_collection1");
        }
        else { col1 = db1.CreateCollection("my_collection1"); }
        col1 = db1.GetCollection("my_collection1", true);
        Assert.IsNotNull(col1);

        var db2 = session2.GetSchema("test2");
        if (db2.ExistsInDatabase())
        {
          session2.DropSchema("test2");
          db2 = session2.CreateSchema("test2");
        }
        else { db2 = session2.CreateSchema("test2"); }
        var col2 = db2.GetCollection("my_collection2");
        if (col2.ExistsInDatabase())
        {
          db2.DropCollection("my_collection2");
          col2 = db2.CreateCollection("my_collection2");
        }
        else { col2 = db2.CreateCollection("my_collection2"); }
        col2 = db2.GetCollection("my_collection2", true);
        Assert.IsNotNull(col2);
        session1.Close();

        DbDoc DbDocs1 = new DbDoc();
        DbDocs1.SetValue("title", "Book 0");
        DbDocs1.SetValue("pages", 10);

        db1 = session1.GetSchema("test1");
        Exception ex = Assert.Throws<MySqlException>(() => db1.ExistsInDatabase());
        StringAssert.Contains("Session state is not valid", ex.Message);

        var result = col2.Add(DbDocs1).Execute();
        Assert.IsNotNull(result);

        session1 = client1.GetSession();
        db1 = session1.GetSchema("test1");
        col1 = db1.GetCollection("my_collection1", true);
        result = col1.Add(DbDocs1).Execute();
        Assert.IsNotNull(result);
        session1.Close();

        db1 = session1.GetSchema("test1");
        ex = Assert.Throws<MySqlException>(() => db1.ExistsInDatabase());
        StringAssert.Contains("Session state is not valid", ex.Message);

        session2.DropSchema("test1");
        session2.DropSchema("test2");
        session2.Close();
        client1.Close();
      }
    }

    [Test, Description("MAX LENGTH OF KEY VALUE PAIR OF USER DEFINED CONNECTION ATTRIBUTES")]
    public void ConnectionAttributesLongValue()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");

      var serverSupportedSize = Convert.ToInt32(session.SQL("select @@Global.performance_schema_session_connect_attrs_size;").Execute().First()[0]);
      var connectAttributesLost = Convert.ToInt32(session.SQL("SHOW STATUS LIKE 'Performance_schema_session_connect_attrs_lost'").Execute().FirstOrDefault()[1]);
      string maxValue = "1234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678";
      var valueSize = System.Text.ASCIIEncoding.Unicode.GetByteCount(maxValue);
      if (serverSupportedSize > valueSize)
      {
        string maxKey = "f";
        string maxCombi = $"[{maxKey}={maxValue},a=bcdefghijk]";
        var userAttrs = new Dictionary<string, object>
        {
            {maxKey,maxValue},
            {"a","bcdefghijk"}
        };
        //Connection String
        using (var s1 = MySQLX.GetSession($"{ConnectionString};connectionattributes={maxCombi}"))
        {
          Assert.AreEqual(SessionState.Open, s1.InternalSession.SessionState);
          var truncatedValues = (int)session.SQL("SHOW STATUS LIKE 'Performance_schema_session_connect_attrs_lost'").Execute().First()[0];
          Assert.True(truncatedValues > connectAttributesLost);
        }
        //Uri
        using (var s1 = MySQLX.GetSession($"{ConnectionStringUri}?connectionattributes={maxCombi}"))
        {
          Assert.AreEqual(SessionState.Open, s1.InternalSession.SessionState);
          var truncatedValues = (int)session.SQL("SHOW STATUS LIKE 'Performance_schema_session_connect_attrs_lost'").Execute().First()[0];
          Assert.True(truncatedValues > connectAttributesLost);
        }
        //Anonymous Object
        using (var s1 = MySQLX.GetSession(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          ConnectionAttributes = maxCombi
        }))
        {
          Assert.AreEqual(SessionState.Open, s1.InternalSession.SessionState);
          var truncatedValues = (int)session.SQL("SHOW STATUS LIKE 'Performance_schema_session_connect_attrs_lost'").Execute().First()[0];
          Assert.True(truncatedValues > connectAttributesLost);
        }
      }
    }

    [Test, Description("GETSESSION THROWS EXCEPTION WHEN EQUAL IS USED AS PARAM FOR CONN ATTRIBUTE IN CONN ANONYMOUS OBJECT-WL#12514")]
    public void GetSessionExceptionWithConnetionAttribute()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "=" }
      , "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual("The requested value '=' is invalid for the given keyword 'connection-attributes'.", ex.Message);
      }
    }

    [Test, Description("Connection Attributes with arrays")]
    public void ConnectionAttributesWithArrays()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");

      object[] arrayCases = new object[] { "[var1 = 1, 2, 3]", "[var1 = { 1, 2, 3}]" };
      for (int i = 0; i < arrayCases.Length; i++)
      {
        //Connection string
        using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=" + arrayCases[i], "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session1 = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
        }
        //Uri
        using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=" + arrayCases[i], "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session1 = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
        }
        //Anonymous Object
        MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
        using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = arrayCases[i] }, "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session1 = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
        }
        //MySqlXConnectionStringBuilder
        MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.Database = schemaName;
        mysqlx0.Port = Convert.ToUInt32(XPort);
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = arrayCases[i].ToString();
        using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session1 = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          }
        }
        //Connection string
        using (Session session1 = MySQLX.GetSession(ConnectionString + ";connection-attributes=" + arrayCases[i]))
        {
          Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        }
        //Uri
        using (Session session1 = MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=" + arrayCases[i]))
        {
          Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        }
        //Anonymous Object
        sb = new MySqlXConnectionStringBuilder(ConnectionString);
        using (Session session1 = MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = arrayCases[i] }))
        {
          Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        }
        //MySqlXConnectionStringBuilder
        mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.Database = schemaName;
        mysqlx0.Port = Convert.ToUInt32(XPort);
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = arrayCases[i].ToString();
        using (Session session1 = MySQLX.GetSession(mysqlx0.ConnectionString))
        {
          Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        }
      }
    }

    [Test]
    public void NormalConnectionWithUri()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      //Normal connection with URI
      using (Client client = MySQLX.GetClient(ConnectionStringUri, "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session1 = client.GetSession())
        {
          Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        }
      }
    }

    [Test, Description("Connection Attributes Repeated - Extending scenarios in ConnectionAttributes()")]
    [Ignore("Bug #33234243 need to fix Uri scenario")]
    public void ConnectionAttributesRepeated()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      //Connection String
      Assert.Catch(() => MySQLX.GetClient(ConnectionString + ";connection-attributes=true;connection-attributes=true;", "{ \"pooling\": { \"enabled\": true } }"));
      Assert.Catch(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=true;connection-attributes=true;"));
      //Uri
      Assert.Catch(() => MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=true&connection-attributes=true", "{ \"pooling\": { \"enabled\": true } }"));
      Assert.Catch(() => MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=true?connection-attributes=true;"));
    }

    [Test, Description("Connection Attributes with key Repeated - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesKeyRepeatedChars()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      var expectedMsg = string.Format(ResourcesX.DuplicateUserDefinedAttribute, "quua");
      //Connection String
      using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=[quua=bar,quua=qux,key]", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Uri
      using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=[quua=bar,quua=qux,key];", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Anonymous Object
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[quua=bar,quua=qux,key=]" }, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      if (Platform.IsWindows())
      {
        //MySqlXConnectionStringBuilder
        MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.Database = schemaName;
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = "[quua=bar,quua=qux,key=]";
        using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
        {
          Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
          Assert.AreEqual(expectedMsg, ex.Message);
        }
        //Connection String
        Exception ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[quua=bar,quua=qux,key]"));
        Assert.AreEqual(expectedMsg, ex1.Message);
        //Uri
        ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=[quua=bar,quua=qux,key];"));
        Assert.AreEqual(expectedMsg, ex1.Message);
        //Anonymous Object
        sb = new MySqlXConnectionStringBuilder(ConnectionString);
        ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[quua=bar,quua=qux,key=]" }));
        Assert.AreEqual(expectedMsg, ex1.Message);
        //MySqlXConnectionStringBuilder
        mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.Database = schemaName;
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = "[quua=bar,quua=qux,key=]";
        ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(mysqlx0.ConnectionString));
        Assert.AreEqual(expectedMsg, ex1.Message);
      }
    }

    [Test, Description("Connection Attributes with key 33 characters - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesWithKey33Chars()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      var errorMsg = "Key name beginning with 'foo32foo32foo32foo32foo32foo3232'... is too long, currently limited to 32";
      using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key]", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(errorMsg, ex.Message);
      }

      using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key];", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(errorMsg, ex.Message);
      }

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key=]" }, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(errorMsg, ex.Message);
      }

      if (Platform.IsWindows())
      {
        MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = "[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key=]";
        using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
        {
          Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
          Assert.AreEqual(errorMsg, ex.Message);
        }

        Exception ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key]"));
        Assert.AreEqual(errorMsg, ex1.Message);

        ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key];"));
        Assert.AreEqual(errorMsg, ex1.Message);

        sb = new MySqlXConnectionStringBuilder(ConnectionString);
        ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key=]" }));
        Assert.AreEqual(errorMsg, ex1.Message);

        mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = "[foo32foo32foo32foo32foo32foo32323=bar,quua=qux,key=]";
        ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(mysqlx0.ConnectionString));
        Assert.AreEqual(errorMsg, ex1.Message);
      }
    }

    [Test, Description("Connection Attributes with invalid combinations - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesInvalidCombinations()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      object[] invalid = new object[] { "var1", "1", "2", "(var1)", "{var1}", "[_testValue = test123, emptyValue]" };
      var errorMsgs = new string[] { @"The value of ""connection-attributes"" must be either a boolean or a list of key-value pairs.", @"Key names in ""connection-attributes"" cannot start with ""_""." };
      for (int i = 0; i < invalid.Length; i++)
      {
        //Connection String
        using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=" + invalid[i], "{ \"pooling\": { \"enabled\": true } }"))
        {
          Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
          Assert.True(errorMsgs.Contains(ex.Message));
        }
        //Uri
        using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=" + invalid[i], "{ \"pooling\": { \"enabled\": true } }"))
        {
          Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
          Assert.True(errorMsgs.Contains(ex.Message));
        }
        //Anonymous object
        MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
        using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = invalid[i] }, "{ \"pooling\": { \"enabled\": true } }"))
        {
          Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
          Assert.True(errorMsgs.Contains(ex.Message));
        }

        if (Platform.IsWindows())
        {
          //MySqlXConnectionStringBuilder
          MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
          mysqlx0.Database = schemaName;
          mysqlx0.CharacterSet = "utf8mb4";
          mysqlx0.SslMode = MySqlSslMode.Required;
          mysqlx0.ConnectTimeout = 10;
          mysqlx0.Keepalive = 10;
          mysqlx0.CertificateFile = sslCa;
          mysqlx0.CertificatePassword = sslCertificatePassword;
          mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
          mysqlx0.CertificateThumbprint = "";
          mysqlx0.ConnectionAttributes = invalid[i].ToString();
          using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
          {
            Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
            Assert.True(errorMsgs.Contains(ex.Message));
          }

          //Connection String
          Exception ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=" + invalid[i]));
          Assert.True(errorMsgs.Contains(ex1.Message));
          //Uri
          ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=" + invalid[i]));
          Assert.True(errorMsgs.Contains(ex1.Message));
          //Anonymous object
          sb = new MySqlXConnectionStringBuilder(ConnectionString);
          ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = invalid[i] }));
          Assert.True(errorMsgs.Contains(ex1.Message));
          //MySqlXConnectionStringBuilder
          mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
          mysqlx0.Database = schemaName;
          mysqlx0.CharacterSet = "utf8mb4";
          mysqlx0.SslMode = MySqlSslMode.Required;
          mysqlx0.ConnectTimeout = 10;
          mysqlx0.Keepalive = 10;
          mysqlx0.CertificateFile = sslCa;
          mysqlx0.CertificatePassword = sslCertificatePassword;
          mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
          mysqlx0.CertificateThumbprint = "";
          mysqlx0.ConnectionAttributes = invalid[i].ToString();
          ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(mysqlx0.ConnectionString));
          Assert.True(errorMsgs.Contains(ex1.Message));
        }
      }
    }

    [Test, Description("Connection Attributes with valid combinations - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesValidCombinations()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");

      object[] invalid = new object[] { "[var1 = 1]" };
      for (int i = 0; i < invalid.Length; i++)
      {
        using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=" + invalid[i], "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
          }
        }

        using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=" + invalid[i], "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
          }
        }

        MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
        using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = invalid[i] }, "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
          }
        }

        MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.Database = schemaName;
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = invalid[i].ToString();
        using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
        {
          using (Session session = client.GetSession())
          {
            Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
          }
        }

        using (Session session = MySQLX.GetSession(ConnectionString + ";connection-attributes=" + invalid[i]))
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }

        using (Session session = MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=" + invalid[i]))
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }

        sb = new MySqlXConnectionStringBuilder(ConnectionString);
        using (Session session = MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = invalid[i] }))
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }

        mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
        mysqlx0.Database = schemaName;
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.Required;
        mysqlx0.ConnectTimeout = 10;
        mysqlx0.Keepalive = 10;
        mysqlx0.CertificateFile = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectionAttributes = invalid[i].ToString();

        using (Session session = MySQLX.GetSession(mysqlx0.ConnectionString))
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }
      }
    }

    [Test, Description("Connection Attributes with key special characters - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesKeySpecialChars()
    {
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=[@#$%^&*        %^()=bar,quua=*(&^&#$%,key]", "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session = client.GetSession())
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }
      }

      using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=[^%&&*^#623%^%33=bar,quua=*(&^&#$%,key]", "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session = client.GetSession())
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }
      }

      using (Client client = MySQLX.GetClient(new
      {
        server = sb.Server,
        port = XPort,
        user = sb.UserID,
        password = sb.Password,
        ConnectionAttributes = "[@#$%^&*()=bar,quua=*(&^&#$%,key=]"
      }, "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session = client.GetSession())
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }
      }

      MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = "[@#$%^&*()=bar,quua=*(&^&#$%,key=]";
      using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
      {
        using (Session session = client.GetSession())
        {
          Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
        }
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";connection-attributes=[@#$%^&*()=bar,quua=*(&^&#$%,key]"))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }

      using (Session session = MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[@#$%^&*()=bar,quua=*(&^&#$%,key=]" }))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }

      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = "[@#$%^&*()=bar,quua=*(&^&#$%,key=]";
      using (Session session = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }

    }

    [Test, Description("Connection Attributes with key with underline characters - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesKeyWithUnderLineChars()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var expectedMsg = @"Key names in ""connection-attributes"" cannot start with ""_"".";
      //Connection string
      using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=[_foo32=bar,quua=qux,key]", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Uri
      using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=[_foo32=bar,quua=qux,key];", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Anonymous object
      using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[_foo32=bar,quua=qux,key=]" }, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = "[_foo32=bar,quua=qux,key=]";
      using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      //Connection string
      Exception ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[_foo32=bar,quua=qux,key]"));
      Assert.AreEqual(expectedMsg, ex1.Message);
      //Uri
      ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=[_foo32=bar,quua=qux,key];"));
      Assert.AreEqual(expectedMsg, ex1.Message);
      //Anonymous object
      ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[_foo32=bar,quua=qux,key=]" }));
      Assert.AreEqual(expectedMsg, ex1.Message);
      //MySqlXConnectionStringBuilder
      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = "[_foo32=bar,quua=qux,key=]";
      ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(mysqlx0.ConnectionString));
      Assert.AreEqual(expectedMsg, ex1.Message);

    }

    [Test, Description("Connection Attributes with key blank characters - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesKeyBlankChars()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var expectedMsg = "Key name in connection attribute cannot be an empty string.";
      //Connection String
      using (Client client = MySQLX.GetClient(ConnectionString + ";connection-attributes=[=bar,quua=qux,key]", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      //Uri
      using (Client client = MySQLX.GetClient(ConnectionStringUri + "?connection-attributes=[=bar,quua=qux,key];", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      //Anonymous object
      using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[=bar,quua=qux,key=]" }, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      //MySqlXConnectionStringBuilder
      MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = "[=bar,quua=qux,key=]";
      using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      //Connection String
      Exception ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[=bar,quua=qux,key]"));
      Assert.AreEqual(expectedMsg, ex1.Message);
      //Uri
      ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?connection-attributes=[=bar,quua=qux,key];"));
      Assert.AreEqual(expectedMsg, ex1.Message);
      //Anonymous object
      ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = "[=bar,quua=qux,key=]" }));
      Assert.AreEqual(expectedMsg, ex1.Message);
      //MySqlXConnectionStringBuilder
      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = "[=bar,quua=qux,key=]";
      ex1 = Assert.Throws<MySqlException>(() => MySQLX.GetSession(mysqlx0.ConnectionString));
      Assert.AreEqual(expectedMsg, ex1.Message);

    }

    [Test, Description("Connection Attributes with value 1025characters - Extending scenarios in ConnectionAttributes()")]
    public void ConnectionAttributesValue1025Chars()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var expectedMsg = "Value is too long for 'foo' attribute, currently limited to 1024";
      var strValue = "[foo=12345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781234567812345678123456781,quua=qux,key=]";
      //Connection String
      using (Client client = MySQLX.GetClient(ConnectionString + $";connection-attributes={strValue}", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Catch(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Uri
      using (Client client = MySQLX.GetClient(ConnectionStringUri + $"?connection-attributes={strValue};", "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Catch(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Anonymous object
      using (Client client = MySQLX.GetClient(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = strValue }, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Catch(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }

      //MySqlXConnectionStringBuilder
      MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = strValue;
      using (Client client = MySQLX.GetClient(mysqlx0.ConnectionString, "{ \"pooling\": { \"enabled\": true } }"))
      {
        Exception ex = Assert.Catch(() => client.GetSession());
        Assert.AreEqual(expectedMsg, ex.Message);
      }
      //Connection String
      Exception ex2 = Assert.Catch(() => MySQLX.GetSession(ConnectionString + $";connection-attributes={strValue}"));
      Assert.AreEqual(expectedMsg, ex2.Message);
      //Uri
      ex2 = Assert.Catch(() => MySQLX.GetSession(ConnectionStringUri + $"?connection-attributes={strValue};"));
      Assert.AreEqual(expectedMsg, ex2.Message);
      //Anonymous object
      ex2 = Assert.Catch(() => MySQLX.GetSession(new { server = sb.Server, port = XPort, user = sb.UserID, password = sb.Password, ConnectionAttributes = strValue }));
      Assert.AreEqual(expectedMsg, ex2.Message);
      //MySqlXConnectionStringBuilder
      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectionAttributes = strValue;
      ex2 = Assert.Catch(() => MySQLX.GetSession(mysqlx0.ConnectionString));
      Assert.AreEqual(expectedMsg, ex2.Message);

    }

    [Test, Description("Supports session restore functionality")]
    public void SessionRestore()
    {
      // This feature was implemented since MySQL Server 8.0.16
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      int size = 2;
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { enabled = true, maxSize = size, queueTimeout = 1000, maxIdleTime = 1000 } }))
      {
        Session session1 = client.GetSession();
        string threadId1 = session1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        session1.SQL("SET character_set_connection = 'big5'").Execute();
        var res0 = session1.SQL("show variables like 'character_set_connection'").Execute();
        if (res0.HasData)
        {
          var row = res0.FetchOne();
          Assert.AreEqual("big5", row[1].ToString());
        }
        session1.Close();
        Session session2 = client.GetSession();
        string threadId2 = session2.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        Assert.True(threadId1.Equals(threadId2));
        session2.Close();
      }
    }

    [Test, Description("Supports new session auth functionality")]
    public void SessionAuthentication()
    {
      // This feature was implemented since MySQL Server 8.0.16
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      int size = 1;
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { enabled = true, maxSize = size, queueTimeout = 1000, maxIdleTime = 1000 } }))
      {
        Session session1 = client.GetSession();
        string threadId1 = session1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        session1.Close();
        Thread.Sleep(2000);
        Session session2 = client.GetSession();
        string threadId2 = session2.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        if (threadId1.Equals(threadId2))
        {
          Assert.Fail("Connection ID should have been different because of the Sleep(Timeout) introduced");
        }
        session2.Close();
      }
    }

    [Test, Description("Supports new session auth functionality")]
    public void MultiSessionAuthentication()
    {
      // This feature was implemented since MySQL Server 8.0.16
      if (!(session.Version.isAtLeast(8, 0, 16))) Assert.Ignore("This test is for MySql 8.0.16 or higher");
      int size = 2;
      using (Client client = MySQLX.GetClient(ConnectionString + ";database=test;", new { pooling = new { enabled = true, maxSize = size, queueTimeout = 1000, maxIdleTime = 1000 } }))
      {
        Session session1 = client.GetSession();
        string threadId1 = session1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        Session session2 = client.GetSession();
        string threadId2 = session2.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        if (threadId1.Equals(threadId2))
        {
          Assert.Fail("Connection ID should have been different because pool should contain two sessions");
        }
        session1.Close();
        session2.Close();

        Session session1_1 = client.GetSession();
        Session session2_1 = client.GetSession();
        string threadId3 = session1_1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        string threadId4 = session2_1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        if (!threadId1.Equals(threadId3))
        {
          Assert.Fail("Connection ID should have been same as session is closed and reopened without any sleep(timeout)");
        }
        if (!threadId2.Equals(threadId4))
        {
          Assert.Fail("Connection ID should have been same as session is closed and reopened without any sleep(timeout)");
        }
        session1_1.Close();
        session2_1.Close();
        //Timeout
        Thread.Sleep(2000);
        Session session3_1 = client.GetSession();
        Session session4_1 = client.GetSession();
        string threadId5 = session3_1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        string threadId6 = session4_1.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        Assert.False(threadId3.Equals(threadId5));
        Assert.False(threadId4.Equals(threadId6));
      }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Verify cleanup after client closing
    /// </summary>
    /// <param name="_client1"></param>
    /// <param name="iteration"></param>
    /// <param name="connectionString"></param>
    /// <param name="clientOptions"></param>
    public void VerifyDataCleanupAfterClientClose(Client _client1, int iteration, string connectionString, string clientOptions)
    {

      Session sess0 = null;
      for (int i = 0; i < iteration; i++)
      {
        sess0 = _client1.GetSession();
      }
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      var res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      _client1.Close();
      _client1 = MySQLX.GetClient(connectionString, clientOptions);
      Session sess4 = _client1.GetSession();
      res0 = sess4.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
    }

    /// <summary>
    /// Verify cleanup after client closing
    /// </summary>
    /// <param name="client1"></param>
    /// <param name="iteration"></param>
    public void VerifyDataCleanupOneSession(Client client1, int iteration)
    {
      Session sess0 = null;
      for (int i = 0; i < iteration; i++)
      {
        sess0 = client1.GetSession();
      }
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      var res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      sess0.Close();
      Session sess4 = client1.GetSession();
      res0 = sess4.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
    }

    /// <summary>
    /// Compare ConnectionIDs with max timeout
    /// </summary>
    /// <param name="client1"></param>
    /// <param name="maxTimeout"></param>
    /// <param name="maxTimeoutGreater"></param>
    public void CompareConnectionIDs(Client client1, int maxTimeout, bool maxTimeoutGreater)
    {
      string connectionID1 = null, connectionID2 = null;
      int cID1 = 0, cID2 = 0, cID3 = 0;
      Session sess0 = client1.GetSession();
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      var res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID1 = row[0].ToString();
        cID1 = Int32.Parse(connectionID1);
        cID3 = cID1 + 1;
      }
      sess0.Close();
      sess0 = client1.GetSession();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID2 = row[0].ToString();
        cID2 = Int32.Parse(connectionID2);
      }
      sess0.Close();
      cID1 = 0; cID2 = 0; cID3 = 0;
      sess0 = client1.GetSession();
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID1 = row[0].ToString();
        cID1 = Int32.Parse(connectionID1);
        cID3 = cID1 + 1;
      }
      sess0.Close();
      Thread.Sleep(maxTimeout);
      sess0 = client1.GetSession();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID2 = row[0].ToString();
        cID2 = Int32.Parse(connectionID2);
      }
      sess0.Close();
      if (maxTimeoutGreater)
      {
        Assert.False(connectionID1.Equals(connectionID2));
      }
      if (maxTimeoutGreater)
      {
        Assert.AreNotEqual(cID3, cID2);
      }
      else
      {
        Assert.True(cID3 == cID2);
      }
    }

    /// <summary>
    /// Compare ConnectionIDs with close of session and client in between
    /// </summary>
    /// <param name="client1"></param>
    /// <param name="client2"></param>
    /// <param name="client3"></param>
    public void CompareConnectionIDs(Client client1, Client client2, Client client3)
    {
      string connectionID1 = null, connectionID2 = null;
      Session sess0 = client1.GetSession();
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      var res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID1 = row[0].ToString();
      }
      sess0.Close();
      sess0 = client1.GetSession();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID2 = row[0].ToString();
      }
      sess0.Close();
      if (!connectionID1.Equals(connectionID2))
      {
        Assert.Fail("Connection ID Matches when a Client1 session0 is closed and opened again");
      }

      Session sess = client2.GetSession();
      sess.SQL("SET character_set_connection = 'big5'").Execute();
      var res = sess.SQL("show variables like 'character_set_connection'").Execute();
      if (res.HasData)
      {
        var row = res.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      res = sess.SQL("SELECT CONNECTION_ID()").Execute();
      if (res.HasData)
      {
        var row = res.FetchOne();
        connectionID1 = row[0].ToString();
      }
      client2.Close();
      sess = client3.GetSession();
      res = sess.SQL("show variables like 'character_set_connection'").Execute();
      if (res.HasData)
      {
        var row = res.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
      res = sess.SQL("SELECT CONNECTION_ID()").Execute();
      if (res.HasData)
      {
        var row = res.FetchOne();
        connectionID2 = row[0].ToString();
      }
      sess.Close();
      Assert.False(connectionID1.Equals(connectionID2));
    }

    /// <summary>
    ///  Compare ConnectionIDs with close of session with maxTimeout introduced as a sleep
    /// </summary>
    /// <param name="client1"></param>
    /// <param name="maxTimeout"></param>
    public void CompareConnectionIDs(Client client1, int maxTimeout, bool maxTimeoutGreater, int sleepTimeout)
    {
      string connectionID1 = null, connectionID2 = null;
      int cID1 = 0, cID2 = 0, cID3 = 0;
      Session sess0 = client1.GetSession();
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      var res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID1 = row[0].ToString();
        cID1 = Int32.Parse(connectionID1);
        cID3 = cID1 + 1;
      }
      sess0.Close();
      sess0 = client1.GetSession();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID2 = row[0].ToString();
        cID2 = Int32.Parse(connectionID2);
      }
      sess0.Close();
      cID1 = 0; cID2 = 0; cID3 = 0;
      sess0 = client1.GetSession();
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("big5", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID1 = row[0].ToString();
        cID1 = Int32.Parse(connectionID1);
        cID3 = cID1 + 1;
      }
      sess0.Close();
      Thread.Sleep(maxTimeout);
      sess0 = client1.GetSession();
      res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        Assert.AreEqual("utf8mb4", row[1].ToString());
      }
      res0 = sess0.SQL("SELECT CONNECTION_ID()").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
        connectionID2 = row[0].ToString();
        cID2 = Int32.Parse(connectionID2);
      }
      sess0.Close();
      if (maxTimeoutGreater)
      {
        Assert.False(connectionID1.Equals(connectionID2), "Connection ID Matches when a session is closed and opened again");
      }
      if (maxTimeout.CompareTo(sleepTimeout) > 0)
      {
        Assert.True(cID3 != cID2);
      }
      else if (maxTimeout.CompareTo(sleepTimeout) == 0)
      {
        Assert.True(cID1 == cID2);
      }
      else
      {
        Assert.True(cID1 == cID2);
      }

    }

    /// <summary>
    /// TestClientQueueTimeout for a client
    /// </summary>
    /// <param name="client"></param>
    /// <param name="minTime"></param>
    /// <param name="maxTime"></param>
    public void TestClientQueueTimeout(Client client, int minTime, int maxTime)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      Exception ex = Assert.Throws<TimeoutException>(() => client.GetSession());
      StringAssert.Contains("timeout", ex.Message);
      stopwatch.Stop();
      Assert.True(stopwatch.ElapsedMilliseconds > minTime && stopwatch.ElapsedMilliseconds < maxTime);
    }

    /// <summary>
    /// Set and check the variable character_set_connection
    /// </summary>
    /// <param name="client1"></param>
    public static void VerifyClient(Client client1)
    {
      Session sess0 = client1.GetSession();
      sess0.SQL("SET character_set_connection = 'big5'").Execute();
      var res0 = sess0.SQL("show variables like 'character_set_connection'").Execute();
      if (res0.HasData)
      {
        var row = res0.FetchOne();
      }
      sess0.Close();
      client1.Close();
    }

    /// <summary>
    /// Check the variable character_set_connection in async way
    /// </summary>
    /// <param name="client1"></param>
    public async void VerifyClientAsync(Client client1)
    {
      Session sess0 = client1.GetSession();
      var res0 = await sess0.SQL("show variables like 'character_set_connection'").ExecuteAsync();
      if (res0.ColumnCount > 1)
      {
        var row = res0.FetchOne();
      }
      sess0.Close();
      client1.Close();
    }

    #endregion Methods

  }
}