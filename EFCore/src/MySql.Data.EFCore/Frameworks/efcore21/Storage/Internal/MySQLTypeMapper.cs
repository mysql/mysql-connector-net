// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal partial class MySQLTypeMapper : RelationalTypeMappingSource
  {
    Dictionary<string, RelationalTypeMapping> extraMapping = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
    {
      { "timestamp", new MySQLDatetimeTypeMapping("timestamp", typeof(DateTime)) }
    };

    public IStringRelationalTypeMapper StringMapper { get; protected set; }
    public IByteArrayRelationalTypeMapper ByteArrayMapper { get; protected set; }

    protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
      var clrType = mappingInfo.ClrType;
      var storeTypeName = mappingInfo.StoreTypeName;
      var storeTypeNameBase = mappingInfo.StoreTypeNameBase?.Split(' ')[0];
      RelationalTypeMapping mapping = null;

      if (storeTypeName != null)
      {
        if (_storeTypeMappings.TryGetValue(storeTypeName, out mapping)
          || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mapping))
        {
          mapping = clrType == null
              || mapping.ClrType == clrType
            ? mapping
            : CheckExtraMapping(storeTypeName, storeTypeNameBase, clrType);
        }
      }
      else if (clrType != null)
      {
        if (_clrTypeMappings.TryGetValue(clrType, out mapping)) ;
        else if (clrType == typeof(string))
        {
          bool isAnsi = mappingInfo.IsUnicode == false;
          bool isFixedLength = mappingInfo.IsFixedLength == true;
          int? size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? (int?)_keyMaxLength : null);
          string baseName = size.HasValue ? (isFixedLength ? "char" : "varchar") : "text";


          mapping = new MySQLStringTypeMapping(
            $"{baseName}{(size.HasValue ? $"({size})" : string.Empty)}",
            isFixedLength ? DbType.StringFixedLength : DbType.String,
            !isAnsi,
            size);
        }
        else if (clrType == typeof(byte[]))
        {
          bool isFixedLength = mappingInfo.IsFixedLength == true;
          int size = mappingInfo.Size.HasValue ? mappingInfo.Size.Value : (isFixedLength ? CHAR_MAX_LENGTH : VARCHAR_MAX_LENGTH);
          mapping = new MySQLBinaryTypeMapping($"{(isFixedLength ? "binary" : "varbinary")}({size})", DbType.Binary, size, isFixedLength);
        }
      }

      return mapping?.Clone(mappingInfo);
    }

    private RelationalTypeMapping CheckExtraMapping(string storeTypeName, string storeTypeNameBase, Type clrType)
    {
      RelationalTypeMapping mapping = null;
      if(extraMapping.TryGetValue(storeTypeName, out mapping)
        || extraMapping.TryGetValue(storeTypeNameBase, out mapping))
      {
        if (mapping.ClrType != clrType)
          mapping = null;
      }

      return mapping;
    }
  }
}
