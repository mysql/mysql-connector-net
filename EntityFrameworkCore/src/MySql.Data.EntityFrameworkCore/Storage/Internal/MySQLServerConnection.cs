// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Extensions;
using System.Reflection;

namespace MySql.Data.EntityFrameworkCore
{
  internal partial class MySQLServerConnection : RelationalConnection
  {
    private string _cnnStr
    {
      get
      {
        if (this.DbConnection != null)
        {
          var cstr = ((MySqlConnectionStringBuilder)((MySqlConnection)this.DbConnection).GetType().GetProperty("Settings", System.Reflection.BindingFlags.Instance
                      | System.Reflection.BindingFlags.NonPublic).GetValue(((MySqlConnection)this.DbConnection), null));
          return cstr.ConnectionString;
        }
        return null;
      }
    }


    protected override DbConnection CreateDbConnection()
    {
      return new MySqlConnection(ConnectionString);
    }


    public MySQLServerConnection CreateSystemConnection()
    {
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(_cnnStr ?? ConnectionString);
      builder.Database = "mysql";

      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(builder.ConnectionString);

      MySQLServerConnection c = new MySQLServerConnection(optionsBuilder.Options, Logger);
      return c;
    }
  }
}