// Copyright © 2004, 2012, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Debugger
{
  /// <summary>
  /// Meta info for routines (stored procedures, functions, triggers).
  /// </summary>
  public class RoutineInfo
  {
    public string Schema { get; set; }

    private string _name;
    public string Name {
      get { return _name; }
      set 
      {
        _name = value.Replace("`", "");
      }
    }

    private string _sourceCode;
    public string SourceCode
    {
      get { return _sourceCode; }
      set
      {
        if (value != null)
          _hash = Debugger.GetTagHashCode(value);
        else
          _hash = "".GetHashCode();
        _sourceCode = value;
      }
    }

    private int _hash;
    public int Hash { get { return _hash; } }

    internal string InstrumentedSourceCode { get; set; }
    public RoutineInfoType Type { get; set; }
    internal CommonTokenStream TokenStream { get; set; }
    internal CommonTree ParsedTree { get; set; }
    internal string PreInstrumentationCode { get; set; }
    internal string PostInstrumentationCode { get; set; }
    internal bool _endOfDeclare { get; set; }
    internal string _leaveLabel { get; set; }
    internal Dictionary<string, StoreType> Locals { get; set; }
    internal CommonTree BeginEnd { get; set; }
    private Dictionary<int, List<ITree>> _statementsPerLine = new Dictionary<int, List<ITree>>();
    internal MetaTrigger TriggerInfo { get; set; }

    internal void RegisterStatement(ITree tree)
    {
      List<ITree> l;
      int line = TokenStream.Get(tree.TokenStartIndex).Line;
      if (!_statementsPerLine.TryGetValue(line, out l))
      {
        l = new List<ITree>();
        l.Add(tree);
        _statementsPerLine.Add(line, l);
      }
      else
      {
        l.Add(tree);
      }
    }

    internal CommonTree GetStatementFromPos(int line, int col)
    {
      List<ITree> l;
      if (_statementsPerLine.TryGetValue(line, out l))
      {
        foreach (ITree t in l)
        {
          if (t.CharPositionInLine == col) return ( CommonTree )t;
        }
      }
      return null;
    }

    internal bool HasLineValidStatement(int line)
    {
      List<ITree> l;
      return _statementsPerLine.TryGetValue(line, out l);
    }

    public string FullName
    {
      get
      {
        if (string.IsNullOrEmpty(Schema))
          return Name;
        else
          return string.Format("{0}.{1}", Schema, Name);
      }
    }

    public string GetFullName(string database)
    {
      if (string.IsNullOrEmpty(Schema))
        return string.Format("{0}.{1}", database, Name);
      else
        return string.Format("{0}.{1}", Schema, Name);
    }

    public static string[] GetFullName(string database, string routineName)
    { 
      string[] fullName = null;
      if (routineName.IndexOf('.') != -1)
        fullName = routineName.Split('.');
      else
        fullName = new string[] { database, routineName };
      //return string.Join(".", fullName);
      return fullName;
    }

    public static string GetRoutineName(string Name)
    {
      if (Name.IndexOf('.') == -1)
      {
        return Name;
      }
      else
      {
        return Name.Split('.')[1];
      }
    }

    internal RoutineInfo()
    {
    }
  }
}
