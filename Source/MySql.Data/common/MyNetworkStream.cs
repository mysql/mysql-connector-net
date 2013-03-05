// Copyright (c) 2009 Sun Microsystems, Inc.
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
using System.Net.Sockets;

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
    const int MaxRetryCount = 2;
    Socket socket;

    public MyNetworkStream(Socket socket, bool ownsSocket)
      : base(socket, ownsSocket)
    {
      this.socket = socket;
    }

    bool IsTimeoutException(SocketException e)
    {
#if CF
       return (e.NativeErrorCode == 10060);
#else
      return (e.SocketErrorCode == SocketError.TimedOut);
#endif
    }

    bool IsWouldBlockException(SocketException e)
    {
#if CF
      return (e.NativeErrorCode == 10035);
#else
      return (e.SocketErrorCode == SocketError.WouldBlock);
#endif
    }


    void HandleOrRethrowException(Exception e)
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
            socket.Blocking = true;
            // return to give the caller possibility to retry the call
            return;
          }
          else if (IsTimeoutException(socketException))
          {
            throw new TimeoutException(socketException.Message, e);
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

  }
}