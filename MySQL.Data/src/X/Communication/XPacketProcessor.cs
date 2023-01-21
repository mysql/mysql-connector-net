// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using MySqlX.Communication;
using System;
using System.IO;
using Mysqlx.Connection;
using Mysqlx.Notice;
using MySqlX.Protocol.X;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

namespace MySql.Data.X.Communication
{
  /// <summary>
  /// Enables X Protocol packets from the network stream to be retrieved and processed
  /// </summary>
  internal class XPacketProcessor
  {
    #region Constants
    private const int HEADER_SIZE = 5;
    private const int MESSAGE_TYPE = HEADER_SIZE - 1;
    #endregion

    #region Fields
    /// <summary>
    /// The instance of the stream that holds the network connection with MySQL Server.
    /// </summary>
    private Stream _stream;

    /// <summary>
    /// This field is used to enable compression and decompression actions in the communication channel.
    /// </summary>
    private XCompressionController _compressionController;

    /// <summary>
    /// A Queue to store the pending packets removed from the <see cref="_stream"/>
    /// </summary>
    private Queue<CommunicationPacket> _packetQueue;

    #endregion Fields

    #region Constructors
    /// <summary>
    /// Creates a new instance of XPacketProcessor.
    /// </summary>
    /// <param name="stream">The stream to be used as communication channel.</param>
    public XPacketProcessor(Stream stream)
    {
      _stream = stream;
      _packetQueue = new Queue<CommunicationPacket>();
    }

    /// <summary>
    /// Creates a new instance of XPacketProcessor.
    /// </summary>
    /// <param name="stream">The stream to be used as communication channel.</param>
    /// <param name="compressionController">The XCompressionController to be used for compression actions.</param>
    public XPacketProcessor(Stream stream, XCompressionController compressionController) : this(stream)
    {
      _compressionController = compressionController;
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    /// Identifies the kind of packet received over the network and execute
    /// the corresponding processing.
    /// </summary>
    private void IdentifyPacket(CommunicationPacket packet)
    {
      if (packet == null) return;
      switch (packet.MessageType)
      {
        case (int)ServerMessageId.NOTICE:
          Frame frame = Frame.Parser.ParseFrom(packet.Buffer);

          if ((NoticeType)frame.Type != NoticeType.Warning) return;

          Warning w = Warning.Parser.ParseFrom(frame.Payload.ToByteArray());
          WarningInfo warning = new WarningInfo(w.Code, w.Msg);
          var closeCodes = ((CloseNotification[])Enum.GetValues(typeof(CloseNotification))).Select(c => (uint)c).ToList();

          if (!closeCodes.Contains(warning.Code)) return;

          MySqlTrace.LogError((int)warning.Code, warning.Message);
          switch (warning.Code)
          {
            case (uint)CloseNotification.IDLE:
              throw new MySqlException(ResourcesX.NoticeIdleConnection, (int)warning.Code, new MySqlException(warning.Message, (int)warning.Code));
            case (uint)CloseNotification.KILLED:
              throw new MySqlException(ResourcesX.NoticeKilledConnection, (int)warning.Code, new MySqlException(warning.Message, (int)warning.Code));
            case (uint)CloseNotification.SHUTDOWN:
              throw new MySqlException(ResourcesX.NoticeServerShutdown, (int)warning.Code, new MySqlException(warning.Message, (int)warning.Code));
            default:
              break;
          }

          break;
      }
    }

    /// <summary>
    /// Reads data from the network stream and create a packet of type <see cref="CommunicationPacket"/>.
    /// </summary>
    /// <returns>A <see cref="CommunicationPacket"/>.</returns>
    public CommunicationPacket GetPacketFromNetworkStream(bool readFromQueue=false)
    {
      CommunicationPacket returnPacket = null;
      var compressionEnabled = _compressionController != null
          && _compressionController.IsCompressionEnabled;
      if (compressionEnabled && _compressionController.LastMessageContainsMultipleMessages)
      {
        returnPacket = _compressionController.ReadNextBufferedMessageAsCommunicationPacket();
        IdentifyPacket(returnPacket);
        return returnPacket;
      }

      if (_packetQueue.Count > 0 && readFromQueue)
      {
        return _packetQueue.Dequeue();
      }

      byte[] header = new byte[HEADER_SIZE];
      ReadFully(header, 0, HEADER_SIZE);
      int length = BitConverter.ToInt32(header, 0);
      byte[] data = new byte[length - 1];
      ReadFully(data, 0, length - 1);
      // If compression is enabled and message is of type compression.
      if (compressionEnabled && header[MESSAGE_TYPE] == (int)ServerMessageId.COMPRESSION)
      {
        var packet = new CommunicationPacket(header[MESSAGE_TYPE], length - 1, data);
        var message = Compression.Parser.ParseFrom(packet.Buffer);
        var payload = message.Payload.ToByteArray();
        var decompressedPayload = _compressionController.Decompress(payload, (int)(message.UncompressedSize));
        data = new byte[decompressedPayload.Length - HEADER_SIZE];
        for (int i = 0; i < data.Length; i++)
        {
          data[i] = decompressedPayload[i + HEADER_SIZE];
        }
        returnPacket = new CommunicationPacket(decompressedPayload[MESSAGE_TYPE], BitConverter.ToInt32(decompressedPayload, 0) - 1, data);
        IdentifyPacket(returnPacket);
        return returnPacket;
      }

      returnPacket = new CommunicationPacket(header[MESSAGE_TYPE], length - 1, data);
      IdentifyPacket(returnPacket);
      return returnPacket;
    }

    /// <summary>
    /// Sends the read/write actions to the MyNetworkStream class.
    /// </summary>
    private void ReadFully(byte[] buffer, int offset, int count)
    {
      int numRead = 0;
      int numToRead = count;
      while (numToRead > 0)
      {
        int read = _stream.Read(buffer, offset + numRead, numToRead);
        if (read == 0)
        {
          throw new EndOfStreamException();
        }
        numRead += read;
        numToRead -= read;
      }
    }


    /// <summary>
    /// Reads the pending packets present in the network channel and processes them accordingly.
    /// </summary>
    public void ProcessPendingPackets(Socket socket)
    {
      while (socket.Available > 0)
      {
        var packet = GetPacketFromNetworkStream(false);
        _packetQueue?.Enqueue(packet);
      }
    }
    #endregion Methods
  }
}
