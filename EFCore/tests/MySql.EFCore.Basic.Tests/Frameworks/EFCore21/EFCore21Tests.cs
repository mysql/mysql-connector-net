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

using Microsoft.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Tests;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class EFCore21Tests : IClassFixture<SakilaLiteFixture>
  {
    private SakilaLiteFixture fixture;

    public EFCore21Tests(SakilaLiteFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    public void TransactionScopeTest()
    {
      using (var context = new SakilaLiteUpdateContext())
      {
        context.InitContext(false);
      }

      using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
      {
        using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.GetContextConnectionString<SakilaLiteUpdateContext>()))
        {
          connection.Open();

          MySqlCommand command = connection.CreateCommand();
          command.CommandText = "DELETE FROM actor";
          command.ExecuteNonQuery();

          var options = new DbContextOptionsBuilder<SakilaLiteUpdateContext>()
            .UseMySQL(connection)
            .Options;

          using (TransactionScope innerScope = new TransactionScope(TransactionScopeOption.Required))
          {
            using (var context = new SakilaLiteUpdateContext(options))
            {
              context.Actor.Add(new Actor
              {
                FirstName = "PENELOPE",
                LastName = "GUINESS"
              });
              context.SaveChanges();
              innerScope.Complete();
            }
          }

          // Commit transaction if all commands succeed, transaction will auto-rollback
          // when disposed if either commands fails
          scope.Complete();
        }
      }
    }
  }
}
