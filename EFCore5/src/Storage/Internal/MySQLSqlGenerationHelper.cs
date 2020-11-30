// Copyright (c) 2020 Oracle and/or its affiliates.
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

using System.Text;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Utils;
using MySql.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Infrastructure.Internal;

namespace MySql.EntityFrameworkCore
{
  public partial class MySQLSqlGenerationHelper : RelationalSqlGenerationHelper
  {
    private readonly IMySQLOptions _options;

    public MySQLSqlGenerationHelper([NotNull] RelationalSqlGenerationHelperDependencies dependencies, IMySQLOptions options)
  : base(dependencies)
    {
      _options = options;
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override string EscapeIdentifier(string identifier)
        => Check.NotEmpty(identifier, nameof(identifier)).Replace("`", "``");

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override void EscapeIdentifier(StringBuilder builder, string identifier)
    {
      Check.NotEmpty(identifier, nameof(identifier));

      var initialLength = builder.Length;
      builder.Append(identifier);
      builder.Replace("`", "``", initialLength, identifier.Length);
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override void DelimitIdentifier(StringBuilder builder, string identifier)
    {
      Check.NotEmpty(identifier, nameof(identifier));

      builder.Append('`');
      EscapeIdentifier(builder, identifier);
      builder.Append('`');
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override string DelimitIdentifier(string identifier)
      => $"`{EscapeIdentifier(Check.NotEmpty(identifier, nameof(identifier)))}`";

    public override string DelimitIdentifier(string name, string schema)
               => base.DelimitIdentifier(GetObjectName(name, schema), GetSchemaName(name, schema));
    protected virtual string GetObjectName(string name, string schema)
    => !string.IsNullOrEmpty(schema) &&  _options.SchemaNameTranslator != null
        ? _options.SchemaNameTranslator(schema, name)
        : name;
    protected virtual string GetSchemaName(string name, string schema) => null;
  }
}
