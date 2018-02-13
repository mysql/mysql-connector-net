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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.EntityFrameworkCore.Tests;
using MySql.Data.EntityFrameworkCore;
using System;
using MySql.Data.EntityFrameworkCore.Extensions;

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

    protected virtual void UseProviderOptions(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseMySQL(MySQLTestStore.baseConnectionString);

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

      var conventionSetBuilder = contextServices.GetRequiredService<IConventionSetBuilder>();
       var conventionSet = contextServices.GetRequiredService<ICoreConventionSetBuilder>().CreateConventionSet();
      conventionSet = conventionSetBuilder == null
          ? conventionSet
          : conventionSetBuilder.AddConventions(conventionSet);
      return new ModelBuilder(conventionSet);
    }

    public ModelBuilder CreateModelBuilder() => CreateConventionBuilder();



    //private ModelBuilder CreateModelBuilder()
    //{      
    //  var contextServices = ((IInfrastructure<IServiceProvider>)CreateContext()).Instance;
    //  var conventionSetBuilder = contextServices.GetRequiredService<IDatabaseProviderServices>().ConventionSetBuilder;
    //  var conventionSet = contextServices.GetRequiredService<ICoreConventionSetBuilder>().CreateConventionSet();
    //  conventionSet = conventionSetBuilder == null
    //      ? conventionSet
    //      : conventionSetBuilder.AddConventions(conventionSet);
    //  return new ModelBuilder(conventionSet);
    //}

  }
}
