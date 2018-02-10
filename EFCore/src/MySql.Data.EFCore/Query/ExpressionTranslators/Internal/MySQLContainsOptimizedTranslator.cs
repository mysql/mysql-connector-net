// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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


using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MySql.Data.EntityFrameworkCore.Query.ExpressionTranslators.Internal
{
    internal class MySQLContainsOptimizedTranslator : IMethodCallTranslator
    {
        
        private static readonly MethodInfo _methodInfo
            = typeof(string).GetRuntimeMethod(nameof(string.Contains), new[] { typeof(string) });

       private static readonly MethodInfo _concat
            = typeof(string).GetRuntimeMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });

        public virtual Expression Translate(MethodCallExpression methodCallExpression)
        {
            ThrowIf.Argument.IsNull(methodCallExpression, nameof(methodCallExpression));

            if (ReferenceEquals(methodCallExpression.Method, _methodInfo))
            {
                var argument = methodCallExpression.Arguments.Count == 1
                                   ? (methodCallExpression.Arguments[0] as ConstantExpression)?.Value as string
                                   : null;


                var sqlArguments = new List<Expression>();
                sqlArguments.Add(ConstantExpression.Constant("%"));
                if (argument != null) 
                    sqlArguments.Add(Expression.Constant(argument));
                else
                    sqlArguments.Add(methodCallExpression.Arguments[0]);

                sqlArguments.Add(ConstantExpression.Constant("%"));

                var concatFunctionExpression = new SqlFunctionExpression("concat", methodCallExpression.Type, sqlArguments);
                return new LikeExpression(                    
                    methodCallExpression.Object,
                    concatFunctionExpression);
            }
            return null;
        }
    }
}
