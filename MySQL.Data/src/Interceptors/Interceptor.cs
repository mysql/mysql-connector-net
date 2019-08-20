// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Interceptor is the base class for the "manager" classes such as ExceptionInterceptor,
  /// CommandInterceptor, etc
  /// </summary>
  internal abstract class Interceptor
  {
    protected MySqlConnection Connection;

    protected void LoadInterceptors(string interceptorList)
    {
      if (String.IsNullOrEmpty(interceptorList)) return;

      string[] interceptors = interceptorList.Split('|');
      foreach (string interceptorType in interceptors)
      {
        if (String.IsNullOrEmpty(interceptorType)) continue;

        string type = ResolveType(interceptorType);
        Type t = Type.GetType(type);
        object interceptorObject = Activator.CreateInstance(t);
        AddInterceptor(interceptorObject);
      }
    }

    protected abstract void AddInterceptor(object o);

    protected virtual string ResolveType(string nameOrType)
    {
      return nameOrType;
    }
  }
}
