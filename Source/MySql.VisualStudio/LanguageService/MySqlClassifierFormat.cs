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

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents an editor format for the MySqlComment type.
  /// </summary>
  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MySqlComment")]
  [Name("MySqlComment")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MySqlComment : ClassificationFormatDefinition
  {
    /// <summary>
    /// Initializes a new instance of the MySqlComment class.
    /// </summary>
    public MySqlComment()
    {
      this.DisplayName = "Comment";
      this.ForegroundColor = Colors.Green;
    }
  }

  /// <summary>
  /// Represents an editor format for the MySqlLineComment type.
  /// </summary>
  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MySqlLineComment")]
  [Name("MySqlLineComment")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MySqlLineComment : ClassificationFormatDefinition
  {
    /// <summary>
    /// Initializes a new instance of the MySqlLineComment class.
    /// </summary>
    public MySqlLineComment()
    {
      this.DisplayName = "Line Comment";
      this.ForegroundColor = Colors.Green;
    }
  }

  /// <summary>
  /// Represents an editor format for the MySqlLiteral type.
  /// </summary>
  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MySqlLiteral")]
  [Name("MySqlLiteral")]
  [UserVisible(true)] //this should be visible to the end user
  [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
  internal sealed class MySqlLiteral : ClassificationFormatDefinition
  {
    /// <summary>
    /// Initializes a new instance of the MySqlLineComment class.
    /// </summary>
    public MySqlLiteral()
    {
      this.DisplayName = "Literal";
      this.ForegroundColor = Colors.Brown;
    }
  }

  /// <summary>
  /// Represents an editor format for the MySqlKeyword type.
  /// </summary>
  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MySqlKeyword")]
  [Name("MySqlKeyword")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MySqlKeyword : ClassificationFormatDefinition
  {
    /// <summary>
    /// Initializes a new instance of the MySqlLineComment class.
    /// </summary>
    public MySqlKeyword()
    {
      this.DisplayName = "Keyword";
      this.ForegroundColor = Colors.Blue;
    }
  }

  /// <summary>
  /// Represents an editor format for the MySqlOperator type.
  /// </summary>
  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MySqlOperator")]
  [Name("MySqlOperator")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MySqlOperator : ClassificationFormatDefinition
  {
    /// <summary>
    /// Initializes a new instance of the MySqlOperator class.
    /// </summary>
    public MySqlOperator()
    {
      this.DisplayName = "Operator";
      this.ForegroundColor = Colors.Gray;
    }
  }

  /// <summary>
  /// Represents an editor format for the MySqlText type.
  /// </summary>
  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MySqlText")]
  [Name("MySqlText")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MySqlText : ClassificationFormatDefinition
  {
    /// <summary>
    /// Initializes a new instance of the MySqlText class.
    /// </summary>
    public MySqlText()
    {
      this.DisplayName = "Text";
      this.ForegroundColor = Colors.Black;
    }
  }
}
