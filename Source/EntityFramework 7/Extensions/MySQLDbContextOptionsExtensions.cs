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

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using System.Data.Common;

namespace MySQL.Data.Entity.Extensions
{
    public static class MySQLDbContextOptionsExtensions
    {
        public static MySQLDbContextOptionsBuilder UseMySQL(this DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            var extension = optionsBuilder.Options.FindExtension<MySQLOptionsExtension>();
            if (extension == null)
                extension = new MySQLOptionsExtension();
            extension.ConnectionString = connectionString;

            IDbContextOptionsBuilderInfrastructure o = optionsBuilder as IDbContextOptionsBuilderInfrastructure;
            o.AddOrUpdateExtension(extension);

            return new MySQLDbContextOptionsBuilder(optionsBuilder);
        }

        public static MySQLDbContextOptionsBuilder UseMySQL(this DbContextOptionsBuilder optionsBuilder, DbConnection connection)
        {
            var extension = optionsBuilder.Options.FindExtension<MySQLOptionsExtension>();
            if (extension == null)
                extension = new MySQLOptionsExtension();
            extension.Connection = connection;

            IDbContextOptionsBuilderInfrastructure o = optionsBuilder as IDbContextOptionsBuilderInfrastructure;
            o.AddOrUpdateExtension(extension);

            return new MySQLDbContextOptionsBuilder(optionsBuilder);
        }



        private static MySQLOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        {
            var existing = optionsBuilder.Options.FindExtension<MySQLOptionsExtension>();
            return existing != null
                ? new MySQLOptionsExtension(existing)
                : new MySQLOptionsExtension();
        }

    }
}