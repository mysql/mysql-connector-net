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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Data.Entity.Infrastructure;

namespace MySql.Data.Entity
{
  public class MySqlDependencyResolver : IDbDependencyResolver
  {
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

    public IEnumerable<object> GetServices(Type type, object key)
    {
      var service = GetService(type, key);
      return service == null ? Enumerable.Empty<object>() : new[] { service };
    }
  }

  public class MySqlProviderInvariantName : IProviderInvariantName
  {
    private const string _providerName = "MySql.Data.MySqlClient";

    public string Name
    {
      get { return _providerName; }
    }

    public static string ProviderName
    {
      get { return MySqlProviderInvariantName._providerName; }
    }
  }

  public class MySqlProviderFactoryResolver : IDbProviderFactoryResolver
  {
    public DbProviderFactory ResolveProviderFactory(DbConnection connection)
    {
#if NET_45_OR_GREATER 
      return DbProviderFactories.GetFactory(connection);
#else
      return DbProviderFactories.GetFactory(MySqlProviderInvariantName.ProviderName);
#endif
    }
  }

  public class MySqlManifestTokenResolver : IManifestTokenResolver
  {
    public string ResolveManifestToken(System.Data.Common.DbConnection connection)
    {
      return MySqlClient.MySqlProviderServices.GetProviderServices(connection).GetProviderManifestToken(connection);
    }
  }

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

    public bool Equals(object other)
    {
      if (ReferenceEquals(this, other))
        return true;

      var modelCacheKey = other as MySqlModelCacheKey;
      return (modelCacheKey != null) && Equals(modelCacheKey);
    }

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
