
// Copyright © 2012, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antlr.Runtime;

namespace MySql.Parser
{
  /// <summary>
  /// Abstract superclass for MySQL Lexers, for now containing some common code, so it's not in the grammar.
  /// Author: kroepke
  /// </summary>
  public abstract class MySQLLexerBase : Lexer
  {
    bool nextTokenIsID = false;

    //private ErrorListener errorListener;

    /**
     * Check ahead in the input stream for a left paren to distinguish between built-in functions
     * and identifiers.
     * TODO: This is the place to support certain SQL modes.
     * @param proposedType the original token type for this input.
     * @return the new token type to emit a token with
     */
    public virtual int checkFunctionAsID(int proposedType)
    {
      return proposedType;
    }

    public int checkFunctionAsID(int proposedType, int alternativeProposedType)
    {
      return (input.LA(1) != '(') ? alternativeProposedType : proposedType;
    }

    /// <summary>
    /// This functions allows certain keywords to be used as identifiers in a version before they were recognized as keywords.
    /// </summary>
    /// <param name="version"></param>
    /// <param name="proposedType"></param>
    /// <param name="alternativeProposedType"></param>
    /// <returns></returns>
    public int checkIDperVersion(double version, int proposedType, int alternativeProposedType)
    {
      return (mysqlVersion >= version) ? proposedType : alternativeProposedType;
    }

    // holds values like 5.0, 5.1, 5.5, 5.6, etc.
    protected double mysqlVersion;

    public Version MySqlVersion
    {
      get { return new Version((int)mysqlVersion, (int)(mysqlVersion * 10 - (int)mysqlVersion * 10)); }
      set { mysqlVersion = (double)value.Major + (double)value.Minor / 10; }
    }

    public MySQLLexerBase() { }

    public MySQLLexerBase(ICharStream input, RecognizerSharedState state)
      : base(input, state)
    {

      //errorListener = new BaseErrorListener(this);
    }
  }

  public class MySQLLexer : MySQL51Lexer
  {
    public MySQLLexer() { }

    public MySQLLexer(ICharStream input)
      : base(input)
    {

      //errorListener = new BaseErrorListener(this);
    }

    public MySQLLexer(ICharStream input, RecognizerSharedState state)
      : base(input, state)
    {

      //errorListener = new BaseErrorListener(this);
    }

    public override int checkFunctionAsID(int proposedType)
    {
      return (input.LA(1) != '(') ? ID : proposedType;
    }
  }
}
