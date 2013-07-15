// Copyright © 2011-2013, Oracle and/or its affiliates. All rights reserved.
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
using Antlr.Runtime;
using MySql.Parser;


namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents a classifier that classifies text as bein part of the MySQL language.
  /// </summary>
  internal sealed class MySqlClassifier : IClassifier
  {
    private IDictionary<MySqlTokenType, IClassificationType> mySqlTypes;

    /// <summary>
    /// Initializes a new instance of a the MySqlClassifier class.
    /// </summary>
    internal MySqlClassifier(IClassificationTypeRegistryService registry)
    {
      BuildTypesList(registry);
    }

        /// <summary>
    /// This method scans the given SnapshotSpan for potential matches for this classification.
    /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
    /// </summary>
    /// <param name="trackingSpan">The span currently being classified</param>
    /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
      //create a list to hold the results
      List<ClassificationSpan> result = new List<ClassificationSpan>();
      string token = null;
      int startIndex;
      ITextSnapshotLine containingLine = null;
      SnapshotSpan snapshotSpan;
      Version version = LanguageServiceUtil.GetVersion();      

      foreach (var line in span.Snapshot.Lines)
      {
        containingLine = line.Start.GetContainingLine();
        string sql = containingLine.GetText();
        CommonTokenStream tokenStream = LanguageServiceUtil.GetTokenStream(sql, version);
        tokenStream.Fill();
        List<IToken> tokens = tokenStream.GetTokens();
        for (int i = 0; i < tokens.Count; i++)
        {
          IToken tok = tokens[i];
          if (tok.Type == MySQL51Lexer.EOF) continue;

          startIndex = containingLine.Start + tok.StartIndex;

          snapshotSpan = new SnapshotSpan(span.Snapshot, new Span(startIndex, tok.Text.Length));
          if (snapshotSpan.IntersectsWith(span))
          {
            result.Add(new ClassificationSpan(snapshotSpan, mySqlTypes[GetTokenType(tok)]));
          }
        }
      }      
      return result;
    }

#pragma warning disable 67
    // This event gets raised if a non-text change would affect the classification in some way,
    // for example typing /* would cause the classification to change in C# without directly
    // affecting the span.
    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

    private MySqlTokenType GetTokenType(IToken token)
    {
      return TokenMap[token.Type];
    }

    private static Dictionary<int, MySqlTokenType> TokenMap;

    static MySqlClassifier()
    {
      TokenMap = new Dictionary<int, MySqlTokenType>();

      TokenMap.Add(MySQL51Lexer.EOF, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.ACCESSIBLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ACTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ADD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ADDDATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AFTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AGAINST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AGGREGATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ALGORITHM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ALL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ALTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ANALYZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AND, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.ANY, MySqlTokenType.Operator);
      //TokenMap.Add(MySQL51Lexer.ARCHIVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ASC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ASCII, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ASENSITIVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ASSIGN, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.AT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AT1, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.AUTHORS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AUTOCOMMIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AUTOEXTEND_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AUTO_INCREMENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AVG, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.AVG_ROW_LENGTH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BACKUP, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.BDB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BEFORE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BEGIN, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.BERKELEYDB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BETWEEN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BIGINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BINARY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BINARY_VALUE, MySqlTokenType.Literal);
      TokenMap.Add(MySQL51Lexer.BINLOG, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BITWISE_AND, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.BITWISE_INVERSION, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.BITWISE_OR, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.BITWISE_XOR, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.BIT_AND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BIT_OR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BIT_XOR, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.BLACKHOLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BLOB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BLOCK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BOOL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BOOLEAN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BOTH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BTREE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.BYTE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CACHE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CALL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CASCADE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CASCADED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CASE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CAST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CATALOG_NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHAIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHANGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHANGED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHAR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHARACTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHARSET, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHECK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CHECKSUM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CIPHER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CLASS_ORIGIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CLIENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CLOSE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COALESCE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CODE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLLATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLLATION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLON, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLUMN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLUMNS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLUMN_FORMAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COLUMN_NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COMMA, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.COMMENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COMMENT_RULE, MySqlTokenType.Comment);
      TokenMap.Add(MySQL51Lexer.COMMIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COMMITTED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COMPACT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COMPLETION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COMPRESSED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONCURRENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONDITION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONNECTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONSISTENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONSTRAINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONSTRAINT_CATALOG, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONSTRAINT_NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONSTRAINT_SCHEMA, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONTAINS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONTEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONTINUE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONTRIBUTORS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CONVERT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COPY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.COUNT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CPU, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CREATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CROSS, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.CSV, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CUBE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURDATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURRENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURRENT_DATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURRENT_TIME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURRENT_TIMESTAMP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURRENT_USER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURSOR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURSOR_NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.CURTIME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.C_COMMENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DASHDASH_COMMENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATA, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATABASE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATABASES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATAFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATETIME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATE_ADD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATE_ADD_INTERVAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATE_SUB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DATE_SUB_INTERVAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DAY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DAY_HOUR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DAY_MICROSECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DAY_MINUTE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DAY_SECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DEALLOCATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DEC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DECIMAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DECLARE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DEFAULT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DEFINER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DELAYED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DELAY_KEY_WRITE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DELETE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DESC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DESCRIBE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DES_KEY_FILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DETERMINISTIC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DIAGNOSTICS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DIGIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DIRECTORY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DISABLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DISCARD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DISK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DISTINCT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DISTINCTROW, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DIV, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DIVISION, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.DO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DOT, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.DOUBLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DROP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DUAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DUMPFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DUPLICATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.DYNAMIC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EACH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ELSE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ELSEIF, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ENABLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ENCLOSED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.END, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ENDS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ENGINE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ENGINES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ENUM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EQUALS, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.ERRORS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ESCAPE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ESCAPED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ESCAPE_SEQUENCE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EVENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EVENTS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EVERY, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.EXAMPLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXCHANGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXCLUSIVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXECUTE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXISTS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXPANSION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXPIRE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXPLAIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXTENDED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXTENT_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.EXTRACT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FALSE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FAST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FAULTS, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.FEDERATED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FETCH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FIELDS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FIRST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FIXED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FLOAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FLOAT4, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FLOAT8, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FLUSH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FOR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FORCE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FOREIGN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FORMAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FOUND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FRAC_SECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FROM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FULL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FULLTEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.FUNCTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GEOMETRY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GEOMETRYCOLLECTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GET, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GET_FORMAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GLOBAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GOTO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GRANT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GRANTS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GREATER_THAN, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.GREATER_THAN_EQUAL, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.GROUP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.GROUP_CONCAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HANDLER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HASH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HAVING, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.HEAP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HELP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HEXA_VALUE, MySqlTokenType.Literal);
      TokenMap.Add(MySQL51Lexer.HIGH_PRIORITY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HOST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HOSTS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HOUR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HOUR_MICROSECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HOUR_MINUTE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.HOUR_SECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ID, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.IDENTIFIED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IF, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IFNULL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IGNORE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IGNORE_SERVER_IDS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IMPORT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INDEX, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INDEXES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INITIAL_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INNER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INNOBASE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INNODB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INOUT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INPLACE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INSENSITIVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INSERT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INSERT_METHOD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INSTALL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT1, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT2, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT3, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT4, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT8, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INTEGER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INTERVAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INTO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.INT_NUMBER, MySqlTokenType.Literal);
      TokenMap.Add(MySQL51Lexer.INVOKER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IO_THREAD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IPC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.IS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ISOLATION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ISSUER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ITERATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.JOIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.JSON, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.KEY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.KEYS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.KEY_BLOCK_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.KILL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LABEL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LANGUAGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LAST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LCURLY, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.LEADING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LEAVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LEAVES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LEFT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LEFT_SHIFT, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.LESS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LESS_THAN, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.LESS_THAN_EQUAL, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.LEVEL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LIKE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LIMIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LINEAR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LINES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LINESTRING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LIST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOAD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOCAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOCALTIME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOCALTIMESTAMP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOCK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOCKS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOGFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOGICAL_AND, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.LOGICAL_OR, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.LOGS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LONG, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LONGBLOB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LONGTEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOOP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LOW_PRIORITY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.LPAREN, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.MASTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_CONNECT_RETRY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_HOST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_LOG_FILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_LOG_POS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_PASSWORD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_PORT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SERVER_ID, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL_CA, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL_CAPATH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL_CERT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL_CIPHER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL_KEY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_SSL_VERIFY_SERVER_CERT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MASTER_USER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MATCH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAXVALUE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_CONNECTIONS_PER_HOUR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_QUERIES_PER_HOUR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_ROWS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_UPDATES_PER_HOUR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_USER_CONNECTIONS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MAX_VALUE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MEDIUM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MEDIUMBLOB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MEDIUMINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MEDIUMTEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MEMORY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MERGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MESSAGE_TEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MICROSECOND, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.MID, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MIDDLEINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MIGRATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MINUS, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.MINUS_MINUS_COMMENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MINUTE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MINUTE_MICROSECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MINUTE_SECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MIN_ROWS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MOD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MODE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MODIFIES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MODIFY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MODULO, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.MONTH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MULT, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.MULTILINESTRING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MULTIPOINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MULTIPOLYGON, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MUTEX, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.MYISAM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.MYSQL_ERRNO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NAMES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NATIONAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NATURAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NCHAR, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.NDB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NDBCLUSTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NEW, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NNUMBER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NODEGROUP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NONE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NOT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NOT_EQUAL, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.NOT_OP, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.NOW, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NO_WAIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NO_WRITE_TO_BINLOG, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NULL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NULLIF, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NULL_SAFE_NOT_EQUAL, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.NUMBER, MySqlTokenType.Literal);
      TokenMap.Add(MySQL51Lexer.NUMERIC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.NVARCHAR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OFFLINE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OFFSET, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OLD_PASSWORD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ON, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ONE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ONE_SHOT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ONLINE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ONLY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OPEN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OPTIMIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OPTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OPTIONALLY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OPTIONS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ORDER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OUT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OUTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OUTFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.OWNER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PACK_KEYS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PAGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PARSER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PARTIAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PARTITION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PARTITIONING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PARTITIONS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PASSWORD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PHASE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PLUGIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PLUGINS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PLUS, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.POINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.POLYGON, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PORT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.POSITION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.POUND_COMMENT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PRECISION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PREPARE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PRESERVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PREV, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PRIMARY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PRIVILEGES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PROCEDURE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PROCESS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PROCESSLIST, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PROFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PROFILES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PROXY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.PURGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.QUARTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.QUERY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.QUICK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RANGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RCURLY, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.READ, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.READS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.READ_ONLY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.READ_WRITE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REAL_ID, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REBUILD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RECOVER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REDOFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REDO_BUFFER_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REDUNDANT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REFERENCES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REGEXP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RELAY_LOG_FILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RELAY_LOG_POS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RELAY_THREAD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RELEASE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RELOAD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REMOVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RENAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REORGANIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REPAIR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REPEAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REPEATABLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REPLACE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REPLICATION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REQUIRE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RESET, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RESIGNAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RESOURCES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RESTORE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RESTRICT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RESUME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RETURN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RETURNED_SQLSTATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RETURNS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.REVOKE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RIGHT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RIGHT_SHIFT, MySqlTokenType.Operator);
      TokenMap.Add(MySQL51Lexer.RLIKE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROLLBACK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROLLUP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROUTINE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROW, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROWS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROW_COUNT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ROW_FORMAT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.RPAREN, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.RTREE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SAVEPOINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SCHEDULE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SCHEDULER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SCHEMA, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SCHEMAS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SCHEMA_NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SECOND_MICROSECOND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SECURITY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SELECT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SEMI, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.SENSITIVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SEPARATOR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SERIAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SERIALIZABLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SERVER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SESSION, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.SESSION_USER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SET, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SHARE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SHARED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SHOW, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SHUTDOWN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SIGNAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SIGNED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SIMPLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SIZE, MySqlTokenType.Literal);
      TokenMap.Add(MySQL51Lexer.SLAVE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SMALLINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SNAPSHOT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SOCKET, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SOME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SONAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SOUNDS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SOURCE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SPATIAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SPECIFIC, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQLEXCEPTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQLSTATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQLWARNING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_BIG_RESULT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_BUFFER_RESULT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_CACHE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_CALC_FOUND_ROWS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_NO_CACHE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_SMALL_RESULT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SQL_THREAD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SSL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.START, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STARTING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STARTS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STATUS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STDDEV, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STDDEV_POP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STDDEV_SAMP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STOP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STORAGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STRAIGHT_JOIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.STRING_LEX, MySqlTokenType.Literal);
      TokenMap.Add(MySQL51Lexer.STRING_KEYWORD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBCLASS_ORIGIN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBDATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBJECT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBPARTITION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBPARTITIONS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBSTR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUBSTRING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUPER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SUSPEND, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SWAPS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.SWITCHES, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.SYSDATE, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.SYSTEM_USER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TABLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TABLES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TABLESPACE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TABLE_NAME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TEMPORARY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TEMPTABLE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TERMINATED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.THAN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.THEN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TIME, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TIMESTAMP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TIMESTAMP_ADD, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TIMESTAMP_DIFF, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TINYBLOB, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TINYINT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TINYTEXT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRADITIONAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRAILING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRANSACTION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRANSACTIONAL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRIGGER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRIGGERS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRIM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRUE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TRUNCATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TYPE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.TYPES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UDF_RETURNS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNCOMMITTED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNDEFINED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNDO, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNDOFILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNDO_BUFFER_SIZE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNICODE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNINSTALL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNION, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNIQUE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNKNOWN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNLOCK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNSIGNED, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UNTIL, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UPDATE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UPGRADE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.USAGE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.USE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.USER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.USE_FRM, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.USING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.UTC_DATE, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.UTC_TIME, MySqlTokenType.Keyword);
      //TokenMap.Add(MySQL51Lexer.UTC_TIMESTAMP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VALUE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VALUES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VALUE_PLACEHOLDER, MySqlTokenType.Text);
      TokenMap.Add(MySQL51Lexer.VARBINARY, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VARCHAR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VARCHARACTER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VARIABLES, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VARIANCE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VARYING, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VAR_POP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VAR_SAMP, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.VIEW, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WAIT, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WARNINGS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WEEK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WHEN, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WHERE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WHILE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WITH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WORK, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WRAPPER, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WRITE, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.WS, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.X509, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.XA, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.XML, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.XOR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.YEAR, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.YEAR_MONTH, MySqlTokenType.Keyword);
      TokenMap.Add(MySQL51Lexer.ZEROFILL, MySqlTokenType.Keyword);
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
  }
}
