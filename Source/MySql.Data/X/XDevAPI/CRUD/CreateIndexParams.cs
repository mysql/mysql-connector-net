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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.CRUD
{
  internal class CreateIndexParams
  {
    private string indexName;
    private bool unique;
    private List<string> docPaths = new List<string>();
    private List<string> types = new List<string>();
    private List<bool> notNulls = new List<bool>();

    public CreateIndexParams(string indexName, bool unique)
    {
      this.indexName = indexName;
      this.unique = unique;
    }

    public void AddField(String docPath, String type, bool notNull)
    {
      docPaths.Add(docPath);
      types.Add(type);
      notNulls.Add(notNull);
    }

    public string IndexName
    {
      get { return this.indexName; }
    }

    public bool IsUnique
    {
      get { return this.unique; }
    }

    public List<String> DocPaths
    {
      get { return this.docPaths; }
    }

    public List<String> Types
    {
      get { return this.types; }
    }

    public List<Boolean> NotNulls
    {
      get { return this.notNulls; }
    }
  }
}
