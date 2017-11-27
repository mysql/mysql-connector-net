// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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
    protected override string ConcatOperator
    {
      get { return String.Empty; }
    }

    public MySQLQuerySqlGenerator(
        [NotNull] IRelationalCommandBuilderFactory relationalCommandBuilderFactory,
        [NotNull] ISqlGenerationHelper sqlGenerationHelper,
        [NotNull] IParameterNameGeneratorFactory parameterNameGeneratorFactory,
        [NotNull] IRelationalTypeMapper relationalTypeMapper,
        [NotNull] SelectExpression selectExpression)
      : base(
          relationalCommandBuilderFactory,
          sqlGenerationHelper,
          parameterNameGeneratorFactory,
          relationalTypeMapper,
          selectExpression)
    {
    }
  }
}
