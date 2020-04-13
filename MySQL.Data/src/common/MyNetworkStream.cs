// Copyright (c) 2009, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace MySql.Data.Common
{
  internal class MyNetworkStream : NetworkStream
  {
    /// <summary>
    /// Wrapper around NetworkStream.
    /// 
    /// MyNetworkStream is equivalent to NetworkStream, except 
    /// 1. It throws TimeoutException if read or write timeout occurs, instead 
    /// of IOException, to match behavior of other streams (named pipe and 
    /// shared memory). This property comes handy in TimedStream.
    ///
    /// 2. It implements workarounds for WSAEWOULDBLOCK errors, that can start 
    /// occuring after stream has times out. For a discussion about the CLR bug,
    /// refer to  http://tinyurl.com/lhgpyf. This error should never occur, as
    /// we're not using asynchronous operations, but apparerntly it does occur
    /// directly after timeout has expired.
    /// The workaround is hinted in the URL above and implemented like this:
    /// For each IO operation, if it throws WSAEWOULDBLOCK, we explicitely set
    /// the socket to Blocking and retry the operation once again.
    /// </summary>
    private const int MaxRetryCount = 2;

    readonly Socket _socket;

    public MyNetworkStream(Socket socket, bool ownsSocket)
      : base(socket, ownsSocket)
    {
      this._socket = socket;
    }

    private static bool IsTimeoutException(SocketException e)
    {
      return (e.SocketErrorCode == SocketError.TimedOut);
    }

    private static bool IsWouldBlockException(SocketException e)
    {
      return (e.SocketErrorCode == SocketError.WouldBlock);
    }

    private void HandleOrRethrowException(Exception e)
    {
      Exception currentException = e;
      while (currentException != null)
      {
        if (currentException is SocketException)
        {
          SocketException socketException = (SocketException)currentException;
          if (IsWouldBlockException(socketException))
          {
            // Workaround  for WSAEWOULDBLOCK
            _socket.Blocking = true;
            // return to give the caller possibility to retry the call
            return;
          }
          if (IsTimeoutException(socketException))
          {
            return;
            //throw new TimeoutException(socketException.Message, e);
          }
        }
        currentException = currentException.InnerException;
      }
      throw (e);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      int retry = 0;
      Exception exception = null;
      do
      {
        try
        {
          return base.Read(buffer, offset, count);
        }
        catch (Exception e)
        {
          exception = e;
          HandleOrRethrowException(e);
        }
      }
      while (++retry < MaxRetryCount);
      if (exception.GetBaseException() is SocketException
        && IsTimeoutException((SocketException)exception.GetBaseException()))
        throw new TimeoutException(exception.Message, exception);
      throw exception;
    }

    public override int ReadByte()
    {
      int retry = 0;
      Exception exception = null;
      do
      {
        try
        {
          return base.ReadByte();
        }
        catch (Exception e)
        {
          exception = e;
          HandleOrRethrowException(e);
        }
      }
      while (++retry < MaxRetryCount);
      throw exception;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      int retry = 0;
      Exception exception = null;
      do
      {
        try
        {
          base.Write(buffer, offset, count);
          return;
        }
        catch (Exception e)
        {
          exception = e;
          HandleOrRethrowException(e);
        }
      }
      while (++retry < MaxRetryCount);
      throw exception;
    }

    public override void Flush()
    {
      int retry = 0;
      Exception exception = null;
      do
      {
        try
        {
          base.Flush();
          return;
        }
        catch (Exception e)
        {
          exception = e;
          HandleOrRethrowException(e);
        }
      }
      while (++retry < MaxRetryCount);
      throw exception;
    }

    #region Create Code

    public static MyNetworkStream CreateStream(string server, uint connectionTimeout, uint keepAlive, uint port, bool unix)
    {
      if (unix) return new MyNetworkStream(StreamCreator.GetUnixSocket(server, connectionTimeout, keepAlive), true);

      MyNetworkStream stream = null;

      IPHostEntry ipHE = GetHostEntry(server);
      foreach (IPAddress address in ipHE.AddressList)
      {
        try
        {
          stream = CreateSocketStream(port, keepAlive, connectionTimeout, address, unix);
          if (stream != null) break;
        }
        catch (Exception ex)
        {
          string exTimeOutMessage = connectionTimeout == 0 ? ResourcesX.TimeOutSingleHost0ms : String.Format(ResourcesX.TimeOutSingleHost, connectionTimeout);

          if (ex is TimeoutException) throw new TimeoutException(exTimeOutMessage);

          SocketException socketException = ex as SocketException;

          // if the exception is a ConnectionRefused then we eat it as we may have other address
          // to attempt
          if (socketException == null) throw;
          if (socketException.SocketErrorCode == SocketError.TimedOut) throw new TimeoutException(exTimeOutMessage);
          if (socketException.SocketErrorCode != SocketError.ConnectionRefused) throw;
        }
      }
      return stream;
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

    private static IPHostEntry GetHostEntry(string hostname)
    {
      IPHostEntry ipHE = ParseIPAddress(hostname);
      if (ipHE != null) return ipHE;

      Task<IPHostEntry> t = Dns.GetHostEntryAsync(hostname);
      t.Wait();
      return t.Result;
    }

    private static MyNetworkStream CreateSocketStream(uint port, uint keepAlive, uint connectionTimeout, IPAddress ip, bool unix)
    {
      EndPoint endPoint;
      endPoint = new IPEndPoint(ip, (int)port);
      Socket socket = unix ?
          new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP) :
          new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

      socket.NoDelay = true;

      if (keepAlive > 0)
      {
        SetKeepAlive(socket, keepAlive);
      }

      IAsyncResult ias = socket.BeginConnect(endPoint, null, null);

      if (connectionTimeout == 0)
      {
        if (!ias.AsyncWaitHandle.WaitOne())
          socket.Close();
      }
      else
      {
        if (!ias.AsyncWaitHandle.WaitOne((int)connectionTimeout, false))
        {
          socket.Close();
          throw new TimeoutException();
        }
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

    #endregion

  }
}