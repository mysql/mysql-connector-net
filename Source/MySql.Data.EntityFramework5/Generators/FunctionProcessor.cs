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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
#if EF6
using System.Data.Entity.Core.Common.CommandTrees;
#else
using System.Data.Common.CommandTrees;
#endif


namespace MySql.Data.Entity
{
  class FunctionProcessor
  {
    private static readonly Dictionary<string, string> bitwiseFunctions =
        new Dictionary<string, string>();
    private static readonly Dictionary<string, string> dateFunctions =
        new Dictionary<string, string>();
    private static readonly Dictionary<string, string> stringFunctions =
        new Dictionary<string, string>();
    private static readonly Dictionary<string, string> mathFunctions =
        new Dictionary<string, string>();

    private static readonly Dictionary<string, string> geoFunctions =
        new Dictionary<string, string>();

    private SqlGenerator callingGenerator;

    static FunctionProcessor()
    {
      bitwiseFunctions.Add("BitwiseAnd", "&");
      bitwiseFunctions.Add("BitwiseNot", "!");
      bitwiseFunctions.Add("BitwiseOr", "|");
      bitwiseFunctions.Add("BitwiseXor", "^");

      dateFunctions.Add("CurrentDateTime", "NOW()");
      dateFunctions.Add("Year", "YEAR({0})");
      dateFunctions.Add("Month", "MONTH({0})");
      dateFunctions.Add("Day", "DAY({0})");
      dateFunctions.Add("Hour", "HOUR({0})");
      dateFunctions.Add("Minute", "MINUTE({0})");
      dateFunctions.Add("Second", "SECOND({0})");

      stringFunctions.Add("Concat", "CONCAT({0}, {1})");
      stringFunctions.Add("IndexOf", "LOCATE({0}, {1})");
      stringFunctions.Add("Left", "LEFT({0}, {1})");
      stringFunctions.Add("Length", "LENGTH({0})");
      stringFunctions.Add("LTrim", "LTRIM({0})");
      stringFunctions.Add("Replace", "REPLACE({0}, {1}, {2})");
      stringFunctions.Add("Reverse", "REVERSE({0})");
      stringFunctions.Add("Right", "RIGHT({0}, {1})");
      stringFunctions.Add("RTrim", "RTRIM({0})");
      stringFunctions.Add("Substring", "SUBSTR({0}, {1}, {2})");
      stringFunctions.Add("ToLower", "LOWER({0})");
      stringFunctions.Add("ToUpper", "UPPER({0})");
      stringFunctions.Add("Trim", "TRIM({0})");

      mathFunctions.Add("Abs", "ABS({0})");
      mathFunctions.Add("Ceiling", "CEILING({0})");
      mathFunctions.Add("Floor", "FLOOR({0})");
      mathFunctions.Add("Round", "ROUND");

      geoFunctions.Add("SpatialDimension", "Dimension({0})");
      geoFunctions.Add("SpatialEnvelope", "Envelope({0})");
      geoFunctions.Add("IsSimpleGeometry", "IsSimple({0})");
      geoFunctions.Add("SpatialTypeName", "GeometryType({0})");
      geoFunctions.Add("CoordinateSystemId", "SRID({0})");
      geoFunctions.Add("Point", "POINT({0}, {1})");
      geoFunctions.Add("XCoordinate", "X({0})");
      geoFunctions.Add("YCoordinate", "Y({0})");
      geoFunctions.Add("GeometryFromText", "GeomFromText({0})");
      geoFunctions.Add("SpatialContains", "MBRContains({0}, {1})");
      geoFunctions.Add("AsText", "AsText({0})");
      geoFunctions.Add("SpatialBuffer", "Buffer({0}, {1})");
      geoFunctions.Add("SpatialDifference", "Difference({0}, {1})");
      geoFunctions.Add("SpatialIntersection", "Intersection({0},{1})");
      geoFunctions.Add("Distance", "GLength(LineString(GEOMFROMWKB(ASBINARY({0})), GEOMFROMWKB(ASBINARY({1}))))");

    }

    public SqlFragment Generate(DbFunctionExpression e, SqlGenerator caller)
    {
      callingGenerator = caller;
      if (bitwiseFunctions.ContainsKey(e.Function.Name))
        return BitwiseFunction(e);
      else if (dateFunctions.ContainsKey(e.Function.Name))
        return GenericFunction(dateFunctions, e);
      else if (stringFunctions.ContainsKey(e.Function.Name))
        return GenericFunction(stringFunctions, e);
      else if (mathFunctions.ContainsKey(e.Function.Name))
        return GenericFunction(mathFunctions, e);
      else if (geoFunctions.ContainsKey(e.Function.Name))
        return GenericFunction(geoFunctions, e);
      else
        return UserDefinedFunction(e);
    }

    private SqlFragment UserDefinedFunction(DbFunctionExpression e)
    {
      FunctionFragment f = new FunctionFragment();
      f.Name = Metadata.TryGetValueMetadataProperty<string>(e.Function,
          "StoreFunctionNameAttribute");

      if (String.IsNullOrEmpty(f.Name))
        f.Name = e.Function.Name;

      f.Quoted = !Metadata.TryGetValueMetadataProperty<bool>(e.Function, "BuiltInAttribute");

      bool isFuncNiladic = Metadata.TryGetValueMetadataProperty<bool>(e.Function, "NiladicFunctionAttribute");
      if (isFuncNiladic && e.Arguments.Count > 0)
        throw new InvalidOperationException("Niladic functions cannot have parameters");

      ListFragment list = new ListFragment();
      string delimiter = "";
      foreach (DbExpression arg in e.Arguments)
      {
        if (delimiter.Length > 0)
          list.Append(new LiteralFragment(delimiter));
        list.Append(arg.Accept(callingGenerator));
        delimiter = ", ";
      }
      f.Argument = list;

      return f;
    }

    private SqlFragment BitwiseFunction(DbFunctionExpression e)
    {
      StringBuilder sql = new StringBuilder();

      int arg = 0;
      if (e.Arguments.Count > 1)
        sql.AppendFormat("({0})", e.Arguments[arg++].Accept(callingGenerator));

      sql.AppendFormat(" {0} ({1})", bitwiseFunctions[e.Function.Name],
          e.Arguments[arg].Accept(callingGenerator));
      return new LiteralFragment(sql.ToString());
    }

    private SqlFragment GenericFunction(Dictionary<string, string> funcs,
        DbFunctionExpression e)
    {
      SqlFragment[] frags = new SqlFragment[e.Arguments.Count];

      for (int i = 0; i < e.Arguments.Count; i++)
        frags[i] = e.Arguments[i].Accept(callingGenerator);

      string sql;

      switch (e.Function.Name)
      {
        case "Round":
          // Special handling for Round as it has more than one signature.
          sql = HandleFunctionRound(e);
          break;
        default:
          sql = String.Format(funcs[e.Function.Name], frags);
          break;
      }

      return new LiteralFragment(sql);
    }

    private string HandleFunctionRound(DbFunctionExpression e)
    {
      StringBuilder sqlBuilder = new StringBuilder();

      sqlBuilder.Append(mathFunctions[e.Function.Name]);
      sqlBuilder.Append("(");

      Debug.Assert(e.Arguments.Count <= 2, "Round should have at most 2 arguments");
      sqlBuilder.Append(e.Arguments[0].Accept(callingGenerator));
      sqlBuilder.Append(", ");

      if (e.Arguments.Count > 1)
      {
        sqlBuilder.Append(e.Arguments[1].Accept(callingGenerator));
      }
      else
      {
        sqlBuilder.Append("0");
      }

      sqlBuilder.Append(")");

      return sqlBuilder.ToString();
    }
  }
}
