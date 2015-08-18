// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data;
using Mysqlx;
using Mysqlx.Session;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MySql.Communication
{
  internal class CommunicationTcp : UniversalStream
  {
    internal CommunicationPacket packet;
    internal Socket socket;

    public override bool CanRead
    {
      get { return _baseStream.CanRead; }
    }

    public override bool CanSeek
    {
      get { return _baseStream.CanSeek; }
    }

    public override bool CanWrite
    {
      get { return _baseStream.CanWrite; }
    }
    
    
    public CommunicationTcp()
    {
      _maxPacketSize = ulong.MaxValue;
      _maxBlockSize = Int32.MaxValue;
    }


    public CommunicationTcp(Stream stream, Encoding encoding, bool compress = false)
      : this()
    {
      _baseStream = stream;
      _encoding = encoding;
      _inStream = new BufferedStream(stream);
      _outStream = stream;      
    }


    public static NetworkStream CreateStream(MySqlConnectionStringBuilder settings, bool unix)
    {
      NetworkStream stream = null;
      IPHostEntry ipHE = GetHostEntry(settings.Server);
      foreach (IPAddress address in ipHE.AddressList)
      {
        try
        {
          stream = CreateSocketStream(settings, address, unix);
          if (stream != null) break;
        }
        catch (Exception ex)
        {
          SocketException socketException = ex as SocketException;
          // if the exception is a ConnectionRefused then we eat it as we may have other address
          // to attempt
          if (socketException == null) throw;
#if !CF
          if (socketException.SocketErrorCode != SocketError.ConnectionRefused) throw;
#endif
        }
      }    

      return stream;
    
    }


    internal static NetworkStream CreateSocketStream(MySqlConnectionStringBuilder settings, IPAddress ip, bool unix)
    {
      EndPoint endPoint;
      endPoint = new IPEndPoint(ip, (int)settings.Port);

      Socket socket = unix ?
          new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP) :
          new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

      if (settings.Keepalive > 0)
      {
        SetKeepAlive(socket, settings.Keepalive);
      }

      IPEndPoint ipe = new IPEndPoint(ip, (int)settings.Port);
      socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

      try
      {
      socket.Connect(ipe);
      }
      catch (Exception)
      {
        socket.Close();
        throw;
      }      
            
      _networkStream = new NetworkStream(socket, true);
      GC.SuppressFinalize(socket);
      
      return _networkStream;

    }


    /// <summary>
    /// Set keepalive + timeout on socket.
    /// </summary>
    /// <param name="s">socket</param>
    /// <param name="time">keepalive timeout, in seconds</param>
    private static void SetKeepAlive(Socket s, uint time)
    {

      uint on = 1;
      uint interval = 1000; // default interval = 1 sec

      uint timeMilliseconds;
      if (time > UInt32.MaxValue / 1000)
        timeMilliseconds = UInt32.MaxValue;
      else
        timeMilliseconds = time * 1000;

      byte[] inOptionValues = new byte[12];
      BitConverter.GetBytes(on).CopyTo(inOptionValues, 0);
      BitConverter.GetBytes(timeMilliseconds).CopyTo(inOptionValues, 4);
      BitConverter.GetBytes(interval).CopyTo(inOptionValues, 8);
      try
      {
        // call WSAIoctl via IOControl
        s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        return;
      }
      catch (NotImplementedException)
      {
        // Mono throws not implemented currently
      }

      // Fallback if Socket.IOControl is not available ( Compact Framework )
      // or not implemented ( Mono ). Keepalive option will still be set, but
      // with timeout is kept default.
      s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
    }


    private static IPHostEntry GetHostEntry(string hostname)
    {
      IPHostEntry ipHE = ParseIPAddress(hostname);
      if (ipHE != null) return ipHE;
      return Dns.GetHostEntry(hostname);
    }

    private static IPHostEntry ParseIPAddress(string hostname)
    {
      IPHostEntry ipHE = null;
      IPAddress addr;
      if (IPAddress.TryParse(hostname, out addr))
      {
        ipHE = new IPHostEntry();
        ipHE.AddressList = new IPAddress[1];
        ipHE.AddressList[0] = addr;
      }
      return ipHE;
    }




    public override CommunicationPacket Read()
    {
      LoadPacket();
      return packet;
    }


    public override void Write()
    {
      throw new NotImplementedException();
    }


    public override void Close()
    {
      throw new NotImplementedException();
    }

    public override void Flush()
    {
      throw new NotImplementedException();
    }

    private void LoadPacket()
    {
      try
      {
        _length = 0;
        int offset = 0;
        while (true)
        {
          ReadFully(_inStream, _header, offset, _header.Length);        
          _length = BitConverter.ToInt32(_header, 0);

          packet.MessageType = _header[4];
          var tempBuffer = new Byte[_length - _header.Length];
          ReadFully(_inStream, tempBuffer, offset, _length - _header.Length);
          packet.Write(tempBuffer);

          // if this block was < maxBlock then it's last one in a multipacket series
          if (_length < _maxBlockSize) break;
          offset += _length;

        }
      }
      catch (Exception)
      {
        
        throw;
      }
    }

    internal static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
    {
      int numRead = 0;
      int numToRead = count;
      while (numToRead > 0)
      {
        int read = stream.Read(buffer, offset + numRead, numToRead);
        if (read == 0)
        {
          throw new EndOfStreamException();
        }
        numRead += read;
        numToRead -= read;
      }
    }


    internal override void SendPacket<T>(T message, int messageId)
    {
      var internalStream = new MemoryStream();
      try
      {
        if (typeof(T) == typeof(AuthenticateStart))
        {
          var authStartMessage = ((AuthenticateStart)(object)message).ToByteArray();        
          //writting header
          using (BinaryWriter writer = new BinaryWriter(internalStream))
          {
            writer.Write((Int32)authStartMessage.Length + 1);
            writer.Write((Byte)messageId);
            writer.Write(authStartMessage);
            _outStream.Write(internalStream.ToArray(), 0, (Int32)internalStream.Length);            
          }
        }

        if (typeof(T) == typeof(AuthenticateContinue))
        {
          var authContMessage = ((AuthenticateContinue)(object)message).ToByteArray();
          //writting header
          using (BinaryWriter writer = new BinaryWriter(internalStream))
          {
            writer.Write((Int32)authContMessage.Length + 1);
            writer.Write((Byte)messageId);
            writer.Write(authContMessage);
            _outStream.Write(internalStream.ToArray(), 0, (Int32)internalStream.Length);
          }        
        }
        _outStream.Flush();
      }
      finally
      {
        internalStream.Dispose();
      }            
    }



  }
}
