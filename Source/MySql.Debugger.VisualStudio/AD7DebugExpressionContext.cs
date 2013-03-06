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
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio;
using System.Diagnostics;

namespace MySql.Debugger.VisualStudio
{
  public class AD7DebugExpressionContext : IDebugExpressionContext2
  {
    private AD7StackFrame _stackFrame;

    public AD7DebugExpressionContext( AD7StackFrame stackFrame )
    {
      _stackFrame = stackFrame;
    }

    int IDebugExpressionContext2.GetName(out string pbstrName)
    {
      pbstrName = _stackFrame.Node.FileName;
      return VSConstants.S_OK;
    }

    int IDebugExpressionContext2.ParseText(
      string pszCode, enum_PARSEFLAGS dwFlags, uint nRadix, 
      out IDebugExpression2 ppExpr, 
      out string pbstrError, out uint pichError)
    {
      pbstrError = null;
      ppExpr = null;
      pichError = 0;
      try
      {
        AD7DebugExpression expr = new AD7DebugExpression(_stackFrame, pszCode);
        bool success = DebuggerManager.Instance.Debugger.TryParseExpression(pszCode, out pbstrError);
        if (success)
          ppExpr = (IDebugExpression2)expr;
      }
      catch (Exception e)
      {
        pbstrError = e.Message;
        return VSConstants.E_FAIL;
      }
      return VSConstants.S_OK;
    }
  }
}
