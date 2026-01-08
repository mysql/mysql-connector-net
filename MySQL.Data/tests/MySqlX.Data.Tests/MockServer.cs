// Copyright © 2025, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class MockServer
  {
    private TcpListener listener;

    private int _port;

    private IPAddress _address;

    private ManualResetEvent _stopserver = new ManualResetEvent(false);

    private CancellationTokenSource cts;

    private bool _usetimeout;

    public int Port
    {
      get => _port;
      set => _port = value;
    }

    public IPAddress Address
    {
      get => _address;
      set => _address = value;
    }

    public MockServer(bool usetimeout)
    {
      GetMockServerInfo();
      _stopserver.Reset();
      _usetimeout = usetimeout;
      cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
    }

    public void GetMockServerInfo()
    {
      TcpListener loopback = new TcpListener(IPAddress.Loopback, 0);
      loopback.Start();
      _port = ((IPEndPoint)loopback.LocalEndpoint).Port;
      loopback.Stop();
      _address = Dns.GetHostEntry("localhost").AddressList[0];
    }

    public void StopServer()
    {
      _stopserver.Set();
    }
    public void DisposeListener()
    {
      if (listener != null)
      {
        listener.Stop();
      }
    }

    public async Task ServerWorker(CancellationToken ct)
    {
      await Task.Run(() =>
      {
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, Port);
        listener = new(endpoint);

        try
        {
          listener.Start();
          while (!_stopserver.WaitOne(1))
          {
            listener.BeginAcceptSocket(new AsyncCallback(beginConnection), null);
          }
        }
        catch (Exception)
        {
        }
        finally
        {
          listener.Stop();
        }
      }, ct).ConfigureAwait(false);
    }

    public void StartServer()
    {
      ServerWorker(cts.Token).ConfigureAwait(false);
    }

    private void beginConnection(IAsyncResult iar)
    {
      try
      {
        Socket client = listener.EndAcceptSocket(iar);
        if (_usetimeout)
        {
          Task.Delay(500).Wait();
          client.Close();
        }
      }
      catch (Exception)
      {
      }
    }
  }
}
