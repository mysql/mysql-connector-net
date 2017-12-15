// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{

  /*
  /// <summary>
  /// This class implements all the unit tests for the memcached client implementation 
  /// of Connector/NET.
  /// </summary>
  public class Memcached
  {

  //TODO: These tests will only run in linux and OS X

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
