// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Session;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Represents a general statement result
  /// </summary>
  public class Result : BaseResult
  {
    internal Result(InternalSession session) : base(session)
    {
      session.GetProtocol().CloseResult(this);
    }

    /// <summary>
    /// The number of records affected by the statement that generated this result.
    /// </summary>
    public UInt64 RecordsAffected
    {
      get { return _recordsAffected; }
    }

    /// <summary>
    /// The last inserted id (if there is one) by the statement that generated this result.
    /// </summary>
    public UInt64 AutoIncrementValue
    {
      get { return _autoIncrementValue;  }
    }

    /// <summary>
    /// Returns the document generated Id.
    /// For multiple document Ids use DocumentIds.
    /// </summary>
    public string DocumentId
    {
      get
      {
        if (DocumentIds == null || DocumentIds.Count == 0)
          return null;
        if (DocumentIds.Count > 1)
          throw new ArgumentOutOfRangeException(Properties.ResourcesX.MoreThanOneDocumentId);
        return DocumentIds[0];
      }
    }

    /// <summary>
    /// Returns a list of generated Ids in the order of the Add() calls
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
