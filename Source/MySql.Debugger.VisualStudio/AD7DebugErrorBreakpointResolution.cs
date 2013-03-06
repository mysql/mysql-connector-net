using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio;
using System.Diagnostics;

namespace MySql.Debugger.VisualStudio
{
  public class AD7DebugErrorBreakpointResolution : IDebugErrorBreakpointResolution2
  {
    public AD7Breakpoint bp { get; private set; }

    public AD7DebugErrorBreakpointResolution( AD7Breakpoint mybp )
    {
      Debug.WriteLine("AD7DebugErrorBreakpointResolution: ctor");
      bp = mybp;
    }

    int IDebugErrorBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
    {
      Debug.WriteLine("AD7DebugErrorBreakpointResolution: GetBreakpointType");
      pBPType[0] = enum_BP_TYPE.BPT_CODE;
      return VSConstants.S_OK;
    }

    int IDebugErrorBreakpointResolution2.GetResolutionInfo(enum_BPERESI_FIELDS dwFields, BP_ERROR_RESOLUTION_INFO[] pErrorResolutionInfo)
    {
      Debug.WriteLine("AD7DebugErrorBreakpointResolution: GetResolutionInfo");
      if ( (dwFields == enum_BPERESI_FIELDS.BPERESI_ALLFIELDS) ||
        (( dwFields & enum_BPERESI_FIELDS.BPERESI_TYPE ) != 0 ) ||
        (( dwFields & enum_BPERESI_FIELDS.BPERESI_MESSAGE ) != 0 ) )
      {
        BP_RESOLUTION_INFO[] resolutionInfo = new BP_RESOLUTION_INFO[1];
        ((IDebugBreakpointResolution2)bp).GetResolutionInfo( enum_BPRESI_FIELDS.BPRESI_ALLFIELDS, resolutionInfo);
        pErrorResolutionInfo[0].dwFields = dwFields;
        pErrorResolutionInfo[0].bpResLocation = resolutionInfo[0].bpResLocation;
        pErrorResolutionInfo[0].pProgram = resolutionInfo[0].pProgram;
        pErrorResolutionInfo[0].pThread = resolutionInfo[0].pThread;
        pErrorResolutionInfo[0].dwType = enum_BP_ERROR_TYPE.BPET_GENERAL_WARNING;
        pErrorResolutionInfo[0].bstrMessage = "Breakpoint invalid in this location.";
      }
      return VSConstants.S_OK;
    }
  }
}
