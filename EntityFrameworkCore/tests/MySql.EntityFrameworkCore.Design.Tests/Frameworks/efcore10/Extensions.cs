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

using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.EntityFrameworkCore.Design.Tests
{
  internal static class Extensions
  {
    public static string GetSchema(this TableModel table)
    {
      return table.SchemaName;
    }

    public static string GetDataType(this ColumnModel column)
    {
      return column.DataType;
    }

    public static int? GetPrimaryKeyOrdinal(this ColumnModel column, int? ordinal)
    {
      return column.PrimaryKeyOrdinal;
    }

    public static int GetOrdinal(this ColumnModel column, int ordinal)
    {
      return column.Ordinal;
    }

    public static int? GetMaxLength(this ColumnModel column, int? maxLength)
    {
      return column.MaxLength;
    }

    public static int? GetPrecision(this ColumnModel column, int? precision)
    {
      return column.Precision;
    }

    public static int? GetScale(this ColumnModel column, int? scale)
    {
      return column.Scale;
    }

    public static string GetDefaultValue(this ColumnModel column)
    {
      return column.DefaultValue;
    }

    public static ColumnModel GetColumn(this ForeignKeyModel fk)
    {
      return fk.Columns.Single().Column;
    }

    public static ColumnModel GetPrincipalColumn(this ForeignKeyModel fk)
    {
      return fk.Columns.Single().PrincipalColumn;
    }

    public static ColumnModel GetColumn(this IndexModel index)
    {
      return index.IndexColumns.Single().Column;
    }

    public static ICollection<IndexColumnModel> GetColumns(this IndexModel index)
    {
      return index.IndexColumns;
    }

    public static ColumnModel GetColumn(this IndexColumnModel indexColumn)
    {
      return indexColumn.Column;
    }
  }
}
