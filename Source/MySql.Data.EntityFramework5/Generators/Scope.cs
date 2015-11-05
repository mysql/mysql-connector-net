// Copyright © 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;

namespace MySql.Data.Entity
{
  class Scope
  {
    private Dictionary<string, InputFragment> scopeTable = new Dictionary<string, InputFragment>();

    public void Add(string name, InputFragment fragment)
    {
      scopeTable.Add(name, fragment);
    }

    public void Remove( string Name, InputFragment fragment)
    {
      if (fragment == null) return;
      if (Name != null)
        scopeTable.Remove(Name);

      if (fragment is SelectStatement)
        Remove((fragment as SelectStatement).From);
      else if (fragment is JoinFragment)
      {
        JoinFragment j = fragment as JoinFragment;
        Remove(j.Left);
        Remove(j.Right);
      }
      else if (fragment is UnionFragment)
      {
        UnionFragment u = fragment as UnionFragment;
        Remove(u.Left);
        Remove(u.Right);
      }
    }

    public void Remove(InputFragment fragment)
    {
      if( fragment == null ) return;
      Remove(fragment.Name, fragment);
    }

    public InputFragment GetFragment(string name)
    {
      if (!scopeTable.ContainsKey(name))
        return null;
      return scopeTable[name];
    }

    public InputFragment FindInputFromProperties(PropertyFragment fragment)
    {
      Debug.Assert(fragment != null);
      PropertyFragment propertyFragment = fragment as PropertyFragment;
      Debug.Assert(propertyFragment != null);

      if (propertyFragment.Properties.Count >= 2)
      {
        for (int x = propertyFragment.Properties.Count - 2; x >= 0; x--)
        {
          string reference = propertyFragment.Properties[x];
          if (reference == null) continue;
          InputFragment input = GetFragment(reference);
          if (input == null) continue;
          if (input.Scoped) return input;
          if (input is SelectStatement)
            return (input as SelectStatement).From;
          continue;
        }
      }
      Debug.Fail("Should have found an input");
      return null;
    }
  }

  public enum OpType : int
  {
    Join = 1,
    Union = 2
  }
}
