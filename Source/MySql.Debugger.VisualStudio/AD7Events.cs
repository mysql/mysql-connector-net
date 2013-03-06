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
  public class AD7Events
  {
    private readonly IDebugEngine2 _engine;
    private readonly IDebugEventCallback2 _callback;

    public AD7Events(IDebugEngine2 engine, IDebugEventCallback2 callback)
    {
      _engine = engine;
      _callback = callback;
    }

    #region Events

    public void EngineCreated()
    {
      Debug.WriteLine("Event EngineCreated");
      Guid iid = new Guid(EngineCreateEvent.IID);
      _callback.Event(_engine, null, null, null, new EngineCreateEvent(_engine), ref iid, EngineCreateEvent.Attributes);
    }

    public void ProgramCreated(IDebugProgram2 program)
    {
      Debug.WriteLine("Event ProgramCreated");
      Guid iid = new Guid(ProgramCreateEvent.IID);
      _callback.Event(_engine, null, program, null, new ProgramCreateEvent(), ref iid, ProgramCreateEvent.Attributes);
    }

    public void EngineLoaded()
    {
      Debug.WriteLine("Event EngineLoaded");
      Guid iid = new Guid(LoadCompleteEvent.IID);
      _callback.Event(_engine, null, null, null, new LoadCompleteEvent(), ref iid, LoadCompleteEvent.Attributes);
    }

    public void DebugEntryPoint()
    {
      Debug.WriteLine("Event DebugEntryPoint");
      Guid iid = new Guid(DebugEntryPointEvent.IID);
      _callback.Event(_engine, null, null, null, new DebugEntryPointEvent(_engine), ref iid, DebugEntryPointEvent.Attributes);
    }

    public void ProcessDestroyed()
    {
      Debug.WriteLine("Event ProcessDestroyed");
      Guid iid = new Guid(ProcessDestroyEvent.IID);
      _callback.Event(_engine, null, null, null, new ProcessDestroyEvent(), ref iid, ProcessDestroyEvent.Attributes);
    }

    public void ProgramDestroyed(IDebugProgram2 program)
    {
      Debug.WriteLine("Event ProgramDestroyed");
      Guid  iid = new Guid(ProgramDestroyedEvent.IID);
      _callback.Event(_engine, null, program, null, new ProgramDestroyedEvent(), ref iid, ProgramDestroyedEvent.Attributes);
    }

    public void Breakpoint(AD7ProgramNode program, AD7Breakpoint breakpoint)
    {
      Debug.WriteLine("Event Breakpoint");
      Guid iid = new Guid(BreakPointEvent.IID);
      _callback.Event(_engine, null, null, null, new BreakPointEvent(breakpoint), ref iid, BreakPointEvent.Attributes);
    }

    public void BreakpointError(AD7ProgramNode program, IDebugErrorBreakpoint2 bpError)
    {
      Debug.WriteLine("Event BreakpointError");
      Guid iid = new Guid(DebugBreakpointErrorEvent.IID);
      _callback.Event(_engine, null, null, null, new DebugBreakpointErrorEvent(bpError), ref iid, DebugBreakpointErrorEvent.Attributes);
    }    

    public void Break(AD7ProgramNode program)
    {
      Debug.WriteLine("Event Break");
      Guid iid = new Guid(BreakEvent.IID);
      _callback.Event(_engine, null, program, program, new BreakEvent(), ref iid, BreakEvent.Attributes);
    }

    public void BreakpointHit(AD7Breakpoint breakpoint, AD7ProgramNode node)
    {
      Debug.WriteLine("Event BreakpointHit");
      Guid iid = new Guid(BreakPointHitEvent.IID);
      _callback.Event(_engine, null, node, node, new BreakPointHitEvent(breakpoint), ref iid, BreakPointHitEvent.Attributes);
    }

    public void StepCompleted(AD7ProgramNode node)
    {
      Debug.WriteLine("Event StepCompleted");
      Guid iid = new Guid(StepCompleteEvent.IID);
      _callback.Event(_engine, null, node, node, new StepCompleteEvent(), ref iid, StepCompleteEvent.Attributes);
    }

    public void ExpressionEvalCompleted(
      AD7ProgramNode node, IDebugExpression2 pExpr, IDebugProperty2 pProp)
    {
      Debug.WriteLine("Event ExpressionEvaluationCompleted");
      Guid iid = new Guid(ExpressionEvalCompleteEvent.IID);
      _callback.Event(
        _engine, null, node, node, new ExpressionEvalCompleteEvent( pExpr, pProp ), 
        ref iid, ExpressionEvalCompleteEvent.Attributes);
    }

    public void ThreadDestroyed(AD7ProgramNode node)
    {
      Debug.WriteLine("Event ThreadDestroyed");
      Guid iid = new Guid(ThreadDestroyedEvent.IID);
      _callback.Event(
        _engine, null, node, node, new ThreadDestroyedEvent(),
        ref iid, ThreadDestroyedEvent.Attributes);
    }

    #endregion
  }

  class AsynchronousEvent : IDebugEvent2
  {
    public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;

    int IDebugEvent2.GetAttributes(out uint eventAttributes)
    {
      eventAttributes = Attributes;
      return VSConstants.S_OK;
    }
  }

  class StoppingEvent : IDebugEvent2
  {
    public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_SYNC_STOP;

    int IDebugEvent2.GetAttributes(out uint eventAttributes)
    {
      eventAttributes = Attributes;
      return VSConstants.S_OK;
    }
  }

  sealed class EngineCreateEvent : AsynchronousEvent, IDebugEngineCreateEvent2
  {
    public const string IID = "FE5B734C-759D-4E59-AB04-F103343BDD06";
    private IDebugEngine2 m_engine;

    public EngineCreateEvent(IDebugEngine2 engine)
    {
      m_engine = engine;
    }

    #region IDebugEngineCreateEvent2 Members

    int IDebugEngineCreateEvent2.GetEngine(out IDebugEngine2 pEngine)
    {
      pEngine = m_engine;

      return VSConstants.S_OK;
    }

    #endregion
  }

  sealed class ProgramCreateEvent : AsynchronousEvent, IDebugProgramCreateEvent2
  {
    public const string IID = "96CD11EE-ECD4-4E89-957E-B5D496FC4139";
  }

  sealed class LoadCompleteEvent : StoppingEvent, IDebugLoadCompleteEvent2
  {
    public const string IID = "B1844850-1349-45D4-9F12-495212F5EB0B";
  }

  sealed class DebugEntryPointEvent : AsynchronousEvent, IDebugEntryPointEvent2
  {
    public const string IID = "E8414A3E-1642-48EC-829E-5F4040E16DA9";
    public object Engine { get; set; }

    public DebugEntryPointEvent(IDebugEngine2 mEngine)
    {
      Engine = mEngine;
    }
  }

  sealed class ProcessDestroyEvent : AsynchronousEvent, IDebugProcessDestroyEvent2
  {                     
    public const string IID = "3E2A0832-17E1-4886-8C0E-204DA242995F";
  }

  sealed class ProgramDestroyedEvent : AsynchronousEvent, IDebugProgramDestroyEvent2
  {
    public const string IID = "E147E9E3-6440-4073-A7B7-A65592C714B5";

    #region IDebugProgramDestroyEvent2 Members

    int IDebugProgramDestroyEvent2.GetExitCode(out uint pdwExit)
    {
      pdwExit = 0;
      return VSConstants.S_OK;
    }

    #endregion
  }

  sealed class ThreadDestroyedEvent : AsynchronousEvent, IDebugThreadDestroyEvent2
  {
    public const string IID = "2C3B7532-A36F-4A6E-9072-49BE649B8541";

    #region IDebugThreadDestroyEvent2 Members

    int IDebugThreadDestroyEvent2.GetExitCode(out uint pdwExit)
    {
      pdwExit = 0;
      return VSConstants.S_OK;
    }

    #endregion
  }
  sealed class BreakPointEvent : AsynchronousEvent, IDebugBreakpointBoundEvent2
  {
    public const string IID = "1DDDB704-CF99-4B8A-B746-DABB01DD13A0";

    private AD7Breakpoint _breakpoint;

    public BreakPointEvent(AD7Breakpoint breakpoint)
    {
      _breakpoint = breakpoint;
    }

    #region IDebugBreakpointBoundEvent2 Members

    int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
    {
      ppEnum = _breakpoint;
      return VSConstants.S_OK;
    }

    int IDebugBreakpointBoundEvent2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBP)
    {
      ppPendingBP = _breakpoint;
      return VSConstants.S_OK;
    }

    #endregion
  }

  sealed class DebugBreakpointErrorEvent : AsynchronousEvent, IDebugBreakpointErrorEvent2
  {
    public const string IID = "ABB0CA42-F82B-4622-84E4-6903AE90F210";

    private IDebugErrorBreakpoint2 _error;

    public DebugBreakpointErrorEvent(IDebugErrorBreakpoint2 error)
    {
      _error = error;
    }

    int IDebugBreakpointErrorEvent2.GetErrorBreakpoint(out IDebugErrorBreakpoint2 ppErrorBP)
    {
      ppErrorBP = _error;
      return VSConstants.S_OK;
    }
  }

  sealed class BreakEvent : StoppingEvent, IDebugBreakEvent2
  {
    public const string IID = "C7405D1D-E24B-44E0-B707-D8A5A4E1641B";
  }

  sealed class BreakPointHitEvent : StoppingEvent, IDebugBreakpointEvent2
  {
    public const string IID = "501C1E21-C557-48B8-BA30-A1EAB0BC4A74";

    private AD7Breakpoint _breakpoint;

    public BreakPointHitEvent(AD7Breakpoint breakpoint)
    {
      _breakpoint = breakpoint;
    }

    #region Implementation of IDebugBreakpointEvent2

    public int EnumBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
    {
      ppEnum = _breakpoint;
      return VSConstants.S_OK;
    }


    #endregion
  }

  sealed class StepCompleteEvent : StoppingEvent, IDebugStepCompleteEvent2
  {
    public const string IID = "0F7F24C1-74D9-4EA6-A3EA-7EDB2D81441D";

  }

  internal sealed class ExpressionEvalCompleteEvent : 
    AsynchronousEvent, IDebugExpressionEvaluationCompleteEvent2
  {
    public const string IID = "C0E13A85-238A-4800-8315-D947C960A843";

    private IDebugExpression2 _expr;
    private IDebugProperty2 _prop;

    public ExpressionEvalCompleteEvent( IDebugExpression2 pExpr, IDebugProperty2 pProp )
    {
      _expr = pExpr;
      _prop = pProp;
    }

    int IDebugExpressionEvaluationCompleteEvent2.GetExpression(out IDebugExpression2 ppExpr)
    {
      ppExpr = _expr;
      return VSConstants.S_OK;
    }

    int IDebugExpressionEvaluationCompleteEvent2.GetResult(out IDebugProperty2 ppResult)
    {
      ppResult = _prop;
      return VSConstants.S_OK;
    }
  }
}
