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
  /// MySQL-specific extension methods for <see cref="IndexBuilder"/>.
  /// </summary>
  public static class MySqlIndexBuilderExtensions
  {
    #region FullText
    /// <summary>
    /// Sets a value indicating whether the index is full text.
    /// </summary>
    /// <param name="indexBuilder"> The index builder. </param>
    /// <param name="fullText"> The value to set. </param>
    /// <param name="parser"> An optional argument (for example, "ngram"), that will be used in an `WITH PARSER` clause. </param>
    /// <returns> The index builder. </returns>
    public static IndexBuilder IsFullText([NotNull] this IndexBuilder indexBuilder, bool fullText = true, string parser = null)
    {
      Check.NotNull(indexBuilder, nameof(indexBuilder));

      indexBuilder.Metadata.SetIsFullText(fullText);
      indexBuilder.Metadata.SetFullTextParser(parser);

      return indexBuilder;
    }

    /// <summary>
    /// Sets a value indicating whether the index is full text.
    /// </summary>
    /// <param name="indexBuilder"> The index builder. </param>
    /// <param name="fullText"> The value to set. </param>
    /// <param name="parser"> An optional argument (for example, "ngram") that will be used in an `WITH PARSER` clause. </param>
    /// <returns> The index builder. </returns>
    public static IndexBuilder<TEntity> IsFullText<TEntity>([NotNull] this IndexBuilder<TEntity> indexBuilder, bool fullText = true, string parser = null)
      => (IndexBuilder<TEntity>)((IndexBuilder)indexBuilder).IsFullText(fullText, parser);

    #endregion FullText

    #region Spatial

    /// <summary>
    /// Sets a value indicating whether the index is spartial.
    /// </summary>
    /// <param name="indexBuilder"> The index builder. </param>
    /// <param name="spatial"> The value to set. </param>
    /// <returns> The index builder. </returns>
    public static IndexBuilder IsSpatial([NotNull] this IndexBuilder indexBuilder, bool spatial = true)
    {
      Check.NotNull(indexBuilder, nameof(indexBuilder));

      indexBuilder.Metadata.SetIsSpatial(spatial);

      return indexBuilder;
    }

    /// <summary>
    /// Sets a value indicating whether the index is spartial.
    /// </summary>
    /// <param name="indexBuilder"> The index builder. </param>
    /// <param name="spatial"> The value to set. </param>
    /// <returns> The index builder. </returns>
    public static IndexBuilder<TEntity> IsSpatial<TEntity>([NotNull] this IndexBuilder<TEntity> indexBuilder, bool spatial = true)
      => (IndexBuilder<TEntity>)((IndexBuilder)indexBuilder).IsSpatial(spatial);

    #endregion Spatial

    #region PrefixLength

    /// <summary>
    /// Sets prefix lengths for the index.
    /// </summary>
    /// <param name="indexBuilder"> The index builder. </param>
    /// <param name="prefixLengths">The prefix lengths to set in the order of the index columns where specified.
    /// A value of `0` indicates, that the full length should be used for that column. </param>
    /// <returns> The index builder. </returns>
    public static IndexBuilder HasPrefixLength([NotNull] this IndexBuilder indexBuilder, params int[] prefixLengths)
    {
      Check.NotNull(indexBuilder, nameof(indexBuilder));

      indexBuilder.Metadata.SetPrefixLength(prefixLengths);

      return indexBuilder;
    }

    /// <summary>
    /// Sets prefix lengths for the index.
    /// </summary>
    /// <param name="indexBuilder"> The index builder. </param>
    /// <param name="prefixLengths">The prefix lengths to set in the order of the index columns where specified.
    /// A value of `0` indicates, that the full length should be used for that column. </param>
    /// <returns> The index builder. </returns>
    public static IndexBuilder<TEntity> HasPrefixLength<TEntity>([NotNull] this IndexBuilder<TEntity> indexBuilder, params int[] prefixLengths)
      => (IndexBuilder<TEntity>)HasPrefixLength((IndexBuilder)indexBuilder, prefixLengths);

    #endregion PrefixLength
  }
}
