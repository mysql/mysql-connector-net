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
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NUnit.Framework;
using System.IO;
using MySql.Parser;

namespace MySql.Parser.Tests
{
  public static class Utility
  {
    public static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors, out StringBuilder sb)
    {
      return ParseSql(sql, expectErrors, out sb, new Version(5, 1));
    }

    public static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors, out StringBuilder sb, Version version )
    {
      // The grammar supports upper case only
      MemoryStream ms = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(sql/*.ToUpper() */));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      //ANTLRInputStream input = new ANTLRInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      lexer.MySqlVersion = version;
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      MySQLParser parser = new MySQLParser(tokens);
      parser.MySqlVersion = version;
      sb = new StringBuilder();
      TextWriter tw = new StringWriter(sb);
      parser.TraceDestination = tw;
      MySQL51Parser.program_return r = parser.program();
      if (!expectErrors)
      {
        if (0 != parser.NumberOfSyntaxErrors)
          Assert.AreEqual("", sb.ToString());
        //Assert.AreEqual( 0, parser.NumberOfSyntaxErrors);
      }
      else
      {
        Assert.AreNotEqual(0, parser.NumberOfSyntaxErrors);
      }
      return r;
    }

    public static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors, Version version)
    {
      StringBuilder sb;
      return ParseSql(sql, expectErrors, out sb, version);
    }

    public static MySQL51Parser.program_return ParseSql(string sql, bool expectErrors)
    {
      StringBuilder sb;
      return ParseSql(sql, expectErrors, out sb);
    }

    public static MySQL51Parser.program_return ParseSql(string sql)
	{
		return ParseSql(sql, false);
	}
  }
}
