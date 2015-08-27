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

using MySql.Protocol;
using System;
using System.Collections.Generic;

namespace MySql.XDevAPI.Results
{
  public class Result
  {
    private List<Warning> _warnings = new List<Warning>();

    internal Result()
    {
    }

    internal void AddWarning(Warning w)
    {
      _warnings.Add(w);
    }

    public IReadOnlyList<Warning> Warnings
    {
      get { return _warnings;  }
    }


    public Error ErrorInfo;

    public bool Failed
    {
      get { return ErrorInfo != null;  }
    }

    public bool Succeeded
    {
      get { return !Failed;  }
    }

    /// <summary>
    /// Class to represent an error in this result
    /// </summary>
    public class Error
    {
      public UInt32 Code;
      public string SqlState;
      public string Message;
    }

    public class Warning
    {
      public uint Code;
      public string Message;
      public uint Level;

      public Warning(uint code, string msg)
      {
        Code = code;
        Message = msg;
      }
    }
  }
}
