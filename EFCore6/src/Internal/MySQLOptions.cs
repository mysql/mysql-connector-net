// Copyright (c) 2021, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.Data.MySqlClient;
using System;

namespace MySql.EntityFrameworkCore.Internal
{
  internal class MySQLOptions : IMySQLOptions
  {
    private static readonly MySQLSchemaNameTranslator _ignoreSchemaNameTranslator = (_, objectName) => objectName;
    public virtual CharacterSet CharSet { get; private set; }
    public virtual MySqlConnectionStringBuilder ConnectionSettings { get; private set; }

    public MySQLOptions()
    {
      ConnectionSettings = new MySqlConnectionStringBuilder();
    }

    public void Initialize(IDbContextOptions options)
    {
      var mySQLOptions = options.FindExtension<MySQLOptionsExtension>() ?? new MySQLOptionsExtension();
      CharSet = mySQLOptions.CharSet ?? new CharacterSet("utf8mb4", 4);
      ConnectionSettings = GetConnectionSettings(mySQLOptions);
    }

    public void Validate(IDbContextOptions options)
    {
      _ = options.FindExtension<MySQLOptionsExtension>() ?? new MySQLOptionsExtension();
    }

    private static MySqlConnectionStringBuilder GetConnectionSettings(MySQLOptionsExtension relationalOptions)
    {
      if (relationalOptions.Connection != null)
      {
        return new MySqlConnectionStringBuilder(relationalOptions.Connection.ConnectionString);
      }

      if (relationalOptions.ConnectionString != null)
      {
        return new MySqlConnectionStringBuilder(relationalOptions.ConnectionString);
      }

      throw new InvalidOperationException(RelationalStrings.NoConnectionOrConnectionString);
    }
    public virtual MySQLSchemaNameTranslator SchemaNameTranslator { get; private set; }
  }
}