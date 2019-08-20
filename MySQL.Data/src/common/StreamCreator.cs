// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Net;
using System.Linq;
using MySql.Data.MySqlClient.Common;

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
      Task<IPAddress[]> dnsTask = Dns.GetHostAddressesAsync(settings.Server);
      dnsTask.Wait();
      if (dnsTask.Result == null || dnsTask.Result.Length == 0)
        throw new ArgumentException(Resources.InvalidHostNameOrAddress);
      IPAddress addr = dnsTask.Result.SingleOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork);
      if (addr == null)
        addr = dnsTask.Result[0];
      TcpClient client = new TcpClient(addr.AddressFamily);
      Task task = null;
      if (!settings.IsSshEnabled())
        task = client.ConnectAsync(settings.Server, (int)settings.Port);
      else
        task = client.ConnectAsync(
          settings.Server == "127.0.0.1" || settings.Server == "::1" 
            ? "localhost"
            : settings.Server,
          3306);
      
      if (!task.Wait(((int)settings.ConnectionTimeout * 1000)))
        throw new MySqlException(Resources.Timeout);
      if (settings.Keepalive > 0)
      {
        SetKeepAlive(client.Client, settings.Keepalive);
      }

      return client.GetStream();
    }

    internal static Stream GetUnixSocketStream(MySqlConnectionStringBuilder settings)
    {
      try
      {
        return new NetworkStream(GetUnixSocket(settings.Server, settings.ConnectionTimeout, settings.Keepalive), true);
      }
      catch (Exception)
      {
        throw;
      }
    }

    internal static Socket GetUnixSocket(string server, uint connectionTimeout, uint keepAlive)
    {
      if (Platform.IsWindows())
        throw new InvalidOperationException(Resources.NoUnixSocketsOnWindows);

      EndPoint endPoint = new UnixEndPoint(server);
      Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
      if (keepAlive > 0)
      {
        SetKeepAlive(socket, keepAlive);
      }
      try
      {
        socket.ReceiveTimeout = (int)connectionTimeout * 1000;
        socket.Connect(endPoint);
        return socket;
      }
      catch (Exception)
      {
        socket.Dispose();
        throw;
      }
    }

    private static Stream GetSharedMemoryStream(MySqlConnectionStringBuilder settings)
    {
      SharedMemoryStream str = new SharedMemoryStream(settings.SharedMemoryName);
      str.Open(settings.ConnectionTimeout);
      return str;
    }

    private static Stream GetNamedPipeStream(MySqlConnectionStringBuilder settings)
    {
      Stream stream = NamedPipeStream.Create(settings.PipeName, settings.Server, settings.ConnectionTimeout);
      return stream;
    }

    /// <summary>
    /// Set the keepalive timeout on the socket.
    /// </summary>
    /// <param name="s">The socket object.</param>
    /// <param name="time">The keepalive timeout, in seconds.</param>
    internal static void SetKeepAlive(Socket s, uint time)
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
