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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;

namespace MySql.Data.Common
{
  internal class MyNetworkStream : Stream
  {
    IInputStream reader;
    IOutputStream writer;
    StreamSocket streamSocket;
    private int timeout;

    public MyNetworkStream(string host, int port, int timeout)
    {
      Server = host;
      Port = port;
      this.timeout = timeout;
    }

    public string Server { get; private set; }
    public int Port { get; private set; }

    public async void Open()
    {
      await OpenConnection();
      reader = streamSocket.InputStream;
      writer = streamSocket.OutputStream;
    }

    private async Task OpenConnection()
    {
      streamSocket = new StreamSocket();

      HostName host = new HostName(Server);

      CancellationTokenSource cts = new CancellationTokenSource();

      try
      {
        cts.CancelAfter(timeout*1000);
        await streamSocket.ConnectAsync(host, Port.ToString()).AsTask(cts.Token);
      }
      catch (TaskCanceledException)
      {
        // we timed out the connection
        streamSocket.Dispose();
        streamSocket = null;
        throw new TimeoutException(Resources.Timeout);
      }
    }

    public override int ReadTimeout
    {
      get  { return base.ReadTimeout; }
      set  { base.ReadTimeout = value; }
    }

    public override int WriteTimeout
    {
      get { return base.WriteTimeout; }
      set { base.WriteTimeout = value; }
    }

    public override bool CanSeek
    {
      get { return false; }
    }

    public override bool CanTimeout
    {
      get { return true; }
    }

    public override long Position
    {
      get  { throw new NotImplementedException();  }
      set  { throw new NotImplementedException();  }
    }

    public override long Length
    {
      get { throw new NotImplementedException(); }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }

    public async override Task<int> Read(byte[] buffer, int offset, int count)
    {
      CancellationTokenSource cts = new CancellationTokenSource();
      try
      {
        cts.CancelAfter(base.ReadTimeout);
        Windows.Storage.Streams.Buffer tempBuffer = new Windows.Storage.Streams.Buffer((uint)count);
        var returnValue = await streamSocket.InputStream.ReadAsync(tempBuffer, (uint)count, InputStreamOptions.Partial).AsTask(cts.Token);
        buffer = tempBuffer.ToArray();
        return (int)returnValue.Length;
      }
      catch (TaskCanceledException)
      {
        streamSocket.Dispose();
        streamSocket = null;
        throw new TimeoutException(Resources.Timeout);
      }
    }

    public async override void Write(byte[] buffer, int offset, int count)
    {
      CancellationTokenSource cts = new CancellationTokenSource();
      try
      {
        cts.CancelAfter(base.WriteTimeout);
        Windows.Storage.Streams.Buffer tempBuffer = new Windows.Storage.Streams.Buffer((uint)count);
        buffer.CopyTo(0, tempBuffer, (uint)offset, count);
        await streamSocket.OutputStream.WriteAsync(tempBuffer).AsTask(cts.Token);
      }
      catch (TaskCanceledException)
      {
        streamSocket.Dispose();
        streamSocket = null;
        throw new TimeoutException(Resources.Timeout);
      }
    }

    public static MyNetworkStream CreateStream(MySqlConnectionStringBuilder settings, bool unix)
    {
      MyNetworkStream s = new MyNetworkStream(settings.Server, (int)settings.Port, (int)settings.ConnectionTimeout);
      s.Open();
      return s;
    }

  }
}