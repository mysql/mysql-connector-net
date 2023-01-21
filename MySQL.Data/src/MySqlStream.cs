// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
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
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for MySqlStream.
  /// </summary>
  internal class MySqlStream
  {
    private byte sequenceByte;
    private int maxBlockSize;
    private ulong maxPacketSize;
    private byte[] packetHeader = new byte[4];
    MySqlPacket packet;
    TimedStream timedStream;
    Stream inStream;
    Stream outStream;
    Socket socket;
    public Socket Socket { get => socket; set => socket = value; }

    internal Stream BaseStream
    {
      get
      {
        return timedStream;
      }
    }
    public MySqlStream(Encoding encoding)
    {
      // we have no idea what the real value is so we start off with the max value
      // The real value will be set in NativeDriver.Configure()
      maxPacketSize = ulong.MaxValue;

      // we default maxBlockSize to MaxValue since we will get the 'real' value in 
      // the authentication handshake and we know that value will not exceed 
      // true maxBlockSize prior to that.
      maxBlockSize = Int32.MaxValue;

      packet = new MySqlPacket(encoding);
    }

    public MySqlStream(Stream baseStream, Encoding encoding, bool compress, Socket pSocket = null)
      : this(encoding)
    {
      timedStream = new TimedStream(baseStream);
      Stream stream;
      if (compress)
        stream = new CompressedStream(timedStream);
      else
        stream = timedStream;

      inStream = stream;
      outStream = stream;
      socket = pSocket;
    }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task CloseAsync(bool execAsync)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
#if !(NETSTANDARD2_0 || NETFRAMEWORK)
      if (execAsync)
      {
        await outStream.DisposeAsync().ConfigureAwait(false);
        await inStream.DisposeAsync().ConfigureAwait(false);
        timedStream.Close();
      }
      else
      {
        outStream.Dispose();
        inStream.Dispose();
        timedStream.Close();
      }
#else
      outStream.Dispose();
      inStream.Dispose();
      timedStream.Close();
#endif
    }

    #region Properties

    public Encoding Encoding
    {
      get { return packet.Encoding; }
      set { packet.Encoding = value; }
    }

    public void ResetTimeout(int timeout)
    {
      timedStream.ResetTimeout(timeout);
    }

    public byte SequenceByte
    {
      get { return sequenceByte; }
      set { sequenceByte = value; }
    }

    public int MaxBlockSize
    {
      get { return maxBlockSize; }
      set { maxBlockSize = value; }
    }

    public ulong MaxPacketSize
    {
      get { return maxPacketSize; }
      set { maxPacketSize = value; }
    }

    #endregion

    #region Packet methods

    /// <summary>
    /// ReadPacket is called by NativeDriver to start reading the next
    /// packet on the stream.
    /// </summary>
    public async Task<MySqlPacket> ReadPacketAsync(bool execAsync)
    {
      //Debug.Assert(packet.Position == packet.Length);

      // make sure we have read all the data from the previous packet
      //Debug.Assert(HasMoreData == false, "HasMoreData is true in OpenPacket");

      await LoadPacketAsync(execAsync).ConfigureAwait(false);

      // now we check if this packet is a server error
      if (packet.Buffer[0] == 0xff)
      {
        packet.ReadByte();  // read off the 0xff

        int code = packet.ReadInteger(2);
        string msg;

        if (packet.Version.isAtLeast(5, 5, 0))
          msg = packet.ReadString(Encoding.UTF8);
        else
          msg = packet.ReadString();

        if (msg.StartsWith("#", StringComparison.Ordinal))
        {
          msg.Substring(1, 5);  /* state code */
          msg = msg.Substring(6);
        }

        switch (code)
        {
          case 4031:
            throw new MySqlException(msg, code, true);
          default:
            throw new MySqlException(msg, code);
        }
      }
      return packet;
    }

    /// <summary>
    /// Reads the specified number of bytes from the stream and stores them at given 
    /// offset in the buffer.
    /// Throws EndOfStreamException if not all bytes can be read.
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="buffer"> Array to store bytes read from the stream </param>
    /// <param name="offset">The offset in buffer at which to begin storing the data read from the current stream. </param>
    /// <param name="count">Number of bytes to read</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    internal static async Task ReadFullyAsync(Stream stream, byte[] buffer, int offset, int count, bool execAsync)
    {
      int numRead = 0;
      int numToRead = count;
      while (numToRead > 0)
      {
        int read = execAsync
          ? await stream.ReadAsync(buffer, offset + numRead, numToRead, CancellationToken.None).ConfigureAwait(false)
          : stream.Read(buffer, offset + numRead, numToRead);

        if (read == 0)
          throw new EndOfStreamException();

        numRead += read;
        numToRead -= read;
      }
    }

    /// <summary>
    /// LoadPacket loads up and decodes the header of the incoming packet.
    /// </summary>
    public async Task LoadPacketAsync(bool execAsync)
    {
      try
      {
        packet.Length = 0;
        int offset = 0;
        while (true)
        {
          await ReadFullyAsync(inStream, packetHeader, 0, 4, execAsync).ConfigureAwait(false);
          sequenceByte = (byte)(packetHeader[3] + 1);
          int length = (int)(packetHeader[0] + (packetHeader[1] << 8) +
              (packetHeader[2] << 16));

          // make roo for the next block
          packet.Length += length;
          await ReadFullyAsync(inStream, packet.Buffer, offset, length, execAsync).ConfigureAwait(false);
          offset += length;

          // if this block was < maxBlock then it's last one in a multipacket series
          if (length < maxBlockSize) break;
        }
        packet.Position = 0;
      }
      catch (IOException ioex)
      {
        throw new MySqlException(Resources.ReadFromStreamFailed, true, ioex);
      }
    }

    public async Task SendPacketAsync(MySqlPacket packet, bool execAsync)
    {
      byte[] buffer = packet.Buffer;
      int length = packet.Position - 4;

      if ((ulong)length > maxPacketSize)
        throw new MySqlException(Resources.QueryTooLarge, (int)MySqlErrorCode.PacketTooLarge);

      int offset = 0;
      do
      {
        int lenToSend = length > maxBlockSize ? maxBlockSize : length;
        buffer[offset] = (byte)(lenToSend & 0xff);
        buffer[offset + 1] = (byte)((lenToSend >> 8) & 0xff);
        buffer[offset + 2] = (byte)((lenToSend >> 16) & 0xff);
        buffer[offset + 3] = sequenceByte++;

        if (Socket != null && Socket.Available > 0)
          await ReadPacketAsync(execAsync).ConfigureAwait(false);

        if (execAsync)
        {
          await outStream.WriteAsync(buffer, offset, lenToSend + 4).ConfigureAwait(false);
          await outStream.FlushAsync().ConfigureAwait(false);
        }
        else
        {
          outStream.Write(buffer, offset, lenToSend + 4);
          outStream.Flush();
        }

        length -= lenToSend;
        offset += lenToSend;
      } while (length > 0);
    }

    public async Task SendEntirePacketDirectlyAsync(byte[] buffer, int count, bool execAsync)
    {
      buffer[0] = (byte)(count & 0xff);
      buffer[1] = (byte)((count >> 8) & 0xff);
      buffer[2] = (byte)((count >> 16) & 0xff);
      buffer[3] = sequenceByte++;

      if (execAsync)
      {
        await outStream.WriteAsync(buffer, 0, count + 4).ConfigureAwait(false);
        await outStream.FlushAsync().ConfigureAwait(false);
      }
      else
      {
        outStream.Write(buffer, 0, count + 4);
        outStream.Flush();
      }
    }

    #endregion
  }
}
