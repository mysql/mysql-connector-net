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


using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.Entity.CodeFirst.Tests
{
  public class ExpressionTests : IUseFixture<SetUpCodeFirstTests>, IDisposable
  {

    private SetUpCodeFirstTests st;

    public void SetFixture(SetUpCodeFirstTests data)
    {
      st = data;
    }

    public void Dispose()
    {
    }
    
    /// <summary>
    /// Using StartsWith on a list when using variable as parameter
    /// </summary>
    [Fact]
    public void CheckStartsWithWhenUsingVariable()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif

      using (SakilaDb db = new SakilaDb())
      {
        string str = "Astaire";          
        var records = db.actors.Where(e => e.last_name.StartsWith(str)).ToArray();
        Assert.Equal(1, records.Count());        
       }
     }    

    /// <summary>
    /// Using StartsWith on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckStartsWithWhenUsingValue()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      using (SakilaDb db = new SakilaDb())
      {
        var result = db.actors.Where(e => e.last_name.StartsWith("Astaire")).ToArray();
        Assert.Equal(1, result.Count());
      }
    }

    /// <summary>
    /// Using EndsWith on a list when using a variable as parameter
    /// </summary>
    [Fact]
    public void CheckEndsWithWhenUsingVariable()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif

      using (SakilaDb db = new SakilaDb())
      {   
        string str = "Astaire";
        var result = db.actors.Where(e => e.last_name.EndsWith(str)).ToArray();
        Assert.Equal(1, result.Count());        
      }
    }

    /// <summary>
    /// Using EndsWith on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckEndsWithWhenUsingValue()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      
      using (SakilaDb db = new SakilaDb())
      {           
        var result = db.actors.Where(e => e.last_name.EndsWith("Astaire")).ToArray(); 
        Assert.Equal(1, result.Count());        
      }
    }


    /// <summary>
    /// Using Contains on a list when using a variable
    /// </summary>
    [Fact]
    public void CheckContainsWhenUsingVariable()
    {

#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      string str = "stai";

      using (SakilaDb db = new SakilaDb())
      {        
        var result = db.actors.Where(e => e.last_name.Contains(str)).ToArray();
        Assert.Equal(1, result.Count());        
      }
    }


    /// <summary>
    /// Using Contains on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckContainsWhenUsingHardCodedValue()
    {
      using (SakilaDb db = new SakilaDb())
      {
          var result = db.actors.Where(e => e.last_name.Contains("stai")).ToArray();
          Assert.Equal(1, result.Count());
      }
    }

    /// <summary>
    /// Using Contains on a list when using a hardcoded value
    /// </summary>
    [Fact]
    public void CheckContainsWhenUsingHardCodedValueWithPorcentageSymbol()
    {
      using (SakilaDb db = new SakilaDb())
      {
        var result = db.actors.Where(e => e.last_name.Contains("%")).ToArray();
        Assert.Equal(0, result.Count());
      }
    }

  }
}
