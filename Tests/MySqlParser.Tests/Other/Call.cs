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
using Antlr.Runtime.Tree;

namespace MySql.Parser.Tests
{
  [TestFixture]
  public class Call
  {
    [Test]
    public void CallSimple()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("call sp", false, out sb);
    }

    [Test]
    public void CallSimple2()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("call sp()", false, out sb);
    }

    [Test]
    public void CallWithArgs()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("call sp( 4 + 3, 'myname', (( 3.1416 * 20 ) + 7 ) / 2.71)", false, out sb);
    }

    [Test]
    public void CallWrong()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("call ", true, out sb);
    }
  }
}
