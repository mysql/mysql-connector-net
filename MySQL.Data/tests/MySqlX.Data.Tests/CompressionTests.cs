// Copyright (c) 2019, 2021, Oracle and/or its affiliates.
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
using System.Collections.Generic;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Compression/decompression based unit tests.
  /// </summary>
  public class CompressionTests : BaseTest
  {
    private const string DEFLATE_STREAM = "DEFLATE_STREAM";
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

      // Validate zstd_stream is the default.
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        session.Close();
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4_message"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        session.Close();
      }

#if !NETFRAMEWORK && DEBUG
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=deflate_stream"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.AreEqual(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.AreEqual(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        session.Close();
      }
#endif
    }

    [Test]
    public void NegotiationWithSpecificCompressionAlgorithm()
    {
      bool success = true;

      var updatedConnectionStringUri = ConnectionStringUri + "?compression=Required";
      try
      {
        // Test with one of the supported compression algorithms.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.zstd_stream.ToString().ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }

        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.lz4_message.ToString().ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms ={DEFLATE_STREAM}");
#if NETFRAMEWORK
                var exception = Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(updatedConnectionStringUri));
                Assert.AreEqual("Compression requested but the compression algorithm negotiation failed.", exception.Message);
#else
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        }
#endif

        // Test with a sublist of supported compression algorithms.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.zstd_stream.ToString().ToUpperInvariant()},{CompressionAlgorithms.lz4_message.ToString().ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }
      }
      catch (Exception ex)
      {
        TestContext.WriteLine("Exception: " + ex.Message);
        success = false;
      }
      finally
      {
        // This line ensures that the list of supported compression algorithms is set to its default value.
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""ZSTD_STREAM,LZ4_MESSAGE,DEFLATE_STREAM"" ");

        Assert.True(success);
      }
    }

    [Test]
    public void ValidateZstdAllocation()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        if (!(CompressionAlgorithms.zstd_stream.ToString() == compressionAlgorithm))
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

    [Test]
    // WL-14001 XProtocol -- support for configurable compression algorithms
    public void ConfigurableCompressionAlgorithms()
    {
      bool success = true;
      try
      {
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""ZSTD_STREAM,LZ4_MESSAGE,DEFLATE_STREAM"" ");
        // FR1_1 Create session with option compression-algorithms for URI, connectionstring, anonymous object, MySqlXConnectionStringBuilder.
        using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4_message;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=lz4_message;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

#if NETFRAMEWORK
         // No exception expected due to compression=preferred, no compression expected
         using (var session = MySQLX.GetSession(new { server = "localhost", port = $"{XPort}", uid = "test", password = "test", compressionalgorithms = "deflate_stream" }))
         {
           var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
           Assert.IsNull(compressionAlgorithm);
         }
#else
        using (var session = MySQLX.GetSession(new { server = "localhost", port = 33060, uid = "test", password = "test", compressionalgorithms = "deflate_stream" }))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          StringAssert.AreEqualIgnoringCase(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        }
#endif

        var sb = new MySqlXConnectionStringBuilder("server=localhost;port=33060;uid=test;password=test;compression-algorithms=lz4_message");
        using (var session = MySQLX.GetSession(sb.GetConnectionString(true)))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        // FR1_2 Create session with option compression-algorithms and set the option with no value either by not including the property in the connection string 
        // or by setting it with an empty value.
        using (var session = MySQLX.GetSession("server=localhost;port=33060;uid=test;password=test;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms="))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }

        // FR2_1,FR2_2 Create session with option compression-algorithms and set the value with multiple compression algorithms for 
        // URI,connectionstring,anonymous object,MySqlXConnectionStringBuilder.check that the negotiation happens in the order provided in the connection string

        using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4_message,zstd_stream,deflate_stream;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=lz4_message,zstd_stream,deflate_stream;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

#if NETFRAMEWORK
         // No exception expected due to compression=preferred, lz4_message compression expected
         using (var session = MySQLX.GetSession(new { server = "localhost", port = $"{XPort}", uid = "test", password = "test", compressionalgorithms = "deflate_stream,lz4_message,zstd_stream" }))
         {
           var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
           Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
         }
#else
        using (var session = MySQLX.GetSession(new { server = "localhost", port = 33060, uid = "test", password = "test", compressionalgorithms = "deflate_stream,lz4_message,zstd_stream" }))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          StringAssert.AreEqualIgnoringCase(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        }
#endif

        sb = new MySqlXConnectionStringBuilder(ConnectionString + ";compression-algorithms=lz4_message,zstd_stream,deflate_stream");
        using (var session = MySQLX.GetSession(sb.GetConnectionString(true)))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        // FR3 Create session with option compression-algorithms and set the option with Algorithm aliases lz4, zstd, and deflate.
        using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=lz4,zstd,deflate;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=ZSTD,deflate_stream"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }

        // FR4_1 Create session with option compression-algorithms.Set the option with unsupported and supported algorithms by client.
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=NotSupported,lz4,SomethingElse;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=ZSTD,NotSupported"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }

        sb = new MySqlXConnectionStringBuilder("server=localhost;port=33060;uid=test;password=test;compression-algorithms=[NotValid,INVALID,NOTSUPPORTED,zstd]");
        using (var session = MySQLX.GetSession(sb.GetConnectionString(true)))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.zstd_stream.ToString(), compressionAlgorithm);
        }

        // FR4_2 Create session and set invalid values to the compression-algorithm option to check if the connection is uncompressed when 
        // compression option is either not set or set to preferred or disabled.
        using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=NotSupported,SomethingElse;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression=disabled;compression-algorithms=lz4,NotSupported,SomethingElse;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression=preferred;compression-algorithms=[NotSupported,SomethingElse];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);
        }

        // FR4_3 Create session and set invalid values to the compression-algorithm option.
        // The connection should terminate with an error when compression option is set to required.

        Exception ex = Assert.Throws<System.NotSupportedException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=NotSupported,SomethingElse;"));
        Assert.AreEqual("Compression requested but the compression algorithm negotiation failed.", ex.Message);

        // FR4_4 Start server with specific compression algorithm and create session with option 
        // compression-algorithms.Set the option with multiple compression algorithms.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.lz4_message.ToString().ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=preferred;compression-algorithms=[lz4_message,deflate,zstd];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        // FR4_5 Start the server with a specific compression algorithm and use some other in the client and when compression option is either 
        // not set or set to preferred or disabled.Verify that the connection is uncompressed.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.zstd_stream.ToString().ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=[lz4_message]"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression=preferred;compression-algorithms=[lz4_message]"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression=disabled;compression-algorithms=[lz4_message]"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);
        }

        //FR4_6,FR_5 Start the server with a specific compression algorithm and use some other in the client and when compression option is set to required.Verify the behaviour
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""LZ4_MESSAGE"" ");
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
          var ele = new List<object>();
          for (int i = 0; i < 1000; i++)
          {
            ele.Add(new { id = $"{i}", title = $"Book {i}" });
          }
          //Verify compression is being done
          Collection coll = CreateCollection("testcompress1");
          var result = ExecuteAddStatement(coll.Add(ele.ToArray()));
          var result1 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
          var result2 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
          Assert.Greater(int.Parse(result1.ToString()), int.Parse(result2.ToString()));
          var result3 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
          var result4 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
          Assert.Greater(int.Parse(result3.ToString()), int.Parse(result4.ToString()));
        }

        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[lz4_message]"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.lz4_message.ToString(), compressionAlgorithm);
        }

        // Server algorithm not contain user defined algorithm, with compression preferred
        using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=[zstd];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNull(compressionAlgorithm);

          var ele = new List<object>();
          for (int i = 0; i < 1000; i++)
          {
            ele.Add(new { id = $"{i}", title = $"Book {i}" });
          }
          //Verify there is no compression 
          Collection coll = CreateCollection("testcompress2");
          var result = ExecuteAddStatement(coll.Add(ele.ToArray()));
          var result1 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
          var result2 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
          Assert.AreEqual(result1, result2);
          var result3 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
          var result4 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
          Assert.AreEqual(result3, result4);
        }

        Exception ex_args = Assert.Throws<System.ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression_algorithms=[lz4_message]"));
        StringAssert.Contains("Option not supported", ex_args.Message);

      }
      catch (Exception ex)
      {
        TestContext.WriteLine("Exception: "+ ex.Message);
        success = false;
      }
      finally
      {
        // This line ensures that the list of supported compression algorithms is set to its default value.
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""ZSTD_STREAM,LZ4_MESSAGE,DEFLATE_STREAM"" ");
        Assert.True(success);
      }
    }

    [Test]
    public void CompressionAlgorithms_Bugs()
    {
      bool success = true;
      try
      {
        // Bug #31544072
#if NETFRAMEWORK
        // Different algorithms available in server hence default compression expected
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNotNull(compressionAlgorithm);
        }
        // With only deflate available,Exeption expected 
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""DEFLATE_STREAM"" ");
        Exception ex_bug1 = Assert.Throws<System.NotSupportedException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"));
        StringAssert.Contains("Compression requested but the compression algorithm negotiation failed", ex_bug1.Message);
#else
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.IsNotNull(compressionAlgorithm);
        }
        // With only deflate available,compression is expected 
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""DEFLATE_STREAM"" ");
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        }
#endif

        // Bug #31541819
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""DEFLATE_STREAM"" ");
#if NETFRAMEWORK
        // Exeption expected due to compression=required
        Exception ex_bug2 = Assert.Throws<System.NotSupportedException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=deflate_stream;"));
        StringAssert.Contains("is not supported in .NET Framework", ex_bug2.Message);
#else
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=deflate_stream;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.AreEqual(CompressionAlgorithms.deflate_stream.ToString(), compressionAlgorithm);
        }
#endif
      }
      catch (Exception ex)
      {
        TestContext.WriteLine("Exception: " + ex.Message);
      }
      finally
      {
        // This line ensures that the list of supported compression algorithms is set to its default value.
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""ZSTD_STREAM,LZ4_MESSAGE,DEFLATE_STREAM"" ");
        Assert.True(success);
      }
    }
  }
}