// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;
using Xunit;
using MySql.Data.MySqlClient;
using System.Data;
using System.Globalization;
using System.Threading;

namespace MySql.Data.Entity.Tests
{
  public class DataTypeTests : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

    /// <summary>
    /// Bug #45457 DbType Time is not supported in entity framework
    /// </summary>
    [Fact]
    public void TimeType()
    {
      using (testEntities context = new testEntities())
      {
        TimeSpan birth = new TimeSpan(11, 3, 2);

        Child c = new Child();
        c.Id = 20;
        c.EmployeeID = 1;
        c.FirstName = "first";
        c.LastName = "last";
        c.BirthTime = birth;
        c.Modified = DateTime.Now;
        context.AddToChildren(c);
        context.SaveChanges();

        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM EmployeeChildren WHERE id=20", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.Equal(birth, dt.Rows[0]["birthtime"]);
      }
    }

    /// <summary>
    /// Bug #45077	Insert problem with Connector/NET 6.0.3 and entity framework
    /// Bug #45175	Wrong SqlType for unsigned smallint when generating Entity Framework Model
    /// </summary>
    [Fact]
    public void UnsignedValues()
    {
      using (testEntities context = new testEntities())
      {
        var row = context.Children.First();
        context.Detach(row);
        context.Attach(row);
      }
    }

    /// <summary>
    /// Bug #44455	insert and update error with entity framework
    /// </summary>
    [Fact]
    public void DoubleValuesNonEnglish()
    {
      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo newCulture = new CultureInfo("da-DK");
      Thread.CurrentThread.CurrentCulture = newCulture;
      Thread.CurrentThread.CurrentUICulture = newCulture;

      try
      {
        using (testEntities context = new testEntities())
        {
          Child c = new Child();          
          c.EmployeeID = 1;
          c.FirstName = "Bam bam";
          c.LastName = "Rubble";
          c.BirthWeight = 8.65;
          c.Modified = DateTime.Now;
          context.AddToChildren(c);
          context.SaveChanges();
        }
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = curCulture;
        Thread.CurrentThread.CurrentUICulture = curUICulture;
      }
    }

    /// <summary>
    /// Bug #46311	TimeStamp table column Entity Framework issue.
    /// </summary>
    [Fact]
    public void TimestampColumn()
    {
      DateTime now = DateTime.Now;

      using (testEntities context = new testEntities())
      {
        Child c = context.Children.First();
        DateTime dt = c.Modified;
        c.Modified = now;
        context.SaveChanges();

        c = context.Children.First();
        dt = c.Modified;
        Assert.Equal(now, dt);
      }
    }

    /// <summary>
    /// Bug #48417	Invalid cast from 'System.String' to 'System.Guid'
    /// </summary>
    [Fact]
    public void GuidType()
    {
      using (testEntities context = new testEntities())
      {
        DataTypeTest dtt = context.DataTypeTests.First();
        string guidAsChar = dtt.idAsChar;
        Assert.Equal(0, String.Compare(guidAsChar, dtt.id.ToString(), true));
        Assert.Equal(0, String.Compare(guidAsChar, dtt.id2.ToString(), true));
      }
    }

    /// <summary>
    /// Bug #62246	Connector/Net Incorrectly Maps Decimal To AnsiString
    /// </summary>
    [Fact]
    public void CanSetDbTypeDecimalFromNewDecimalParameter()
    {
      MySqlParameter newDecimalParameter = new MySqlParameter
      {
        ParameterName = "TestNewDecimal",
        Size = 10,
        Scale = 2,
        MySqlDbType = MySqlDbType.NewDecimal,
        Value = 1111111.12,
        IsNullable = true
      };

      Assert.Equal(DbType.Decimal, newDecimalParameter.DbType);
    }
  }
}
