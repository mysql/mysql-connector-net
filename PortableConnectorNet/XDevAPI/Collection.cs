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
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public class Collection : DatabaseObject
  {
    internal Collection(BaseSession session, Schema schema, string name)
      : base(session, schema, name)
    {

    }

    public Collection Add(object item)
    {
      throw new NotImplementedException();
    }

    public void Run()
    {
      throw new NotImplementedException();
    }

    public Collection Bind(params object[] parameters)
    {
      throw new NotImplementedException();
    }

    public Collection Find(string condition)
    {
      throw new NotImplementedException();
    }

    public Collection Execute()
    {
      throw new NotImplementedException();
    }

    public Collection One()
    {
      throw new NotImplementedException();
    }

    public Collection Add()
    {
      throw new NotImplementedException();
    }

    public Collection Limit(int limit)
    {
      throw new NotImplementedException();
    }

    public object First()
    {
      throw new NotImplementedException();
    }

    public object Next()
    {
      throw new NotImplementedException();
    }

    public Collection As(string alias)
    {
      throw new NotImplementedException();
    }

    public Collection On(string condition)
    {
      throw new NotImplementedException();
    }

    public Collection Join(Collection collection)
    {
      throw new NotImplementedException();
    }

    public Collection Fields(params string[] fields)
    {
      throw new NotImplementedException();
    }

    public Collection Sort(string columns)
    {
      throw new NotImplementedException();
    }

    public Collection Join(Collection collection, string condition)
    {
      throw new NotImplementedException();
    }

    public Collection Find()
    {
      throw new NotImplementedException();
    }

    public async Task<Collection> FindAsync(string p)
    {
      throw new NotImplementedException();
    }

    public void OnCompleted(Action completedMethod)
    {
      throw new NotImplementedException();
    }

    public override bool ExistsInDatabase()
    {
      throw new NotImplementedException();
    }
  }
}
