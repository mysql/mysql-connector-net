// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.MySqlClient.Memcached
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using System.IO;
  using System.Net;
  //using System.Net.Sockets;
  using MySql.Data.Common;
  using MySql.Data.MySqlClient.Properties;

  /// <summary>
  /// An interface of the client memcached protocol. This class is abstract for 
  /// implementation of the Memcached client interface see <see cref="TextClient"/> for the 
  /// text protocol version and <see cref="BinaryClient"/> for the binary protocol version.
  /// </summary>
  public abstract class Client
  {
    protected uint port;
    protected string server;
    protected Stream stream;

    public static Client GetInstance(string server, uint port, MemcachedFlags flags)
    {
      if ((flags | MemcachedFlags.TextProtocol) != 0)
      {
        return new TextClient(server, port);
      }
      else if ( ( flags | MemcachedFlags.BinaryProtocol ) != 0 )
      {
        return new BinaryClient(server, port);
      }
      return null;
    }

    /// <summary>
    /// Opens the client connection.
    /// </summary>
    public virtual void Open()
    {
      this.stream = StreamCreator.GetStream(server, port, null, 10, new DBVersion(), 60);
    }

    /// <summary>
    /// Closes the client connection.
    /// </summary>
    public virtual void Close()
    {
      stream.Dispose();
    }

    protected Client(string server, uint port)
    {
      this.server = server;
      this.port = port;
    }

    public abstract void Add(string key, object data, TimeSpan expiration);
    public abstract void Append(string key, object data);
    public abstract void Cas(string key, object data, TimeSpan expiration, ulong casUnique);
    public abstract void Decrement(string key, int amount);
    public abstract void Delete(string key);
    public abstract void FlushAll(TimeSpan delay);
    public abstract KeyValuePair<string, object> Get(string key);
    public abstract void Increment(string key, int amount);
    public abstract void Prepend(string key, object data);
    public abstract void Replace(string key, object data, TimeSpan expiration);
    public abstract void Set(string key, object data, TimeSpan expiration);
  }

  [Flags]
  public enum MemcachedFlags : ushort
  {
    TextProtocol = 0x1,
    BinaryProtocol = 0x2,
    Tcp = 0x4,
    Udp = 0x8,
    Pooled = 0x10
  }
}
