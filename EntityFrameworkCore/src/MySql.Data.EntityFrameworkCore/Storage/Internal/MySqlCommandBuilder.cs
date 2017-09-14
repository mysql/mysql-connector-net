// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
    internal class MySQLCommandBuilder : RelationalCommandBuilder, IInfrastructure<IndentedStringBuilder>
    {

        private readonly ISensitiveDataLogger _logger;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly IndentedStringBuilder _commandTextBuilder = new IndentedStringBuilder();

        public MySQLCommandBuilder([NotNull] ISensitiveDataLogger<RelationalCommandBuilderFactory> logger,
            [NotNull] DiagnosticSource diagnosticSource,
            [NotNull] IRelationalTypeMapper typeMapper) : base(logger, diagnosticSource, typeMapper)
        {
            _logger = logger;
            _diagnosticSource = diagnosticSource;
            ParameterBuilder = new RelationalParameterBuilder(typeMapper);
        }

        public override IRelationalParameterBuilder ParameterBuilder { get; }

        IndentedStringBuilder IInfrastructure<IndentedStringBuilder>.Instance
            => _commandTextBuilder;


        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override IRelationalCommand Build()
            => new MySQLRelationalCommand(
                _logger,
                _diagnosticSource,
                _commandTextBuilder.ToString(),
                ParameterBuilder.Parameters);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string ToString() => _commandTextBuilder.ToString();
    }
}
