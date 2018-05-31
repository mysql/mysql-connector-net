// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySqlX.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Expr;
using MySqlX.Serialization;
using static Mysqlx.Crud.UpdateOperation.Types;
using System.Collections.Generic;
using System;

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

    public Expr GetValue(UpdateType operationType)
    {
      bool evaluateStringExpression = true;
      if (operationType == UpdateType.ArrayAppend || operationType == UpdateType.ArrayInsert || operationType == UpdateType.ItemSet)
      {
        Value = ExprUtil.ParseAnonymousObject(Value) ?? Value;
        if (Value is string)
        {
          try
          {
            JsonParser.Parse(Value as string);
          }
          catch (Exception)
          {
            evaluateStringExpression = false;
          }
        }
      }

      return ExprUtil.ArgObjectToExpr(Value, false, evaluateStringExpression);
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

      if (p.tokenPos < p.tokens.Count)
        throw new ArgumentException("Invalid document path.");

      return identifier;
    }

    public UpdateSpec SetValue(object o)
    {
      Value = o;
      return this;
    }
  }
}
