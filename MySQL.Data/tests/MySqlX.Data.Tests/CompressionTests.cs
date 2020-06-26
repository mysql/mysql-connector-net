// Copyright (c) 2019, 2020 Oracle and/or its affiliates.
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
using NUnit.Framework;
using MySqlX.Communication;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Compression/decompression based unit tests.
  /// </summary>
  public class CompressionTests : BaseTest
  {
    [Test]
    public void ConnectionOptionIsValidUsingBuilder()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.Compression = CompressionType.Preferred;
      StringAssert.Contains("compression=Preferred", builder.ToString());

      builder.Compression = CompressionType.Required;
      StringAssert.Contains("compression=Required", builder.ToString());

      builder.Compression = CompressionType.Disabled;
      StringAssert.Contains("compression=Disabled", builder.ToString());
    }

    [Test]
    public void ConnectionOptionIsValidUsingConnectionUri()
    {
      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=PreFerRed"))
      {
        Assert.AreEqual(CompressionType.Preferred, session.Settings.Compression);
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=required"))
      {
        Assert.AreEqual(CompressionType.Required, session.Settings.Compression);
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=DISABLED"))
      {
        Assert.AreEqual(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }

      // Test whitespace
      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression= DISABLED"))
      {
        Assert.AreEqual(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression= DISABLED  "))
      {
        Assert.AreEqual(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }
    }

    [Test]
    public void ConnectionOptionIsValidUsingAnonymousObject()
    {
      var connectionData = new
      {
        server = "localhost",
        user = "test",
        password = "test",
        port = 33060,
        compression = CompressionType.Required
      };

      using (var session = MySQLX.GetSession(connectionData))
      {
        Assert.AreEqual(CompressionType.Required, session.Settings.Compression);
        session.Close();
      }
    }

    [Test]
    public void ConnectionOptionIsValidUsingConnectionString()
    {
      var builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=PreFerRed");
      Assert.AreEqual(CompressionType.Preferred, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=required");
      Assert.AreEqual(CompressionType.Required, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=DISABLED");
      Assert.AreEqual(CompressionType.Disabled, builder.Compression);

      // Test whitespace
      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=  required");
      Assert.AreEqual(CompressionType.Required, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=    required");
      Assert.AreEqual(CompressionType.Required, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=  required  ");
      Assert.AreEqual(CompressionType.Required, builder.Compression);
    }

    [Test]
    public void PreferredIsTheDefaultValue()
    {
      var builder = new MySqlXConnectionStringBuilder();
      Assert.AreEqual(CompressionType.Preferred, builder.Compression);

      // Empty value is ignored.
      var updatedConnectionStringUri = ConnectionStringUri + "?compression=";
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        Assert.AreEqual(CompressionType.Preferred, session.Settings.Compression);
        session.Close();
      }

      // Whitespace is ignored.
      updatedConnectionStringUri = ConnectionStringUri + "?compression= ";
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        Assert.AreEqual(CompressionType.Preferred, session.Settings.Compression);
        session.Close();
      }
    }

    [Test]
    public void SettingAnInvalidCompressionTypeRaisesException()
    {
      string[] invalidValues = { "test", "true", "123" };
      foreach (var invalidValue in invalidValues)
      {
        var exception = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder($"server=localhost;port=33060;compression={invalidValue}"));
        Assert.AreEqual($"The connection property 'compression' acceptable values are: 'preferred', 'required' or 'disabled'. The value '{invalidValue}' is not acceptable.", exception.Message);

        exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"server=localhost;port=33060;user=root;compression={invalidValue}"));
        Assert.AreEqual($"The connection property 'compression' acceptable values are: 'preferred', 'required' or 'disabled'. The value '{invalidValue}' is not acceptable.", exception.Message);
      }
    }

    [Test]
    public void SessionRetainsTheSpecifiedCompressionType()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.AreEqual(CompressionType.Preferred, session.Settings.Compression);
        session.Close();
      }

      var updatedConnectionStringUri = ConnectionStringUri + "?compression=Disabled";
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        Assert.AreEqual(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }
    }

    [Test]
    public void ValidateRequiredCompressionType()
    {
      // Compression supported starting server 8.0.19.
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 19))
      {
        var exception = Assert.Throws<NotSupportedException>(() => MySQLX.GetSession($"{ConnectionStringUri}?compression=Required"));
        Assert.AreEqual("Compression requested but the server does not support it.", exception.Message);

        return;
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=Required"))
      {
        session.Close();
      }
    }

    [Test]
    public void NegotiationSucceedsWithExpectedCompressionAlgorithm()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 19))
        return;

      bool success = true;

      try
      {
        // Validate zstd_stream is the default.
        using (var session = MySQLX.GetSession(ConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
          compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
          Assert.AreEqual(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
          session.Close();
        }

        // Update client supported list to lz4_message.
#if DEBUG
        XCompressionController.ClientSupportedCompressionAlgorithms = new string[]
        {
        XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM,
        };
#endif

        using (var session = MySQLX.GetSession(ConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM, compressionAlgorithm);
          compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
          Assert.AreEqual(XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM, compressionAlgorithm);
          session.Close();
        }

#if !NET452 && DEBUG
        // Update client supported list to deflate_stream.
        XCompressionController.ClientSupportedCompressionAlgorithms = new string[]
        {
        XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM,
        };

        using (var session = MySQLX.GetSession(ConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
          compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
          Assert.AreEqual(XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
          session.Close();
        }
#endif
      }
      catch (Exception)
      {
        success = false;
      }
      finally
      {
#if DEBUG
        // Reset it to its original value to prevent conflicts with other tests.
        XCompressionController.ClientSupportedCompressionAlgorithms = new string[]
        {
        XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM,
        XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM,
#if !NET452
        XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM
#endif
        };
#endif
        Assert.True(success);
      }
    }

    [Test]
    public void NegotiationWithSpecificCompressionAlgorithm()
    {
      bool success = true;

      var updatedConnectionStringUri = ConnectionStringUri + "?compression=Required";
      try
      {
        // Test with one of the supported compression algorithms.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM.ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        }

        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM.ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM, compressionAlgorithm);
        }

        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM.ToUpperInvariant()}\"");
#if NET452
                var exception = Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(updatedConnectionStringUri));
                Assert.AreEqual("Compression requested but the compression algorithm negotiation failed.", exception.Message);
#else
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        }
#endif

        // Test with a sublist of supported compression algorithms.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM.ToUpperInvariant()},{XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM.ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        }
      }
      catch (Exception)
      {
        success = false;
      }
      finally
      {
        // This line ensures that the list of supported compression algorithms is set to its default value.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM.ToUpperInvariant()},{XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM.ToUpperInvariant()},{XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM.ToUpperInvariant()}\"");

        Assert.True(success);
      }
    }

    [Test]
    public void ValidateZstdAllocation()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        if (!(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM == compressionAlgorithm))
        {
          return;
        }
      }

      // Ensure resources are being released on each session.
      // If a memory allocation error is raised then a resource has not been released.
      for (int i = 0; i < 4000; i++)
      {
        var session = MySQLX.GetSession(ConnectionStringUri);
        session.Close();
      }
    }
  }
}