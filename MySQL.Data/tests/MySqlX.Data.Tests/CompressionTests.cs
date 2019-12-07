// Copyright (c) 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.X.Communication;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Compression/decompression based unit tests.
  /// </summary>
  public class CompressionTests : BaseTest
  {
    [Fact]
    public void ConnectionOptionIsValidUsingBuilder()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.Compression = CompressionType.Preferred;
      Assert.Contains("compression=Preferred", builder.ToString());

      builder.Compression = CompressionType.Required;
      Assert.Contains("compression=Required", builder.ToString());

      builder.Compression = CompressionType.Disabled;
      Assert.Contains("compression=Disabled", builder.ToString());
    }

    [Fact]
    public void ConnectionOptionIsValidUsingConnectionUri()
    {
      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=PreFerRed"))
      {
        Assert.Equal(CompressionType.Preferred, session.Settings.Compression);
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=required"))
      {
        Assert.Equal(CompressionType.Required, session.Settings.Compression);
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=DISABLED"))
      {
        Assert.Equal(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }

      // Test whitespace
      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression= DISABLED"))
      {
        Assert.Equal(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression= DISABLED  "))
      {
        Assert.Equal(CompressionType.Disabled, session.Settings.Compression);
        session.Close();
      }
    }

    [Fact]
    public void ConnectionOptionIsValidUsingAnonymousObject()
    {
      var connectionData = new {
        server = "localhost",
        user = "test",
        password = "test",
        port = 33060,
        compression = CompressionType.Required
      };

      using (var session = MySQLX.GetSession(connectionData))
      {
        Assert.Equal(CompressionType.Required, session.Settings.Compression);
        session.Close();
      }
    }

    [Fact]
    public void ConnectionOptionIsValidUsingConnectionString()
    {
      var builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=PreFerRed");
      Assert.Equal(CompressionType.Preferred, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=required");
      Assert.Equal(CompressionType.Required, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=DISABLED");
      Assert.Equal(CompressionType.Disabled, builder.Compression);

      // Test whitespace
      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=  required");
      Assert.Equal(CompressionType.Required, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=    required");
      Assert.Equal(CompressionType.Required, builder.Compression);

      builder = new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=  required  ");
      Assert.Equal(CompressionType.Required, builder.Compression);
    }

    [Fact]
    public void PreferredIsTheDefaultValue()
    {
      var builder = new MySqlXConnectionStringBuilder();
      Assert.Equal(CompressionType.Preferred, builder.Compression);
    }

    [Fact]
    public void SettingAnInvalidCompressionTypeRaisesException()
    {
      var exception = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=test"));
      Assert.Equal("The connection property 'compression' acceptable values are: 'preferred', 'required' or 'disabled'. The value 'test' is not acceptable.", exception.Message);

      exception = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("server=localhost;port=33060;compression=true"));
      Assert.Equal("The connection property 'compression' acceptable values are: 'preferred', 'required' or 'disabled'. The value 'true' is not acceptable.", exception.Message);
    }

    [Fact]
    public void SessionRetainsTheSpecifiedCompressionType()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(CompressionType.Preferred, session.Settings.Compression);
        session.Close();
      }
    }

    [Fact]
    public void ValidateRequiredCompressionType()
    {
      // Compression supported starting server 8.0.19.
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 19))
      {
        var exception = Assert.Throws<NotSupportedException>(() => MySQLX.GetSession($"{ConnectionStringUri}?compression=Required"));
        Assert.Equal("Compression requested but the server does not support it.", exception.Message);

        return;
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=Required"))
      {
        session.Close();
      }
    }

    [Fact]
    public void NegotiationSucceedsWithExpectedCompressionAlgorithm()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 19))
      {
        return;
      }

      // Validate zstd_stream is the default.
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.Equal(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.Equal(XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        session.Close();
      }

      // Update client supported list to lz4_message.
      XCompressionController.ClientSupportedCompressionAlgorithms = new string[]
      {
        XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM,
      };

      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.Equal(XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM, compressionAlgorithm);
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.Equal(XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM, compressionAlgorithm);
        session.Close();
      }

#if !NET452
      // Update client supported list to deflate_stream.
      XCompressionController.ClientSupportedCompressionAlgorithms = new string[]
      {
        XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM,
      };

      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.Equal(XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.Equal(XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM, compressionAlgorithm);
        session.Close();
      }
#endif

      // Reset it to its original value to prevent conflicts with other tests.
      XCompressionController.ClientSupportedCompressionAlgorithms = new string[]
      {
        XCompressionController.ZSTD_STREAM_COMPRESSION_ALGORITHM,
        XCompressionController.LZ4_MESSAGE_COMPRESSION_ALGORITHM,
#if !NET452
        XCompressionController.DEFLATE_STREAM_COMPRESSION_ALGORITHM
#endif
      };
    }
  }
}
