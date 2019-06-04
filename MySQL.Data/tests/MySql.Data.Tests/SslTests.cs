// Copyright (c) 2018, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class SslTests : TestBase
  {
    private string _sslCa;
    private string _sslCert;
    private string _sslKey;

    public SslTests(TestFixture fixture) : base(fixture)
    {
      _sslCa =   "ca.pem";
      _sslCert = "client-cert.pem";
      _sslKey = "client-key.pem";
    }

#region General

    [Fact]
    [Trait("Category", "Security")]
    public void SslModePreferredByDefault()
    {
      MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", Connection);
      using (MySqlDataReader reader = command.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.StartsWith("TLSv1", reader.GetString(1));
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SslModeOverriden()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.AllowPublicKeyRetrieval = true;
      builder.Database = "";
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(string.Empty, reader.GetString(1));
        }
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void RepeatedSslConnectionOptionsNotAllowed()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslca={_sslCa}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "sslca"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.CertificateFile = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};certificatefile={_sslCa}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "certificatefile"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};certificatefile={_sslCa}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "certificatefile"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.CertificateFile = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslca={_sslCa}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "sslca"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslcert={_sslCert};sslkey={_sslKey};sslca={_sslCa}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "sslca"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.CertificatePassword = "pass";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};certificatepassword=pass"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "certificatepassword"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCert = _sslCert;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslcert={_sslCert}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "sslcert"), exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslKey = _sslKey;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslkey={_sslKey}"));
      Assert.Equal(string.Format(Resources.DuplicatedSslConnectionOption, "sslkey"), exception.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidOptionsWhenSslDisabled()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.SslCa = "value";
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.Equal(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.SslCert = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.Equal(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.SslKey = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.Equal(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.CertificateFile = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.Equal(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.CertificatePassword = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.Equal(Resources.InvalidOptionWhenSslDisabled, exception.Message);
    }

    #endregion

#region PFX

    /// <summary>
    /// A client can connect to MySQL server using SSL and a pfx file.
    /// <remarks>
    /// This test requires starting the server with SSL support.
    /// For instance, the following command line enables SSL in the server:
    /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
    /// </remarks>
    /// </summary>
    [Fact]
    [Trait("Category", "Security")]
    public void CanConnectUsingFileBasedCertificate()
    {
      string connstr = ConnectionSettings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.Equal(ConnectionState.Open, c.State);
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.StartsWith("TLSv1", reader.GetString(1));
        }
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingPFXCertificates()
    {
      string connstr = ConnectionSettings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.Equal(ConnectionState.Open, c.State);
      }
    }

#endregion

#region PEM

    [Fact]
    [Trait("Category", "Security")]
    public void SslCertificateConnectionOptionsExistAndDefaultToNull()
    {
      var builder = new MySqlConnectionStringBuilder();

      // Options exist.
      Assert.True(builder.values.ContainsKey("sslca"));
      Assert.True(builder.values.ContainsKey("sslcert"));
      Assert.True(builder.values.ContainsKey("sslkey"));

      // Options default to null.
      Assert.Null(builder["sslca"]);
      Assert.Null(builder["sslcert"]);
      Assert.Null(builder["sslkey"]);
      Assert.Null(builder["ssl-ca"]);
      Assert.Null(builder["ssl-cert"]);
      Assert.Null(builder["ssl-key"]);
      Assert.Null(builder.SslCa);
      Assert.Null(builder.SslCert);
      Assert.Null(builder.SslKey);

      // Null or whitespace options are ignored.
      var connectionString = $"host={ConnectionSettings.Server};user={ConnectionSettings.UserID};port={ConnectionSettings.Port};password={ConnectionSettings.Password};";
      builder = new MySqlConnectionStringBuilder(connectionString);
      builder.SslCa = null;
      builder.SslCert = string.Empty;
      builder.SslKey = "  ";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        Assert.Null(connection.Settings.SslCa);
        Assert.Null(connection.Settings.SslCert);
        Assert.Equal("  ", connection.Settings.SslKey);
        connection.Open();
        connection.Close();
      }

      // Failing to provide a value defaults to null.
      connectionString = $"{connectionString}sslca=;sslcert=;sslkey=";
      using (var connection = new MySqlConnection(connectionString))
      {
        Assert.Null(connection.Settings.SslCa);
        Assert.Null(connection.Settings.SslCert);
        Assert.Null(connection.Settings.SslKey);
        connection.Open();
        connection.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingSslCaConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(string.Format(Resources.FilePathNotSet, nameof(ConnectionSettings.SslCa)), exception.InnerException.Message);
      }

      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(string.Format(Resources.FilePathNotSet, nameof(ConnectionSettings.SslCa)), exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingSslCertConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = string.Empty;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(string.Format(Resources.FilePathNotSet, nameof(ConnectionSettings.SslCert)), exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingSslKeyConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(string.Format(Resources.FilePathNotSet, nameof(ConnectionSettings.SslKey)), exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidFileNameForSslCaConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = "C:\\certs\\ca.pema";
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileNotFound, exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidFileNameForSslCertConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = "C:\\certs\\client-cert";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileNotFound, exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidFileNameForSslKeyConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = "file";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileNotFound, exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingCaPemCertificate()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingAllCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_altered.pem");
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SslCaConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslMode = MySqlSslMode.Preferred;

      // Connection attempt is successful since SslMode=Preferred causign SslCa to be ignored.
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SslCertandKeyConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslCert = null;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Required causing SslCa, SslCert and SslKey to be ignored.
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void AttemptConnectionWithDummyPemCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCa.Replace("ca.pem", "ca_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileIsNotACertificate, exception.InnerException.Message);
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert.Replace("client-cert.pem", "client-cert_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileIsNotACertificate, exception.InnerException.Message);
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileIsNotAKey, exception.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void AttemptConnectionWithSwitchedPemCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslCa = _sslCert;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.SslCertificateIsNotCA, exception.InnerException.Message);
      }

      builder.SslCa = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileIsNotACertificate, exception.InnerException.Message);
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.InvalidSslCertificate, exception.InnerException.Message);
      }

      builder.SslCert = _sslCert;
      builder.SslKey = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.Equal(Resources.SslConnectionError, exception.Message);
        Assert.Equal(Resources.FileIsNotAKey, exception.InnerException.Message);
      }
    }

    #endregion
  }
}
