﻿// Copyright © 2015, 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.EntityFrameworkCore.Query
{
  internal partial class MySQLQuerySqlGenerator : DefaultQuerySqlGenerator
  {
    protected override string TypedFalseLiteral
    {
      get
      {
        return "('0')";
      }
    }

    protected override string TypedTrueLiteral
    {
      get
      {
        return "('1')";
      }
    }


    protected override void GenerateTop([NotNull]SelectExpression selectExpression)
    {
      //Nothing to do
    }       

    protected override void GenerateLimitOffset([NotNull] SelectExpression selectExpression)
    {

      ThrowIf.Argument.IsNull(selectExpression, "selectExpression");

            if ((selectExpression.Limit != null)
                     || (selectExpression.Offset != null))
            {
                Sql.AppendLine()
                    .Append("LIMIT ");

                Visit(selectExpression.Limit ?? Expression.Constant(-1));
                
                if (selectExpression.Offset != null)
                {
                    Sql.Append(" OFFSET ");

                    Visit(selectExpression.Offset);
                }
            }
        }
  }
}
