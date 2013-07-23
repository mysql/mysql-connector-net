// Copyright (c) 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NET_45_OR_GREATER
#if EF6
using System.Data.Entity.Config;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Internal;
#endif
#endif
using MySql.Data.MySqlClient;

namespace MySql.Data.Entity
{
  public class MySqlConfiguration
#if EF6 
    : DbConfiguration
#endif
  {
    public MySqlConfiguration()
    {
#if EF6
      AddDbProviderFactory("MySql.Data.MySqlClient", new MySqlClientFactory());
      AddDbProviderServices(new MySqlProviderServices());
      AddDbSpatialServices(MySqlSpatialServices.Instance);
      AddDependencyResolver(new MySqlProviderServices());
      //THESE LINES CONFLICT WITH THE OVERRIDE IN CONNECTOR CLASSES, EF LOADS THIS AS PART OF ITS CODE AND THE CODE WITH CUSTOM IMPLEMENTATION IS NOT REACHED
      //AddMigrationSqlGenerator(() => { return new MySqlMigrationSqlGenerator(); });
      //AddMigrationSqlGenerator<MySqlMigrationSqlGenerator>(() => { return new MySqlMigrationSqlGenerator(); });
      SetDefaultConnectionFactory(new MySqlConnectionFactory());
      SetDefaultDbSpatialServices(MySqlSpatialServices.Instance);
#endif
    }
  }
}
