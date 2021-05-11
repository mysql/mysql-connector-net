// Copyright (c) 2021, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace MySql.EntityFrameworkCore.Metadata.Conventions
{
  /// <summary>
  /// A convention that configures the default model <see cref="MySQLValueGenerationStrategy"/> as
  /// <see cref="MySQLValueGenerationStrategy.IdentityColumn"/>.
  /// </summary>
  internal class MySQLValueGenerationStrategyConvention : IModelInitializedConvention, IModelFinalizedConvention
  {
    /// <summary>
    /// Parameter object containing service dependencies.
    /// </summary>
    protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

    /// <summary>
    /// Creates a new instance of <see cref="MySQLValueGenerationStrategyConvention" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing dependencies for this convention. </param>
    /// <param name="relationalDependencies">  Parameter object containing relational dependencies for this convention. </param>
    public MySQLValueGenerationStrategyConvention(
        [NotNull] ProviderConventionSetBuilderDependencies dependencies,
        [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
    {
      Dependencies = dependencies;
    }

    public void ProcessModelFinalized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
    }

    public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
    }

    public IModel ProcessModelFinalized(IModel model)
    {
      return null;
    }
  }
}
