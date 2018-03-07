// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using System;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class BasicSelectTests : BaseTest
  {
    [Fact]
    public void SimpleSelect()
    {
      CreateBooksTable();
      Table books = GetTable("test", "books");

      RowResult result = books.Select("name", "pages").Execute();
      var rows = result.FetchAll();
      Assert.True(result.Columns.Count == 2);
      Assert.True(rows.Count == 2);
    }

    [Fact]
    public void SimpleSelectWithWhere()
    {
      CreateBooksTable();
      Table books = GetTable("test", "books");

      RowResult result = books.Select("name", "pages").Where("pages > 250").Execute();
      var rows = result.FetchAll();
      Assert.True(result.Columns.Count == 2);
      Assert.True(rows.Count == 1);
    }

    private void CreateBooksTable()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test.books");
      ExecuteSQL("CREATE TABLE test.books(id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(100), pages INT)");
      ExecuteSQL("INSERT INTO test.books VALUES (NULL, 'Moby Dick', 500)");
      ExecuteSQL("INSERT INTO test.books VALUES (NULL, 'A Tale of Two Cities', 250)");
    }

    public enum LockMode { Exclusive, Shared }
    [Theory]
    [InlineData(LockContention.Default, LockMode.Exclusive)]
    [InlineData(LockContention.NoWait, LockMode.Exclusive)]
    [InlineData(LockContention.SkipLocked, LockMode.Exclusive)]
    [InlineData(LockContention.Default, LockMode.Shared)]
    [InlineData(LockContention.NoWait, LockMode.Shared)]
    [InlineData(LockContention.SkipLocked, LockMode.Shared)]
    public void LockExclusiveAndSharedWithWaitingOptions(LockContention lockOption, LockMode lockMode)
    {
      if (!session.XSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      CreateBooksTable();
      string tableName = "books";
      string schemaName = "test";

      // first session locks the row
      using (Session s1 = MySQLX.GetSession(ConnectionString))
      {
        var t1 = s1.GetSchema(schemaName).GetTable(tableName);
        s1.StartTransaction();
        RowResult r1 = t1.Select().Where("id = :id").Bind("id", 1).LockExclusive().Execute();
        var rows1 = r1.FetchAll();
        Assert.Equal(1, rows1.Count);
        Assert.Equal(1, rows1[0]["id"]);

        // second session tries to read the locked row
        using (Session s2 = MySQLX.GetSession(ConnectionString))
        {
          var t2 = s2.GetSchema(schemaName).GetTable(tableName);
          s2.SQL("SET innodb_lock_wait_timeout = 1").Execute();
          s2.StartTransaction();
          var stmt2 = t2.Select();
          if (lockMode == LockMode.Exclusive)
            stmt2.LockExclusive(lockOption);
          else
            stmt2.LockShared(lockOption);

          switch (lockOption)
          {
            case LockContention.Default:
              // error 1205 Lock wait timeout exceeded; try restarting transaction
              Assert.Equal(1205u, Assert.ThrowsAny<MySqlException>(() => stmt2.Execute().FetchAll()).Code);
              break;
            case LockContention.NoWait:
              // error 1205 Lock wait timeout exceeded; try restarting transaction
              uint expectedError = 1205; 
              if (session.XSession.GetServerVersion().isAtLeast(8, 0, 5))
                // error 3572 Statement aborted because lock(s) could not be acquired immediately and NOWAIT is set
                expectedError = 3572;
              Assert.Equal(expectedError, Assert.ThrowsAny<MySqlException>(() => stmt2.Execute().FetchAll()).Code);
              break;
            case LockContention.SkipLocked:
              if(!session.XSession.GetServerVersion().isAtLeast(8, 0, 5))
              {
                // error 1205 Lock wait timeout exceeded; try restarting transaction
                Assert.Equal(1205u, Assert.ThrowsAny<MySqlException>(() => stmt2.Execute().FetchAll()).Code);
                break;
              }
              var rows2 = stmt2.Execute().FetchAll();
              Assert.Equal(1, rows2.Count);
              Assert.Equal(2, rows2[0]["id"]);
              break;
            default:
              throw new NotImplementedException(lockOption.ToString());
          }
        }
        // first session frees the lock
        s1.Commit();
      }
    }
  }
}
