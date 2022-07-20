// Copyright (c) 2022, Oracle and/or its affiliates.
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
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Data.Common;
using System.Reflection;
using System.Transactions;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLRelationalConnection : RelationalConnection, IMySQLRelationalConnection
  {
    private readonly MySQLOptionsExtension _mySqlOptionsExtension;

    public MySQLRelationalConnection([NotNull] RelationalConnectionDependencies dependencies)
      : base(dependencies)
    {
      _mySqlOptionsExtension = Dependencies.ContextOptions.FindExtension<MySQLOptionsExtension>() ?? new MySQLOptionsExtension();
    }

    protected override DbConnection CreateDbConnection() => new MySqlConnection(ConnectionString);

    private string? _cnnStr
    {
      get
      {
        if (this.DbConnection != null)
        {
          var cstr = (MySqlConnectionStringBuilder)((MySqlConnection)DbConnection).GetType().GetProperty("Settings", BindingFlags.Instance
                | BindingFlags.NonPublic)!.GetValue((MySqlConnection)DbConnection, null)!;
          return cstr?.ConnectionString;
        }
        return null;
      }
    }

    public virtual IMySQLRelationalConnection CreateSourceConnection()
    {
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(_cnnStr ?? ConnectionString);
      builder.Database = "mysql";

      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(builder.ConnectionString);

      MySQLRelationalConnection c = new MySQLRelationalConnection(Dependencies with { ContextOptions = optionsBuilder.Options });

      return c;
    }

    public override void EnlistTransaction(Transaction? transaction)
    {
      try
      {
        base.EnlistTransaction(transaction);
      }
      catch (MySqlException e)
      {
        if (e.Message == "Already enlisted in a Transaction.")
        {
          // Return expected exception type.
          throw new InvalidOperationException(e.Message, e);
        }

        throw;
      }
    }
  }
}