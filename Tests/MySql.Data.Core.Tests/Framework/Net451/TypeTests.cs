// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using Xunit;
using MySql.Data.Types;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  public class TypeTests
  {
    /// <summary>
    /// Test fix for http://bugs.mysql.com/bug.php?id=40555
    /// Make MySql.Data.Types.MySqlDateTime serializable.
    /// </summary>
    [Fact]
    public void TestSerializationMySqlDataTime()
    {
      MySqlDateTime dt = new MySqlDateTime(2011, 10, 6, 11, 38, 01, 0);
      Assert.Equal(2011, dt.Year);
      Assert.Equal(10, dt.Month);
      Assert.Equal(6, dt.Day);
      Assert.Equal(11, dt.Hour);
      Assert.Equal(38, dt.Minute);
      Assert.Equal(1, dt.Second);
      BinaryFormatter bf = new BinaryFormatter();
      MemoryStream ms = new MemoryStream(1024);
      bf.Serialize(ms, dt);
      ms.Position = 0;
      object o = bf.Deserialize(ms);
      dt = (MySqlDateTime)o;
      Assert.Equal(2011, dt.Year);
      Assert.Equal(10, dt.Month);
      Assert.Equal(6, dt.Day);
      Assert.Equal(11, dt.Hour);
      Assert.Equal(38, dt.Minute);
      Assert.Equal(1, dt.Second);
    }
  }
}
