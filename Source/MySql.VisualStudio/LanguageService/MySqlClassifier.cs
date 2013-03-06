// Copyright © 2011, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents a classifier that classifies text as bein part of the MySQL language.
  /// </summary>
  internal sealed class MySqlClassifier : ITagger<ClassificationTag>
  {
    private ITextBuffer buffer;
    private ITagAggregator<MySqlTokenTag> aggregator;
    private IDictionary<MySqlTokenType, IClassificationType> mySqlTypes;

    /// <summary>
    /// Initializes a new instance of a the MySqlClassifier class.
    /// </summary>
    /// <param name="buffer">The <see cref="ITextBuffer"/> to associate with this instance.</param>
    /// <param name="mySqlTagAggregator">The <see cref="ITagAggregator"/> to associate with this instance.</param>
    /// <param name="typeService">The <see cref="IClassificationTypeRegistryService"/> used to get MySql types.</param>
    internal MySqlClassifier(ITextBuffer buffer, ITagAggregator<MySqlTokenTag> mySqlTagAggregator, IClassificationTypeRegistryService typeService)
    {
      this.buffer = buffer;
      this.aggregator = mySqlTagAggregator;
      BuildTypesList(typeService);
      this.buffer.Changed += new EventHandler<TextContentChangedEventArgs>(HandleBufferChanged);
    }

    /// <summary>
    /// Occurs when tags change in response to a change in the text buffer.
    /// </summary>
    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    /// <summary>
    /// Gets the tags found in the specified spans.
    /// </summary>
    /// <param name="spans">Spans to check for supported tags.</param>
    /// <returns>A <see cref="IEnumerable"/> containing the list of tags.</returns>
    public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
      foreach (var tagSpan in this.aggregator.GetTags(spans))
      {
        var tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);
        yield return new TagSpan<ClassificationTag>(tagSpans[0], new ClassificationTag(mySqlTypes[tagSpan.Tag.Type]));
      }
    }

    private void BuildTypesList(IClassificationTypeRegistryService typeService)
    {
      mySqlTypes = new Dictionary<MySqlTokenType, IClassificationType>();
      mySqlTypes[MySqlTokenType.Comment] = typeService.GetClassificationType("MySqlComment");
      mySqlTypes[MySqlTokenType.Keyword] = typeService.GetClassificationType("MySqlKeyword");
      mySqlTypes[MySqlTokenType.Operator] = typeService.GetClassificationType("MySqlOperator");
      mySqlTypes[MySqlTokenType.Literal] = typeService.GetClassificationType("MySqlLiteral");
      mySqlTypes[MySqlTokenType.Text] = typeService.GetClassificationType("MySqlText");
    }

    private void HandleBufferChanged(object sender, TextContentChangedEventArgs e)
    {
      if (e.Changes.Count == 0)
        return;

      var tempHandler = TagsChanged;
      if (tempHandler == null)
        return;

      ITextSnapshot snapshot = e.After;
      SnapshotSpan totalAffectedSpan = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));

      tempHandler(this, new SnapshotSpanEventArgs(totalAffectedSpan));
    }
  }
}
