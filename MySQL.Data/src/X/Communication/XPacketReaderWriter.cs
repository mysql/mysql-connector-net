// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
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

using Google.Protobuf;
using MySql.Data.Common;
using MySql.Data.X.Communication;
using Mysqlx;
using Mysqlx.Connection;
using System;
using System.IO;
using System.Net.Sockets;

namespace MySqlX.Communication
{
  internal class XPacketReaderWriter
  {
    private Stream _stream;
    private Socket _socket;
    private XPacketProcessor _packetProcessor;

    /// <summary>
    /// Constructor that sets the stream used to read or write data.
    /// </summary>
    /// <param name="stream">The stream used to read or write data.</param>
    public XPacketReaderWriter(Stream stream, Socket socket)
    {
      _stream = stream;
      _socket = socket;
      _packetProcessor = new XPacketProcessor(stream);
    }

    /// <summary>
    /// Constructor that sets the stream used to read or write data and the compression controller.
    /// </summary>
    /// <param name="stream">The stream used to read or write data.</param>
    /// <param name="compressionController">The compression controller.</param>
    public XPacketReaderWriter(Stream stream, XCompressionController compressionReadController, XCompressionController compressionWriteController, Socket socket)
    {
      _stream = stream;
      _socket = socket;
      CompressionReadController = compressionReadController;
      CompressionWriteController = compressionWriteController;
      _packetProcessor = new XPacketProcessor(stream, CompressionReadController);
    }

    /// <summary>
    /// Gets or sets the compression controller uses to manage compression operations.
    /// </summary>
    public XCompressionController CompressionReadController { get; private set; }
    public XCompressionController CompressionWriteController { get; private set; }

    /// <summary>
    /// Writes X Protocol frames to the X Plugin.
    /// </summary>
    /// <param name="id">The integer representation of the client message identifier used for the message.</param>
    /// <param name="message">The message to include in the X Protocol frame.</param>
    public void Write(int id, IMessage message)
    {
      var messageSize = message.CalculateSize();
      _packetProcessor.ProcessPendingPackets(_socket);
      if (CompressionWriteController != null
          && CompressionWriteController.IsCompressionEnabled
          && messageSize > XCompressionController.COMPRESSION_THRESHOLD
          && CompressionWriteController.ClientSupportedCompressedMessages.Contains((ClientMessageId)id)
          )
      {
        // Build the compression protobuf message.
        var messageHeader = new byte[5];
        var messageBytes = message.ToByteArray();
        byte[] payload = new byte[messageHeader.Length + messageBytes.Length];
        var sizeArray = BitConverter.GetBytes(messageSize + 1);
        Buffer.BlockCopy(sizeArray, 0, messageHeader, 0, sizeArray.Length);
        messageHeader[4] = (byte)id;
        Buffer.BlockCopy(messageHeader, 0, payload, 0, messageHeader.Length);
        Buffer.BlockCopy(messageBytes, 0, payload, messageHeader.Length, messageBytes.Length);

        var compression = new Compression();
        compression.UncompressedSize = (ulong)(messageSize + messageHeader.Length);
        compression.ClientMessages = (ClientMessages.Types.Type)id;
        compression.Payload = ByteString.CopyFrom(CompressionWriteController.Compress(payload));

        // Build the X Protocol frame.
        _stream.Write(BitConverter.GetBytes(compression.CalculateSize() + 1), 0, 4);
        _stream.WriteByte((byte)(ClientMessageId.COMPRESSION));
        if (messageSize > 0)
        {
          compression.WriteTo(_stream);
        }
      }
      else
      {
        _stream.Write(BitConverter.GetBytes(messageSize + 1), 0, 4);
        _stream.WriteByte((byte)id);
        if (messageSize > 0)
        {
          message.WriteTo(_stream);
        }
      }

      _stream.Flush();
    }

    /// <summary>
    /// Writes X Protocol frames to the X Plugin.
    /// </summary>
    /// <param name="id">The client message identifier used for the message.</param>
    /// <param name="message">The message to include in the X Protocol frame.</param>
    public void Write(ClientMessageId id, IMessage message)
    {
      Write((int)id, message);
    }

    /// <summary>
    /// Reads X Protocol frames incoming from the X Plugin.
    /// </summary>
    /// <returns>A <see cref="CommunicationPacket"/> instance representing the X Protocol frame that was read.</returns>
    public CommunicationPacket Read()
    {
      return _packetProcessor.GetPacketFromNetworkStream(true);
    }

  }
}
