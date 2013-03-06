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

namespace MySql.Debugger
{
  public enum SteppingTypeEnum : int
  {
    None = 0,
    StepInto = 1,
    StepOver = 2,
    StepOut = 3
  };

  public enum RoutineInfoType : int
  {
    Procedure = 1,
    Function = 2,
    Trigger = 3
  }

  public enum RoutineType : int
  {
    Procedure = 1,
    Function = 2
  }

  public enum TriggerActionTiming : int
  {
    Before = 1,
    After = 2
  }

  public enum TriggerEvent : int
  {
    Insert = 1,
    Update = 2,
    Delete = 3
  }

  public enum ArgTypeEnum : int
  {
    In = 0,
    Out = 1,
    InOut = 2
  }

  public enum VarKindEnum : int
  {
    Local = 0,
    Argument = 1,
    Session = 2,
    Global = 3,
    Internal = 4
  }
}
