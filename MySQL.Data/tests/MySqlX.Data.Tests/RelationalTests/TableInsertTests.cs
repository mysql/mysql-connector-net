// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableInsertTests : BaseTest
  {
    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");
    public TableInsertTests()
    {
    }

    [Test]
    public void InsertMultipleValues()
    {
      ExecuteSQL("CREATE TABLE test.test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var result = ExecuteInsertStatement(table.Insert("name", "age")
        .Values("Henry", "22")
        .Values("Patric", 30)
        );
      Assert.That(result.AffectedItemsCount, Is.EqualTo(2));

      var selectResult = ExecuteSelectStatement(table.Select());
      while (selectResult.Next()) ;
      Assert.That(selectResult.Rows.Count, Is.EqualTo(2));
      Assert.That(selectResult.Rows.ToArray()[0][0], Is.EqualTo("Henry"));
      Assert.That(selectResult.Rows.ToArray()[0][1], Is.EqualTo(22));
      Assert.That(selectResult.Rows.ToArray()[1][0], Is.EqualTo("Patric"));
      Assert.That(selectResult.Rows.ToArray()[1][1], Is.EqualTo(30));

      Assert.That(table.Count(), Is.EqualTo(2));
    }

    [Test]
    public void InsertExpressions()
    {
      ExecuteSQL("CREATE TABLE test.test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var result = ExecuteInsertStatement(table.Insert("name", "age").Values("MARK", "34"));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      var selectResult = ExecuteSelectStatement(table.Select());
      while (selectResult.Next()) ;
      Assert.That(selectResult.Rows, Has.One.Items);
      Assert.That(selectResult.Rows.ToArray()[0][0], Is.EqualTo("MARK"));
      Assert.That(selectResult.Rows.ToArray()[0][1], Is.EqualTo(34));
    }

    [Test]
    public void ReuseStatement()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var stmt = table.Insert("name", "age");
      var result = ExecuteInsertStatement(stmt.Values("MARK", "34"));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
      // error 5014 - Wrong number of fields in row being inserted
      Assert.That(Assert.Throws<MySqlException>(() => result = ExecuteInsertStatement(stmt.Values("George", 34, 1))).Code, Is.EqualTo(5014u));
      Assert.That(Assert.Throws<MySqlException>(() => ExecuteInsertStatement(stmt.Values("George", 34))).Code, Is.EqualTo(5014u));
      Assert.That(ExecuteSelectStatement(table.Select()).FetchAll(), Has.One.Items);
    }

    /// <summary>
    /// Bug#31692694 - TABLEINSERTSTATEMENT STRING SPECIAL CARAC VALUES OBJECT ARE UNCORRECTLY MANAGED
    /// </summary>
    [TestCase("-")]
    [TestCase("+")]
    [TestCase("*")]
    [TestCase("/")]
    [TestCase("\\")]
    [TestCase("=")]
    [TestCase("(")]
    [TestCase(")")]
    public void InsertWithExpressionsAlikeValues(string specialChar)
    {
      ExecuteSQL("CREATE TABLE test(ID INT, COLUMN_TEST VARCHAR(40))");
      string value = $"ImproperFieldNameBug {specialChar} Bug";

      Table table = testSchema.GetTable("test");
      var insertResult = table.Insert("ID", "COLUMN_TEST").Values("1", value).Execute();
      var selectResult = ExecuteSelectStatement(table.Select());
      while (selectResult.Next()) ;

      Assert.That(insertResult.AffectedItemsCount, Is.EqualTo(1));
      Assert.That(selectResult.Rows, Has.One.Items);
      Assert.That(selectResult.Rows.ToArray()[0][0].ToString(), Is.EqualTo("1").IgnoreCase);
      Assert.That(selectResult.Rows.ToArray()[0][1].ToString(), Is.EqualTo(value).IgnoreCase);
    }
  }
}
