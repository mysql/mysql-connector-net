// Copyright © 2004, 2016 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Reflection;
#if !NETSTANDARD1_3
using System.Security.Permissions;
#endif

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Represents a set of methods for creating instances of the MySQL client implementation of the data source classes.
  /// </summary>
#if !NETSTANDARD1_3
  [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]  
#endif
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
#if NETSTANDARD1_3
          string fullName = typeof(MySqlClientFactory).GetTypeInfo().Assembly.FullName;
#else
					string fullName = Assembly.GetExecutingAssembly().FullName;
#endif
					string assemblyName = fullName.Replace("MySql.Data", "MySql.Data.Entity");
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

