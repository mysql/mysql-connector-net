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
