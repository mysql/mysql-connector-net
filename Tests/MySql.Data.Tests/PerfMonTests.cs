// Copyright © 2004, 2010, Oracle and/or its affiliates. All rights reserved.
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

#if !MONO

using System;
using System.Data;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Diagnostics;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for StoredProcedure.
  /// </summary>
  [TestFixture]
  public class PerfMonTests : BaseTest
  {
    public PerfMonTests()
    {
      csAdditions = ";use performance monitor=true;";
    }

    public override void Setup()
    {
      base.Setup();
      execSQL("CREATE TABLE Test (id INT, name VARCHAR(100))");
    }

    /// <summary>
    /// This test doesn't work from the CI setup currently
    /// </summary>
    [Test]
    public void ProcedureFromCache()
    {
      return;
      if (Version < new Version(5, 0)) return;

      execSQL("DROP PROCEDURE IF EXISTS spTest");
      execSQL("CREATE PROCEDURE spTest(id int) BEGIN END");

      PerformanceCounter hardQuery = new PerformanceCounter(
         ".NET Data Provider for MySQL", "HardProcedureQueries", true);
      PerformanceCounter softQuery = new PerformanceCounter(
         ".NET Data Provider for MySQL", "SoftProcedureQueries", true);
      long hardCount = hardQuery.RawValue;
      long softCount = softQuery.RawValue;

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.ExecuteScalar();

      Assert.AreEqual(hardCount + 1, hardQuery.RawValue);
      Assert.AreEqual(softCount, softQuery.RawValue);
      hardCount = hardQuery.RawValue;

      MySqlCommand cmd2 = new MySqlCommand("spTest", conn);
      cmd2.CommandType = CommandType.StoredProcedure;
      cmd2.Parameters.AddWithValue("?id", 1);
      cmd2.ExecuteScalar();

      Assert.AreEqual(hardCount, hardQuery.RawValue);
      Assert.AreEqual(softCount + 1, softQuery.RawValue);
    }

  }
}

#endif
