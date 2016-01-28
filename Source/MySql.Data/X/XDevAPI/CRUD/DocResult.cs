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

using System.Collections.Generic;
using System.Diagnostics;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using MySqlX.Session;

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Represents the result of an operation the includes a collection of documents
  /// </summary>
  public sealed class DocResult : BufferingResult<DbDoc>
  {
    System.Text.Encoding _encoding = System.Text.Encoding.UTF8;

    internal DocResult(InternalSession session) : base(session)
    {
      // this is just a single column "doc"
      Debug.Assert(_columns.Count == 1);
    }

    protected override DbDoc ReadItem(bool dumping)
    {
      List<byte[]> values = Protocol.ReadRow(this);
      if (values == null) return null;

      Debug.Assert(values.Count == 1);
      return new DbDoc(_encoding.GetString(values[0]));
    }
  }
}
