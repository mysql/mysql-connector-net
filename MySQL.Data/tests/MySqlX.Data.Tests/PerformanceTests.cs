// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using System.Collections.Generic;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Provides methods designed to identify performance issues. Tests in this class are only meant to be ran
  /// when working with Performance Profiling.
  /// </summary>
  public class PerformanceTests
  {
    private const string PERFORMANCE_SCHEMA = "PerfSchema";
    private const string HOST = "localhost";
    private const string USER = "root";
    private const int X_DEVAPI_PORT = 33060;

    private static Session _session;
    private static string _connectionString;
    private static string _connectionURI;
    private static Schema _schema;
    private static DbDoc _emptyDocument;
    
    public PerformanceTests()
    {
      _connectionString = $"server={HOST};user={USER};port={X_DEVAPI_PORT};database={PERFORMANCE_SCHEMA}";
      _connectionURI = $"mysqlx://{USER}:@{HOST}:{X_DEVAPI_PORT}?database={PERFORMANCE_SCHEMA}";

      _session = MySQLX.GetSession($"server={HOST};user={USER};port={X_DEVAPI_PORT};");
      _schema = _session.GetSchema(PERFORMANCE_SCHEMA);
      if (_schema.ExistsInDatabase())
        _session.DropSchema(PERFORMANCE_SCHEMA);

      _schema = _session.CreateSchema(PERFORMANCE_SCHEMA);
      _session.SetCurrentSchema(PERFORMANCE_SCHEMA);

      _emptyDocument = new DbDoc();
    }

    #region CreateAndCloseSession

    public void SessionCreateWithConnectionString()
    {
      using (var internalSession = MySQLX.GetSession(_connectionString))
      { }

      _session = MySQLX.GetSession(_connectionString);
      _session.Close();
    }

    public void SessionCreateWithURI()
    {
      using (var internalSession = MySQLX.GetSession(_connectionURI))
      { }

      _session = MySQLX.GetSession(_connectionURI);
      _session.Close();
    }

    public void SessionCreateWithAnonymousObject()
    {
      var connectionObject = new { server = HOST, user = USER, port = X_DEVAPI_PORT };
      using (var internalSession = MySQLX.GetSession(connectionObject))
      { }

      _session = MySQLX.GetSession(connectionObject);
      _session.Close();
    }

    public void SessionCreateWithComplexConnectionString()
    {
      var connectionString = _connectionString + ";auth=AUTO;charset=utf8mb4;sslmode=required";
      using (var internalSession = MySQLX.GetSession(connectionString))
      { }

      _session = MySQLX.GetSession(_connectionString);
      _session.Close();
    }

    public void SessionCreateWithComplexConnectionURI()
    {
      var connectionUri = _connectionURI + "&auth=AUTO&charset=utf8mb4&sslmode=required";
      using (var internalSession = MySQLX.GetSession(connectionUri))
      { }

      _session = MySQLX.GetSession(_connectionURI);
      _session.Close();
    }

    public void SessionCreateWithComplexAnonymousObject()
    {
      var connectionObject = new
      {
        server = HOST,
        user = USER,
        port = X_DEVAPI_PORT,
        auth = MySqlAuthenticationMode.AUTO,
        charset = "utf8mb4",
        sslmode = MySqlSslMode.Required
      };
      using (var internalSession = MySQLX.GetSession(connectionObject))
      { }

      _session = MySQLX.GetSession(connectionObject);
      _session.Close();
    }

    #endregion

    #region RawSQL

    public void SQLRaw()
    {
      ExecuteSQL("CREATE TABLE test(a VARCHAR(255), b INT, c DATE, d BIT, e TINYINT(1))");
      ExecuteSQL("CREATE TABLE test2(a VARCHAR(255), b INT, c DATE, d BIT, e TINYINT(1))");
      ExecuteSQL("CREATE TABLE test3(a VARCHAR(255), b INT, c DATE, d BIT, e TINYINT(1))");
      for (int i = 0; i < 10; i++)
      {
        ExecuteSQL("INSERT INTO test VALUES('a', 1, '2018-01-01', 1, 0)");
        ExecuteSQL("INSERT INTO test2 VALUES('a', 1, '2018-01-01', 1, 0)");
        ExecuteSQL("INSERT INTO test3 VALUES('a', 1, '2018-01-01', 1, 0)");
      }
      ExecuteSQL("SELECT * FROM test");
      ExecuteSQL("SELECT * FROM test2");
      ExecuteSQL("SELECT * FROM test3");
      ExecuteSQL("DROP TABLE test");
      ExecuteSQL("DROP TABLE test2");
      ExecuteSQL("DROP TABLE test3");
      ExecuteSQL("SHOW DATABASES");
      ExecuteSQL($"USE {PERFORMANCE_SCHEMA}");
      ExecuteSQL($"SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{PERFORMANCE_SCHEMA}';");
      ExecuteSQL("START TRANSACTION");
      ExecuteSQL("ROLLBACK");
    }

    private SqlResult ExecuteSQL(string sql)
    {
      return _session.SQL(sql).Execute();
    }

    #endregion

    public void SchemaGetCollectionValidatingExistence()
    {
      var collection = _schema.CreateCollection("test", true);
      collection = _schema.GetCollection("test", true);
    }

    public void SchemaGetCollectionNotValidatingExistence()
    {
      var collection = _schema.CreateCollection("test");
      collection = _schema.GetCollection("test");
    }

    public void SchemaExistsInDatabase()
    {
      _schema.ExistsInDatabase();
    }

    public void DbDocCreateEmpty()
    {
      var document = new DbDoc();
    }

    public void DbDocCreateWithJSONString()
    {
      var document = new DbDoc("{ \"_id\": 1, \"title\": \"Book 1\", \"pagess\":10, \"author\":\"Ana\" }");
    }

    public void DbDocCreateWithAnonymousObject()
    {
      var document = new DbDoc(new { _id = 2, title = "Book 2", pages = 20, author = "John" });
    }

    public void DbDocCreateWithDbDocObject()
    {
      var document = _emptyDocument;
      document.Id = 3;
      document.SetValue("title", "Book 3");
      document.SetValue("pages", 30);
      document.SetValue("author", "Pedro");
      document = new DbDoc(document);
    }

    public void DbDocCreateWithDictionary()
    {
      var dictionary = new Dictionary<string, object>();
      dictionary.Add("title", "Book 4");
      dictionary.Add("pages", 40);
      dictionary.Add("author", "Laura");
      var document = new DbDoc(dictionary);
    }

    public void DbDocToString()
    {
      var document = new DbDoc("{ \"_id\": 1, \"title\": \"Book 1\", \"pagess\":10, \"author\":\"Ana\" }");
      document.ToString();
    }

    public void DbDocEquals()
    {
      var document = new DbDoc("{ \"_id\": 1, \"title\": \"Book 1\", \"pagess\":10, \"author\":\"Ana\" }");
      var document2 = new DbDoc(new { _id = 2, title = "Book 2", pages = 20, author = "John" });
      document.Equals(document2);
    }

    public void SessionSSLModeNone()
    {
      var session = new Session($"mysqlx://{USER}:@{HOST}:{X_DEVAPI_PORT}?auth=SHA256_MEMORY&sslmode=none");
      session.Close();
    }
  }
}
