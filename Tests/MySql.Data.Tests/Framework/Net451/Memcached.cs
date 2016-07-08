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

namespace MySql.Data.MySqlClient.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using MySql.Data.MySqlClient.Memcached;
  using NUnit.Framework;

  /*
  /// <summary>
  /// This class implements all the unit tests for the memcached client implementation 
  /// of Connector/NET.
  /// </summary>
  public class Memcached
  {
    // A server that must be running Innodb memcached plugin in the standard 11211 port.
    public static readonly string SERVER = "192.168.56.99";
    public static readonly uint PORT = 11211;

    [Test]
    public void SetTest()
    {
      Client cli = Client.GetInstance( SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "hello", TimeSpan.Zero);
      string s = ( string )cli.Get("key1").Value;
      cli.Close();
      Assert.AreEqual("hello", s);
    }

    [Test]
    public void AddTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Add("key4", "hello", TimeSpan.Zero);
      string s = (string)cli.Get("key4").Value;
      cli.Close();
      Assert.AreEqual("hello", s);
    }

    [Test]
    public void AppendTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "Hello", TimeSpan.Zero);
      cli.Append("key1", "World");
      string s = (string)cli.Get("key1").Value;
      cli.Close();
      Assert.AreEqual("HelloWorld", s);
    }

    [Test]
    public void PrependTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "hello", TimeSpan.Zero);
      cli.Prepend("key1", "World");
      string s = (string)cli.Get("key1").Value;
      cli.Close();
      Assert.AreEqual("Worldhello", s);
    }

    [Test]
    public void ReplaceTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "hello", TimeSpan.Zero);
      cli.Replace("key1", "world", TimeSpan.Zero);
      string s = (string)cli.Get("key1").Value;
      cli.Close();
      Assert.AreEqual("world", s);
    }

    [Test]
    public void DeleteTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "hello", TimeSpan.Zero);
      try
      {
        cli.Delete("key1");
        string s = (string)cli.Get("key1").Value;
        Assert.Fail();
      }
      catch (MemcachedException) { }
      finally
      {
        cli.Close();
      }      
    }

    [Test]
    public void IncrementTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "1", TimeSpan.Zero);
      cli.Increment("key1", 1);
      int n = Convert.ToInt32(cli.Get("key1").Value);
      cli.Close();
      Assert.AreEqual(2, n);
    }

    [Test]
    public void DecrementTest()
    {
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.TextProtocol);
      cli.Open();
      cli.Set("key1", "1", TimeSpan.Zero);
      cli.Decrement("key1", 1);
      int n = Convert.ToInt32(cli.Get("key1").Value);
      cli.Close();
      Assert.AreEqual(0, n);
    }

    [Test]
    public void SimpleBinaryTest()
    {
      //System.Diagnostics.Debug.Write(BitConverter.IsLittleEndian);
      Client cli = Client.GetInstance(SERVER, PORT, MemcachedFlags.BinaryProtocol);
      cli.Open();
      cli.Set("key3", "hello4", TimeSpan.Zero);
      string s = (string)cli.Get("key3").Value;
      cli.Close();
      Assert.AreEqual("hello4", s);
    }
  }
   * */
}
