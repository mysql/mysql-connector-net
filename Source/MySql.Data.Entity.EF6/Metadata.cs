// Copyright © 2008, 2013,  Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
#if EF6
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Spatial;
#else
using System.Data.Metadata.Edm;
using System.Data.Common.CommandTrees;
#if NET_45_OR_GREATER
using System.Data.Spatial;
#endif
#endif


namespace MySql.Data.Entity
{
  class Metadata
  {
    public static string GetNumericLiteral(PrimitiveTypeKind type, object value)
    {
      switch (type)
      {
        case PrimitiveTypeKind.Byte:
        case PrimitiveTypeKind.Int16:
        case PrimitiveTypeKind.Int32:
        case PrimitiveTypeKind.Int64:
        case PrimitiveTypeKind.SByte:
          return value.ToString();
        case PrimitiveTypeKind.Double:
          return ((double)value).ToString("R", CultureInfo.InvariantCulture);
        case PrimitiveTypeKind.Single:
          return ((float)value).ToString("R", CultureInfo.InvariantCulture);
        case PrimitiveTypeKind.Decimal:
          return ((decimal)value).ToString(CultureInfo.InvariantCulture);
      }
      return null;
    }

    public static bool IsNumericType(TypeUsage typeUsage)
    {
      PrimitiveType pt = (PrimitiveType)typeUsage.EdmType;

      switch (pt.PrimitiveTypeKind)
      {
        case PrimitiveTypeKind.Byte:
        case PrimitiveTypeKind.Double:
        case PrimitiveTypeKind.Single:
        case PrimitiveTypeKind.Int16:
        case PrimitiveTypeKind.Int32:
        case PrimitiveTypeKind.Int64:
        case PrimitiveTypeKind.SByte:
          return true;
        default:
          return false;
      }
    }

    public static DbType GetDbType(TypeUsage typeUsage)
    {
      PrimitiveType pt = (PrimitiveType)typeUsage.EdmType;

      switch (pt.PrimitiveTypeKind)
      {
#if NET_45_OR_GREATER          
        case PrimitiveTypeKind.Geometry: return DbType.Object;
#endif
        case PrimitiveTypeKind.Binary: return DbType.Binary;
        case PrimitiveTypeKind.Boolean: return DbType.Boolean;
        case PrimitiveTypeKind.Byte: return DbType.Byte;
        case PrimitiveTypeKind.DateTime: return DbType.DateTime;
        case PrimitiveTypeKind.DateTimeOffset: return DbType.DateTime;
        case PrimitiveTypeKind.Decimal: return DbType.Decimal;
        case PrimitiveTypeKind.Double: return DbType.Double;
        case PrimitiveTypeKind.Single: return DbType.Single;
        case PrimitiveTypeKind.Guid: return DbType.Guid;
        case PrimitiveTypeKind.Int16: return DbType.Int16;
        case PrimitiveTypeKind.Int32: return DbType.Int32;
        case PrimitiveTypeKind.Int64: return DbType.Int64;
        case PrimitiveTypeKind.SByte: return DbType.SByte;
        case PrimitiveTypeKind.String: return DbType.String;
        case PrimitiveTypeKind.Time: return DbType.Time;
        //                case PrimitiveTypeKind.UInt16: return DbType.UInt16;
        //                case PrimitiveTypeKind.UInt32: return DbType.UInt32;
        //                case PrimitiveTypeKind.UInt64: return DbType.UInt64;
        default:
          throw new InvalidOperationException(
              string.Format("Unknown PrimitiveTypeKind {0}", pt.PrimitiveTypeKind));
      }
    }


    public static object NormalizeValue(TypeUsage type, object value)
    {
      PrimitiveType pt = (PrimitiveType)type.EdmType;
#if NET_45_OR_GREATER
      if (value != null &&
               value != DBNull.Value &&
               Type.GetTypeCode(value.GetType()) == TypeCode.Object)
      {
        DbGeometry geometryValue = value as DbGeometry;
        if (geometryValue != null)
        {
          Byte[] geometryValBinary = new Byte[25];
          var buffer = geometryValue.AsBinary();
          var srid = geometryValue.WellKnownValue.CoordinateSystemId;
          for (int i = 0; i < buffer.Length; i++)
          {
            if (i < 4)
            {
              geometryValBinary[i] = (byte)(srid & 0xff);
              srid >>= 8;
            }            
            geometryValBinary[i + 4] = buffer[i];
          }
          return geometryValBinary;
        }
      }
#endif
      if (pt.PrimitiveTypeKind != PrimitiveTypeKind.DateTimeOffset) return value;
      DateTimeOffset dto = (DateTimeOffset)value;
      DateTime dt = dto.DateTime;
      if (dt.Year < 1970)
        return new DateTime(1970, 1, 1, 0, 0, 1);
      return dt;
    }

    public static ParameterDirection ModeToDirection(ParameterMode mode)
    {
      switch (mode)
      {
        case ParameterMode.In: return ParameterDirection.Input;
        case ParameterMode.Out: return ParameterDirection.Output;
        case ParameterMode.InOut: return ParameterDirection.InputOutput;
        default:
          Debug.Assert(mode == ParameterMode.ReturnValue);
          return ParameterDirection.ReturnValue;
      }
    }

    public static bool IsComparisonOperator(string op)
    {
      switch (op)
      {
        case "=":
        case "<":
        case ">":
        case "<=":
        case ">=":
        case "!=": return true;
        default: return false;
      }
    }

    public static string GetOperator(DbExpressionKind expressionKind)
    {
      switch (expressionKind)
      {
        case DbExpressionKind.Equals: return "=";
        case DbExpressionKind.LessThan: return "<";
        case DbExpressionKind.GreaterThan: return ">";
        case DbExpressionKind.LessThanOrEquals: return "<=";
        case DbExpressionKind.GreaterThanOrEquals: return ">=";
        case DbExpressionKind.NotEquals: return "!=";
        case DbExpressionKind.LeftOuterJoin: return "LEFT OUTER JOIN";
        case DbExpressionKind.InnerJoin: return "INNER JOIN";
        case DbExpressionKind.CrossJoin: return "CROSS JOIN";
        case DbExpressionKind.FullOuterJoin: return "OUTER JOIN";
      }
      throw new NotSupportedException("expression kind not supported");
    }

    internal static IList<EdmProperty> GetProperties(EdmType type)
    {
      if (type is EntityType)
        return ((EntityType)type).Properties;
      if (type is ComplexType)
        return ((ComplexType)type).Properties;
      if (type is RowType)
        return ((RowType)type).Properties;
      throw new NotSupportedException();
    }

    internal static T TryGetValueMetadataProperty<T>(MetadataItem mi, string name)
    {
      MetadataProperty property;
      bool exists = mi.MetadataProperties.TryGetValue(name, true, out property);
      if (exists) return (T)property.Value;
      return default(T);
    }
  }
}
