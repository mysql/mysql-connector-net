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
  /// A debugger breakpoint.
  /// </summary>
  public class Breakpoint
  {
    protected Debugger _debugger { get; set; }
    public bool Disabled { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public int StartColumn { get; set; }
    public int EndColumn { get; set; }
    public int Line { get { return StartLine; } }
    public string Condition { get; protected set; }
    private object _previousConditionValue;
    public bool IsFake { get; set; }
    public int Hash { get; set; }
    public string RoutineName { get; set; }
    public EnumBreakpointConditionStyle ConditionStyle { get; protected set; }
    public uint HitCount { get; set; }
    public uint PassCount { get; protected set; }
    public EnumBreakpointPassCountStyle PassCountStyle { get; protected set; }

    public void SetCondition(string condition, EnumBreakpointConditionStyle style)
    {
      ConditionStyle = style;
      Condition = condition;
      _previousConditionValue = _debugger.Eval(condition);
    }

    public void SetPassCount(uint passCount, EnumBreakpointPassCountStyle style)
    {
      PassCount = passCount;
      PassCountStyle = style;
      HitCount = 0;
    }

    /// <summary>
    /// Returns true of the breakpoint is triggered either becuase pass count has been reached, or the condition has been evaluated to true.
    /// </summary>
    /// <returns></returns>
    public bool IsTriggered()
    {
      if (ConditionStyle != EnumBreakpointConditionStyle.None)
      {
        object newValue = _debugger.Eval(Condition);
        try
        {
          if ((ConditionStyle == EnumBreakpointConditionStyle.WhenTrue) && (Convert.ToInt32(newValue) == 1))
            return true;
          else if ((ConditionStyle == EnumBreakpointConditionStyle.WhenChanged) && (_previousConditionValue != newValue))
            return true;
        }
        finally
        {
          _previousConditionValue = newValue;
        }
      }
      else if (PassCountStyle != EnumBreakpointPassCountStyle.None)
      {
        HitCount++;
        if ((PassCountStyle == EnumBreakpointPassCountStyle.Equal) && (HitCount == PassCount))
          return true;
        else if ((PassCountStyle == EnumBreakpointPassCountStyle.EqualOrGreater) && (HitCount >= PassCount))
          return true;
        else if ((PassCountStyle == EnumBreakpointPassCountStyle.Mod) && ((PassCount % HitCount) == 0))
          return true;
      }
      else
      {
        return true;
      }
      return false;
    }

    public Breakpoint( Debugger debugger )
    {
      IsFake = false;
      Disabled = false;
      StartColumn = 0;
      EndColumn = UInt16.MaxValue;
      _debugger = debugger;
      ConditionStyle = EnumBreakpointConditionStyle.None;
      PassCountStyle = EnumBreakpointPassCountStyle.None;
    }
  }

  public enum EnumBreakpointConditionStyle : int
  {
    None = 0,
    WhenTrue = 1,
    WhenChanged = 2
  }

  public enum EnumBreakpointPassCountStyle : int
  {
    None = 0,
    Equal = 1,
    EqualOrGreater = 2,
    Mod = 3
  }
}
