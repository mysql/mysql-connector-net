// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace MySql.Data.MySqlClient
{
#if NET452
  [Editor("MySql.Data.MySqlClient.Design.DBParametersEditor,MySql.Design", typeof(System.Drawing.Design.UITypeEditor))]
#endif
  [ListBindable(true)]
  public sealed partial class MySqlParameterCollection : DbParameterCollection
  {
    /// <summary>
    /// Gets a value that indicates whether the <see cref="MySqlParameterCollection"/>
    /// has a fixed size. 
    /// </summary>
    public override bool IsFixedSize
    {
      get { return (_items as IList).IsFixedSize; }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="MySqlParameterCollection"/>
    /// is read-only. 
    /// </summary>
    public override bool IsReadOnly
    {
      get { return (_items as IList).IsReadOnly; }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="MySqlParameterCollection"/>
    /// is synchronized. 
    /// </summary>
    public override bool IsSynchronized
    {
      get { return (_items as IList).IsSynchronized; }
    }

  }
}
