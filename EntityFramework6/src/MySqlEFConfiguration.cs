﻿// Copyright (c) 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Internal;
using MySql.Data.MySqlClient;

namespace MySql.Data.Entity
{
  public class MySqlEFConfiguration : DbConfiguration
  {
    public MySqlEFConfiguration()
    {
      AddDependencyResolver(new MySqlDependencyResolver());

      SetProviderFactory(MySqlProviderInvariantName.ProviderName, new MySqlClientFactory());
      SetProviderServices(MySqlProviderInvariantName.ProviderName, new MySqlProviderServices());
      SetDefaultConnectionFactory(new MySqlConnectionFactory());
      SetMigrationSqlGenerator(MySqlProviderInvariantName.ProviderName, () => new MySqlMigrationSqlGenerator());
#if NET_45_OR_GREATER
      SetProviderFactoryResolver(new MySqlProviderFactoryResolver()); 
#endif
      SetManifestTokenResolver(new MySqlManifestTokenResolver());
      SetHistoryContext(MySqlProviderInvariantName.ProviderName, (existingConnection, defaultSchema) => new MySqlHistoryContext(existingConnection, defaultSchema));
//      //CURRENTLY IS NOT SUPPORTED WORK WITH TRANSACTIONS AND EXECUTION STRATEGY AT THE SAME TIME: http://msdn.microsoft.com/en-US/data/dn307226
//      //IF WE SET THE EXECUTION STRATEGY HERE THAT WILL AFFECT THE USERS WHEN THEY TRY TO USE TRANSACTIONS, FOR THAT REASON EXECUTION STRATEGY WILL BE ENABLED ON DEMAND BY THEM
//      //SetExecutionStrategy(MySqlProviderInvariantName.ProviderName, () => { return new MySqlExecutionStrategy(); });
    }
  }
}
