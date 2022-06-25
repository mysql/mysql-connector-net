// Copyright (c) 2022, Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MySql.Data.Common.DnsClient
{
  internal class DnsResolver
  {
    /// <summary>
    /// The default DNS server port.
    /// </summary>
    private const int DefaultPort = 53;
    private ushort _uniqueId;

    private readonly List<IPEndPoint> _dnsServers = new List<IPEndPoint>();

    internal DnsResolver()
    {
      AddLocalServers();
      _uniqueId = (ushort)(new Random()).Next();
    }

    /// <summary>
    /// Fills a list of the endpoints in the local network configuration.
    /// </summary>
    private void AddLocalServers()
    {
      var interfaces = NetworkInterface.GetAllNetworkInterfaces();

      foreach (var networkInterface in interfaces
        .Where(p => (p.OperationalStatus == OperationalStatus.Up || p.OperationalStatus == OperationalStatus.Unknown)
        && p.NetworkInterfaceType != NetworkInterfaceType.Loopback))
      {
        var interfaceProperties = networkInterface.GetIPProperties();

        foreach (var address in interfaceProperties.DnsAddresses)
        {
          var entry = new IPEndPoint(address, DefaultPort);

          if (!_dnsServers.Contains(entry))
            _dnsServers.Add(entry);
        }
      }
    }

    /// <summary> Execute a query on a DNS server. </summary>
    /// <param name="domainName">Domain name to look up. </param>
    /// <returns> DNS response for request. </returns>
    internal DnsResponse Query(string domainName)
    {
      var question = new DnsQuestion(domainName);
      var request = new DnsRequest();
      request.AddQuestion(question);
      return GetResponse(request);
    }

    private DnsResponse GetResponse(DnsRequest request)
    {
      request.Header.Id = _uniqueId;
      request.Header.RecursionDesired = true;
      return UdpRequest(request);
    }

    private DnsResponse UdpRequest(DnsRequest request)
    {
      // RFC1035 max. size of a UDP datagram is 512 bytes
      byte[] responseMessage = new byte[512];

      // number of retries: 3
      for (int intAttempts = 0; intAttempts < 3; intAttempts++)
      {
        foreach (var dnsServer in _dnsServers)
        {
          Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
          socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);

          try
          {
            socket.SendTo(request.GetData(), dnsServer);
            int intReceived = socket.Receive(responseMessage);
            byte[] data = new byte[intReceived];
            Array.Copy(responseMessage, data, intReceived);
            DnsResponse response = new DnsResponse(dnsServer, data);
            return response;
          }
          catch (SocketException)
          {
            continue;
          }
          finally
          {
            socket.Close();
            _uniqueId++;
          }
        }
      }

      DnsResponse responseTimeout = new DnsResponse();
      responseTimeout.Error = "Timeout Error";
      return responseTimeout;
    }
  }
}
