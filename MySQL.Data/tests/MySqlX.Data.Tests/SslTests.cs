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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class SslTests : BaseTest
  {
    private string _sslCa;
    private string _sslCert;
    private string _sslKey;
    private string _connectionString;

    public SslTests()
    {
      _sslCa = "ca.pem";
      _sslCert = "client-cert.pem";
      _sslKey = "client-key.pem";

      // Update the port to the default xport.
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.Port = Convert.ToUInt32(XPort);
      _connectionString = builder.ConnectionString;
    }

    #region General

    [Fact]
    public void SslSession()
    {
      using (var s3 = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(SessionState.Open, s3.InternalSession.SessionState);
        var result = ExecuteSQLStatement(s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    public void SslEmptyCertificate()
    {
      string connstring = ConnectionStringUri + $"/?ssl-ca=";
      // if certificate is empty, it connects without a certificate
      using (var s1 = MySQLX.GetSession(connstring))
      {
        Assert.Equal(SessionState.Open, s1.InternalSession.SessionState);
        var result = ExecuteSQLStatement(s1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    public void SslOptions()
    {
      string connectionUri = ConnectionStringUri;
      // sslmode is valid.
      using (var connection = MySQLX.GetSession(connectionUri + "?sslmode=required"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }

      using (var connection = MySQLX.GetSession(connectionUri + "?ssl-mode=required"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }

      // sslenable is invalid.
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?sslenable"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?ssl-enable"));

      // sslmode=Required is default value.
      using (var connection = MySQLX.GetSession(connectionUri))
      {
        Assert.Equal(connection.Settings.SslMode, MySqlSslMode.Required);
      }

      // sslmode case insensitive.
      using (var connection = MySQLX.GetSession(connectionUri + "?SsL-mOdE=required"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }
      using (var connection = MySQLX.GetSession(connectionUri + "?SsL-mOdE=VeRiFyca&ssl-ca=../../../../MySql.Data.Tests/client.pfx&ssl-ca-pwd=pass"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
        var uri = connection.Uri;
      }

      // Duplicate SSL connection options send error message.
      ArgumentException ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?sslmode=Required&ssl mode=None"));
      Assert.EndsWith("is duplicated.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?ssl-ca-pwd=pass&ssl-ca-pwd=pass"));
      Assert.EndsWith("is duplicated.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?certificatepassword=pass&certificatepassword=pass"));
      Assert.EndsWith("is duplicated.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?certificatepassword=pass&ssl-ca-pwd=pass"));
      Assert.EndsWith("is duplicated.", ex.Message);

      // Send error if sslmode=None and another ssl parameter exists.
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?sslmode=None&ssl-ca=../../../../MySql.Data.Tests/certificates/client.pfx").InternalSession.SessionState);
    }

    [Fact]
    public void SslRequiredByDefault()
    {
      using (var connection = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(MySqlSslMode.Required, connection.Settings.SslMode);
      }
    }

    [Fact]
    public void SslPreferredIsInvalid()
    {
      var expectedErrorMessage = "Value '{0}' is not of the correct type.";

      // In connection string.
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUri + "?ssl-mode=Preferred"));
      Assert.Equal(string.Format(expectedErrorMessage, "Preferred"), exception.Message);
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUri + "?ssl-mode=Prefered"));
      Assert.Equal(string.Format(expectedErrorMessage, "Prefered"), exception.Message);

      // In anonymous object.
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionObject = new{
        server = builder.Server,
        port = builder.Port,
        user = builder.UserID,
        password = builder.Password,
        sslmode = MySqlSslMode.Prefered
      };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionObject));
      Assert.Equal(string.Format(expectedErrorMessage, "Prefered"), exception.Message);

      // In MySqlXConnectionStringBuilder.
      builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslMode = MySqlSslMode.Prefered;
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionObject));
      Assert.Equal(string.Format(expectedErrorMessage, "Prefered"), exception.Message);

      builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslMode = MySqlSslMode.Preferred;
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionObject));
      Assert.Equal(string.Format(expectedErrorMessage, "Prefered"), exception.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void RepeatedSslConnectionOptionsNotAllowed()
    {
      var expectedErrorMessage = "SSL connection option '{0}' is duplicated.";
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?sslca={_sslCa}&sslca={_sslCa}"));
      Assert.Equal(string.Format(expectedErrorMessage, "sslca"), exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?certificatefile={_sslCa}&certificatefile={_sslCa}"));
      Assert.Equal(string.Format(expectedErrorMessage, "certificatefile"), exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?sslca={_sslCa}&certificatefile={_sslCa}"));
      Assert.Equal(string.Format(expectedErrorMessage, "certificatefile"), exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?certificatefile={_sslCa}&sslca={_sslCa}"));
      Assert.Equal(string.Format(expectedErrorMessage, "sslca"), exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?certificatepassword=pass&certificatepassword=pass"));
      Assert.Equal(string.Format(expectedErrorMessage, "certificatepassword"), exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?sslcert={_sslCert}&sslcert={_sslCert}"));
      Assert.Equal(string.Format(expectedErrorMessage, "sslcert"), exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"{ConnectionStringUri}?sslkey={_sslKey}&sslkey={_sslKey}"));
      Assert.Equal(string.Format(expectedErrorMessage, "sslkey"), exception.Message);
    }

    #endregion

    #region PFX Certificates

    [Fact]
    public void SslCertificate()
    {
      string path = "../../../../MySql.Data.Tests/";
      string connstring = ConnectionStringUri + $"/?ssl-ca={path}client.pfx&ssl-ca-pwd=pass";
      using (var s3 = MySQLX.GetSession(connstring))
      {
        Assert.Equal(SessionState.Open, s3.InternalSession.SessionState);
        var result = ExecuteSQLStatement(s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    public void SslCrl()
    {
      string connstring = ConnectionStringUri + "/?ssl-crl=crlcert.pfx";
      Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(connstring));
    }

    [Fact]
    public void SslCertificatePathKeepsCase()
    {
      var certificatePath = "../../../../MySql.Data.Tests/client.pfx";
      // Connection string in basic format.
      string connString = ConnectionString + ";ssl-ca=" + certificatePath + ";ssl-ca-pwd=pass;";
      var stringBuilder = new MySqlXConnectionStringBuilder(connString);
      Assert.Equal(certificatePath, stringBuilder.CertificateFile);
      Assert.Equal(certificatePath, stringBuilder.SslCa);
      Assert.True(stringBuilder.ConnectionString.Contains(certificatePath));
      connString = stringBuilder.ToString();
      Assert.True(connString.Contains(certificatePath));

      // Connection string in uri format.
      string connStringUri = ConnectionStringUri + "/?ssl-ca=" + certificatePath + "& ssl-ca-pwd=pass;";
      using (var session = MySQLX.GetSession(connStringUri))
      {
        Assert.Equal(certificatePath, session.Settings.CertificateFile);
        Assert.Equal(certificatePath, session.Settings.SslCa);
        Assert.True(session.Settings.ConnectionString.Contains(certificatePath));
        connString = session.Settings.ToString();
        Assert.True(connString.Contains(certificatePath));
      }
    }

    // Fix Bug 24510329 - UNABLE TO CONNECT USING TLS/SSL OPTIONS FOR THE MYSQLX URI SCHEME.
    [Theory]
    [InlineData("../../../../MySql.Data.Tests/client.pfx")]
    [InlineData("(../../../../MySql.Data.Tests/client.pfx)")]
    [InlineData(@"(..\..\..\..\MySql.Data.Tests\client.pfx")]
    [InlineData("..\\..\\..\\..\\MySql.Data.Tests\\client.pfx")]
    public void SslCertificatePathVariations(string certificatePath)
    {
      string connStringUri = ConnectionStringUri + "/?ssl-ca=" + certificatePath + "& ssl-ca-pwd=pass;";

      using (var session = MySQLX.GetSession(connStringUri))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    #endregion

    #region PEM Certificates

    [Fact]
    [Trait("Category", "Security")]
    public void SslCertificateConnectionOptionsExistAndDefaultToNull()
    {
      var builder = new MySqlXConnectionStringBuilder();

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
      builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = null;
      builder.SslCert = string.Empty;
      builder.SslKey = "  ";
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        Assert.Null(session.Settings.SslCa);
        Assert.Null(session.Settings.SslCert);
        Assert.Equal("  ", session.Settings.SslKey);
        session.Close();
      }

      // Failing to provide a value defaults to null.
      using (var session = MySQLX.GetSession($"{_connectionString};sslca=;sslcert=;sslkey="))
      {
        Assert.Null(session.Settings.SslCa);
        Assert.Null(session.Settings.SslCert);
        Assert.Null(session.Settings.SslKey);
        session.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingSslCaConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslCa)), exception.InnerException.Message);
      
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslCa)), exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingSslCertConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = string.Empty;
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslCert)), exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingSslKeyConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslKey)), exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidFileNameForSslCaConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = "C:\\certs\\ca.pema";
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileNotFound, exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidFileNameForSslCertConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = "C:\\certs\\client-cert";
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileNotFound, exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void InvalidFileNameForSslKeyConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = "file";
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileNotFound, exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingCaPemCertificate()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingAllCertificates()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }

      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_altered.pem");
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SslCaConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = "dummy_file";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Preferred causing SslCa to be ignored.
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SslCertandKeyConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(_connectionString);
      builder.SslCa = "dummy_file";
      builder.SslCert = null;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Required causing SslCa, SslCert and SslKey to be ignored.
      using (var session =  MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void AttemptConnectionWithDummyPemCertificates()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCa.Replace("ca.pem", "ca_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileIsNotACertificate, exception.InnerException.Message);

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert.Replace("client-cert.pem", "client-cert_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileIsNotACertificate, exception.InnerException.Message);

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileIsNotAKey, exception.InnerException.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void AttemptConnectionWitSwitchedPemCertificates()
    {
      var builder = new MySqlXConnectionStringBuilder(_connectionString);
      builder.SslCa = _sslCert;
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.SslCertificateIsNotCA, exception.InnerException.Message);
      
      builder.SslCa = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyCA;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileIsNotACertificate, exception.InnerException.Message);

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.InvalidSslCertificate, exception.InnerException.Message);
      
      builder.SslCert = _sslCert;
      builder.SslKey = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.Equal(MySql.Data.Resources.SslConnectionError, exception.Message);
      Assert.Equal(MySql.Data.Resources.FileIsNotAKey, exception.InnerException.Message);
    }

    #endregion
  }
}
