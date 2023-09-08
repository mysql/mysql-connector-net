// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySql.EntityFrameworkCore.Utils;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  /// MySQL specific extension methods for <see cref="KeyBuilder"/>.
  /// </summary>
  public static class MySQLKeyBuilderExtensions
  {
    /// <summary>
    /// Sets prefix lengths for the key.
    /// </summary>
    /// <param name="keyBuilder"> The key builder. </param>
    /// <param name="prefixLengths">The prefix lengths to set in the order of the key columns where specified.
    /// A value of `0` indicates, that the full length should be used for that column. </param>
    /// <returns> The key builder. </returns>
    public static KeyBuilder HasPrefixLength([NotNull] this KeyBuilder keyBuilder, params int[] prefixLengths)
    {
      Check.NotNull(keyBuilder, nameof(keyBuilder));

      keyBuilder.Metadata.SetPrefixLength(prefixLengths);

      return keyBuilder;
    }
  }
}
