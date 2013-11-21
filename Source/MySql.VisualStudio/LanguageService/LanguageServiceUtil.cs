// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
#if CLR4
using System.Linq;
#endif
using System.IO;
using System.Text;
using MySql.Parser;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace MySql.Data.VisualStudio
{
  internal static class LanguageServiceUtil
  {
    private static Version _version;
    private static string _sql = "";

    // Cache of parsed text.
    // This cache is organized by input text then by server version (for each combination of the previous two,
    // returns a CommonTokenStream).
    private static Dictionary<string,  Dictionary<Version, TokenStreamRemovable>> _parserCache = 
      new Dictionary<string, Dictionary<Version, TokenStreamRemovable>>();
    
    private static TokenStreamRemovable _lastTokenizedStream = null;

    public static TokenStreamRemovable GetTokenStream(string sql, Version version)
    {
      Dictionary<Version, TokenStreamRemovable> lines = null;      
      if (_parserCache.TryGetValue(sql, out lines))
      {
        TokenStreamRemovable tsr = null;
        if( lines.TryGetValue( version, out tsr ) )
          return tsr;
      }
      // no cache entry, then parse
      // The grammar supports upper case only
      MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sql));//ASCIIEncoding.ASCII.GetBytes(sql));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      lexer.MySqlVersion = version;
      TokenStreamRemovable tokens = new TokenStreamRemovable(lexer);

      if (lines == null)
      {
        lines = new Dictionary<Version, TokenStreamRemovable>();        
        _parserCache.Add(sql, lines);
      }
      lines.Add(version, tokens);
      return tokens;
    }

    internal static MySQL51Parser.program_return ParseSql(string sql)
    {
      return ParseSql(sql, false);
    }

    internal static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors, out StringBuilder sb)
    {
      CommonTokenStream ts;
      return ParseSql(sql, expectErrors, out sb, out ts);
    }

    internal static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors, out StringBuilder sb, string version)
    {
      Version ver = GetVersion(version);
      CommonTokenStream ts;
      return ParseSql(sql, expectErrors, out sb, out ts, ver);
    }

    internal static MySQL51Parser.program_return ParseSql(
      string sql, bool expectErrors, out StringBuilder sb, out CommonTokenStream tokensOutput, Version version)
    {
      // The grammar supports upper case only
      MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sql));//ASCIIEncoding.ASCII.GetBytes(sql));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      //ANTLRInputStream input = new ANTLRInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      lexer.MySqlVersion = version;
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      tokensOutput = tokens;
      return DoParse(tokens, expectErrors, out sb, lexer.MySqlVersion);
    }

    internal static MySQL51Parser.program_return ParseSql(
      string sql, bool expectErrors, out StringBuilder sb, CommonTokenStream tokens)
    {
      DbConnection con = GetConnection();
      return DoParse( tokens, expectErrors, out sb, GetVersion( con.ServerVersion ));
    }

    internal static MySQL51Parser.program_return ParseSql(
      string sql, bool expectErrors, out StringBuilder sb, out CommonTokenStream tokensOutput)
    {
      DbConnection con = GetConnection();
      // The grammar supports upper case only
      MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sql));//ASCIIEncoding.ASCII.GetBytes(sql));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      //ANTLRInputStream input = new ANTLRInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      lexer.MySqlVersion = GetVersion( con.ServerVersion );
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      tokensOutput = tokens;
      return DoParse(tokens, expectErrors, out sb, lexer.MySqlVersion);
    }

    private static MySQL51Parser.program_return DoParse( 
      CommonTokenStream tokens, bool expectErrors, out StringBuilder sb, Version version )
    {
      MySQLParser parser = new MySQLParser(tokens);
      parser.MySqlVersion = version;
      sb = new StringBuilder();
      TextWriter tw = new StringWriter(sb);
      parser.TraceDestination = tw;
      MySQL51Parser.program_return r = null;
      try
      {
        r = parser.program();
      }
      catch (RewriteEmptyStreamException e)
      {
        if (!expectErrors)
        {
          sb.AppendLine();
          sb.Append(e.Message);
        }
      }
      return r;
    }

    private static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors)
    {
      StringBuilder sb;
      CommonTokenStream ts;
      return ParseSql(sql, expectErrors, out sb, out ts);
    }

    internal static Version GetVersion( string versionString )
    {
      Version version;
      int i = 0;
      while (i < versionString.Length &&
          (Char.IsDigit(versionString[i]) || versionString[i] == '.'))
        i++;
      version = new Version(versionString.Substring(0, i));
      return version;
    }

    /// <summary>
    /// Gets MySql server version from the connection string (useful for recognize the exact syntax for that version in
    /// the parser's grammar and scanner).
    /// </summary>
    /// <returns></returns>
    internal static Version GetVersion()
    {
      DbConnection con = GetConnection();
      if (con != null)
      {
        return GetVersion(con.ServerVersion);
      }
      else
      {
        // default to server 5.1
        return new Version(5, 1);
      }
    }

    public static DbConnection GetConnection()
    {
      DbConnection connection = DocumentNode.GetCurrentConnection();
      if (connection == null)
      {
        Editors.EditorBroker broker = MySql.Data.VisualStudio.Editors.EditorBroker.Broker;
        if (broker != null)
        {
          connection = broker.GetCurrentConnection();
        }
      }
      return connection;
    }

    public static string GetRoutineName(string sql)
    {
      StringBuilder sb;
      MySQL51Parser.program_return pr = LanguageServiceUtil.ParseSql(sql, false, out sb);
      if (sb.Length != 0)
      {
        throw new ApplicationException(string.Format("Syntactic error in stored routine: {0}", sb.ToString()));
      }
      else
      {
        CommonTree t = (CommonTree)pr.Tree;
        if (t.IsNil)
          t = (CommonTree)t.GetChild(0);
        string name;
        if (string.Equals(t.GetChild(1).Text, "definer", StringComparison.OrdinalIgnoreCase))
          name = t.GetChild(3).Text;
        else
          name = t.GetChild(1).Text;
        return name.Replace("`", "");
      }
    }
  }
}
