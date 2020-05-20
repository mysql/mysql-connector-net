// Copyright (c) 2018, 2020, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace MySqlX.Data.Tests
{
  public class ClientTests : BaseTest
  {
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
      new MultiHostData { ConnectionData = $"server=10.10.10.10,127.0.0.1;port={XPort};user=root;connecttimeout={_connectionTimeout};" },
      new MultiHostData { ConnectionData = $"server=unknown,localhost;port={XPort};user=root;connecttimeout={_connectionTimeout};" },
      new MultiHostData { ConnectionData = $"server=(address=10.10.10.10,priority=20),(address=127.0.0.1,priority=100);port={XPort};user=root;connecttimeout={_connectionTimeout};" },
      new MultiHostData { ConnectionData = $"mysqlx://root@[10.10.10.10,127.0.0.1:{XPort}]?connecttimeout={_connectionTimeout}" },
      new MultiHostData { ConnectionData = $"mysqlx://root@[unknown,localhost:{XPort}]?connecttimeout={_connectionTimeout}" },
      new MultiHostData { ConnectionData = $"mysqlx://root@[(address=10.10.10.10,priority=20),(address=127.0.0.1:{XPort},priority=100)]?connecttimeout={_connectionTimeout}" },
      new MultiHostData { ConnectionData = new { server = "10.10.10.10,127.0.0.1", user = "root", port = XPort, connecttimeout = _connectionTimeout } },
      new MultiHostData { ConnectionData = new { server = "unknown,localhost", user = "root", port = XPort, connecttimeout = _connectionTimeout } },
      new MultiHostData { ConnectionData = new { server = "(address=10.10.10.10,priority=100), (address=20.20.20.20,priority=90), (address=127.0.0.1,priority=20)", user = "root", port = XPort, connecttimeout = _connectionTimeout } }
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
      Assert.AreEqual(string.Format("Table 'test.testresetsession{0}' doesn't exist", id), ex.Message);

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
      if (!(session.Version.isAtLeast(8, 0, 16))) return;

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
      builder.Server = "localhost";
      builder.Port = 33060;
      builder.UserID = "root";
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
  }
}