// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Expr;

namespace MySqlX.XDevAPI.CRUD
{
  internal class UpdateSpec
  {
    public UpdateSpec(UpdateOperation.Types.UpdateType updateType, string docPath)
    {
      Type = updateType;
      Path = docPath;
    }

    public string Path { get; private set; }
    public UpdateOperation.Types.UpdateType Type { get; private set; }
    public object Value { get; private set; }

    public bool HasValue
    {
      get { return Value != null;  }
    }

    public Expr GetValue()
    {
      return ExprUtil.ArgObjectToExpr(Value, false);
    }

    public ColumnIdentifier GetSource(bool isRelational)
    {
      var source = Path;
      // accomodate parser's documentField() handling by removing "@"
      if (source.Length > 0 && source[0] == '@')
      {
        source = source.Substring(1);
      }
      ExprParser p = new ExprParser(Path, false);
      ColumnIdentifier identifier;
      if (isRelational)
        identifier = p.ParseTableUpdateField();
      else
        identifier = p.DocumentField().Identifier;

      return identifier;
    }

    public UpdateSpec SetValue(object o)
    {
      Value = o;
      return this;
    }
  }
}
