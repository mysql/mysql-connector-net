// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using System.Linq;

namespace MySQL.Data.Entity.Update
{
  public class MySQLModificationCommandBatchFactory : IModificationCommandBatchFactory
  {
    private readonly IRelationalCommandBuilderFactory _commandBuilderFactory;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly MySQLUpdateSqlGenerator _updateSqlGenerator;
    private readonly IRelationalValueBufferFactoryFactory _valueBufferFactoryFactory;
    private readonly IDbContextOptions _options;

    public MySQLModificationCommandBatchFactory(
        [NotNull] IRelationalCommandBuilderFactory commandBuilderFactory,
        [NotNull] ISqlGenerator sqlGenerator,
        [NotNull] MySQLUpdateSqlGenerator updateSqlGenerator,
        [NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
        [NotNull] IDbContextOptions options)
    {
      ThrowIf.Argument.IsNull(commandBuilderFactory, "commandBuilderFactory");
      ThrowIf.Argument.IsNull(updateSqlGenerator, "updateSqlGenerator");
      ThrowIf.Argument.IsNull(valueBufferFactoryFactory, "valueBufferFactoryFactory");
      ThrowIf.Argument.IsNull(options, "options");

      _commandBuilderFactory = commandBuilderFactory;
      _sqlGenerator = sqlGenerator;
      _updateSqlGenerator = updateSqlGenerator;
      _valueBufferFactoryFactory = valueBufferFactoryFactory;
      _options = options;
    }

    public virtual ModificationCommandBatch Create()
    {
      var optionsExtension = _options.Extensions.OfType<MySQLOptionsExtension>().FirstOrDefault();

      return new MySQLModificationCommandBatch(
          _commandBuilderFactory,
          _sqlGenerator,
          _updateSqlGenerator,
          _valueBufferFactoryFactory,
          optionsExtension?.MaxBatchSize);
    }
  }
}
