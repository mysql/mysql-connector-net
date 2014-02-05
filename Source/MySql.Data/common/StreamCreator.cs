// Copyright (c) 2004-2008 MySQL AB, 2008-2009 Sun Microsystems, Inc, 2014 Oracle and/or its affiliates.
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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Diagnostics;
using MySql.Data.MySqlClient.Properties;
using System.Runtime.InteropServices;
using MySql.Data.MySqlClient;


namespace MySql.Data.Common
{
  /// <summary>
  /// Summary description for StreamCreator.
  /// </summary>
  internal class StreamCreator
  {
    string hostList;
    uint port;
    string pipeName;
    uint timeOut;
    uint keepalive;
    DBVersion driverVersion;

    public StreamCreator(string hosts, uint port, string pipeName, uint keepalive, DBVersion driverVersion)
    {
      hostList = hosts;
      if (hostList == null || hostList.Length == 0)
        hostList = "localhost";
      this.port = port;
      this.pipeName = pipeName;
      this.keepalive = keepalive;
      this.driverVersion = driverVersion;
    }

    private Stream GetStreamFromHost(string pipeName, string hostName, uint timeout)
    {
      Stream stream = null;
      if (pipeName != null && pipeName.Length != 0)
      {
#if !CF
        stream = NamedPipeStream.Create(pipeName, hostName, timeout);
#endif
      }
      else
      {
        IPHostEntry ipHE = GetHostEntry(hostName);
        foreach (IPAddress address in ipHE.AddressList)
        {
          try
          {
            stream = CreateSocketStream(address, false);
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
      }
      return stream;
    }

    public Stream GetStream(uint timeout)
    {
      timeOut = timeout;

      if (hostList.StartsWith("/", StringComparison.Ordinal))
        return CreateSocketStream(null, true);

      string[] dnsHosts = hostList.Split(',');

      Random random = new Random((int)DateTime.Now.Ticks);
      int index = random.Next(dnsHosts.Length);
      int pos = 0;
      Stream stream = null;

      while (stream == null && pos < dnsHosts.Length)
      {
        stream = GetStreamFromHost(pipeName, dnsHosts[index++], timeout);
        if (index == dnsHosts.Length) index = 0;
        pos++;
      }
      return stream;
    }

    private IPHostEntry ParseIPAddress(string hostname)
    {
      IPHostEntry ipHE = null;
#if !CF
      IPAddress addr;
      if (IPAddress.TryParse(hostname, out addr))
      {
        ipHE = new IPHostEntry();
        ipHE.AddressList = new IPAddress[1];
        ipHE.AddressList[0] = addr;
      }
#endif
      return ipHE;
    }

#if CF
        IPHostEntry GetDnsHostEntry(string hostname)
        {
            return Dns.GetHostEntry(hostname);
        }
#else
    IPHostEntry GetDnsHostEntry(string hostname)
    {
      LowResolutionStopwatch stopwatch = new LowResolutionStopwatch();

      try
      {
        stopwatch.Start();
        return Dns.GetHostEntry(hostname);
      }
      catch (SocketException ex)
      {
        string message = String.Format(Resources.GetHostEntryFailed,
        stopwatch.Elapsed, hostname, ex.SocketErrorCode,
        ex.ErrorCode, ex.NativeErrorCode);
        throw new Exception(message, ex);
      }
      finally
      {
        stopwatch.Stop();
      }
    }
#endif

    private IPHostEntry GetHostEntry(string hostname)
    {
      IPHostEntry ipHE = ParseIPAddress(hostname);
      if (ipHE != null) return ipHE;
      return GetDnsHostEntry(hostname);
    }

#if !CF

    private static EndPoint CreateUnixEndPoint(string host)
    {
      // first we need to load the Mono.posix assembly			
      Assembly a = Assembly.Load(@"Mono.Posix, Version=2.0.0.0, 				
                Culture=neutral, PublicKeyToken=0738eb9f132ed756");

      // then we need to construct a UnixEndPoint object
      EndPoint ep = (EndPoint)a.CreateInstance("Mono.Posix.UnixEndPoint",
          false, BindingFlags.CreateInstance, null,
          new object[1] { host }, null, null);
      return ep;
    }
#endif

    private Stream CreateSocketStream(IPAddress ip, bool unix)
    {
      EndPoint endPoint;
#if !CF
      if (!Platform.IsWindows() && unix)
        endPoint = CreateUnixEndPoint(hostList);
      else
#endif
      
      endPoint = new IPEndPoint(ip, (int)port);

      Socket socket = unix ?
          new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP) :
          new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      if (keepalive > 0)
      {
        SetKeepAlive(socket, keepalive);
      }


      IAsyncResult ias = socket.BeginConnect(endPoint, null, null);
      if (!ias.AsyncWaitHandle.WaitOne((int)timeOut * 1000, false))
      {
        socket.Close();
        return null;
      }
      try
      {
        socket.EndConnect(ias);
      }
      catch (Exception)
      {
        socket.Close();
        throw;
      }
      MyNetworkStream stream = new MyNetworkStream(socket, true);
      GC.SuppressFinalize(socket);
      GC.SuppressFinalize(stream);
      return stream;
    }



    /// <summary>
    /// Set keepalive + timeout on socket.
    /// </summary>
    /// <param name="s">socket</param>
    /// <param name="time">keepalive timeout, in seconds</param>
    private static void SetKeepAlive(Socket s, uint time)
    {

#if !CF
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
#endif
      // Fallback if Socket.IOControl is not available ( Compact Framework )
      // or not implemented ( Mono ). Keepalive option will still be set, but
      // with timeout is kept default.
      s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
    }
  }
}
