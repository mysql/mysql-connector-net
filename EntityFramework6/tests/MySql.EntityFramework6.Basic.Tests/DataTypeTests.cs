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
using System.Linq;
using Xunit;
using MySql.Data.MySqlClient;
using System.Data;
using System.Globalization;
using System.Threading;

namespace MySql.Data.Entity.Tests
{
  public class DataTypeTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public DataTypeTests(DefaultFixture data)
    {
      st = data;
      st.Setup(this.GetType());
    }

    /// <summary>
    /// Bug #45457 DbType Time is not supported in entity framework
    /// </summary>
    [Fact]
    public void TimeType()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        TimeSpan birth = new TimeSpan(11, 3, 2);

        Child c = new Child();
        c.ChildId = "ABC";
        c.Name = "first";
        c.BirthTime = birth;
        c.Label = Guid.NewGuid();
        ctx.Children.Add(c);
        ctx.SaveChanges();

        Child d = ctx.Children.Where(x => x.ChildId == "ABC").Single();
        Assert.Equal(birth, d.BirthTime);

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
        using (DefaultContext ctx = st.GetDefaultContext())
        {
          Product p = new Product();
          p.Name = "New Product";
          p.Weight = 8.65f;
          p.CreatedDate = DateTime.Now;
          ctx.Products.Add(p);
          ctx.SaveChanges();
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
    [Fact(Skip ="Fix Me")]
    public void TimestampColumn()
    {
      DateTime now = DateTime.Now;

      using (DefaultContext ctx = st.GetDefaultContext())
      {
        Product p = new Product() { Name = "My Product", MinAge = 7, Weight = 8.0f };
        ctx.Products.Add(p);
        ctx.SaveChanges();

        p = ctx.Products.First();
        p.CreatedDate = now;
        ctx.SaveChanges();

        p = ctx.Products.First();
        Assert.Equal(now, p.CreatedDate);
      }
    }

    /// <summary>
    /// Bug #48417	Invalid cast from 'System.String' to 'System.Guid'
    /// </summary>
    [Fact]
    public void GuidType()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        TimeSpan birth = new TimeSpan(11, 3, 2);
        Guid g = Guid.NewGuid();
        
        Child c = new Child();
        c.ChildId = "GUID";
        c.Name = "first";
        c.BirthTime = birth;
        c.Label = g;
        ctx.Children.Add(c);
        ctx.SaveChanges();

        Child d = ctx.Children.Where(x => x.ChildId == "GUID").Single();
        Assert.Equal(g, d.Label);

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
