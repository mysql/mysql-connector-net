// Copyright (c) 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Data.Entity.Infrastructure;

namespace MySql.Data.EntityFramework
{
  /// <summary>
  /// Provides the capability to resolve a dependency.
  /// </summary>
  public class MySqlDependencyResolver : IDbDependencyResolver
  {
    /// <summary>
    /// Attempts to resolve a dependency for a given contract type and optionally a given key.
    /// </summary>
    /// <param name="type">The base class that defines the dependency to be resolved.</param>
    /// <param name="key">Optionally, the key of the dependency to be resolved.</param>
    /// <returns>The resolved dependency.</returns>
    public object GetService(Type type, object key)
    {
      EServiceType servType;
      if (Enum.TryParse(type.Name, true, out servType))
      {
        switch (servType)
        {
          case EServiceType.DbProviderFactory:
            return new MySqlClient.MySqlClientFactory();
          case EServiceType.IDbConnectionFactory:
            return new MySqlConnectionFactory();
          case EServiceType.MigrationSqlGenerator:
            return new MySqlMigrationSqlGenerator();
          case EServiceType.DbProviderServices:
            return new MySqlClient.MySqlProviderServices();
          case EServiceType.IProviderInvariantName:
            return new MySqlProviderInvariantName();
#if NET_45_OR_GREATER
          case EServiceType.IDbProviderFactoryResolver:
            return new MySqlProviderFactoryResolver(); 
#endif
          case EServiceType.IManifestTokenResolver:
            return new MySqlManifestTokenResolver();
          case EServiceType.IDbModelCacheKey:
            return new SingletonDependencyResolver<Func<System.Data.Entity.DbContext, IDbModelCacheKey>>(new MySqlModelCacheKeyFactory().Create);
          case EServiceType.IDbExecutionStrategy:
            return new MySqlExecutionStrategy();
        }
      }
      return null;
    }

    /// <summary>
    /// Attempts to resolve a dependency for all of the registered services with the given type and key combination.
    /// </summary>
    /// <param name="type">The base class that defines the dependency to be resolved.</param>
    /// <param name="key">Optionally, the key of the dependency to be resolved.</param>
    /// <returns>All services that resolve the dependency.</returns>
    public IEnumerable<object> GetServices(Type type, object key)
    {
      var service = GetService(type, key);
      return service == null ? Enumerable.Empty<object>() : new[] { service };
    }
  }

  /// <summary>
  /// Used to resolve a provider invariant name from a provider factory.
  /// </summary>
  public class MySqlProviderInvariantName : IProviderInvariantName
  {
    private const string _providerName = "MySql.Data.MySqlClient";

    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name
    {
      get { return _providerName; }
    }

    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public static string ProviderName
    {
      get { return MySqlProviderInvariantName._providerName; }
    }
  }

  /// <summary>
  /// Service that obtains the provider factory from a given connection.
  /// </summary>
  public class MySqlProviderFactoryResolver : IDbProviderFactoryResolver
  {
    /// <summary>
    /// Returns the <see cref="DbProviderFactory"/> for the given connection.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <returns>The provider factory for the connection.</returns>
    public DbProviderFactory ResolveProviderFactory(DbConnection connection)
    {
#if NET_45_OR_GREATER 
      return DbProviderFactories.GetFactory(connection);
#else
      return DbProviderFactories.GetFactory(MySqlProviderInvariantName.ProviderName);
#endif
    }
  }

  /// <summary>
  /// Gets a provider manifest token for the given connection.
  /// </summary>
  public class MySqlManifestTokenResolver : IManifestTokenResolver
  {
    /// <summary>
    /// Returns the manifest token to use for the given connection.
    /// </summary>
    /// <param name="connection">The connection for which a manifest token is required.</param>
    /// <returns>The manifest token to use.</returns>
    public string ResolveManifestToken(System.Data.Common.DbConnection connection)
    {
      return MySqlClient.MySqlProviderServices.GetProviderServices(connection).GetProviderManifestToken(connection);
    }
  }

  /// <summary>
  /// Represents a key value that uniquely identifies an Entity Framework model that has been loaded into memory. 
  /// </summary>
  public class MySqlModelCacheKey : IDbModelCacheKey
  {
    private readonly Type _ctxType;
    private readonly string _providerName;
    private readonly Type _providerType;
    private readonly string _customKey;

    public MySqlModelCacheKey(Type contextType, string providerName, Type providerType, string customKey)
    {
      _ctxType = contextType;
      _providerName = providerName;
      _providerType = providerType;
      _customKey = customKey;
    }

    /// <summary>
    /// Determines whether the current cached model key is equal to the specified cached
    /// model key.
    /// </summary>
    /// <param name="other">The cached model key to compare to the current cached model key.</param>
    /// <returns><c>true</c> if the current cached model key is equal to the specified cached model key;
    /// otherwise, <c>false</c>.</returns>
    public bool Equals(object other)
    {
      if (ReferenceEquals(this, other))
        return true;

      var modelCacheKey = other as MySqlModelCacheKey;
      return (modelCacheKey != null) && Equals(modelCacheKey);
    }

    /// <summary>
    /// Returns the hash function for this cached model key.
    /// </summary>
    /// <returns>The hash function for this cached model key.</returns>
    public int GetHashCode()
    {
      unchecked
      {
        int hash = 43;
        hash = hash * 47 + _ctxType.GetHashCode();
        hash = hash * 47 +  _providerName.GetHashCode();
        hash = hash * 47 + _providerType.GetHashCode();
        hash = hash * 47 + (!string.IsNullOrWhiteSpace(_customKey) ? _customKey.GetHashCode() : 0);
        return hash;
      }
    }

    private bool Equals(MySqlModelCacheKey other)
    {
      return (_ctxType == other._ctxType && string.Equals(_providerName, other._providerName) && Equals(_providerType, other._providerType) && string.Equals(_customKey, other._customKey));
    }
  }

  internal class MySqlModelCacheKeyFactory
  {
    public IDbModelCacheKey Create(System.Data.Entity.DbContext context)
    {
      string customKey = null;

      var modelCacheKeyProvider = context as IDbModelCacheKeyProvider;
      if (modelCacheKeyProvider != null)
      {
        customKey = modelCacheKeyProvider.CacheKey;
      }

      return new MySqlModelCacheKey(context.GetType(), MySqlProviderInvariantName.ProviderName, typeof(MySqlClient.MySqlClientFactory), customKey);
    }
  }

  internal enum EServiceType
  {
    DbProviderFactory,
    DbProviderServices,
    IDbConnectionFactory,
    DbSpatialServices,
    MigrationSqlGenerator,
    IProviderInvariantName,
    IDbProviderFactoryResolver,
    IManifestTokenResolver,
    HistoryContext,
    IDbModelCacheKey,
    IDbExecutionStrategy
  }
}
