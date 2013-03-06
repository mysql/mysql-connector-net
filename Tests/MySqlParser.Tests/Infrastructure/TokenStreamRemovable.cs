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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Antlr.Runtime;
using MySql.Parser;

namespace MySql.Parser.Tests
{
  [TestFixture]
  public class TokenStreamRemovable
  {
    [Test]
    public void TestTokenRemove()
    {
      string sql = "select *, a, c, d from table1 where a is null";
      MemoryStream ms = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(sql));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      //ANTLRInputStream input = new ANTLRInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      MySql.Parser.TokenStreamRemovable tsr = new MySql.Parser.TokenStreamRemovable(lexer);
      tsr.Fill();
      List<IToken> tokens = tsr.GetTokens();
      IToken removed = null;
      foreach( IToken t in tokens )
      {
        if (t.Text == "d")
        {
          removed = t;
          break;
        }
      }
      tsr.Remove(removed);
      tokens = tsr.GetTokens();
    }
  }
}
