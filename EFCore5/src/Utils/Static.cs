// Copyright (c) 2020 Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.EntityFrameworkCore.Utils
{
  /// <summary>
  /// This class used to initialize and retrieve an array of bool values with an specific number of elements and values.
  /// </summary>
  internal static class Statics
  {
    internal static readonly bool[][] TrueArrays =
    {
            Array.Empty<bool>(),
            new[] { true },
            new[] { true, true },
            new[] { true, true, true },
            new[] { true, true, true, true },
            new[] { true, true, true, true, true },
            new[] { true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
            new[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
        };

    internal static readonly bool[][] FalseArrays =
    {
            Array.Empty<bool>(),
            new[] { false },
            new[] { false, false },
            new[] { false, false, false },
            new[] { false, false, false, false },
            new[] { false, false, false, false, false },
            new[] { false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
        };
    
    internal static IEnumerable<bool> GetTrueValues(int dimensions)
        => dimensions <= 16
            ? TrueArrays[dimensions]
            : Enumerable.Repeat(true, dimensions);

    internal static IEnumerable<bool> GetFalseValues(int dimensions)
        => dimensions <= 16
            ? FalseArrays[dimensions]
            : Enumerable.Repeat(true, dimensions);
  }
}
