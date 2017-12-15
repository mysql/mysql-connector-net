// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.EntityFrameworkCore.Extensions;

namespace MySql.Data.EntityFrameworkCore.Infraestructure
{
  /// <summary>
  /// Represents the <see cref="RelationalOptionsExtension"/> implementation for MySQL.
  /// </summary>
  public partial class MySQLOptionsExtension : RelationalOptionsExtension
  {
    /// <summary>
    ///     This method supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This method may change or be removed in future releases.
    /// </summary>
    public override void ApplyServices(IServiceCollection services)
    {
      services.AddEntityFrameworkMySQL();
    }

    internal MySQLOptionsExtension WithConnectionString([NotNull] string connectionString)
    {
      ThrowIf.Argument.IsEmpty(connectionString, nameof(connectionString));

      var clone = Clone();

      clone.ConnectionString = connectionString;

      return clone;
    }

    internal MySQLOptionsExtension WithConnection([NotNull] DbConnection connection)
    {
      ThrowIf.Argument.IsNull(connection, nameof(connection));

      var clone = Clone();

      clone.Connection = connection;

      return clone;
    }

    protected virtual MySQLOptionsExtension Clone()
    {
      return new MySQLOptionsExtension(this);
    }
  }
}