// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Data.Common;
using System.Reflection;
using System.Security.Permissions;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Represents a set of methods for creating instances of the MySQL client implementation of the data source classes.
  /// </summary>
  [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
  public sealed partial class MySqlClientFactory : DbProviderFactory, IServiceProvider
  {
    /// <summary>
    /// Gets an instance of the <see cref="MySqlClientFactory"/>. 
    /// This can be used to retrieve strongly typed data objects. 
    /// </summary>
    public static MySqlClientFactory Instance = new MySqlClientFactory();
    private Type _dbServicesType;
    private FieldInfo _mySqlDbProviderServicesInstance;

    /// <summary>
    /// Returns a strongly typed <see cref="DbCommand"/> instance. 
    /// </summary>
    /// <returns>A new strongly typed instance of <b>DbCommand</b>.</returns>
    public override DbCommand CreateCommand()
    {
      return new MySqlCommand();
    }

    /// <summary>
    /// Returns a strongly typed <see cref="DbConnection"/> instance. 
    /// </summary>
    /// <returns>A new strongly typed instance of <b>DbConnection</b>.</returns>
    public override DbConnection CreateConnection()
    {
      return new MySqlConnection();
    }

    /// <summary>
    /// Returns a strongly typed <see cref="DbParameter"/> instance. 
    /// </summary>
    /// <returns>A new strongly typed instance of <b>DbParameter</b>.</returns>
    public override DbParameter CreateParameter()
    {
      return new MySqlParameter();
    }

    /// <summary>
    /// Returns a strongly typed <see cref="DbConnectionStringBuilder"/> instance. 
    /// </summary>
    /// <returns>A new strongly typed instance of <b>DbConnectionStringBuilder</b>.</returns>
    public override DbConnectionStringBuilder CreateConnectionStringBuilder()
    {
      return new MySqlConnectionStringBuilder();
    }

    /// <summary>
    /// Returns a strongly typed <see cref="DbCommandBuilder"/> instance. 
    /// </summary>
    /// <returns>A new strongly typed instance of <b>DbCommandBuilder</b>.</returns>
    public override DbCommandBuilder CreateCommandBuilder()
    {
      return new MySqlCommandBuilder();
    }

    /// <summary>
    /// Returns a strongly typed <see cref="DbDataAdapter"/> instance. 
    /// </summary>
    /// <returns>A new strongly typed instance of <b>DbDataAdapter</b>.</returns>
    public override DbDataAdapter CreateDataAdapter()
    {
      return new MySqlDataAdapter();
    }

    #region IServiceProvider Members

    /// <summary>
    /// Provide a simple caching layer
    /// </summary>
    private Type DbServicesType => _dbServicesType ?? (_dbServicesType = Type.GetType(
        @"System.Data.Common.DbProviderServices, System.Data.Entity, 
												Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        false));

    private FieldInfo MySqlDbProviderServicesInstance
    {
      get
      {
        if (_mySqlDbProviderServicesInstance == null)
        {
          string fullName = Assembly.GetExecutingAssembly().FullName;
          string assemblyName = fullName.Replace("MySql.Data", "MySql.Data.EntityFramework");
          string assemblyEf5Name = fullName.Replace("MySql.Data", "MySql.Data.Entity.EF5");
          fullName = $"MySql.Data.MySqlClient.MySqlProviderServices, {assemblyEf5Name}";

          Type providerServicesType = Type.GetType(fullName, false);
          if (providerServicesType == null)
          {
            fullName = $"MySql.Data.MySqlClient.MySqlProviderServices, {assemblyName}";
            providerServicesType = Type.GetType(fullName, false);
            if (providerServicesType == null)
              throw new DllNotFoundException(fullName);
          }
          _mySqlDbProviderServicesInstance = providerServicesType.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }
        return _mySqlDbProviderServicesInstance;
      }
    }

    object IServiceProvider.GetService(Type serviceType)
    {
      // DbProviderServices is the only service we offer up right now
      return serviceType != DbServicesType ? null : MySqlDbProviderServicesInstance?.GetValue(null);
    }

    #endregion
  }
}

