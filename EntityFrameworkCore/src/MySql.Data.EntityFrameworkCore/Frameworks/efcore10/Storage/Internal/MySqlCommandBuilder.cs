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
