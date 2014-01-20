// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Configuration;
using Xunit;
using MySql.Data.MySqlClient.Tests;

namespace MySql.Data.Tests.Stress
{
  public class SetUpStressClass : SetUpClass, IDisposable
  {
    public SetUpStressClass()
    {
      Initialize();
      LoadBaseConfiguration();    
    }  
  }

  /// <summary>
  /// This Setup class changes the initialization semantic to be exeucuted once per unit test, instead of once per Fixture class.
  /// </summary>
  public class SetUpClassPerTestInit : SetUpStressClass
  {
    public SetUpClassPerTestInit()
    {
      // No initialization
    }

  }

  /// <summary>
  /// This is a companion of the previous class to allow customization before inialization code.
  /// </summary>
  public class SpecialFixtureWithCustomConnectionString : IUseFixture<SetUpClassPerTestInit>, IDisposable
  {
    protected SetUpClassPerTestInit st;

    public virtual void SetFixture(SetUpClassPerTestInit data)
    {
      st = data;
      st.OnGetConnectionStringInfo += new SetUpStressClass.GetConnectionStringInfoCallback(OnGetConnectionStringInfo);
      st.MyInit();
      //if (st.conn.State != ConnectionState.Open)
      //  st.conn.Open();
    }

    /// <summary>
    /// Override to provide special connect options like using pipes, compression, etc.
    /// </summary>
    /// <returns></returns>
    protected virtual string OnGetConnectionStringInfo()
    {
      return "protocol=sockets;";
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing) return;
      st.Dispose();
    }
  }

}
