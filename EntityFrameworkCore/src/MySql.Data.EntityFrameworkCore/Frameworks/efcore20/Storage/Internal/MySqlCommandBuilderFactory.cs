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


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLCommandBuilderFactory : RelationalCommandBuilderFactory
  {
    private readonly IDiagnosticsLogger<DbLoggerCategory.Database.Command> _logger;
    private readonly IRelationalTypeMapper _typeMapper;

    public MySQLCommandBuilderFactory(
        [NotNull] IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger,
        [NotNull] IRelationalTypeMapper typeMapper)
      : base(logger, typeMapper)
    {
      _logger = logger;
      _typeMapper = typeMapper;
    }

    protected override IRelationalCommandBuilder CreateCore(
      [NotNull] IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, 
      [NotNull] IRelationalTypeMapper relationalTypeMapper)
    {
      return new MySQLCommandBuilder(_logger, _typeMapper);
    }
  }
}
