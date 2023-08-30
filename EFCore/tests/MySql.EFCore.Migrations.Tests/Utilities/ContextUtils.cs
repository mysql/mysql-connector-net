// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Extensions;
using System;

namespace MySql.EntityFrameworkCore.Migrations.Tests.Utilities
{
  public class ContextUtils
  {
    protected ContextUtils()
    {
    }
    public static ContextUtils Instance { get; } = new ContextUtils();

    public DbContext CreateContext() => new DbContext(CreateOptions());

    public IServiceProvider CreateContextServices()
           => ((IInfrastructure<IServiceProvider>)CreateContext()).Instance;

    private IServiceProvider CreateServiceProvider()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL();

      return serviceCollection.BuildServiceProvider();

    }

    protected virtual void UseProviderOptions(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseMySQL(MySQLTestStore.BaseConnectionString);

    public DbContextOptions CreateOptions(IModel model)
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      UseProviderOptions(optionsBuilder.UseModel(model));

      return optionsBuilder.Options;
    }

    public DbContextOptions CreateOptions()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      UseProviderOptions(optionsBuilder);
      return optionsBuilder.Options;
    }

    private ModelBuilder CreateConventionBuilder()
    {
      var contextServices = CreateContextServices();
      var modelBuilder = new ModelBuilder(contextServices.GetRequiredService<IProviderConventionSetBuilder>().CreateConventionSet());
      contextServices.GetRequiredService<IModelRuntimeInitializer>().Initialize(modelBuilder.FinalizeModel());

      return modelBuilder;
    }

    public ModelBuilder CreateModelBuilder() => CreateConventionBuilder();

  }
}
