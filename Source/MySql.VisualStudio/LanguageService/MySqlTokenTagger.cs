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
using Microsoft.VisualStudio.Text.Tagging;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents a provider of MySQL tags.
  /// </summary>
  internal sealed class MySqlTokenTagger : ITagger<MySqlTokenTag>
  {
    private Dictionary<string, object> keywords;
    private Dictionary<string, object> operators;
    private Tokenizer tokenizer = new Tokenizer(true);

    /// <summary>
    /// Initializes a new instance of MySqlTokenTagger.
    /// </summary>
    /// <param name="buffer">The <see cref="ITextBuffer"/>.</param>
    internal MySqlTokenTagger(ITextBuffer buffer)
    {
      this.tokenizer.ReturnComments = true;
      this.Initialize();
    }

    /// <summary>
    /// Occurs when tags change in response to a change in the text buffer.
    /// </summary>
    public event EventHandler<SnapshotSpanEventArgs> TagsChanged
    {
      add { }
      remove { }
    }

    /// <summary>
    /// Gets the tags found in the specified spans.
    /// </summary>
    /// <param name="spans">Spans to check for supported tags.</param>
    /// <returns>A <see cref="IEnumerable"/> containing the list of tags.</returns>
    public IEnumerable<ITagSpan<MySqlTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
      string token = null;
      int startIndex;
      ITextSnapshotLine containingLine = null;
      SnapshotSpan snapshotSpan;

      // Not efficient, but we need to make sure every single line is re-tagged
      // specially because of scrolling in the VS editor.
      tokenizer.BlockComment = false;
      foreach (SnapshotSpan span in spans)
      {
        foreach (var line in span.Snapshot.Lines)
        {
          containingLine = line.Start.GetContainingLine();

          tokenizer.Text = containingLine.GetText();
          token = tokenizer.NextToken();

          while (!string.IsNullOrWhiteSpace(token))
          {
            token = token.Trim();
            startIndex = containingLine.Start + tokenizer.StartIndex;

            snapshotSpan = new SnapshotSpan(span.Snapshot, new Span(startIndex, token.Length));
            if (snapshotSpan.IntersectsWith(span))
            {
              yield return new TagSpan<MySqlTokenTag>(snapshotSpan, new MySqlTokenTag(GetTokenType(token)));
            }

            tokenizer.BlockComment = (tokenizer.BlockComment && !token.EndsWith("*/")) ? true : false;

            token = tokenizer.NextToken();
          }
        }
      }
    }

    private void Initialize()
    {
      InitializeKeywords();
      InitializeOperators();
    }

    private void InitializeKeywords()
    {
      if (keywords != null) return;
      keywords = new Dictionary<string, object>();

      keywords.Add(":", null);
      keywords.Add("ACCESSIBLE", null);
      keywords.Add("ADD", null);
      keywords.Add("AFTER", null);
      keywords.Add("ALL", null);
      keywords.Add("ALTER", null);
      keywords.Add("ANALYZE", null);
      keywords.Add("AND", null);
      keywords.Add("AS", null);
      keywords.Add("ASC", null);
      keywords.Add("ASENSITIVE", null);
      keywords.Add("BEFORE", null);
      keywords.Add("BEGIN", null);
      keywords.Add("BETWEEN", null);
      keywords.Add("BIGINT", null);
      keywords.Add("BINARY", null);
      keywords.Add("BIT", null);
      keywords.Add("BLOB", null);
      keywords.Add("BOTH", null);
      keywords.Add("BY", null);
      keywords.Add("CALL", null);
      keywords.Add("CASCADE", null);
      keywords.Add("CASE", null);
      keywords.Add("CHANGE", null);
      keywords.Add("CHAR", null);
      keywords.Add("CHARACTER", null);
      keywords.Add("CHECK", null);
      keywords.Add("COLLATE", null);      
      keywords.Add("COLUMN", null);
      keywords.Add("CONDITION", null);
      keywords.Add("CONSTRAINT", null);
      keywords.Add("CONTINUE", null);
      keywords.Add("CONVERT", null);
      keywords.Add("COUNT", null);
      keywords.Add("CREATE", null);
      keywords.Add("CROSS", null);
      keywords.Add("CURRENT_DATE", null);
      keywords.Add("CURRENT_TIME", null);
      keywords.Add("CURRENT_TIMESTAMP", null);
      keywords.Add("CURRENT_USER", null);
      keywords.Add("CURSOR", null);
      keywords.Add("DATE", null);
      keywords.Add("DATETIME", null);
      keywords.Add("DECIMAL", null);
      keywords.Add("DECLARE", null);
      keywords.Add("DEFINER", null);
      keywords.Add("DESC", null);
      keywords.Add("DELETE", null);
      keywords.Add("DISTINCT", null);
      keywords.Add("DISTINCTROW", null);
      keywords.Add("DOUBLE", null);
      keywords.Add("DROP", null);
      keywords.Add("DUMPFILE", null);
      keywords.Add("EACH", null);
      keywords.Add("ELSE", null);
      keywords.Add("END", null);
      keywords.Add("ENUM", null);
      keywords.Add("FLOAT", null);
      keywords.Add("FOR", null);
      keywords.Add("FROM", null);
      keywords.Add("FUNCTION", null);
      keywords.Add("GROUP", null);
      keywords.Add("HAVING", null);
      keywords.Add("HIGH_PRIORITY", null);
      keywords.Add("KILL", null);
      keywords.Add("IF", null);
      keywords.Add("IN", null);
      keywords.Add("INNER", null);
      keywords.Add("INSERT", null);
      keywords.Add("INT", null);
      keywords.Add("INTEGER", null);
      keywords.Add("INTO", null);      
      keywords.Add("JOIN", null);
      keywords.Add("LEFT", null);
      keywords.Add("LIMIT", null);      
      keywords.Add("LOCK", null);
      keywords.Add("LONGBLOB", null);
      keywords.Add("LONGTEXT", null);
      keywords.Add("MEDIUMBLOB", null);
      keywords.Add("MEDIUMINT", null);
      keywords.Add("MEDIUMTEXT", null);
      keywords.Add("MODE", null);
      keywords.Add("NUMERIC", null);
      keywords.Add("OFFSET", null);
      keywords.Add("ON", null);
      keywords.Add("ORDER", null);
      keywords.Add("OUT", null);
      keywords.Add("OUTFILE", null);
      keywords.Add("PROCEDURE", null);
      keywords.Add("PROCESSLIST", null);
      keywords.Add("REAL", null);
      keywords.Add("RENAME", null);
      keywords.Add("REPLACE", null);
      keywords.Add("RETURN", null);
      keywords.Add("RETURNS", null);
      keywords.Add("RIGHT", null);
      keywords.Add("ROLLUP", null);
      keywords.Add("ROW", null);
      keywords.Add("SELECT", null);
      keywords.Add("SET", null);
      keywords.Add("SHARE", null);
      keywords.Add("SHOW", null);
      keywords.Add("SMALLINT", null);
      keywords.Add("SQL_BIG_RESULT", null);
      keywords.Add("SQL_BUFFER_RESULT", null);
      keywords.Add("SQL_CACHE", null);
      keywords.Add("SQL_CALC_FOUND_ROWS", null);
      keywords.Add("SQL_NO_CACHE", null);
      keywords.Add("SQL_SMALL_RESULT", null);
      keywords.Add("STATUS", null);
      keywords.Add("STRAIGHT_JOIN", null);
      keywords.Add("TABLE", null);
      keywords.Add("TEXT", null);
      keywords.Add("THEN", null);
      keywords.Add("TIME", null);
      keywords.Add("TIMESTAMP", null);
      keywords.Add("TINYBLOB", null);
      keywords.Add("TINYINT", null);
      keywords.Add("TINYTEXT", null);
      keywords.Add("TRIGGER", null);
      keywords.Add("TRUNCATE", null);
      keywords.Add("UPDATE", null);
      keywords.Add("VARBINARY", null);
      keywords.Add("VARCHAR", null);
      keywords.Add("VIEW", null);
      keywords.Add("WHEN", null);
      keywords.Add("WHERE", null);
      keywords.Add("WITH", null);

      // functions
      
      

      // trigger keywords
      
      
      
      
      
      
      

      // Other
      
      
      
      
      
      

      keywords.Add("", null);
      
      
      
      
      

      // update
      
      

      // delete
      
      
      /*
      DATABASE	:	'DATABASE';
DATABASES	:	'DATABASES';
DAY_HOUR	:	'DAY_HOUR';
DAY_MICROSECOND	:	'DAY_MICROSECOND';
DAY_MINUTE	:	'DAY_MINUTE';
DAY_SECOND	:	'DAY_SECOND';
DEC	:	'DEC';
//DECIMAL	:	'DECIMAL';		// datatype defined below 
DECLARE	:	'DECLARE';
DEFAULT	:	'DEFAULT';
DELAYED	:	'DELAYED';
DELETE	:	'DELETE';
DESC	:	'DESC';
DESCRIBE	:	'DESCRIBE';
DETERMINISTIC	:	'DETERMINISTIC';
DISTINCT	:	'DISTINCT';
DISTINCTROW	:	'DISTINCTROW';
DIV	:	'DIV';
//DOUBLE	:	'DOUBLE';		// datatype defined below 
DROP	:	'DROP';
DUAL	:	'DUAL';
EACH	:	'EACH';
ELSE	:	'ELSE';
ELSEIF	:	'ELSEIF';
ENCLOSED	:	'ENCLOSED';
ESCAPED	:	'ESCAPED';
EXISTS	:	'EXISTS';
EXIT	:	'EXIT';
EXPLAIN	:	'EXPLAIN';
FALSE	:	'FALSE';
FETCH	:	'FETCH';
//FLOAT	:	'FLOAT';		// datatype defined below 
FLOAT4	:	'FLOAT4';
FLOAT8	:	'FLOAT8';
FOR	:	'FOR';
FORCE	:	'FORCE';
FOREIGN	:	'FOREIGN';
FROM	:	'FROM';
FULLTEXT	:	'FULLTEXT';
GOTO	:	'GOTO';
GRANT	:	'GRANT';
GROUP	:	'GROUP';
HAVING	:	'HAVING';
HIGH_PRIORITY	:	'HIGH_PRIORITY';
HOUR_MICROSECOND	:	'HOUR_MICROSECOND';
HOUR_MINUTE	:	'HOUR_MINUTE';
HOUR_SECOND	:	'HOUR_SECOND';
IF	:	'IF';
IFNULL	:	'IFNULL';
IGNORE	:	'IGNORE';
IN	:	'IN';
INDEX	:	'INDEX';
INFILE	:	'INFILE';
INNER	:	'INNER';
INNODB  : 'INNODB';
INOUT	:	'INOUT';
INSENSITIVE	:	'INSENSITIVE';
//INSERT	:	'INSERT';	// reserved keyword and function below
//INT	:	'INT';		// datatype defined below 
INT1	:	'INT1';
INT2	:	'INT2';
INT3	:	'INT3';
INT4	:	'INT4';
INT8	:	'INT8';
//INTEGER	:	'INTEGER';		// datatype defined below 
//INTERVAL	:	'INTERVAL';		// reserved keyword and function below
INTO	:	'INTO';
IS	:	'IS';
ITERATE	:	'ITERATE';
JOIN	:	'JOIN';
KEY	:	'KEY';
KEYS	:	'KEYS';
KILL	:	'KILL';
LABEL	:	'LABEL';
LEADING	:	'LEADING';
LEAVE	:	'LEAVE';
//LEFT	:	'LEFT';	// reserved keyword and function below
LIKE	:	'LIKE';
LIMIT	:	'LIMIT';
LINEAR	:	'LINEAR';
LINES	:	'LINES';
LOAD	:	'LOAD';
LOCALTIME	:	'LOCALTIME';
LOCALTIMESTAMP	:	'LOCALTIMESTAMP';
LOCK	:	'LOCK';
LONG	:	'LONG';
//LONGBLOB	:	'LONGBLOB';		// datatype defined below 
//LONGTEXT	:	'LONGTEXT';		// datatype defined below 
LOOP	:	'LOOP';
LOW_PRIORITY	:	'LOW_PRIORITY';
MASTER_SSL_VERIFY_SERVER_CERT	:	'MASTER_SSL_VERIFY_SERVER_CERT';
MATCH	:	'MATCH';
//MEDIUMBLOB	:	'MEDIUMBLOB';		// datatype defined below 
//MEDIUMINT	:	'MEDIUMINT';		// datatype defined below 
//MEDIUMTEXT	:	'MEDIUMTEXT';		// datatype defined below 
MIDDLEINT	:	'MIDDLEINT';		// datatype defined below 
MINUTE_MICROSECOND	:	'MINUTE_MICROSECOND';
MINUTE_SECOND	:	'MINUTE_SECOND';
MOD	:	'MOD';
MYISAM : 'MYISAM';
MODIFIES	:	'MODIFIES';
NATURAL	:	'NATURAL';
NOT	:	'NOT';
NO_WRITE_TO_BINLOG	:	'NO_WRITE_TO_BINLOG';
NULL	:	'NULL';
NULLIF	:	'NULLIF';
//NUMERIC	:	'NUMERIC';		// datatype defined below 
ON	:	'ON';
OPTIMIZE	:	'OPTIMIZE';
OPTION	:	'OPTION';
OPTIONALLY	:	'OPTIONALLY';
OR	:	'OR';
ORDER	:	'ORDER';
OUT	:	'OUT';
OUTER	:	'OUTER';
OUTFILE	:	'OUTFILE';
PRECISION	:	'PRECISION';
PRIMARY	:	'PRIMARY';
PROCEDURE	:	'PROCEDURE';
PURGE	:	'PURGE';
RANGE	:	'RANGE';
READ	:	'READ';
READS	:	'READS';
READ_ONLY	:	'READ_ONLY';
READ_WRITE	:	'READ_WRITE';
//REAL	:	'REAL';		// datatype defined below 
REFERENCES	:	'REFERENCES';
REGEXP	:	'REGEXP';
RELEASE	:	'RELEASE';
RENAME	:	'RENAME';
REPEAT	:	'REPEAT';
REPLACE	:	'REPLACE';
REQUIRE	:	'REQUIRE';
RESTRICT	:	'RESTRICT';
RETURN	:	'RETURN';
REVOKE	:	'REVOKE';
//RIGHT	:	'RIGHT';	// reserved keyword and function below
RLIKE	:	'RLIKE';
SCHEDULER : 'SCHEDULER';
SCHEMA	:	'SCHEMA';
SCHEMAS	:	'SCHEMAS';
SECOND_MICROSECOND	:	'SECOND_MICROSECOND';
SELECT	:	'SELECT';
SENSITIVE	:	'SENSITIVE';
SEPARATOR	:	'SEPARATOR';
SET	:	'SET';
SHOW	:	'SHOW';
//SMALLINT	:	'SMALLINT';		// datatype defined below 
SPATIAL	:	'SPATIAL';
SPECIFIC	:	'SPECIFIC';
SQL	:	'SQL';
SQLEXCEPTION	:	'SQLEXCEPTION';
SQLSTATE	:	'SQLSTATE';
SQLWARNING	:	'SQLWARNING';
SQL_BIG_RESULT	:	'SQL_BIG_RESULT';
SQL_CALC_FOUND_ROWS	:	'SQL_CALC_FOUND_ROWS';
SQL_SMALL_RESULT	:	'SQL_SMALL_RESULT';
SSL	:	'SSL';
STARTING	:	'STARTING';
STRAIGHT_JOIN	:	'STRAIGHT_JOIN';
TABLE	:	'TABLE';
TERMINATED	:	'TERMINATED';
THEN	:	'THEN';
//TINYBLOB	:	'TINYBLOB';		// datatype defined below 
//TINYINT	:	'TINYINT';		// datatype defined below 
//TINYTEXT	:	'TINYTEXT';		// datatype defined below 
TO	:	'TO';
TRAILING	:	'TRAILING';
TRIGGER	:	'TRIGGER';
TRUE	:	'TRUE';
UNDO	:	'UNDO';
UNION	:	'UNION';
UNIQUE	:	'UNIQUE';
UNLOCK	:	'UNLOCK';
UNSIGNED	:	'UNSIGNED';
UPDATE	:	'UPDATE';
USAGE	:	'USAGE';
USE	:	'USE';
USING	:	'USING';
//UTC_DATE	:	'UTC_DATE';		// next three are functions defined below
//UTC_TIME	:	'UTC_TIME';
//UTC_TIMESTAMP	:	'UTC_TIMESTAMP';
VALUES	:	'VALUES';
//VARBINARY	:	'VARBINARY';		// datatype defined below 
//VARCHAR	:	'VARCHAR';		// datatype defined below 
VARCHARACTER	:	'VARCHARACTER';
VARYING	:	'VARYING';
WHEN	:	'WHEN';
WHERE	:	'WHERE';
WHILE	:	'WHILE';
WITH	:	'WITH';
WRITE	:	'WRITE';
XOR	:	'XOR';
YEAR_MONTH	:	'YEAR_MONTH';
ZEROFILL	:	'ZEROFILL';
      */

      // procedures and functions    

      

      
    }

    private void InitializeOperators()
    {
      if (operators != null) return;
      operators = new Dictionary<string, object>();

      // Logical
      operators.Add("AND", null);
      operators.Add("&&", null);
      operators.Add("&", null);
      operators.Add("NOT", null);
      operators.Add("!", null);
      operators.Add("||", null);
      operators.Add("|", null);
      operators.Add("OR", null);
      operators.Add("XOR", null);
      operators.Add("^", null);

      // Assignment
      operators.Add("=", null);
      operators.Add(":=", null);

      // Arithmetic
      operators.Add("+", null);
      operators.Add("-", null);
      operators.Add("*", null);
      operators.Add("/", null);
      operators.Add("DIV", null);
      operators.Add("%", null);

      // Comparison
      operators.Add("BETWEEN", null);
      operators.Add("<=>", null);
      operators.Add(">=", null);
      operators.Add(">", null);
      operators.Add("IS", null);
      operators.Add("IS NOT NULL", null);
      operators.Add("IS NULL", null);
      operators.Add("<=", null);
      operators.Add("<", null);
      operators.Add("LIKE", null);
      operators.Add("!=", null);
      operators.Add("<>", null);
      //operators.Add("%", null);
      operators.Add("SOUNDS LIKE", null);

      // Others
      operators.Add("BINARY", null);
      operators.Add("~", null);
      operators.Add("<<", null);
      operators.Add(">>", null);
      operators.Add("REGEXP", null);
      operators.Add("RLIKE", null);
    }

    private MySqlTokenType GetTokenType(string token)
    {
      if (tokenizer.LineComment) return MySqlTokenType.Comment;
      else if (tokenizer.BlockComment) return MySqlTokenType.Comment;
      else if (IsKeyword(token)) return MySqlTokenType.Keyword;
      else if (IsOperator(token)) return MySqlTokenType.Operator;
      else if (tokenizer.Quoted) return MySqlTokenType.Literal;
      return MySqlTokenType.Text;
    }

    private bool IsKeyword(string token)
    {
      return keywords.ContainsKey(token.ToUpperInvariant());
    }

    private bool IsOperator(string token)
    {
      return operators.ContainsKey(token.ToUpper());
    }
  }
}
