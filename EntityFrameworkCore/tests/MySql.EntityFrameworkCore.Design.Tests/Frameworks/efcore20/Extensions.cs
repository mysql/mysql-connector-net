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
    public static string GetSchema(this DatabaseTable table)
    {
      return table.Schema;
    }

    public static string GetDataType(this DatabaseColumn column)
    {
      return column.StoreType;
    }

    public static int? GetPrimaryKeyOrdinal(this DatabaseColumn column, int? ordinal)
    {
      return ordinal;
    }

    public static int? GetOrdinal(this DatabaseColumn column, int ordinal)
    {
      return ordinal;
    }

    public static int? GetMaxLength(this DatabaseColumn column, int? maxlength)
    {
      return maxlength;
    }

    public static int? GetPrecision(this DatabaseColumn column, int? precision)
    {
      return precision;
    }

    public static int? GetScale(this DatabaseColumn column, int? scale)
    {
      return scale;
    }

    public static string GetDefaultValue(this DatabaseColumn column)
    {
      return column.DefaultValueSql;
    }

    public static DatabaseColumn GetColumn(this DatabaseForeignKey fk)
    {
      return fk.Columns.Single();
    }

    public static DatabaseColumn GetPrincipalColumn(this DatabaseForeignKey fk)
    {
      return fk.PrincipalColumns.Single();
    }

    public static DatabaseColumn GetColumn(this DatabaseIndex index)
    {
      return index.Columns.Single();
    }

    public static IList<DatabaseColumn> GetColumns(this DatabaseIndex index)
    {
      return index.Columns;
    }

    public static DatabaseColumn GetColumn(this DatabaseColumn column)
    {
      return column;
    }
  }
}
