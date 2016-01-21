// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.Data.Entity.Query.Expressions;
using Microsoft.Data.Entity.Query.Sql;
using Microsoft.Data.Entity.Storage;

namespace MySQL.Data.Entity.Query
{
  public class MySQLQueryGeneratorFactory : ISqlQueryGeneratorFactory
  {
    private readonly IRelationalCommandBuilderFactory _relationalCommandBuilderFactory;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IParameterNameGeneratorFactory _parameterNameGeneratorFactory;
    private readonly ISqlCommandBuilder _sqlCommandBuilder;

    public MySQLQueryGeneratorFactory(IRelationalCommandBuilderFactory commandBuilderFactory,
            ISqlGenerator sqlGenerator,
            IParameterNameGeneratorFactory parameterNameGeneratorFactory,
            ISqlCommandBuilder sqlCommandBuilder)
    {
      ThrowIf.Argument.IsNull(commandBuilderFactory, nameof(commandBuilderFactory));
      ThrowIf.Argument.IsNull(sqlGenerator, nameof(sqlGenerator));
      ThrowIf.Argument.IsNull(parameterNameGeneratorFactory, nameof(parameterNameGeneratorFactory));

      _relationalCommandBuilderFactory = commandBuilderFactory;
      _sqlGenerator = sqlGenerator;
      _parameterNameGeneratorFactory = parameterNameGeneratorFactory;
      _sqlCommandBuilder = sqlCommandBuilder;
    }

    public ISqlQueryGenerator CreateGenerator(SelectExpression selectExpression)
    {
      ThrowIf.Argument.IsNull(selectExpression, nameof(selectExpression));
      return new MySQLQuerySqlGenerator(_relationalCommandBuilderFactory, _sqlGenerator, _parameterNameGeneratorFactory, selectExpression);
    }

    public ISqlQueryGenerator CreateRawCommandGenerator(SelectExpression selectExpression, string sql, object[] parameters)
    {
      throw new NotImplementedException();
    }
  }
}
