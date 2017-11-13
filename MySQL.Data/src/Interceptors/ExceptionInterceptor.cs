// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;


namespace MySql.Data.MySqlClient.Interceptors
{
  /// <summary>
  /// BaseExceptionInterceptor is the base class that should be used for all userland 
  /// exception interceptors
  /// </summary>
  public abstract class BaseExceptionInterceptor
  {
    public abstract Exception InterceptException(Exception exception);

    protected MySqlConnection ActiveConnection { get; private set; }

    public virtual void Init(MySqlConnection connection)
    {
      ActiveConnection = connection;
    }
  }

  /// <summary>
  /// StandardExceptionInterceptor is the standard interceptor that simply throws the exception.
  /// It is the default action.
  /// </summary>
  internal sealed class StandardExceptionInterceptor : BaseExceptionInterceptor
  {
    public override Exception InterceptException(Exception exception)
    {
      return exception;
    }
  }

  /// <summary>
  /// ExceptionInterceptor is the "manager" class that keeps the list of registered interceptors
  /// for the given connection.
  /// </summary>
  internal sealed class ExceptionInterceptor : Interceptor
  {
    readonly List<BaseExceptionInterceptor> _interceptors = new List<BaseExceptionInterceptor>();

    public ExceptionInterceptor(MySqlConnection connection) 
    {
      Connection = connection;

      LoadInterceptors(connection.Settings.ExceptionInterceptors);

      // we always have the standard interceptor
      _interceptors.Add(new StandardExceptionInterceptor());

    }

    protected override void AddInterceptor(object o)
    {
      if (o == null)
        throw new ArgumentException("Unable to instantiate ExceptionInterceptor");

      if (!(o is BaseExceptionInterceptor))
        throw new InvalidOperationException(String.Format(Resources.TypeIsNotExceptionInterceptor,
          o.GetType()));
      BaseExceptionInterceptor ie = o as BaseExceptionInterceptor;
      ie.Init(Connection);
      _interceptors.Insert(0, (BaseExceptionInterceptor)o);
    }

    public void Throw(Exception exception)
    {
      Exception e = _interceptors.Aggregate(exception, (current, ie) => ie.InterceptException(current));
      throw e;
    }

#if !NETSTANDARD1_3
    protected override string ResolveType(string nameOrType)
    {
      if (MySqlConfiguration.Settings == null || MySqlConfiguration.Settings.ExceptionInterceptors == null)
        return base.ResolveType(nameOrType);
      foreach (InterceptorConfigurationElement e in MySqlConfiguration.Settings.ExceptionInterceptors)
        if (String.Compare(e.Name, nameOrType, true) == 0)
          return e.Type;
      return base.ResolveType(nameOrType);
    }
#endif
  }
}
