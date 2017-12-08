// Copyright © 2004, 2017 Oracle and/or its affiliates. All rights reserved.
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


using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using System.IO.Pipes;
using System.Net;
#if !NETSTANDARD1_3
using MySql.Data.MySqlClient.Common;
using System.IO.MemoryMappedFiles;
#endif

namespace MySql.Data.Common
{
  /// <summary>
  /// Summary description for StreamCreator.
  /// </summary>
  internal class StreamCreator
  {
    readonly string _hostList;
    uint _port;
    string pipeName;
    uint keepalive;
    DBVersion driverVersion;

    public StreamCreator(string hosts, uint port, string pipeName, uint keepalive, DBVersion driverVersion)
    {
      _hostList = hosts;
      if (string.IsNullOrEmpty(_hostList))
        _hostList = "localhost";
      this._port = port;
      this.pipeName = pipeName;
      this.keepalive = keepalive;
      this.driverVersion = driverVersion;
    }

    public static Stream GetStream(string server, uint port, string pipename, uint keepalive, DBVersion v, uint timeout)
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder
      {
        Server = server,
        Port = port,
        PipeName = pipename,
        Keepalive = keepalive,
        ConnectionTimeout = timeout
      };

      return GetStream(settings);
    }

    public static Stream GetStream(MySqlConnectionStringBuilder settings)
    {
      switch (settings.ConnectionProtocol)
      {
        case MySqlConnectionProtocol.Tcp: return GetTcpStream(settings);
        case MySqlConnectionProtocol.UnixSocket: return GetUnixSocketStream(settings);        
        case MySqlConnectionProtocol.SharedMemory: return GetSharedMemoryStream(settings);
        case MySqlConnectionProtocol.NamedPipe: return GetNamedPipeStream(settings);
      }
      throw new InvalidOperationException(Resources.UnknownConnectionProtocol);
    }

    private static Stream GetTcpStream(MySqlConnectionStringBuilder settings)
    {
      TcpClient client = new TcpClient(AddressFamily.InterNetwork);
      Task task = client.ConnectAsync(settings.Server, (int)settings.Port);

      if (!task.Wait(((int)settings.ConnectionTimeout * 1000)))
        throw new MySqlException(Resources.Timeout);
      if (settings.Keepalive > 0)
      {
        SetKeepAlive(client.Client, settings.Keepalive);
      }
      return client.GetStream();
    }

    private static Stream GetUnixSocketStream(MySqlConnectionStringBuilder settings)
    {
      if (Platform.IsWindows())
        throw new InvalidOperationException(Resources.NoUnixSocketsOnWindows);

      EndPoint endPoint = new UnixEndPoint(settings.Server);
      Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
      if (settings.Keepalive > 0)
      {
        SetKeepAlive(socket, settings.Keepalive);
      }
      try
      {
        socket.ReceiveTimeout = (int)settings.ConnectionTimeout * 1000;
        socket.Connect(endPoint);
        return new NetworkStream(socket, true);
      }
      catch (Exception)
      {
        socket.Dispose();
        throw;
      }
    }

    private static Stream GetSharedMemoryStream(MySqlConnectionStringBuilder settings)
    {
#if NETSTANDARD1_3
      throw new NotSupportedException("Shared memory streams not currently supported.");
#else
      SharedMemoryStream str = new SharedMemoryStream(settings.SharedMemoryName);
      str.Open(settings.ConnectionTimeout);
      return str;
#endif
    }

    private static Stream GetNamedPipeStream(MySqlConnectionStringBuilder settings)
    {
#if NETSTANDARD1_3
      NamedPipeClientStream pipeStream = new NamedPipeClientStream(settings.Server, settings.PipeName, PipeDirection.InOut);
      pipeStream.Connect((int)settings.ConnectionTimeout * 1000);
      return pipeStream;
#else
      Stream stream = NamedPipeStream.Create(settings.PipeName, settings.Server, settings.ConnectionTimeout);
      return stream;
#endif
    }

    /// <summary>
    /// Set the keepalive timeout on the socket.
    /// </summary>
    /// <param name="s">The socket object.</param>
    /// <param name="time">The keepalive timeout, in seconds.</param>
    private static void SetKeepAlive(Socket s, uint time)
    {
      uint on = 1;
      uint interval = 1000; // default interval = 1 sec

      uint timeMilliseconds;
      if (time > UInt32.MaxValue / 1000)
        timeMilliseconds = UInt32.MaxValue;
      else
        timeMilliseconds = time * 1000;

      // Use Socket.IOControl to implement equivalent of
      // WSAIoctl with  SOL_KEEPALIVE_VALS 

      // the native structure passed to WSAIoctl is
      //struct tcp_keepalive {
      //    ULONG onoff;
      //    ULONG keepalivetime;
      //    ULONG keepaliveinterval;
      //};
      // marshal the equivalent of the native structure into a byte array

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
  }
}
