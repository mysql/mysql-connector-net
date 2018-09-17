// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace MySql.Data.EntityFrameworkCore.Tests.DbContextClasses
{
  #region Contexts

  public partial class SakilaLiteContext : MyTestContext
  {
    // Database scalar function mapping
    [DbFunction]
    public static int FilmsByActorCount(short actorId)
    {
      throw new Exception();
    }

    partial void OnModelCreating20(ModelBuilder modelBuilder)
    {
      // Self-contained type configuration for code first
      modelBuilder.ApplyConfiguration<Customer>(new CustomerConfiguration());
    }
  }

  public class SakilaLiteOwnedTypesContext : SakilaLiteContext
  {
    protected override void SetCustomerEntity(ModelBuilder modelBuilder)
    {
      // configures Address as an owned Customer type
      base.SetCustomerEntity(modelBuilder);
      modelBuilder.Entity<Customer>()
        .OwnsOne(e => e.Address,
          owned =>
          {
            EntityTypeBuilder<SakilaAddress> entity = new EntityTypeBuilder<SakilaAddress>(((IInfrastructure<InternalEntityTypeBuilder>)owned).Instance);
            base.SetAddressEntity(entity);
          });
    }

    protected override void SetAddressEntity(ModelBuilder modelBuilder)
    {
      // configuration is set as owned entity
    }

    public override void PopulateData()
    {
      // Customer data
      this.Database.ExecuteSqlCommand(SakilaLiteData.CustomerData);
    }
  }

  #endregion

  #region Configurations

  class CustomerConfiguration : IEntityTypeConfiguration<Customer>
  {
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
      // defines a model-level query filter
      builder.HasQueryFilter(e => e.Active);
    }
  }

  #endregion
}
