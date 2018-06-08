using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// The purpose of this class is to incorporate MySqlBaseConnectionStringBuilder, MySqlConnectionStringBuilder and MySqlXConnectionStringBuilder
  /// tests that aren't affected by previously opened connections/sessions.
  /// </summary>
  public class XConnectionStringBuilderTests
  {
    private static string _connectionString;
    private static string _xConnectionString;
    private static string _connectionStringWithSslMode;

    static XConnectionStringBuilderTests()
    {
      _connectionString = "server=localhost;user=root;port=3306;";
      _xConnectionString = "server=localhost;user=root;port=33060;";
      _connectionStringWithSslMode = _connectionString + "sslmode=required;";
    }

    [Fact]
    public void SessionCanBeOpened()
    {
      Session session = null;
      session = MySQLX.GetSession(_xConnectionString);
    }

    [Fact]
    public void ConnectionAfterSessionCanBeOpened()
    {
      Session session = null;
      session = MySQLX.GetSession(_xConnectionString);

      var connection = new MySqlConnection(_connectionStringWithSslMode);
      connection.Open();
      connection.Close();
    }

    [Fact]
    public void Bug28151070_3()
    {
      var connection = new MySqlConnection(_connectionStringWithSslMode);
      connection.Open();
      connection.Close();

      Session session = null;
      session = MySQLX.GetSession(_xConnectionString);
    }

    [Fact]
    public void Bug28151070_4()
    {
      var connection = new MySqlConnection(_connectionStringWithSslMode);
      connection.Open();
      connection.Close();
    }

    [Fact]
    public void Bug28151070_5()
    {
      Session session = null;
      session = MySQLX.GetSession(_xConnectionString);

      var builder = new MySqlXConnectionStringBuilder();
      builder.Auth = MySqlAuthenticationMode.AUTO;
      builder.SslCa = "";

      var builder2 = new MySqlConnectionStringBuilder();
      builder2.Auth = MySqlAuthenticationMode.AUTO;
      builder2.SslCa = "";
    }
  }
}
