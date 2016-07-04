// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;

namespace MySQL.Data.Entity.Extensions
{
  public class MySQLDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<MySQLDbContextOptionsBuilder, MySQLOptionsExtension>
    {
        public MySQLDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
        }

    protected override MySQLOptionsExtension CloneExtension()
    {
      return new MySQLOptionsExtension(OptionsBuilder.Options.GetExtension<MySQLOptionsExtension>());
    }


  }
}
