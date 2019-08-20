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
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// BaseCommandInterceptor is the base class that should be used for all userland 
  /// command interceptors
  /// </summary>
  public abstract class BaseCommandInterceptor
  {
	  /// <summary>
    /// Gets the active connection.
    /// </summary>
    protected MySqlConnection ActiveConnection { get; private set; }

    /// <summary>
    /// Executes an SQL statements that returns a scalar value such as a calculation.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <param name="returnValue">A scalar value that represents the result returned by the execution of the SQL statement.</param>
    /// <returns><c>false</c>.</returns>
    /// <remarks>This method is intended to be overriden.</remarks>
    public virtual bool ExecuteScalar(string sql, ref object returnValue)
    {
      return false;
    }

    /// <summary>
    /// Executes an SQL statement that returns the number of affected rows.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <param name="returnValue">The number of affected rows.</param>
    /// <returns><c>false</c>.</returns>
    /// <remarks>This method is intended to be overriden.</remarks>
    public virtual bool ExecuteNonQuery(string sql, ref int returnValue)
    {
      return false;
    }

    /// <summary>
    /// Executes an SQL statement that will return a resultset.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <param name="behavior">A value that describes the results of the query and its effect on the database.</param>
    /// <param name="returnValue">A <see cref="MySqlDataReader"/> object containing the result of the statement execution.</param>
    /// <returns><c>false</c>.</returns>
    /// <remarks>This method is intended to be overriden.</remarks>
    public virtual bool ExecuteReader(string sql, CommandBehavior behavior, ref MySqlDataReader returnValue)
    {
      return false;
    }

    /// <summary>
    /// Sets the active connection.
    /// </summary>
    /// <param name="connection">The active connection.</param>
    public virtual void Init(MySqlConnection connection)
    {
      ActiveConnection = connection;
    }
  }

  /// <summary>
  /// CommandInterceptor is the "manager" class that keeps the list of registered interceptors
  /// for the given connection.
  /// </summary>
  internal sealed partial class CommandInterceptor : Interceptor
  {
    bool _insideInterceptor = false;
    readonly List<BaseCommandInterceptor> _interceptors = new List<BaseCommandInterceptor>();

    public CommandInterceptor(MySqlConnection connection)
    {
      Connection = connection;

      LoadInterceptors(connection.Settings.CommandInterceptors);
    }

    public bool ExecuteScalar(string sql, ref object returnValue)
    {
      if (_insideInterceptor) return false;
      _insideInterceptor = true;

      bool handled = false;

      foreach (BaseCommandInterceptor bci in _interceptors)
        handled |= bci.ExecuteScalar(sql, ref returnValue);

      _insideInterceptor = false;
      return handled;
    }

    public bool ExecuteNonQuery(string sql, ref int returnValue)
    {
      if (_insideInterceptor) return false;
      _insideInterceptor = true;

      bool handled = false;

      foreach (BaseCommandInterceptor bci in _interceptors)
        handled |= bci.ExecuteNonQuery(sql, ref returnValue);

      _insideInterceptor = false;
      return handled;
    }

    public bool ExecuteReader(string sql, CommandBehavior behavior, ref MySqlDataReader returnValue)
    {
      if (_insideInterceptor) return false;
      _insideInterceptor = true;

      bool handled = false;

      foreach (BaseCommandInterceptor bci in _interceptors)
        handled |= bci.ExecuteReader(sql, behavior, ref returnValue);

      _insideInterceptor = false;
      return handled;
    }

    protected override void AddInterceptor(object o)
    {
      if (o == null)
        throw new ArgumentException("Unable to instantiate CommandInterceptor");

      if (!(o is BaseCommandInterceptor))
        throw new InvalidOperationException(String.Format(Resources.TypeIsNotCommandInterceptor,
          o.GetType()));
      BaseCommandInterceptor ie = (BaseCommandInterceptor) o;
      ie.Init(Connection);
      _interceptors.Insert(0, (BaseCommandInterceptor)o);
    }


    protected override string ResolveType(string nameOrType)
    {
      if (MySqlConfiguration.Settings == null || MySqlConfiguration.Settings.CommandInterceptors == null)
        return base.ResolveType(nameOrType);
      foreach (InterceptorConfigurationElement e in MySqlConfiguration.Settings.CommandInterceptors)
        if (String.Compare(e.Name, nameOrType, true) == 0)
          return e.Type;
      return base.ResolveType(nameOrType);
    }
  }
}
