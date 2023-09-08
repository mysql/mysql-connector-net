// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   MySQL specific extension methods for <see cref="DbContext.Database" />.
  /// </summary>
  public static class MySQLDatabaseFacadeExtensions
  {
    /// <summary>
    ///   <para>
    ///   Indicates whether the database provider currently in use is the MySQL provider.
    ///   </para>
    ///   <para>
    ///   This method can only be used after the <see cref="DbContext" /> has been configured because
    ///   it is only then that the provider is known. This method cannot be used
    ///   in <see cref="DbContext.OnConfiguring" /> because this is where application code sets the
    ///   provider to use as part of configuring the context.
    ///   </para>
    /// </summary>
    /// <param name="database"> The facade from <see cref="DbContext.Database" />. </param>
    /// <returns><see langword="true"/> if MySQL is being used; otherwise, <see langword="false"/>. </returns>
    public static bool IsMySql(this DatabaseFacade database)
      => database.ProviderName!.Equals(
      typeof(MySQLOptionsExtension).GetTypeInfo().Assembly.GetName().Name,
      StringComparison.Ordinal);
  }
}
