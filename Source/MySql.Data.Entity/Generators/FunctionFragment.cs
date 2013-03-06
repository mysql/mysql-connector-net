// Copyright (C) 2008-2009 Sun Microsystems, Inc.
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
using System.Data.Common.CommandTrees;

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
            mathFunctions.Add("Round", "ROUND({0})");
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
            return null;
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

        private SqlFragment GenericFunction(Dictionary<string,string> funcs, 
            DbFunctionExpression e)
        {
            SqlFragment[] frags = new SqlFragment[e.Arguments.Count];

            for (int i=0; i < e.Arguments.Count; i++)
                frags[i] = e.Arguments[i].Accept(callingGenerator);

            string sql = String.Format(funcs[e.Function.Name], frags);
            return new LiteralFragment(sql);
        }
    }
}
