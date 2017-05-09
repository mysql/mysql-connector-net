// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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


using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class ExpressionTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public ExpressionTests(DefaultFixture fixture)
    {
      st = fixture;
      if (st.Setup(this.GetType()))
        LoadData();
    }

    void LoadData()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        ctx.Products.Add(new Product() { Name = "Garbage Truck", MinAge = 8 });
        ctx.Products.Add(new Product() { Name = "Fire Truck", MinAge= 12 });
        ctx.Products.Add(new Product() { Name = "Hula Hoop", MinAge=18 });
        ctx.SaveChanges();
      }
    }

    /// <summary>
    /// Using StartsWith on a list when using variable as parameter
    /// </summary>
    [Fact]
    public void CheckStartsWithWhenUsingVariable()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        string str = "Garbage";
        var records = ctx.Products.Where(p => p.Name.StartsWith(str)).ToArray();
        Assert.Equal(1, records.Count());        
       }
     }    

    /// <summary>
    /// Using StartsWith on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckStartsWithWhenUsingValue()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var records = ctx.Products.Where(p => p.Name.StartsWith("Garbage")).ToArray();
        Assert.Equal(1, records.Count());
      }
    }

    /// <summary>
    /// Using EndsWith on a list when using a variable as parameter
    /// </summary>
    [Fact]
    public void CheckEndsWithWhenUsingVariable()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        string str = "Hoop";
        var records = ctx.Products.Where(p => p.Name.EndsWith(str)).ToArray();
        Assert.Equal(1, records.Count());
      }
    }

    /// <summary>
    /// Using EndsWith on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckEndsWithWhenUsingValue()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var records = ctx.Products.Where(p => p.Name.EndsWith("Hoop")).ToArray();
        Assert.Equal(1, records.Count());
      }
    }


    /// <summary>
    /// Using Contains on a list when using a variable
    /// </summary>
    [Fact]
    public void CheckContainsWhenUsingVariable()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        string str = "bage";
        var records = ctx.Products.Where(p => p.Name.Contains(str)).ToArray();
        Assert.Equal(1, records.Count());
      }
    }


    /// <summary>
    /// Using Contains on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckContainsWhenUsingHardCodedValue()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var records = ctx.Products.Where(p => p.Name.Contains("bage")).ToArray();
        Assert.Equal(1, records.Count());
      }
    }

    /// <summary>
    /// Using Contains on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckContainsWhenUsingHardCodedValueWithPercentageSymbol()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var records = ctx.Products.Where(p => p.Name.Contains("%")).ToArray();
        Assert.Equal(0, records.Count());
      }
    }

  }
}
