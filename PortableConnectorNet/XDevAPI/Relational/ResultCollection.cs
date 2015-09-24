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
using MySql.Protocol;
using MySql.XDevAPI.Common;

namespace MySql.XDevAPI.Relational
{
  public class ResultCollection
  {
    ProtocolBase _protocol;
    int _position = -1;
    List<Result> _results = new List<Result>();
    Result _activeResult;

    public IEnumerable<Result> Result
    {
      get { return _results; }
    }

    public Result Next()
    {
      // if we are loading a table result we need to dump it
      if (_activeResult != null && _activeResult is TableResult)
        (_activeResult as TableResult).Dump();

      // move to the next result
      _position++;
      if (_position == _results.Count)
      {
        Result rs = _protocol.GetNextResult();
        if (rs == null) return null;

        _results.Add(rs);
      }
      _activeResult = _results[_position];
      return _activeResult;
    }
  }
}
