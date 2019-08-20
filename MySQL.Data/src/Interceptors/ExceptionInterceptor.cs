// Copyright © 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using MySql.Data.MySqlClient;


namespace MySql.Data.MySqlClient.Interceptors
{
  /// <summary>
  /// BaseExceptionInterceptor is the base class that should be used for all userland 
  /// exception interceptors.
  /// </summary>
  public abstract class BaseExceptionInterceptor
  {
    /// <summary>
    /// Returns the received exception.
    /// </summary>
    /// <param name="exception">The exception to be returned.</param>
    /// <returns>The exception originally received.</returns>
    public abstract Exception InterceptException(Exception exception);

    /// <summary>
    /// Gets the active connection.
    /// </summary>
    protected MySqlConnection ActiveConnection { get; private set; }

    /// <summary>
    /// Initilizes this object by setting the active connection.
    /// </summary>
    /// <param name="connection">The connection to become active.</param>
    public virtual void Init(MySqlConnection connection)
    {
      ActiveConnection = connection;
    }
  }

  /// <summary>
  /// StandardExceptionInterceptor is the standard interceptor that simply returns the exception.
  /// It is the default action.
  /// </summary>
  internal sealed class StandardExceptionInterceptor : BaseExceptionInterceptor
  {
    /// <summary>
    /// Returns the received exception, which is the default action
    /// </summary>
    /// <param name="exception">The exception to be returned.</param>
    /// <returns>The exception originally received.</returns>
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

    protected override string ResolveType(string nameOrType)
    {
      if (MySqlConfiguration.Settings == null || MySqlConfiguration.Settings.ExceptionInterceptors == null)
        return base.ResolveType(nameOrType);
      foreach (InterceptorConfigurationElement e in MySqlConfiguration.Settings.ExceptionInterceptors)
        if (String.Compare(e.Name, nameOrType, true) == 0)
          return e.Type;
      return base.ResolveType(nameOrType);
    }
  }
}
