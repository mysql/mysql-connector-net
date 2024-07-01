// Copyright © 2018, 2024, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;

namespace MySqlX.Data.Tests
{
  public class SslTests : BaseTest
  {
    #region General

    [Test]
    public void SslSession()
    {
      using (var s3 = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.That(s3.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var result = ExecuteSQLStatement(s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.That(result[0][1].ToString(), Does.StartWith("TLSv1"));
      }
    }

    [Test]
    public void SslEmptyCertificate()
    {
      string connstring = ConnectionStringUri + $"/?ssl-ca=";
      // if certificate is empty, it connects without a certificate
      using (var s1 = MySQLX.GetSession(connstring))
      {
        Assert.That(s1.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var result = ExecuteSQLStatement(s1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.That(result[0][1].ToString(), Does.StartWith("TLSv1"));
      }
    }

    [Test]
    public void SslOptions()
    {
      string connectionUri = ConnectionStringUri;
      // sslmode is valid.
      using (var connection = MySQLX.GetSession(connectionUri + "?sslmode=required"))
      {
        Assert.That(connection.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
      }

      using (var connection = MySQLX.GetSession(connectionUri + "?ssl-mode=required"))
      {
        Assert.That(connection.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
      }

      // sslenable is invalid.
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?sslenable"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "?ssl-enable"));

      // sslmode=Required is default value.
      using (var connection = MySQLX.GetSession(connectionUri))
      {
        Assert.That(MySqlSslMode.Required, Is.EqualTo(connection.Settings.SslMode));
      }

      // sslmode case insensitive.
      using (var connection = MySQLX.GetSession(connectionUri + "?SsL-mOdE=required"))
      {
        Assert.That(connection.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
      }
      using (var connection = MySQLX.GetSession(connectionUri + "?SsL-mOdE=VeRiFyca&ssl-ca=../../../../MySql.Data.Tests/client.pfx&ssl-ca-pwd=pass"))
      {
        Assert.That(connection.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var uri = connection.Uri;
      }

      // Duplicate SSL connection options do not send error message.
      Assert.DoesNotThrow(() => MySQLX.GetSession(connectionUri + "?sslmode=Required&ssl mode=None"));
      Assert.DoesNotThrow(() => MySQLX.GetSession(connectionUri + "?ssl-ca-pwd=pass&ssl-ca-pwd=pass"));
      Assert.DoesNotThrow(() => MySQLX.GetSession(connectionUri + "?certificatepassword=pass&certificatepassword=pass"));
      Assert.DoesNotThrow(() => MySQLX.GetSession(connectionUri + "?certificatepassword=pass&ssl-ca-pwd=pass"));

      // Does not error out if sslmode=Disabled and another ssl parameter exists.
      Assert.DoesNotThrow(() => MySQLX.GetSession(connectionUri + "?sslmode=Disabled&ssl-ca=../../../../MySql.Data.Tests/certificates/client.pfx"));
    }

    [Test]
    public void SslRequiredByDefault()
    {
      using (var connection = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.That(connection.Settings.SslMode, Is.EqualTo(MySqlSslMode.Required));
      }
    }

    [Test]
    public void SslPreferredIsInvalid()
    {
      string prefered = "Prefered";
#if NET8_0
      prefered = "Preferred";
#endif
      var expectedErrorMessage = "Value '{0}' is not of the correct type.";

      // In connection string.
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUri + "?ssl-mode=Preferred"));
      Assert.That(exception.Message, Is.EqualTo(string.Format(expectedErrorMessage, "Preferred")));
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUri + "?ssl-mode=Prefered"));
      Assert.That(exception.Message, Is.EqualTo(string.Format(expectedErrorMessage, "Prefered")));

      // In anonymous object.
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionObject = new
      {
        server = builder.Server,
        port = builder.Port,
        user = builder.UserID,
        password = builder.Password,
        sslmode = MySqlSslMode.Prefered
      };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionObject));
      Assert.That(exception.Message, Is.EqualTo(string.Format(expectedErrorMessage, prefered)));

      // In MySqlXConnectionStringBuilder.
      builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslMode = MySqlSslMode.Prefered;
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionObject));
      Assert.That(exception.Message, Is.EqualTo(string.Format(expectedErrorMessage, prefered)));

      builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslMode = MySqlSslMode.Preferred;
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionObject));
      Assert.That(exception.Message, Is.EqualTo(string.Format(expectedErrorMessage, prefered)));
    }

    [Test]
    [Property("Category", "Security")]
    public void GetUriWithSSLParameters()
    {
      var session = GetSession();

      var builder = new MySqlXConnectionStringBuilder();
      builder.Server = session.Settings.Server;
      builder.UserID = session.Settings.UserID; ;
      builder.Password = session.Settings.Password;
      builder.Port = session.Settings.Port;
      builder.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      builder.Database = session.Settings.Database;
      builder.CharacterSet = session.Settings.CharacterSet;
      builder.SslMode = MySqlSslMode.Required;
      builder.SslCa = "../../../../MySql.Data.Tests/client.pfx";
      builder.CertificatePassword = sslCertificatePassword;
      builder.ConnectTimeout = 10000;
      builder.Auth = MySqlAuthenticationMode.AUTO;

      var connectionString = builder.ConnectionString;
      string uri = null;

      // Create session with connection string.
      using (var internalSession = MySQLX.GetSession(connectionString))
      {
        uri = internalSession.Uri;
      }

      // Create session with the uri version of the connection string.
      using (var internalSession = MySQLX.GetSession(uri))
      {
        // Compare values of the connection options.
        foreach (string connectionOption in builder.Keys)
        {
          // SslCrl connection option is skipped since it isn't currently supported.
          if (connectionOption == "sslcrl")
            continue;

          // Authentication mode AUTO/DEFAULT is internally assigned, hence it is expected to be different in this scenario. 
          if (connectionOption == "auth")
            Assert.That(internalSession.Settings[connectionOption], Is.EqualTo(MySqlAuthenticationMode.PLAIN));
          else
            Assert.That(internalSession.Settings[connectionOption], Is.EqualTo(builder[connectionOption]));
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    [Ignore("Check failure: The remote certificate was rejected by the provided RemoteCertificateValidationCallback.")]
    public void GetUriKeepsSSLMode()
    {
      var globalSession = GetSession();
      var builder = new MySqlXConnectionStringBuilder();
      builder.Server = globalSession.Settings.Server;
      builder.UserID = globalSession.Settings.UserID;
      builder.Password = globalSession.Settings.Password;
      builder.Port = globalSession.Settings.Port;
      builder.Database = schemaName;
      builder.CharacterSet = globalSession.Settings.CharacterSet;
      builder.SslMode = MySqlSslMode.VerifyCA;
      // Setting SslCa will also set CertificateFile.
      builder.SslCa = "client.pfx";
      builder.CertificatePassword = sslCertificatePassword;
      builder.ConnectTimeout = 10000;
      // Auth will change to the authentication mode internally used PLAIN, MySQL41, SHA256_MEMORY: 
      builder.Auth = MySqlAuthenticationMode.AUTO;
      // Doesn't show in the session.URI because Tcp is the default value. Tcp, Socket and Sockets are treated the same.
      builder.ConnectionProtocol = MySqlConnectionProtocol.Tcp;

      string uri = null;
      using (var internalSession = MySQLX.GetSession(builder.ConnectionString))
      {
        uri = internalSession.Uri;
      }

      using (var internalSession = MySQLX.GetSession(uri))
      {
        Assert.That(internalSession.Settings.Server, Is.EqualTo(builder.Server));
        Assert.That(internalSession.Settings.UserID, Is.EqualTo(builder.UserID));
        Assert.That(internalSession.Settings.Password, Is.EqualTo(builder.Password));
        Assert.That(internalSession.Settings.Port, Is.EqualTo(builder.Port));
        Assert.That(internalSession.Settings.Database, Is.EqualTo(builder.Database));
        Assert.That(internalSession.Settings.CharacterSet, Is.EqualTo(builder.CharacterSet));
        Assert.That(internalSession.Settings.SslMode, Is.EqualTo(builder.SslMode));
        Assert.That(internalSession.Settings.SslCa, Is.EqualTo(builder.SslCa));
        Assert.That(internalSession.Settings.CertificatePassword, Is.EqualTo(builder.CertificatePassword));
        Assert.That(internalSession.Settings.ConnectTimeout, Is.EqualTo(builder.ConnectTimeout));
        Assert.That(internalSession.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
      }
    }

    /// <summary>
    /// WL14811 - Remove support for TLS 1.0 and 1.1
    /// </summary>
    [TestCase("[]", 1)]
    [TestCase("Tlsv1, Tlsv1.1", 2)]
    [TestCase("Tlsv1, foo", 2)]
    [TestCase("Tlsv1", 2)]
    [TestCase("Tlsv1.1", 2)]
    [TestCase("foo, bar", 3)]
    [TestCase("Tlsv1.0, Tlsv1.2", 0)]
    [TestCase("foo, Tlsv1.2", 0)]
    //#if NET48 || NETCOREAPP3_1 || NET5_0 || NET6_0
    //    [TestCase("Tlsv1.3", "Tlsv1.3")]
    //    [TestCase("Tlsv1.0, Tlsv1.1, Tlsv1.2, Tlsv1.3", "Tlsv1.3")]
    //#endif
#if NET452
    [TestCase("Tlsv1.3", 4)]
    [TestCase("Tlsv1.0, Tlsv1.1, Tlsv1.2, Tlsv1.3", 0)]
#endif
    [Property("Category", "Security")]
    public void TlsVersionTest(string tlsVersion, int error)
    {
      var globalSession = GetSession();
      var builder = new MySqlXConnectionStringBuilder
      {
        Server = globalSession.Settings.Server,
        UserID = globalSession.Settings.UserID,
        Password = globalSession.Settings.Password,
        Port = globalSession.Settings.Port,
        Database = schemaName
      };
      void SetTlsVersion() { builder.TlsVersion = tlsVersion; }

      string ex;
      string tls = "TLSv1";

      switch (error)
      {
        case 1:
          ex = Assert.Throws<ArgumentException>(SetTlsVersion).Message;
          Assert.That(ex, Is.EqualTo(MySql.Data.Resources.TlsVersionsEmpty).IgnoreCase);
          break;
        case 2:
          ex = Assert.Throws<ArgumentException>(SetTlsVersion).Message;
          Assert.That(ex, Is.EqualTo(MySql.Data.Resources.TlsUnsupportedVersions).IgnoreCase);
          break;
        case 3:
          ex = Assert.Throws<ArgumentException>(SetTlsVersion).Message;
          Assert.That(ex, Is.EqualTo(MySql.Data.Resources.TlsNonValidProtocols).IgnoreCase);
          break;
        case 4:
          SetTlsVersion();
          Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(builder.ConnectionString));
          break;
        default:
          SetTlsVersion();
          string uri = null;

          using (var internalSession = MySQLX.GetSession(builder.ConnectionString))
          {
            uri = internalSession.Uri;
            Assert.That(internalSession.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
            Assert.That(internalSession.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_version'").Execute().FetchAll()[0][1].ToString(), Does.StartWith(tls));
          }
          using (var internalSession = MySQLX.GetSession(uri))
          {
            Assert.That(internalSession.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
            Assert.That(internalSession.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_version'").Execute().FetchAll()[0][1].ToString(), Does.StartWith(tls));
          }

          break;
      }
    }

    /// <summary>
    /// WL14811 - Remove support for TLS 1.0 and 1.1
    /// </summary>
    [TestCase("Tlsv1.0, Tlsv1.2")]
    [TestCase("foo, Tlsv1.2")]
    public void TlsVersionNoSslTest(string tlsVersion)
    {
      var globalSession = GetSession();
      var builder = new MySqlXConnectionStringBuilder
      {
        Server = globalSession.Settings.Server,
        UserID = globalSession.Settings.UserID,
        Password = globalSession.Settings.Password,
        Port = globalSession.Settings.Port,
        Database = schemaName,
        TlsVersion = tlsVersion,
        SslMode = MySqlSslMode.Disabled
      };

      string uri = null;

      using (var internalSession = MySQLX.GetSession(builder.ConnectionString))
      {
        uri = internalSession.Uri;
        Assert.That(internalSession.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(internalSession.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_version'").Execute().FetchAll()[0][1].ToString(), Is.Empty);
      }
      using (var internalSession = MySQLX.GetSession(uri))
      {
        Assert.That(internalSession.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(internalSession.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_version'").Execute().FetchAll()[0][1].ToString(), Is.Empty);
      }
    }

    #endregion

    #region PFX Certificates

    [Test]
    public void SslCertificate()
    {
      string path = "../../../../MySql.Data.Tests/";
      string connstring = ConnectionStringUri + $"/?ssl-ca={path}client.pfx&ssl-ca-pwd=pass";
      using (var s3 = MySQLX.GetSession(connstring))
      {
        Assert.That(s3.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var result = ExecuteSQLStatement(s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.That(result[0][1].ToString(), Does.StartWith("TLSv1"));
      }
    }

    [Test]
    public void SslCrl()
    {
      string connstring = ConnectionStringUri + "/?ssl-crl=crlcert.pfx";
      Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(connstring));
    }

    [Test]
    public void SslCertificatePathKeepsCase()
    {
      var certificatePath = "../../../../MySql.Data.Tests/client.pfx";
      // Connection string in basic format.
      string connString = ConnectionString + ";ssl-ca=" + certificatePath + ";ssl-ca-pwd=pass;";
      var stringBuilder = new MySqlXConnectionStringBuilder(connString);
      Assert.That(stringBuilder.CertificateFile, Is.EqualTo(certificatePath));
      Assert.That(stringBuilder.SslCa, Is.EqualTo(certificatePath));
      Assert.That(stringBuilder.ConnectionString.Contains(certificatePath));
      connString = stringBuilder.ToString();
      Assert.That(connString.Contains(certificatePath));

      // Connection string in uri format.
      string connStringUri = ConnectionStringUri + "/?ssl-ca=" + certificatePath + "& ssl-ca-pwd=pass;";
      using (var session = MySQLX.GetSession(connStringUri))
      {
        Assert.That(session.Settings.CertificateFile, Is.EqualTo(certificatePath));
        Assert.That(session.Settings.SslCa, Is.EqualTo(certificatePath));
        Assert.That(session.Settings.ConnectionString.Contains(certificatePath));
        connString = session.Settings.ToString();
        Assert.That(connString.Contains(certificatePath));
      }
    }

    // Fix Bug 24510329 - UNABLE TO CONNECT USING TLS/SSL OPTIONS FOR THE MYSQLX URI SCHEME.
    [TestCase("../../../../MySql.Data.Tests/client.pfx")]
    [TestCase("(../../../../MySql.Data.Tests/client.pfx)")]
    [TestCase(@"(..\..\..\..\MySql.Data.Tests\client.pfx")]
    [TestCase("..\\..\\..\\..\\MySql.Data.Tests\\client.pfx")]
    public void SslCertificatePathVariations(string certificatePath)
    {
      string connStringUri = ConnectionStringUri + "/?ssl-ca=" + certificatePath + "& ssl-ca-pwd=pass;";

      using (var session = MySQLX.GetSession(connStringUri))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
      }
    }

    #endregion

    #region PEM Certificates

    [Test]
    [Property("Category", "Security")]
    public void SslCertificateConnectionOptionsExistAndDefaultToNull()
    {
      var builder = new MySqlXConnectionStringBuilder();

      // Options exist.
      Assert.That(builder.values.ContainsKey("sslca"));
      Assert.That(builder.values.ContainsKey("sslcert"));
      Assert.That(builder.values.ContainsKey("sslkey"));

      // Options default to null.
      Assert.That(builder["sslca"], Is.Null);
      Assert.That(builder["sslcert"], Is.Null);
      Assert.That(builder["sslkey"], Is.Null);
      Assert.That(builder["ssl-ca"], Is.Null);
      Assert.That(builder["ssl-cert"], Is.Null);
      Assert.That(builder["ssl-key"], Is.Null);
      Assert.That(builder.SslCa, Is.Null);
      Assert.That(builder.SslCert, Is.Null);
      Assert.That(builder.SslKey, Is.Null);

      // Null or whitespace options are ignored.
      builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = null;
      builder.SslCert = string.Empty;
      builder.SslKey = "  ";
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        Assert.That(session.Settings.SslCa, Is.Null);
        Assert.That(session.Settings.SslCert, Is.Null);
        Assert.That(session.Settings.SslKey, Is.EqualTo("  "));
        session.Close();
      }

      // Failing to provide a value defaults to null.
      using (var session = MySQLX.GetSession($"{ConnectionString};sslca=;sslcert=;sslkey="))
      {
        Assert.That(session.Settings.SslCa, Is.Null);
        Assert.That(session.Settings.SslCert, Is.Null);
        Assert.That(session.Settings.SslKey, Is.Null);
        session.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslCaConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslCa))));

      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslCa))));
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslCertConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa;
      builder.SslCert = string.Empty;
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslCert))));
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslKeyConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa;
      builder.SslCert = sslCert;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(MySql.Data.Resources.FilePathNotSet, nameof(builder.SslKey))));
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslCaConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = "C:\\certs\\ca.pema";
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileNotFound));
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslCertConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa;
      builder.SslCert = "C:\\certs\\client-cert";
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileNotFound));
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslKeyConnectionOption()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa;
      builder.SslCert = sslCert;
      builder.SslKey = "file";
      builder.SslMode = MySqlSslMode.VerifyFull;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileNotFound));
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingCaPemCertificate()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingAllCertificates()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa;
      builder.SslCert = sslCert;
      builder.SslKey = sslKey;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }

      builder.SslKey = sslKey.Replace("client-key.pem", "client-key_altered.pem");
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
    }

    [Test]
    [Property("Category", "Security")]
    public void SslCaConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Preferred causing SslCa to be ignored.
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslCertandKeyConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslCert = null;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Required causing SslCa, SslCert and SslKey to be ignored.
      using (var session = MySQLX.GetSession(builder.ConnectionString))
      {
        session.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AttemptConnectionWithDummyPemCertificates()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCa.Replace("ca.pem", "ca_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileIsNotACertificate));

      builder.SslCa = sslCa;
      builder.SslCert = sslCert.Replace("client-cert.pem", "client-cert_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileIsNotACertificate));

      builder.SslCa = sslCa;
      builder.SslCert = sslCert;
      builder.SslKey = sslKey.Replace("client-key.pem", "client-key_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileIsNotAKey));
    }

    [Test]
    [Property("Category", "Security")]
    public void AttemptConnectionWitSwitchedPemCertificates()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.SslCa = sslCert;
      builder.SslMode = MySqlSslMode.VerifyCA;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.SslCertificateIsNotCA));

      builder.SslCa = sslKey;
      builder.SslMode = MySqlSslMode.VerifyCA;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileIsNotACertificate));

      builder.SslCa = sslCa;
      builder.SslCert = sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.InvalidSslCertificate));

      builder.SslCert = sslCert;
      builder.SslKey = sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(builder.ConnectionString));
      Assert.That(exception.Message, Is.EqualTo(MySql.Data.Resources.SslConnectionError));
      Assert.That(exception.InnerException.Message, Is.EqualTo(MySql.Data.Resources.FileIsNotAKey));
    }

    #endregion

    [Test, Description("Wrong Certificate with TLS with URI")]
    public void PfxCertificateWithUri()
    {
      if (!session.Version.isAtLeast(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");

      var connStr = ConnectionStringUri + $"?ssl-mode=VerifyCA&ssl-ca={clientPfxIncorrect}&ssl-ca-pwd={sslCertificatePassword}";
      Assert.That(() => MySQLX.GetSession(connStr), Throws.Exception);

      connStr = ConnectionStringUri + $"?ssl-mode=VerifyCA&ssl-ca={clientPfx}&ssl-ca-pwd=Wrongpassword";
      Assert.That(() => MySQLX.GetSession(connStr), Throws.Exception);

      connStr = ConnectionStringUri + $"?ssl-mode=Required&ssl-ca={clientPfx}&ssl-ca-pwd={sslCertificatePassword}";
      using (var c = MySQLX.GetSession(connStr))
      {
        Assert.That(c.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var res = ExecuteSQLStatement(c.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.That(res[0][1].ToString(), Does.StartWith("TLSv1"));
      }
    }

    [Test, Description("MySQLX ran with Automation for server with TLS")]
    public void PfxCertificateWithConnectionString()
    {
      if (!session.Version.isAtLeast(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");

      var connStr = ConnectionString + $";ssl-mode=Required;ssl-ca={clientPfx};ssl-ca-pwd={sslCertificatePassword};";
      using (Session c = MySQLX.GetSession(connStr))
      {
        Assert.That(c.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var res = ExecuteSQLStatement(c.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.That(res[0][1].ToString(), Does.StartWith("TLSv1"));
      }

      //wrong certificate
      connStr = ConnectionString + $";CertificateFile={clientPfxIncorrect};CertificatePassword={sslCertificatePassword};SSL Mode=Required;";
      Assert.That(() => MySQLX.GetSession(connStr), Throws.Exception);
      //wrong password
      connStr = ConnectionString + $";CertificateFile={clientPfx};CertificatePassword=WrongPassword;SSL Mode=Required;";
      Assert.That(() => MySQLX.GetSession(connStr), Throws.Exception);
    }

    [Test]
    public void ConnectUsingCertificateFileAndTlsVersionXplugin()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");

      var builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.SslMode = MySqlSslMode.Required;
      builder.CertificateFile = "client.pfx";
      builder.CertificatePassword = "pass";
      builder.TlsVersion = "Tlsv1.2";
      using (var session1 = MySQLX.GetSession(builder.ConnectionString))
      {
        var result = session1.SQL("show variables like '%tls_version%'").Execute().FetchOne();
        Assert.That(result[1].ToString(), Does.Contain("TLSv1"));

        result = session1.SQL("show status like 'Mysqlx_ssl_cipher'").Execute().FetchOne();
        Assert.That(result[1].ToString().Trim().Length > 0);

        result = session1.SQL("show status like 'Mysqlx_ssl_version'").Execute().FetchOne();
        Assert.That(result[1].ToString(), Is.EqualTo("TLSv1.2").IgnoreCase);
      }
    }

    [TestCase("TLSv1.2")]
    public void TlsVersionInConnectionStringXplugin(string tlsVersion)
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 16)) Assert.Ignore("This test is for MySql 8.0.16 or higher.");

      var connStr = ConnectionString + $";sslmode=Required;tls-version={tlsVersion}";
      using (var c = MySQLX.GetSession(connStr))
      {
        Assert.That(c.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var res = c.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(res[0][1].ToString(), Is.EqualTo(tlsVersion).IgnoreCase);
      }
    }

    [Test, Description("Verify PEM options (SslCa,SslCert,SslKey) with different SSL modes")]
    public void PemCertDifferentSSLmodes()
    {
      if (!session.Version.isAtLeast(8, 0, 16)) Assert.Ignore("This test is for MySql 8.0.16 or higher.");
      string CommandText1 = "SHOW STATUS LIKE '%Ssl_cipher%';";
      string CommandText2 = "show  status like '%Ssl_version%';";
      string connStr = null;

      string[] sslmodes = { "Disabled", "Prefered", "Preferred", "Required", "VerifyCA" };
      MySqlSslMode[] sslmode =
          { MySqlSslMode.Disabled,MySqlSslMode.Prefered,MySqlSslMode.Preferred,MySqlSslMode.Required,MySqlSslMode.VerifyCA };
      string tls = "TLSv1.2";

      for (int i = 0; i < sslmodes.Length; i++)
      {
        //Uri
        connStr = ConnectionStringUri + $"?Ssl-ca={sslCa}&SslCert={sslCert}&SslKey={sslKey}&ssl-ca-pwd={sslCertificatePassword}&ssl-mode={sslmodes[i]}";
        if (i == 0)
        {
          using (var session1 = MySQLX.GetSession(connStr))
          {
            Assert.That(session1.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
          }
        }
        else
        {
          string sslcompare = sslmodes[i];
          if (i == 2)
          {
            sslcompare = "Preferred";
          }
          if (i == 1 || i == 2)
          {
            Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connStr));
          }
          else
          {
            using (var session1 = MySQLX.GetSession(connStr))
            {
              var result = session1.SQL(CommandText1).Execute().FetchAll();
              Assert.That(result[0][1].ToString(), Is.EqualTo(result[0][1].ToString()), "Matching the Cipher");
              result = session1.SQL(CommandText2).Execute().FetchAll();
              Assert.That(result[0][1].ToString(), Is.EqualTo(tls), "Matching the TLS version");
            }
          }
        }

        //Connection string
        connStr = ConnectionStringUserWithSSLPEM + $";ssl-ca-pwd={sslCertificatePassword};ssl-mode={sslmodes[i]}";
        if (i == 0)
        {
          using (var session1 = MySQLX.GetSession(connStr))
          {
            Assert.That(session1.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
          }
        }
        else
        {
          if (i == 1 || i == 2)
          {
            string sslcompare = sslmodes[i];
            if (i == 2)
            {
              sslcompare = "Preferred";
            }
            Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connStr));
          }
          else
          {
            using (var session1 = MySQLX.GetSession(connStr))
            {
              var result = session1.SQL(CommandText1).Execute().FetchAll();
              Assert.That(result[0][1].ToString(), Is.EqualTo(result[0][1].ToString()), "Matching the Cipher");
              result = session1.SQL(CommandText2).Execute().FetchAll();
              Assert.That(result[0][1].ToString(), Is.EqualTo(tls), "Matching the TLS version");
            }
          }
        }

        //Anonymous Object
        var connObject = new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslCert,
          SslKey = sslKey,
          CertificatePassword = sslCertificatePassword,
          SslCa = sslCa,
          sslmode = sslmode[i]
        };

        if (i == 0)
        {
          using (var session1 = MySQLX.GetSession(connStr))
          {
            Assert.That(session1.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
          }
        }
        else
        {
          string sslcompare = sslmodes[i];
          if (i == 2)
          {
            sslcompare = "Prefered";
          }
          if (i == 1 | i == 2)
          {
            Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connObject));
          }
          else
          {
            using (var session1 = MySQLX.GetSession(connObject))
            {
              var result = session1.SQL(CommandText1).Execute().FetchAll();
              Assert.That(result[0][1].ToString(), Is.EqualTo(result[0][1].ToString()), "Matching the Cipher");
              result = session1.SQL(CommandText2).Execute().FetchAll();
              Assert.That(result[0][1].ToString(), Is.EqualTo(tls), "Matching the TLS version");
            }
          }
        }

        //Connection String builder
        MySqlXConnectionStringBuilder conn = new MySqlXConnectionStringBuilder(ConnectionString);
        conn.SslCert = sslCert;
        conn.SslKey = sslKey;
        conn.SslCa = sslCa;
        conn.SslMode = sslmode[i];

        if (i == 1 || i == 2)
        {
          string sslcompare = sslmodes[i];
          if (i == 2)
          {
            sslcompare = "Prefered";
          }
          Assert.Throws<ArgumentException>(() => MySQLX.GetSession(conn.ConnectionString));
        }
        else if (i != 0)
        {
          using (var session1 = MySQLX.GetSession(conn.ConnectionString))
          {
            var result = session1.SQL(CommandText1).Execute().FetchAll();
            Assert.That(result[0][1].ToString(), Is.EqualTo(result[0][1].ToString()), "Matching the Cipher");
            result = session1.SQL(CommandText2).Execute().FetchAll();
            Assert.That(result[0][1].ToString(), Is.EqualTo(tls), "Matching the TLS version");
          }
        }
      }
    }

    [Test, Description("MySQLX-scenario (wrong/no ssl-ca,correct ssl-key/ssl-cert,ssl-mode VerifyCA)")]
    public void IncorrectSslCAVerifyCAMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      string sslmode = "VerifyCA";
      string[] sslcalist = new string[] { "ca_dummy.pem", "", " ", null, "file", "file.pem" };
      for (int i = 0; i < sslcalist.Length; i++)
      {
        var connStr = ConnectionStringUri + $"?Ssl-ca={sslcalist[i]}&SslCert={sslCert}&SslKey={sslKey}&ssl-ca-pwd={sslCertificatePassword}&ssl-mode={sslmode}";
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));

        connStr = ConnectionStringUri + $"?&SslCert={sslCert}&SslKey={sslKey}&ssl-mode={sslmode}";
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));

        connStr = ConnectionString + $";Ssl-ca={sslcalist[i]};SslCert={sslCert};SslKey={sslKey};ssl-mode={sslmode}";
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));

        connStr = ConnectionString + $";SslCert={sslCert};SslKey={sslKey};ssl-mode={sslmode}";
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));

        var connObject = new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslCert,
          SslKey = sslKey,
          CertificatePassword = sslCertificatePassword,
          SslCa = sslcalist[i],
          sslmode = MySqlSslMode.VerifyCA
        };
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connObject));

        var connObject1 = new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslCert,
          SslKey = sslKey,
          CertificatePassword = sslCertificatePassword,
          sslmode = MySqlSslMode.VerifyCA
        };
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connObject1));

        MySqlConnectionStringBuilder connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslCert;
        connClassic.SslKey = sslKey;
        connClassic.SslCa = sslcalist[i];
        connClassic.SslMode = MySqlSslMode.VerifyCA;
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connClassic.ConnectionString));

        connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslCert;
        connClassic.SslKey = sslKey;
        connClassic.SslMode = MySqlSslMode.VerifyCA;
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connClassic.ConnectionString));

        MySqlXConnectionStringBuilder conn = new MySqlXConnectionStringBuilder(ConnectionString);
        conn.SslCert = sslCert;
        conn.SslKey = sslKey;
        conn.SslCa = sslcalist[i];
        conn.SslMode = MySqlSslMode.VerifyCA;
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connClassic.ConnectionString));
      }
    }

    [Test, Description("MySQLX-Scenario (correct ssl-ca,wrong/no ssl-key/ssl-cert,ssl-mode VerifyCA)")]
    public void IncorrectSslkeyAndSslcertVerifyCAMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      string sslmode = "VerifyCA";
      string[] sslcertlist = new string[] { "", " ", null, "file", "file.pem" };
      string[] sslkeylist = new string[] { "", " ", null, "file", "file.pem" };
      List<string> csAndUriList = new List<string>();
      List<object> connObjectList = new List<object>();
      for (int i = 0; i < sslcertlist.Length; i++)
      {
        csAndUriList.Clear();
        connObjectList.Clear();
        csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslCa}&SslCert={sslcertlist[i]}&SslKey={sslkeylist[i]}&ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslCa}&ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionString + $";Ssl-ca={sslCa};SslCert={sslcertlist[i]};SslKey={sslkeylist[i]};ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionString + $";Ssl-ca={sslCa};ssl-mode={sslmode}");
        foreach (string item in csAndUriList)
        {
          AssertTlsConnection(item);
        }

        connObjectList.Add(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslcertlist[i],
          SslKey = sslkeylist[i],
          CertificatePassword = sslCertificatePassword,
          SslCa = sslCa,
          sslmode = MySqlSslMode.VerifyCA
        });
        connObjectList.Add(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          CertificatePassword = sslCertificatePassword,
          SslCa = sslCa,
          sslmode = MySqlSslMode.VerifyCA
        });
        foreach (var item in connObjectList)
        {
          AssertTlsConnection(item);
        }

        MySqlConnectionStringBuilder connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = sslCa;
        connClassic.SslMode = MySqlSslMode.VerifyCA;
        AssertTlsConnection(connClassic.ConnectionString);

        connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCa = sslCa;
        connClassic.SslMode = MySqlSslMode.VerifyCA;
        AssertTlsConnection(connClassic.ConnectionString);

        MySqlXConnectionStringBuilder conn = new MySqlXConnectionStringBuilder(ConnectionString);
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = sslCa;
        conn.SslMode = MySqlSslMode.VerifyCA;
        AssertTlsConnection(conn.ConnectionString);

        conn = new MySqlXConnectionStringBuilder(ConnectionString);
        conn.SslCa = sslCa;
        conn.SslMode = MySqlSslMode.VerifyCA;
        AssertTlsConnection(conn.ConnectionString);
      }

    }

    [Test, Description("MySQLX-Scenario (wrong ssl-ca,correct ssl-key/ssl-cert,ssl-mode required and default)")]
    public void IncorrectSslCACorrectKeyAndCertRequiredMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      string sslmode = "Required";
      string[] sslcalist = new string[] { "ca_dummy.pem", "", " ", null, "file", "file.pem" };
      List<string> csAndUriList = new List<string>();
      List<object> connObjectList = new List<object>();
      for (int i = 0; i < sslcalist.Length; i++)
      {
        csAndUriList.Clear();
        connObjectList.Clear();
        //Connection String and Uris
        csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslcalist[i]}&SslCert={sslCert}&SslKey={sslKey}&ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslcalist[i]}&SslCert={sslCert}&SslKey={sslKey}");
        csAndUriList.Add(ConnectionString + $";Ssl-ca={sslcalist[i]};SslCert={sslCert};SslKey={sslKey};ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionString + $";Ssl-ca={sslcalist[i]};SslCert={sslCert};SslKey={sslKey}");
        foreach (string item in csAndUriList)
        {
          AssertTlsConnection(item);
        }
        //Anonymous Objects
        connObjectList.Add(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslCert,
          SslKey = sslKey,
          CertificatePassword = sslCertificatePassword,
          SslCa = sslcalist[i],
          sslmode = MySqlSslMode.Required
        });
        connObjectList.Add(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslCert,
          SslKey = sslKey,
          CertificatePassword = sslCertificatePassword,
          SslCa = sslcalist[i]
        });
        foreach (var item in connObjectList)
        {
          AssertTlsConnection(item);
        }
        //MySqlConnectionStringBuilder
        var connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslCert;
        connClassic.SslKey = sslKey;
        connClassic.SslCa = sslcalist[i];
        connClassic.SslMode = MySqlSslMode.Required;
        AssertTlsConnection(connClassic.ConnectionString);

        connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslCert;
        connClassic.SslKey = sslKey;
        connClassic.SslCa = sslcalist[i];
        AssertTlsConnection(connClassic.ConnectionString);
      }
    }

    [Test, Description("MySQLX-Scenario (no ssl-ca,correct ssl-key/ssl-cert,ssl-mode required and default)")]
    public void NoSslcaWithSslkeySslcertRequiredMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      string sslmode = "Required";
      List<string> csAndUriList = new List<string>();
      List<object> connObjectList = new List<object>();
      //Connection String and Uris
      csAndUriList.Add(ConnectionStringUri + $"?SslCert={sslCert}&SslKey={sslKey}&ssl-mode={sslmode}");
      csAndUriList.Add(ConnectionStringUri + $"?SslCert={sslCert}&SslKey={sslKey}");
      csAndUriList.Add(ConnectionString + $";SslCert={sslCert};SslKey={sslKey};ssl-mode={sslmode}");
      csAndUriList.Add(ConnectionString + $";SslCert={sslCert};SslKey={sslKey}");
      foreach (string item in csAndUriList)
      {
        AssertTlsConnection(item);
      }
      //Anonymous Objects
      connObjectList.Add(new
      {
        server = Host,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        SslCert = sslCert,
        SslKey = sslKey,
        CertificatePassword = sslCertificatePassword,
        sslmode = MySqlSslMode.Required
      });
      connObjectList.Add(new
      {
        server = Host,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        SslCert = sslCert,
        SslKey = sslKey,
        CertificatePassword = sslCertificatePassword,
      });
      foreach (var item in connObjectList)
      {
        AssertTlsConnection(item);
      }
    }

    [Test, Description("MySQLX-Scenario (correct ssl-ca,wrong ssl-key/ssl-cert,ssl-mode required and default)")]
    public void CorrectSslcaWrongSslkeySslcertRequiredMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      string sslmode = "Required";
      string[] sslcertlist = new string[] { "", " ", null, "file", "file.pem" };
      string[] sslkeylist = new string[] { "", " ", null, "file", "file.pem" };
      List<string> csAndUriList = new List<string>();
      List<object> connObjectList = new List<object>();
      for (int i = 0; i < sslcertlist.Length; i++)
      {
        csAndUriList.Clear();
        connObjectList.Clear();
        //Connection String and Uri
        csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslCa}&SslCert={sslcertlist[i]}&SslKey={sslkeylist[i]}&ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslCa}&SslCert={sslcertlist[i]}&SslKey={sslkeylist[i]}");
        csAndUriList.Add(ConnectionString + $";Ssl-ca={sslCa};SslCert={sslcertlist[i]};SslKey={sslkeylist[i]};ssl-mode={sslmode}");
        csAndUriList.Add(ConnectionString + $";Ssl-ca={sslCa};SslCert={sslcertlist[i]};SslKey={sslkeylist[i]}");
        foreach (string item in csAndUriList)
        {
          AssertTlsConnection(item);
        }
        //Anonymous Objects
        connObjectList.Add(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslcertlist[i],
          SslKey = sslkeylist[i],
          CertificatePassword = sslCertificatePassword,
          SslCa = sslCa,
          sslmode = MySqlSslMode.Required
        });
        connObjectList.Add(new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          password = session.Settings.Password,
          SslCert = sslcertlist[i],
          SslKey = sslkeylist[i],
          CertificatePassword = sslCertificatePassword,
          SslCa = sslCa,
        });

        foreach (var item in connObjectList)
        {
          AssertTlsConnection(item);
        }
        //MySqlConnectionStringBuilder and MySqlXConnectionStringBuilder
        MySqlConnectionStringBuilder connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = sslCa;
        connClassic.SslMode = MySqlSslMode.Required;
        AssertTlsConnection(connClassic.ConnectionString);

        connClassic = new MySqlConnectionStringBuilder(ConnectionString);
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = sslCa;
        AssertTlsConnection(connClassic.ConnectionString);

        MySqlXConnectionStringBuilder conn = new MySqlXConnectionStringBuilder(ConnectionString);
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = sslCa;
        conn.SslMode = MySqlSslMode.Required;
        AssertTlsConnection(conn.ConnectionString);

        conn = new MySqlXConnectionStringBuilder(ConnectionString);
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = sslCa;
        AssertTlsConnection(conn.ConnectionString);
      }
    }

    [Test, Description("MySQLX-Scenario (correct ssl-ca,no ssl-key/ssl-cert,ssl-mode required and default)")]
    public void CorrectSslcaNoSslkeyorCertRequiredMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      string sslmode = "Required";
      List<string> csAndUriList = new List<string>();
      //Connection string and Uris
      csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslCa}&ssl-mode={sslmode}");
      csAndUriList.Add(ConnectionStringUri + $"?Ssl-ca={sslCa}");
      csAndUriList.Add(ConnectionString + $";Ssl-ca={sslCa};ssl-mode={sslmode}");
      csAndUriList.Add(ConnectionString + $";Ssl-ca={sslCa}");
      foreach (string item in csAndUriList)
      {
        AssertTlsConnection(item);
      }
      //Anonymous Objects
      List<object> connObjectList = new List<object>();
      connObjectList.Add(new
      {
        server = Host,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        CertificatePassword = sslCertificatePassword,
        SslCa = sslCa,
        sslmode = MySqlSslMode.Required
      });
      connObjectList.Add(new
      {
        server = Host,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        CertificatePassword = sslCertificatePassword,
        SslCa = sslCa,
      });

      foreach (var item in connObjectList)
      {
        AssertTlsConnection(item);
      }
      //MySqlConnectionStringBuilder and MySqlXConnectionStringBuilder
      MySqlConnectionStringBuilder connClassic = new MySqlConnectionStringBuilder(ConnectionString);
      connClassic.SslCa = sslCa;
      connClassic.SslMode = MySqlSslMode.Required;
      AssertTlsConnection(connClassic.ConnectionString);

      connClassic = new MySqlConnectionStringBuilder(ConnectionString);
      connClassic.SslCa = sslCa;
      AssertTlsConnection(connClassic.ConnectionString);

      MySqlXConnectionStringBuilder conn = new MySqlXConnectionStringBuilder(ConnectionString);
      conn.SslCa = sslCa;
      conn.SslMode = MySqlSslMode.Required;
      AssertTlsConnection(conn.ConnectionString);

      conn = new MySqlXConnectionStringBuilder(ConnectionString);
      conn.SslCa = sslCa;
      AssertTlsConnection(conn.ConnectionString);
    }

    [Test, Description("mixed spelling ssl-ca, ssl-key/ssl-cert, ssl-mode VerifyCA and Required)")]
    public void MixedspellingSslcaSslkeySslcert()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      string[] sslmode = new string[] { "VerifyCA", "Required" };
      for (int i = 0; i < sslmode.Length; i++)
      {
        string[] sslcalist = new string[] { "Ssl-ca", "ssl-ca", "ssl-CA", "sSl-cA", "sslca", "SslCa", "SSLCA" };
        for (int j = 0; j < sslcalist.Length; j++)
        {
          var connStr = ConnectionStringUri + $"?{sslcalist[j]}={sslCa}&SslCert={sslCert}&SslKey={sslKey}&ssl-mode={sslmode[i]}";
          AssertTlsConnection(connStr);

          connStr = ConnectionString + $";{sslcalist[j]}={sslCa}&SslCert={sslCert}&SslKey={sslKey}&ssl-mode={sslmode[i]}";
          AssertTlsConnection(connStr);
        }

        string[] sslcertlist = new string[] { "Ssl-cert", "ssl-cert", "ssl-CERT", "sSl-cErt", "sslcert", "SslCert", "SSLCERT" };
        for (int j = 0; j < sslcertlist.Length; j++)
        {
          var connStr = ConnectionStringUri + $"?Ssl-ca={sslCa}&{sslcertlist[j]}={sslCert}&SslKey={sslKey}&ssl-mode={sslmode[i]}";
          AssertTlsConnection(connStr);

          connStr = ConnectionString + $";Ssl-ca={sslCa};{sslcertlist[j]}={sslCert};SslKey={sslKey}&ssl-mode={sslmode[i]}";
          AssertTlsConnection(connStr);
        }

        string[] sslkeylist = new string[] { "Ssl-key", "ssl-key", "ssl-KEY", "sSl-kEy", "sslkey", "SslKey", "SSLKEY" };
        for (int j = 0; j < sslkeylist.Length; j++)
        {
          var connStr = ConnectionStringUri + $"?Ssl-ca={sslCa}&Ssl-cert={sslCert}&{sslkeylist[j]}={sslKey}&ssl-mode={sslmode[i]}";
          AssertTlsConnection(connStr);

          connStr = ConnectionString + $";Ssl-ca={sslCa};{sslcertlist[j]}={sslCert};{sslkeylist[j]}={sslKey}&ssl-mode={sslmode[i]}";
          AssertTlsConnection(connStr);
        }
      }
    }

    [Test, Description("checking errors when invalid values are used ")]
    public void InvalidTlsversionValues()
    {
      string[] version = new string[] { "null", "v1", "[ ]", "[TLSv1.9]", "[TLSv1.1,TLSv1.7]", "ui9" };
      var conStr = $"{ConnectionString};SslCa={sslCa};SslCert={sslCert};SslKey={sslKey};ssl-ca-pwd={sslCertificatePassword}";
      foreach (string tlsVersion in version)
      {
        Assert.Throws<ArgumentException>(() => MySQLX.GetSession(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version={tlsVersion}"));
      }

      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version=[TLSv1];tls-version=[TLSv1]"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version=[TLSv1.1,TLSv1.2];tls-version=[TLSv1.1,TLSv1.2]"));
    }

    /// <summary>
    ///   Bug 30411413
    /// </summary>
    [Test, Description("bug:checking behaviour of error obtained due to repeated tls option")]
    public void RepeatedTlsOption()
    {
      var conStr = $"{ConnectionString};SslCa={sslCa};SslCert={sslCert};SslKey={sslKey};ssl-ca-pwd={sslCertificatePassword}";
      Assert.Throws<ArgumentException>(() => new MySqlConnection(conStr + ";tls-version=TLSv1.2;tls-version=TLSv1.1"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(conStr + ";tls-version=TLSv1.2;tls-version=TLSv1.2"));
    }

    [Test, Description("checking different versions of TLS")]
    public void SecurityTlsCheck()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      MySqlSslMode[] modes = { MySqlSslMode.Required, MySqlSslMode.VerifyCA, MySqlSslMode.VerifyFull };
      String[] version, ver1Tls;
      var conStrX = $"{ConnectionString};SslCa={sslCa};SslCert={sslCert};SslKey={sslKey};ssl-ca-pwd={sslCertificatePassword}";
      foreach (MySqlSslMode mode in modes)
      {
        using (Session session1 = MySQLX.GetSession(conStrX + $";ssl-mode={mode};tls-version=TLSv1.2"))
        {
          var sess = session1.SQL("select variable_value from performance_schema.session_status where variable_name='mysqlx_ssl_version'").Execute().FetchOne()[0];
          Assert.That(sess, Is.EqualTo("TLSv1.2"));
        }

        version = new string[] { "[TLSv1.1,TLSv1.2]", "[TLSv1,TLSv1.2]" };
        ver1Tls = new string[] { "TLSv1.2", "TLSv1.2" };
        for (int i = 0; i < 2; i++)
        {
          using (Session session1 = MySQLX.GetSession(conStrX + ";ssl-mode=" + mode + ";tls-version=" + version[i]))
          {
            var sess = session1.SQL("select variable_value from performance_schema.session_status where variable_name='mysqlx_ssl_version'").Execute().FetchOne()[0];
            Assert.That(sess, Is.EqualTo(ver1Tls[i]));
          }
        }
      }
    }

    [Test, Description("bug: checking behaviour of TLSv1.3 in dotnet framework 4.8")]
    [Ignore("Bug30411389")]
    public void Tlsv13Bug()
    {
      Session session1 = null;
      var conStr = $"{ConnectionString};SslCa={sslCa};SslCert={sslCert};SslKey={sslKey};ssl-ca-pwd={sslCertificatePassword}";

      using (session1 = MySQLX.GetSession(conStr + ";tls-version=TLSv1.3"))
      {
        var sess1 = session1.SQL("select variable_value from performance_schema.session_status where variable_name='mysqlx_ssl_version'").Execute().FetchOne()[0];
        Assert.That(sess1, Is.EqualTo("TLSv1.3"));
      }

      using (session1 = MySQLX.GetSession(conStr + ";tls-version=TLSv1.2"))
      {
        var sess1 = session1.SQL("select variable_value from performance_schema.session_status where variable_name='mysqlx_ssl_version'").Execute().FetchOne()[0];
        Assert.That(sess1, Is.EqualTo("TLSv1.2"));
      }

      using (session1 = MySQLX.GetSession(conStr + ";tls-version=TLSv1.2,TLSv1.3"))
      {
        var sess1 = session1.SQL("select variable_value from performance_schema.session_status where variable_name='mysqlx_ssl_version'").Execute().FetchOne()[0];
        Assert.That(sess1, Is.EqualTo("TLSv1.3"));
      }
    }

    [Test, Description("checking TLSv1.3 in Linux")]
    [Ignore("Fix this")]
    public void Tlsv13Linux()
    {
      if (Platform.IsWindows()) Assert.Ignore("This test is for Linux OS only");

      MySqlSslMode[] modes = { MySqlSslMode.Required, MySqlSslMode.VerifyCA, MySqlSslMode.VerifyFull };
      var conStr = $"{ConnectionString};SslCa={sslCa};SslCert={sslCert};SslKey={sslKey};ssl-ca-pwd={sslCertificatePassword}";
      foreach (MySqlSslMode mode in modes)
      {
        string[] version = new string[] { "TLSv1.3", "[TLSv1.3]" };

        foreach (string tlsVersion in version)
        {
          Assert.That(MySQLX.GetSession(conStr + ";ssl-mode=" + mode + ";tls-version=" + tlsVersion).InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        }

        version = new string[] { "[TLSv1,TLSv1.3]", "[TLSv1.1,TLSv1.3]", "[TLSv1,TLSv1.2,TLSv1.3]", "[TLSv1.2,TLSv1.3]", "[TLSv1,TLSv1.1,TLSv1.2,TLSv1.3]" };

        for (int i = 0; i < version.Length; i++)
        {
          using (var session1 = MySQLX.GetSession(conStr + ";ssl-mode=" + mode + ";tls-version=" + version[i]))
          {
            var sess = session1.SQL("select variable_value from performance_schema.session_status where variable_name='mysqlx_ssl_version'").Execute().FetchOne()[0];
            Assert.That(sess.ToString().Contains("TLSv1"));
          }
        }
      }
    }

    private void AssertTlsConnection(string inputString)
    {
      string CommandText1 = "SHOW STATUS LIKE '%Ssl_cipher%';";
      string CommandText2 = "show  status like '%Ssl_version%';";
      string cipher = "ECDHE-RSA-AES256-GCM-SHA384";
      string tls = "TLSv1.2";
      using (var session1 = MySQLX.GetSession(inputString))
      {
        var result = session1.SQL(CommandText1).Execute().FetchAll();
        Assert.That(result[0][1].ToString(), Is.EqualTo(cipher), "Matching the Cipher");
        result = session1.SQL(CommandText2).Execute().FetchAll();
        Assert.That(result[0][1].ToString(), Is.EqualTo(tls), "Matching the TLS version");
      }
    }

    private void AssertTlsConnection(object inputObject)
    {
      string CommandText1 = "SHOW STATUS LIKE '%Ssl_cipher%';";
      string CommandText2 = "show  status like '%Ssl_version%';";
      string cipher = "ECDHE-RSA-AES256-GCM-SHA384";
      string tls = "TLSv1.2";
      using (var session1 = MySQLX.GetSession(inputObject))
      {
        var result = session1.SQL(CommandText1).Execute().FetchAll();
        Assert.That(result[0][1].ToString(), Is.EqualTo(cipher), "Matching the Cipher");
        result = session1.SQL(CommandText2).Execute().FetchAll();
        Assert.That(result[0][1].ToString(), Is.EqualTo(tls), "Matching the TLS version");
      }
    }

    /// <summary>
    /// WL14828 - Align TLS option checking across connectors
    /// </summary>
    [TestCase(MySqlSslMode.Disabled)]
    [TestCase(MySqlSslMode.Disabled, "ssl-ca=foo")]
    [TestCase(MySqlSslMode.Disabled, "ssl-cert=foo")]
    [TestCase(MySqlSslMode.Disabled, "ssl-key=foo")]
    [TestCase(MySqlSslMode.Disabled, "tls-version=TLSv1.2")]
    [Theory]
    public void SslOptionsCombinedWhenDisabled(MySqlSslMode sslMode, string sslOption = "")
    {
      // ConnectionString
      var connStr = ConnectionString + $";ssl-mode={sslMode};{sslOption}";

      using var session = MySQLX.GetSession(connStr);
      var encryption = session.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_cipher'").Execute().FetchAll()[0][1].ToString();

      Assert.That(encryption, Is.Empty);
      Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));

      // ConnectionString Uri
      var connStrUri = ConnectionStringUri + $"?ssl-mode={sslMode}&{sslOption}";

      using var sessionUri = MySQLX.GetSession(connStrUri);
      encryption = sessionUri.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_cipher'").Execute().FetchAll()[0][1].ToString();

      Assert.That(encryption, Is.Empty);
      Assert.That(sessionUri.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
    }

    [Test]
    public void SslRequiredAndDisabled()
    {
      var connStrUri = ConnectionStringUri + $"?ssl-mode={MySqlSslMode.Required}&ssl-mode={MySqlSslMode.Disabled}";

      using var sessionUriDisabled = MySQLX.GetSession(connStrUri);
      var encryption = sessionUriDisabled.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_cipher'").Execute().FetchAll()[0][1].ToString();

      Assert.That(encryption, Is.Empty);
      Assert.That(sessionUriDisabled.InternalSession.SessionState, Is.EqualTo(SessionState.Open));

      connStrUri = ConnectionStringUri + $"?ssl-mode={MySqlSslMode.Disabled}&ssl-mode={MySqlSslMode.Required}";

      using var sessionUriRequired = MySQLX.GetSession(connStrUri);
      encryption = sessionUriRequired.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_cipher'").Execute().FetchAll()[0][1].ToString();

      Assert.That(encryption, Is.Not.Empty);
      Assert.That(sessionUriRequired.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
    }
  }
}
