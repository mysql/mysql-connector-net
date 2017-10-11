// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Sessions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MySql.Data;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Represents a general statement result.
  /// </summary>
  public class Result : BaseResult
  {
    internal Result(InternalSession session) : base(session)
    {
      if (session == null) return;
      session.GetProtocol().CloseResult(this);
    }

    /// <summary>
    /// Gets the number of records affected by the statement that generated this result.
    /// </summary>
    public UInt64 RecordsAffected
    {
      get { return _recordsAffected; }
    }

    /// <summary>
    /// Gets the last inserted identifier (if there is one) by the statement that generated this result.
    /// </summary>
    public UInt64 AutoIncrementValue
    {
      get { return _autoIncrementValue;  }
    }

    /// <summary>
    /// Gets the generated identifier of the document.
    /// For multiple document identifiers use DocumentIds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">More than 1 document identifier was generated.</exception>
    public string DocumentId
    {
      get
      {
        if (DocumentIds == null || DocumentIds.Count == 0)
          return null;
        if (DocumentIds.Count > 1)
          throw new ArgumentOutOfRangeException(ResourcesX.MoreThanOneDocumentId);
        return DocumentIds[0];
      }
    }

    /// <summary>
    /// Gets the list of generated identifiers in the order of the Add() calls.
    /// </summary>
#if NET_45_OR_GREATER
    public IReadOnlyList<string> DocumentIds
#else
    public ReadOnlyCollection<string> DocumentIds
#endif
    {
      get; internal set;
    }
  }
}
