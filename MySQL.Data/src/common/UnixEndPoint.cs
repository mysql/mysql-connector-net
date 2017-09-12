// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MySql.Data.Common
{
  [Serializable]
  internal class UnixEndPoint : EndPoint
  {
    public string SocketName { get; private set; }

    public UnixEndPoint(string socketName)
    {
      this.SocketName = socketName;
    }

    public override EndPoint Create(SocketAddress socketAddress)
    {
      int size = socketAddress.Size - 2;
      byte[] bytes = new byte[size];
      for (int i = 0; i < size; i++)
      {
        bytes[i] = socketAddress[i + 2];
      }
      return new UnixEndPoint(Encoding.UTF8.GetString(bytes));
    }

    public override SocketAddress Serialize()
    {
      byte[] bytes = Encoding.UTF8.GetBytes(SocketName);
      SocketAddress socketAddress = new SocketAddress(System.Net.Sockets.AddressFamily.Unix, bytes.Length + 3);
      for (int i = 0; i < bytes.Length; i++)
      {
        socketAddress[i + 2] = bytes[i];
      }
      return socketAddress;
    }
  }
}
