// Copyright (C) 2004-2007 MySQL AB
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
using System.Data;
using System.IO;
using NUnit.Framework;
using System.Transactions;
using System.Data.Common;
using System.Threading;
using System.Diagnostics;
using System.Text;
using MySql.Data.Types;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class TypeTests : BaseTest
  {
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
      BinaryFormatter bf = new BinaryFormatter();
      MemoryStream ms = new MemoryStream( 1024 );
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
  }
}
