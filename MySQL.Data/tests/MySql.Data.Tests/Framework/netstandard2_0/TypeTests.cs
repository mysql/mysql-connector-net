// Copyright (c) 2013, 2022, Oracle and/or its affiliates.
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

using NUnit.Framework;
using MySql.Data.Types;
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  public class TypeTests : TestBase
  {
#if !NET7_0_OR_GREATER
    /// <summary>
    /// Test fix for http://bugs.mysql.com/bug.php?id=40555
    /// Make MySql.Data.Types.MySqlDateTime serializable.
    /// </summary>
    [Test]
    public void TestSerializationMySqlDataTime()
    {
      MySqlDateTime dt = new MySqlDateTime(2011, 10, 6, 11, 38, 01, 0);
      Assert.AreEqual(2011, dt.Year);
      Assert.AreEqual(10, dt.Month);
      Assert.AreEqual(6, dt.Day);
      Assert.AreEqual(11, dt.Hour);
      Assert.AreEqual(38, dt.Minute);
      Assert.AreEqual(1, dt.Second);
      System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf =
        new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
      MemoryStream ms = new MemoryStream(1024);
      bf.Serialize(ms, dt);
      ms.Position = 0;
      object o = bf.Deserialize(ms);
      dt = (MySqlDateTime)o;
      Assert.AreEqual(2011, dt.Year);
      Assert.AreEqual(10, dt.Month);
      Assert.AreEqual(6, dt.Day);
      Assert.AreEqual(11, dt.Hour);
      Assert.AreEqual(38, dt.Minute);
      Assert.AreEqual(1, dt.Second);
    }
#endif
  }
}