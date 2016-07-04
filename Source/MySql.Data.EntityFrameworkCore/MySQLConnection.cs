// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Storage;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.Data.Entity;
using MySQL.Data.Entity.Extensions;

namespace MySQL.Data.Entity
{
  public class MySQLConnection : RelationalConnection
    {
        public int flag;

        public MySQLConnection(
          [NotNull]IDbContextOptions options, 
          [NotNull]ILogger<MySQLConnection> logger) 
          : base(options, logger)
        {
        }

        public MySQLConnection(
          [NotNull]IDbContextOptions options,
          [NotNull]ILogger logger)
          : base(options, logger)
        {
        }

    protected override DbConnection CreateDbConnection()
        {
            return new MySqlConnection(ConnectionString);
        }


        public MySQLConnection CreateSystemConnection()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
            builder.Database = "mysql";

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseMySQL(builder.ConnectionString);

            MySQLConnection c = new MySQLConnection(optionsBuilder.Options, Logger);
            return c;
        }
    }
}