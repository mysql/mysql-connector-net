// Copyright (c) 2008 MySQL AB, 2008-2009 Sun Microsystems, Inc.
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
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using MySql.Data.MySqlClient.Tests;
using System.Data.EntityClient;
using System.Data.Common;
using System.Data.Objects;
using MySql.Data.Entity.Tests.Properties;

namespace MySql.Data.Entity.Tests
{
  [TestFixture]
  public class UpdateTests : BaseEdmTest
  {
    [Test]
    public void UpdateAllRows()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM toys", conn);
      object count = cmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        foreach (Toy t in context.Toys)
          t.Name = "Top";
        context.SaveChanges();
      }

      cmd.CommandText = "SELECT COUNT(*) FROM Toys WHERE name='Top'";
      object newCount = cmd.ExecuteScalar();
      Assert.AreEqual(count, newCount);
    }

    /// <summary>
    /// Fix for "Connector/Net Generates Incorrect SELECT Clause after UPDATE" (MySql bug #62134, Oracle bug #13491689).
    /// </summary>
    [Test]
    public void UpdateSimple()
    {      
      using (testEntities context = new testEntities())
      {        
        MySqlTrace.Listeners.Clear();
        MySqlTrace.Switch.Level = SourceLevels.All;
        GenericListener listener = new GenericListener();
        MySqlTrace.Listeners.Add(listener);
        Product pc = null;
        try
        {
          pc = new Product();
          pc.Name= "Acme";
          context.AddToProducts(pc);
          context.SaveChanges();
          pc.Name = "Acme 2";
          context.SaveChanges();
        }
        finally
        {
#if CLR4
          context.Products.DeleteObject(pc);
#endif
        }
        // Check sql        
        Regex rx = new Regex(@"Query Opened: (?<item>UPDATE .*)", RegexOptions.Compiled | RegexOptions.Singleline);
        foreach( string s in listener.Strings )
        {
          Match m = rx.Match(s);
          if (m.Success)
          {
            CheckSql(m.Groups["item"].Value, SQLSyntax.UpdateWithSelect);
            Assert.Pass();
          }
        }
        Assert.Fail();
      }
    }
  }
}