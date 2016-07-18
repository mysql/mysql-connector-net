// Copyright © 2004, 2016 Oracle and/or its affiliates. All rights reserved.
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
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Common
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
      return client.GetStream();
      //TODO:  reimplement or remove keepalive
    }

    private static Stream GetUnixSocketStream(MySqlConnectionStringBuilder settings)
    {
      if (Platform.IsWindows())
        throw new InvalidOperationException(Resources.NoUnixSocketsOnWindows);
      return null;
//      MyNetworkStream s = await MyNetworkStream.CreateStreamAsync(settings, true);
  //    return s;
    }

    private static Stream GetSharedMemoryStream(MySqlConnectionStringBuilder settings)
    {
#if NETCORE10
      throw new NotSupportedException("Shared memory streams not currently supported.");
#else
      SharedMemoryStream str = new SharedMemoryStream(settings.SharedMemoryName);
      str.Open(settings.ConnectionTimeout);
      return str;
#endif
    }

    private static Stream GetNamedPipeStream(MySqlConnectionStringBuilder settings)
    {
      NamedPipeClientStream pipeStream = new NamedPipeClientStream(settings.Server, settings.PipeName, PipeDirection.InOut);
      pipeStream.Connect((int)settings.ConnectionTimeout);
      return pipeStream;
    }
  }
}
