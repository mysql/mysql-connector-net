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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MySql.Debugger;

namespace MySql.Debugger.VisualStudio
{
  public class AD7Breakpoint : IDebugPendingBreakpoint2, IEnumDebugBoundBreakpoints2, IDebugBoundBreakpoint2, IDebugBreakpointResolution2
  {
    private AD7Events _callback;

    private AD7ProgramNode _node;
    internal AD7ProgramNode Node { get { return _node; } }

    internal IDebugBreakpointRequest2 _breakpointRequest;
    private BP_REQUEST_INFO _bpRequestInfo;
    
    private uint _lineNumber;
    internal int LineNumber { get { return (int)_lineNumber; } }

    internal string FileName { get { return _node.FileName; } }
    
    private TEXT_POSITION _beginPosition;
    private TEXT_POSITION _endPosition;
    private Breakpoint _coreBreakpoint;
    
    private bool _disabled;
    internal bool Disabled { get { return _disabled; } }

    internal Breakpoint CoreBreakpoint { 
      get { return _coreBreakpoint; } 
      set { _coreBreakpoint = value; } 
    }

    private IEnumDebugErrorBreakpoints2 enumError;

    internal AD7Breakpoint(AD7ProgramNode node, AD7Events callback)
    {
      Debug.WriteLine("AD7Breakpoint: ctor");
      _node = node;
      _callback = callback;
      _breakpointRequest = null;
    }

    public AD7Breakpoint(AD7ProgramNode node, AD7Events callback, IDebugBreakpointRequest2 breakpointRequest)
    {
      Debug.WriteLine("AD7Breakpoint: ctor");
      _node = node;
      _callback = callback;
      _breakpointRequest = breakpointRequest;
      
      BP_REQUEST_INFO[] requestInfo = new BP_REQUEST_INFO[1];
      _breakpointRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_BPLOCATION | enum_BPREQI_FIELDS.BPREQI_CONDITION, requestInfo);
      _bpRequestInfo = requestInfo[0];
    }

    public int Delete()
    {
      Debug.WriteLine("AD7Breakpoint: Delete");
      return VSConstants.S_OK;
    }

    public int Enable(int fEnable)
    {
      Debug.WriteLine("AD7Breakpoint: Enable");
      if( CoreBreakpoint == null )
        // defer real enabling for later
        _disabled = (fEnable == 0);
      else
        CoreBreakpoint.Disabled = (fEnable == 0);
      return VSConstants.S_OK;
    }

    public int SetCondition(BP_CONDITION bpCondition)
    {
      Debug.WriteLine("AD7Breakpoint: SetCondition");
      EnumBreakpointConditionStyle style = EnumBreakpointConditionStyle.None;
      switch (bpCondition.styleCondition)
      {
        case enum_BP_COND_STYLE.BP_COND_WHEN_CHANGED:
          style = EnumBreakpointConditionStyle.WhenChanged;
          break;
        case enum_BP_COND_STYLE.BP_COND_WHEN_TRUE:
          style = EnumBreakpointConditionStyle.WhenTrue;
          break;
      }
      try
      {
        CoreBreakpoint.SetCondition(bpCondition.bstrCondition, style);
      }
      catch (DebuggerException e)
      {
        MessageBox.Show(e.Message, "Error when setting condition breakpoint", MessageBoxButtons.OK);
        return VSConstants.E_FAIL;
      }
      return VSConstants.S_OK;
    }

    public int SetPassCount(BP_PASSCOUNT bpPassCount)
    {
      Debug.WriteLine("AD7Breakpoint: SetPassCount");
      EnumBreakpointPassCountStyle style = EnumBreakpointPassCountStyle.None;
      switch (bpPassCount.stylePassCount)
      {
        case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL:
          style = EnumBreakpointPassCountStyle.Equal;
          break;
        case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL_OR_GREATER:
          style = EnumBreakpointPassCountStyle.EqualOrGreater;
          break;
        case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD:
          style = EnumBreakpointPassCountStyle.Mod;
          break;
      }
      CoreBreakpoint.SetPassCount(bpPassCount.dwPassCount, style);
      return VSConstants.S_OK;
    }

    #region IDebugPendingBreakpoint2 Members

    int IDebugPendingBreakpoint2.Bind()
    {
      Debug.WriteLine("AD7Breakpoint: Bind");
      enumError = null;
      
      // set breakpoint line
      int result = FindBreakpointLine();
      if (result != VSConstants.S_OK)
        return result;

      if (((IDebugPendingBreakpoint2)this).CanBind(out enumError) == VSConstants.S_OK)
      {
        // bind breakpoint
        DebuggerManager.Instance.BindBreakpoint(this);
        _callback.Breakpoint(_node, this);
        return VSConstants.S_OK;
      }
      else
      {
        IDebugErrorBreakpoint2[] err = new IDebugErrorBreakpoint2[ 1 ];
        uint cnt = 0;
        enumError.Reset();
        enumError.Next(1, err, ref cnt);
        enumError.Reset();
        _callback.BreakpointError(_node, err[0]);
        return VSConstants.S_OK;
      }
    }

    private int FindBreakpointLine()
    {
      IDebugDocumentPosition2 docPosition = (IDebugDocumentPosition2)(Marshal.GetObjectForIUnknown(_bpRequestInfo.bpLocation.unionmember2));

      TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
      TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
      docPosition.GetRange(startPosition, endPosition);
      _lineNumber = startPosition[0].dwLine + 1;
      _beginPosition = startPosition[0];
      _endPosition = endPosition[0];
      string fileName;
      docPosition.GetFileName(out fileName);
      if (fileName != _node.FileName)
        return VSConstants.E_FAIL;
      else
        return VSConstants.S_OK;
    }

    int IDebugPendingBreakpoint2.CanBind(out IEnumDebugErrorBreakpoints2 ppErrorEnum)
    {
      Debug.WriteLine("AD7Breakpoint: CanBind");
      int result = VSConstants.S_OK;
      ppErrorEnum = null;
      if (_lineNumber == 0)
        result = FindBreakpointLine();
      if (result != VSConstants.S_OK)
        return result;
      if (DebuggerManager.Instance.Debugger.CanBindBreakpoint( ( int )this._lineNumber))
      {
        ppErrorEnum = null;
        return VSConstants.S_OK;
      }
      else
      {
        // TODO: Can also return deleted breakpoint, see constant E_BP_DELETED
        Ad7EnumDebugErrorBreakpoints enumBp = new Ad7EnumDebugErrorBreakpoints();
        enumBp.Add(new AD7DebugErrorBreakpoint(this));
        ppErrorEnum = enumBp;
        return VSConstants.E_FAIL;
      }
    }

    int IDebugPendingBreakpoint2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
    {
      Debug.WriteLine("AD7Breakpoint: EnumBoundBreakpoints");
      if (enumError == null)
      {
        ppEnum = this;
        return VSConstants.S_OK;
      }
      else
      {
        ppEnum = null;
        return VSConstants.E_FAIL;
      }
    }

    int IDebugPendingBreakpoint2.EnumErrorBreakpoints(enum_BP_ERROR_TYPE bpErrorType, out IEnumDebugErrorBreakpoints2 ppEnum)
    {
      Debug.WriteLine("AD7Breakpoint: EnumErrorBreakpoints");
      if ( ((bpErrorType & enum_BP_ERROR_TYPE.BPET_GENERAL_WARNING) != 0) ||
        (( bpErrorType & enum_BP_ERROR_TYPE.BPET_TYPE_ERROR ) != 0 ))
      {
        ppEnum = enumError;
      }
      else
      {
        ppEnum = null;
      }
      return VSConstants.S_OK;
    }

    int IDebugPendingBreakpoint2.GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest)
    {
      Debug.WriteLine("AD7Breakpoint: GetBreakpointRequest");
      ppBPRequest = null;
      return VSConstants.S_OK;
    }

    int IDebugPendingBreakpoint2.GetState(PENDING_BP_STATE_INFO[] pState)
    {
      Debug.WriteLine("AD7Breakpoint: GetState");
      PENDING_BP_STATE_INFO state = new PENDING_BP_STATE_INFO
      {
        Flags = enum_PENDING_BP_STATE_FLAGS.PBPSF_NONE, 
        state = enum_PENDING_BP_STATE.PBPS_ENABLED };
      pState[0] = state;
      return VSConstants.S_OK;
    }

    int IDebugPendingBreakpoint2.Virtualize(int fVirtualize)
    {
      Debug.WriteLine("AD7Breakpoint: Virtualize");
      return VSConstants.S_OK;
    }

    #endregion

    #region IEnumDebugBoundBreakpoints2 Members

    int IEnumDebugBoundBreakpoints2.Clone(out IEnumDebugBoundBreakpoints2 ppEnum)
    {
      Debug.WriteLine("AD7Breakpoint: Clone");
      throw new NotImplementedException();
    }

    int IEnumDebugBoundBreakpoints2.GetCount(out uint pcelt)
    {
      Debug.WriteLine("AD7Breakpoint: GetCount");
      if (enumError == null)
        pcelt = 1;
      else
        pcelt = 0;
      return VSConstants.S_OK;
    }

    int IEnumDebugBoundBreakpoints2.Next(uint celt, IDebugBoundBreakpoint2[] rgelt, ref uint pceltFetched)
    {
      Debug.WriteLine("AD7Breakpoint: Next");
      if (enumError == null)
      {
        rgelt[0] = this;
        pceltFetched = 1;
      }
      else
      {
        pceltFetched = 0;
      }
      return VSConstants.S_OK;
    }

    int IEnumDebugBoundBreakpoints2.Reset()
    {
      Debug.WriteLine("AD7Breakpoint: Reset");
      return VSConstants.S_OK;
    }

    int IEnumDebugBoundBreakpoints2.Skip(uint celt)
    {
      Debug.WriteLine("AD7Breakpoint: Skip");
      throw new NotImplementedException();
    }

    #endregion

    #region IDebugBoundBreakpoint2 Members

    int IDebugBoundBreakpoint2.GetBreakpointResolution(out IDebugBreakpointResolution2 ppBPResolution)
    {
      Debug.WriteLine("AD7Breakpoint: GetBreakpointResolution");
      ppBPResolution = this;
      return VSConstants.S_OK;
    }

    int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
    {
      Debug.WriteLine("AD7Breakpoint: GetHitCount");
      pdwHitCount = CoreBreakpoint.HitCount;
      return VSConstants.S_OK;
    }

    int IDebugBoundBreakpoint2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
    {
      Debug.WriteLine("AD7Breakpoint: GetPendingBreakpoint");
      ppPendingBreakpoint = this;
      return VSConstants.S_OK;
    }

    int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState)
    {
      Debug.WriteLine("AD7Breakpoint: GetState");
      pState[0] = enum_BP_STATE.BPS_ENABLED;
      return VSConstants.S_OK;
    }

    int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
    {
      Debug.WriteLine("AD7Breakpoint: SetHitCount");
      CoreBreakpoint.HitCount = dwHitCount;
      return VSConstants.S_OK;
    }

    #endregion

    #region IDebugBreakpointResolution2 Members

    int IDebugBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
    {
      Debug.WriteLine("AD7Breakpoint: GetBreakpointType");
      pBPType[0] = enum_BP_TYPE.BPT_CODE;
      return VSConstants.S_OK;
    }

    int IDebugBreakpointResolution2.GetResolutionInfo(enum_BPRESI_FIELDS dwFields, BP_RESOLUTION_INFO[] pBPResolutionInfo)
    {
      Debug.WriteLine("AD7Breakpoint: GetResolutionInfo");
      AD7DocumentContext documentContext = new AD7DocumentContext(_node.FileName, ( int )_lineNumber, _beginPosition, _endPosition, this._node.Debugger.Debugger.CurrentScope );
      if (dwFields == enum_BPRESI_FIELDS.BPRESI_ALLFIELDS)
      {
        var loc = new BP_RESOLUTION_LOCATION
        {
          bpType = (uint)enum_BP_TYPE.BPT_CODE,
          unionmember1 = Marshal.GetComInterfaceForObject(documentContext, typeof(IDebugCodeContext2))
        };

        pBPResolutionInfo[0].bpResLocation = loc;
        pBPResolutionInfo[0].pProgram = _node;
        pBPResolutionInfo[0].pThread = _node;
      }

      return VSConstants.S_OK;
    }

    #endregion
  }
}
