// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class ConnectionTests
  {
    [Fact]
    public void CanCreateConnectionString()
    {
      using (var connection = new MySQLServerConnection(CreateOptions(), new Logger<MySQLServerConnection>(new LoggerFactory())))
      {
        Assert.IsType<MySqlConnection>(connection.DbConnection);
      }
    }

    [Fact]
    public void CanCreateMainConnection()
    {
      using (var connection = new MySQLServerConnection(CreateOptions(), new Logger<MySQLServerConnection>(new LoggerFactory())))
      {
        using (var master = connection.CreateSystemConnection())
        {
          var csb = new MySqlConnectionStringBuilder(master.ConnectionString);
          var csb1 = new MySqlConnectionStringBuilder(MySQLTestStore.baseConnectionString + "database=mysql");
          Assert.True(csb.Database == csb1.Database);
          Assert.True(csb.Port == csb1.Port);
          Assert.True(csb.Server == csb1.Server);
          Assert.True(csb.UserID == csb1.UserID);
        }
      }
    }

    public static IDbContextOptions CreateOptions()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.baseConnectionString + "database=test;");
      return optionsBuilder.Options;
    }

  }
}
