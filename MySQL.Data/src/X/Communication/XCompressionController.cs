// Copyright (c) 2019, 2023, Oracle and/or its affiliates.
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

using K4os.Compression.LZ4.Streams;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ZstdSharp;

namespace MySqlX.Communication
{
  /// <summary>
  /// Provides support for configuring X Protocol compressed messages.
  /// </summary>
  internal class XCompressionController
  {
    #region Constants

    /// <summary>
    /// The capabilities sub-key used to specify the compression algorithm.
    /// </summary>
    internal const string ALGORITHMS_SUBKEY = "algorithm";

    /// <summary>
    /// The capabilities key used to specify the compression capability.
    /// </summary>
    internal const string COMPRESSION_KEY = "compression";

    /// <summary>
    /// Messages with a value lower than this threshold will not be compressed.
    /// </summary>
    internal const int COMPRESSION_THRESHOLD = 1000;

    /// <summary>
    /// Default value for enabling or disabling combined compressed messages.
    /// </summary>
    internal const bool DEFAULT_SERVER_COMBINE_MIXED_MESSAGES_VALUE = true;

    /// <summary>
    /// Default value for the maximum number of combined compressed messages contained in a compression message.
    /// </summary>
    internal const int DEFAULT_SERVER_MAX_COMBINE_MESSAGES_VALUE = 100;

    /// <summary>
    /// The capabilities sub-key used to specify if combining compressed messages is permitted.
    /// </summary>
    internal const string SERVER_COMBINE_MIXED_MESSAGES_SUBKEY = "server_combine_mixed_messages";

    /// <summary>
    /// The capabilities sub-key used to specify the maximum number of compressed messages contained in a compression message.
    /// </summary>
    internal const string SERVER_MAX_COMBINE_MESSAGES_SUBKEY = "server_max_combine_messages";

    #endregion

    #region Fields

    /// <summary>
    /// Buffer used to store the data received from the server.
    /// </summary>
    private MemoryStream _buffer;

    /// <summary>
    /// Deflate stream used for compressing data.
    /// </summary>
    private DeflateStream _deflateCompressStream;

    /// <summary>
    /// Deflate stream used for decompressing data.
    /// </summary>
    private DeflateStream _deflateDecompressStream;

    /// <summary>
    /// Flag indicating if the initialization is for compression or decompression.
    /// </summary>
    private bool _initializeForCompression;

    /// <summary>
    /// Stores the communication packet generated the last time ReadNextBufferedMessage method was called.
    /// </summary>
    CommunicationPacket _lastCommunicationPacket;

    /// <summary>
    /// Stream used to store multiple X Protocol messages.
    /// </summary>
    MemoryStream _multipleMessagesStream;

    /// <summary>
    /// ZStandard stream used for decompressing data.
    /// </summary>
    private DecompressionStream _zstdDecompressStream;

    #endregion

    /// <summary>
    /// Main constructor used to set the compression algorithm and initialize the list of messages to
    /// be compressed by the client.
    /// </summary>
    /// <param name="compressionAlgorithm">The compression algorithm to use.</param>
    /// <param name="initializeForCompression">Flag indicating if the initialization is for compression or decompression.</param>
    public XCompressionController(CompressionAlgorithms compressionAlgorithm, bool initializeForCompression)
    {
      if (!Enum.IsDefined(typeof(CompressionAlgorithms), compressionAlgorithm))
      {
        throw new NotSupportedException(string.Format(ResourcesX.CompressionAlgorithmNotSupported, compressionAlgorithm));
      }

      CompressionAlgorithm = compressionAlgorithm;
      _initializeForCompression = initializeForCompression;

      // Set the list of messages that should be compressed.
      ClientSupportedCompressedMessages = new List<ClientMessageId>
      {
        ClientMessageId.CRUD_DELETE,
        ClientMessageId.CRUD_FIND,
        ClientMessageId.CRUD_INSERT,
        ClientMessageId.CRUD_UPDATE,
        ClientMessageId.SQL_STMT_EXECUTE
      };

      // Initialize stream objects.
      _buffer = new MemoryStream();
      switch (CompressionAlgorithm)
      {
        case CompressionAlgorithms.zstd_stream:
          if (!_initializeForCompression)
          {
            _zstdDecompressStream = new DecompressionStream(_buffer);
          }
          break;
#if !NETFRAMEWORK
        case CompressionAlgorithms.deflate_stream:
          if (_initializeForCompression)
          {
            _deflateCompressStream = new DeflateStream(_buffer, CompressionMode.Compress, true);
          }
          else
          {
            _deflateDecompressStream = new DeflateStream(_buffer, CompressionMode.Decompress, true);
          }

          break;
#endif
      }
    }

    /// <summary>
    /// Gets or sets the list of messages that should be compressed by the client when compression is enabled.
    /// </summary>
    internal List<ClientMessageId> ClientSupportedCompressedMessages { get; private set; }

    /// <summary>
    /// Gets or sets the compression algorithm.
    /// </summary>
    internal CompressionAlgorithms? CompressionAlgorithm { get; private set; }

    /// <summary>
    /// Flag indicating if compression is enabled.
    /// </summary>
    internal bool IsCompressionEnabled => CompressionAlgorithm != null;

    /// <summary>
    /// Flag indicating if the last decompressed message contains multiple messages.
    /// </summary>
    internal bool LastMessageContainsMultipleMessages { get; private set; }

    /// <summary>
    /// General method used to compress data using the compression algorithm defined in the constructor.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <returns>A compressed byte array.</returns>
    internal byte[] Compress(byte[] input)
    {
      if (!IsCompressionEnabled)
      {
        throw new Exception(ResourcesX.CompressionNotEnabled);
      }

      switch (CompressionAlgorithm)
      {
        case (CompressionAlgorithms.zstd_stream):
          return CompressUsingZstdStream(input);
        case (CompressionAlgorithms.lz4_message):
          return CompressUsingLz4Message(input);
#if !NETFRAMEWORK
        case (CompressionAlgorithms.deflate_stream):
          return CompressUsingDeflateStream(input);
#endif
        default:
          throw new NotSupportedException(string.Format(ResourcesX.CompressionAlgorithmNotSupported, CompressionAlgorithm));
      }
    }

    /// <summary>
    /// Compresses data using the deflate_stream algorithm.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <returns>A compressed byte array.</returns>
    public byte[] CompressUsingDeflateStream(byte[] input)
    {
      bool cacheCapacityIsZero = _buffer.Capacity == 0;
      if (cacheCapacityIsZero)
      {
        _buffer.WriteByte(0x78);
        _buffer.WriteByte(0x9c);
      }

#if !NETFRAMEWORK
      _deflateCompressStream.Write(input, 0, input.Length);
      _deflateCompressStream.Flush();
      var compressedData = _buffer.ToArray();
      _buffer.SetLength(0);

      return compressedData;
#else
      throw new NotSupportedException(string.Format(ResourcesX.CompressionForSpecificAlgorithmNotSupportedInNetFramework, "deflate_stream"));
#endif
    }

    /// <summary>
    /// Compresses data using the lz4_message algorithm.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <returns>A compressed byte array.</returns>
    private byte[] CompressUsingLz4Message(byte[] input)
    {
      byte[] compressedData;
      using (var source = new MemoryStream(input))
      using (var memory = new MemoryStream())
      using (var target = LZ4Stream.Encode(memory))
      {
        source.CopyTo(target);
        target.Close();
        compressedData = memory.ToArray();
      }

      return compressedData;
    }

    /// <summary>
    /// Compresses data using the zstd_stream algorithm.
    /// </summary>
    /// <param name="input">The data to compress.</param>
    /// <returns>A compressed byte array.</returns>
    private byte[] CompressUsingZstdStream(byte[] input)
    {
      byte[] compressedData;
      using (var memoryStream = new MemoryStream())
      using (var compressionStream = new CompressionStream(memoryStream))
      {
        compressionStream.Write(input, 0, input.Length);
        compressionStream.Flush();
        compressionStream.Close();
        compressedData = memoryStream.ToArray();
      }

      return compressedData;
    }

    /// <summary>
    /// General method used to decompress data using the compression algorithm defined in the constructor.
    /// </summary>
    /// <param name="input">The data to decompress.</param>
    /// <param name="length">The expected length of the decompressed data.</param>
    /// <returns>A decompressed byte array.</returns>
    internal byte[] Decompress(byte[] input, int length)
    {
      if (!IsCompressionEnabled)
      {
        throw new Exception(ResourcesX.CompressionNotEnabled);
      }

      if (LastMessageContainsMultipleMessages)
      {
        throw new Exception(ResourcesX.CompressionPendingMessagesToProcess);
      }

      byte[] decompressedData;
      switch (CompressionAlgorithm)
      {
        case (CompressionAlgorithms.zstd_stream):
          decompressedData = DecompressUsingZstdStream(input, length);
          break;
        case (CompressionAlgorithms.lz4_message):
          decompressedData = DecompressUsingLz4Message(input, length);
          break;
#if !NETFRAMEWORK
        case (CompressionAlgorithms.deflate_stream):
          decompressedData = DecompressUsingDeflateStream(input, length);
          break;
#endif
        default:
          throw new NotSupportedException(string.Format(ResourcesX.CompressionAlgorithmNotSupported, CompressionAlgorithm));
      }

      if (decompressedData == null)
      {
        return null;
      }

      var messageSizeBytes = new byte[4];
      Buffer.BlockCopy(decompressedData, 0, messageSizeBytes, 0, messageSizeBytes.Length);
      var firstMessageSize = BitConverter.ToInt32(messageSizeBytes, 0) + 4;
      LastMessageContainsMultipleMessages = firstMessageSize < decompressedData.Length;
      if (!LastMessageContainsMultipleMessages
          || (_multipleMessagesStream != null
              && _multipleMessagesStream.Position == _multipleMessagesStream.Length))
      {
        return decompressedData;
      }
      else
      {
        _multipleMessagesStream = new MemoryStream(decompressedData);
        return ReadNextBufferedMessage();
      }
    }

    /// <summary>
    /// Decompresses data using the deflate_stream compression algorithm.
    /// </summary>
    /// <param name="input">The data to decompress.</param>
    /// <param name="length">The expected length of the decompressed data.</param>
    /// <returns>A decompressed byte array.</returns>
    private byte[] DecompressUsingDeflateStream(byte[] input, int length)
    {
      if (input[0] == 0x78 && (input[1] == 0x9c || input[1] == 0x5E))
      {
        _buffer.Write(input, 2, input.Length - 2);
      }
      else
      {
        _buffer.Write(input, 0, input.Length);
      }

      _buffer.Position = 0;
      var target = new MemoryStream();
      _deflateDecompressStream.CopyTo(target, length);
      _deflateDecompressStream.Flush();
      var decompressedData = target.ToArray();
      _buffer.SetLength(0);
      target.Dispose();

      return decompressedData;
    }

    /// <summary>
    /// Decompresses data using the lz4_message compression algorithm.
    /// </summary>
    /// <param name="input">The data to decompress.</param>
    /// <param name="length">The expected length of the decompressed data.</param>
    /// <returns>A decompressed byte array.</returns>
    private byte[] DecompressUsingLz4Message(byte[] input, int length)
    {
      byte[] decompressedData;
      using (var memory = new MemoryStream(input))
      using (var source = LZ4Stream.Decode(memory))
      using (var target = new MemoryStream())
      {
        source.CopyTo(target);
        decompressedData = target.ToArray();
      }

      return decompressedData;
    }

    /// <summary>
    /// Decompresses data using the zstd_stream compression algorithm.
    /// </summary>
    /// <param name="input">The data to decompress.</param>
    /// <param name="length">The expected length of the decompressed data.</param>
    /// <returns>A decompressed byte array.</returns>
    private byte[] DecompressUsingZstdStream(byte[] input, int length)
    {
      _buffer.Write(input, 0, input.Length);
      _buffer.Position -= input.Length;

      byte[] decompressedData;
      using (var target = new MemoryStream())
      {
        _zstdDecompressStream.CopyTo(target, length);
        decompressedData = target.ToArray();
        target.Dispose();
        _buffer.SetLength(0);
      }

      return decompressedData;
    }

    /// <summary>
    /// Closes and disposes of any open streams.
    /// </summary>
    internal void Close()
    {
      _deflateCompressStream?.Dispose();
      _deflateDecompressStream?.Dispose();
      _multipleMessagesStream?.Dispose();
      _zstdDecompressStream?.Dispose();
    }

    /// <summary>
    /// Gets the byte array representing the next X Protocol frame that is stored in cache.
    /// </summary>
    /// <returns>A byte array representing an X Protocol frame.</returns>
    internal byte[] ReadNextBufferedMessage()
    {
      if (_multipleMessagesStream == null)
      {
        return null;
      }

      var messageSizeBytes = new byte[4];
      _multipleMessagesStream.Read(messageSizeBytes, 0, messageSizeBytes.Length);
      byte messageType = (byte)_multipleMessagesStream.ReadByte();
      var messageSize = BitConverter.ToInt32(messageSizeBytes, 0);
      var data = new byte[messageSize - 1];
      _multipleMessagesStream.Read(data, 0, data.Length);
      _lastCommunicationPacket = new CommunicationPacket(messageType, messageSize - 1, data);
      var message = new byte[messageSizeBytes.Length + 1 + data.Length];
      Buffer.BlockCopy(messageSizeBytes, 0, message, 0, messageSizeBytes.Length);
      Buffer.BlockCopy(new byte[] { messageType }, 0, message, messageSizeBytes.Length, 1);
      Buffer.BlockCopy(data, 0, message, messageSizeBytes.Length + 1, data.Length);

      if (_multipleMessagesStream.Position == _multipleMessagesStream.Length)
      {
        LastMessageContainsMultipleMessages = false;
        _multipleMessagesStream.Close();
        _multipleMessagesStream = null;
      }

      return message;
    }

    /// <summary>
    /// Gets a <see cref="CommunicationPacket"/> representing the next X Protocol frame that is stored in cache.
    /// </summary>
    /// <returns>A <see cref="CommunicationPacket"/> with the next X Protocol frame.</returns>
    internal CommunicationPacket ReadNextBufferedMessageAsCommunicationPacket()
    {
      ReadNextBufferedMessage();
      return _lastCommunicationPacket;
    }

  }
}
