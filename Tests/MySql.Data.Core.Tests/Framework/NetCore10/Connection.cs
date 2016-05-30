// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.MySqlClient.Test
{
  // This project can output the Class library as a NuGet Package.
  // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
  public class Connection : Common
  {
    [Fact]
    public void ConnectionOpen()
    {
      CheckAppSettings();

      MySqlConnection connection = new MySqlConnection();

      connection.Open();
      Assert.True(connection.State == ConnectionState.Open);

      connection.Close();
      Assert.True(connection.State == ConnectionState.Closed);
    }

    [Fact]
    public void ConnectionOpenFail()
    {
      if (File.Exists("appsettings.json"))
        File.Move("appsettings.json", "appsettings1.json");

      MySqlConnection connection = new MySqlConnection();

      var ex = Assert.Throws<MySqlException>(() => connection.Open());
      Assert.Equal(connection.ConnectionString, string.Empty);

      if (File.Exists("appsettings1.json"))
        File.Move("appsettings1.json", "appsettings.json");

    }
  }
}
