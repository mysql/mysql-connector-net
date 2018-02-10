// Copyright © 2004, 2017 Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.Data.Common;
using MySql.Data.Types;
using System.Data;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlDataReader : DbDataReader, IDataReader, IDataRecord
  {
    /// <summary>
    /// Returns a DataTable that describes the column metadata of the MySqlDataReader.
    /// </summary>
    /// <returns></returns>
    public override DataTable GetSchemaTable()
    {
      // Only Results from SQL SELECT Queries 
      // get a DataTable for schema of the result
      // otherwise, DataTable is null reference
      if (FieldCount == 0) return null;

      DataTable dataTableSchema = new DataTable("SchemaTable");

      dataTableSchema.Columns.Add("ColumnName", typeof(string));
      dataTableSchema.Columns.Add("ColumnOrdinal", typeof(int));
      dataTableSchema.Columns.Add("ColumnSize", typeof(int));
      dataTableSchema.Columns.Add("NumericPrecision", typeof(int));
      dataTableSchema.Columns.Add("NumericScale", typeof(int));
      dataTableSchema.Columns.Add("IsUnique", typeof(bool));
      dataTableSchema.Columns.Add("IsKey", typeof(bool));
      DataColumn dc = dataTableSchema.Columns["IsKey"];
      dc.AllowDBNull = true; // IsKey can have a DBNull
      dataTableSchema.Columns.Add("BaseCatalogName", typeof(string));
      dataTableSchema.Columns.Add("BaseColumnName", typeof(string));
      dataTableSchema.Columns.Add("BaseSchemaName", typeof(string));
      dataTableSchema.Columns.Add("BaseTableName", typeof(string));
      dataTableSchema.Columns.Add("DataType", typeof(Type));
      dataTableSchema.Columns.Add("AllowDBNull", typeof(bool));
      dataTableSchema.Columns.Add("ProviderType", typeof(int));
      dataTableSchema.Columns.Add("IsAliased", typeof(bool));
      dataTableSchema.Columns.Add("IsExpression", typeof(bool));
      dataTableSchema.Columns.Add("IsIdentity", typeof(bool));
      dataTableSchema.Columns.Add("IsAutoIncrement", typeof(bool));
      dataTableSchema.Columns.Add("IsRowVersion", typeof(bool));
      dataTableSchema.Columns.Add("IsHidden", typeof(bool));
      dataTableSchema.Columns.Add("IsLong", typeof(bool));
      dataTableSchema.Columns.Add("IsReadOnly", typeof(bool));

      int ord = 1;
      for (int i = 0; i < FieldCount; i++)
      {
        MySqlField f = ResultSet.Fields[i];
        DataRow r = dataTableSchema.NewRow();
        r["ColumnName"] = f.ColumnName;
        r["ColumnOrdinal"] = ord++;
        r["ColumnSize"] = f.IsTextField ? f.ColumnLength / f.MaxLength : f.ColumnLength;
        int prec = f.Precision;
        int pscale = f.Scale;
        if (prec != -1)
          r["NumericPrecision"] = (short)prec;
        if (pscale != -1)
          r["NumericScale"] = (short)pscale;
        r["DataType"] = GetFieldType(i);
        r["ProviderType"] = (int)f.Type;       
        r["IsLong"] = f.IsBlob && (f.ColumnLength > 255 || f.ColumnLength == -1);
        r["AllowDBNull"] = f.AllowsNull;
        r["IsReadOnly"] = false;
        r["IsRowVersion"] = false;
        r["IsUnique"] = false;
        r["IsKey"] = f.IsPrimaryKey;
        r["IsAutoIncrement"] = f.IsAutoIncrement;
        r["BaseSchemaName"] = f.DatabaseName;
        r["BaseCatalogName"] = null;
        r["BaseTableName"] = f.RealTableName;
        r["BaseColumnName"] = f.OriginalColumnName;

        dataTableSchema.Rows.Add(r);
      }

      return dataTableSchema;
    }

  }
}
