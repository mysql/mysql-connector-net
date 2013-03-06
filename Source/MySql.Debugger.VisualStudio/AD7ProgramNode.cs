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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;


namespace MySql.Debugger.VisualStudio
{
  public class AD7ProgramNode : IDebugProgramNode2, IDebugProgram2, IDebugProgramNodeAttach2, IDebugEngineProgram2, IDebugThread2, IEnumDebugThreads2
  {
    public AD7Process Process { get; set; }
    public Guid Id { get; set; }
    private string _filename;
    public string FileName { 
      get { return _filename; } 
      set { _filename = value; }
    }
    internal string ProgramContents { get; set; }
    public DebuggerManager Debugger { get; set; }
    internal string ConnectionString { get; set; }
    internal NativeWindow ParentWindow { get; set; }

    public AD7ProgramNode(AD7Process process)
    {
      Process = process;
      Id = Guid.NewGuid();
      //ProgramContents = MySql.Debugger.Debugger.NormalizeTag(File.ReadAllText(_filename));
    }

    #region IDebugProgramNode2 Members

    #region Deprecated

    int IDebugProgramNode2.Attach_V7(IDebugProgram2 pMDMProgram, IDebugEventCallback2 pCallback, uint dwReason)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Attach_V7");
      throw new NotImplementedException();
    }

    int IDebugProgramNode2.DetachDebugger_V7()
    {
      Debug.WriteLine("AD7ProgramNode: Entering DetachDebugger_V7");
      throw new NotImplementedException();
    }

    int IDebugProgramNode2.GetHostMachineName_V7(out string pbstrHostMachineName)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetHostMachineName_V7");
      throw new NotImplementedException();
    }

    #endregion

    int IDebugProgramNode2.GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetEngineInfo");
      pbstrEngine = AD7Guids.EngineName;
      pguidEngine = AD7Guids.EngineGuid;

      return VSConstants.S_OK;
    }

    int IDebugProgramNode2.GetHostName(enum_GETHOSTNAME_TYPE dwHostNameType, out string pbstrHostName)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetHostName");
      pbstrHostName = null;
      return VSConstants.E_NOTIMPL;
    }

    int IDebugProgramNode2.GetHostPid(AD_PROCESS_ID[] pHostProcessId)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetHostPid");
      pHostProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
      pHostProcessId[0].guidProcessId = Process.Id;

      return VSConstants.S_OK;
    }

    int IDebugProgramNode2.GetProgramName(out string pbstrProgramName)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetProgramName");
      pbstrProgramName = null;
      return VSConstants.E_NOTIMPL;
    }

    #endregion

    #region IDebugProgram2 Members
    
    int IDebugProgram2.Attach(IDebugEventCallback2 pCallback)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Attach");
      return VSConstants.S_OK;
    }

    int IDebugProgram2.CanDetach()
    {
      Debug.WriteLine("AD7ProgramNode: Entering CanDetach");
      return VSConstants.S_OK;
    }

    int IDebugProgram2.CauseBreak()
    {
      Debug.WriteLine("AD7ProgramNode: Entering CauseBreak");
      //TODO Break debug
      //Debugger.BreakpointHit();
      return VSConstants.S_OK;
    }

    int IDebugProgram2.Continue(IDebugThread2 pThread)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Continue");
      DebuggerManager.Instance.Run();
      return VSConstants.S_OK;
    }

    int IDebugProgram2.Detach()
    {
      Debug.WriteLine("AD7ProgramNode: Entering Detach");
      DebuggerManager.Instance.Debugger.Stop();
      return VSConstants.S_OK;
    }

    int IDebugProgram2.EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
    {
      Debug.WriteLine("AD7ProgramNode: Entering EnumCodeContexts");
      throw new NotImplementedException();
    }

    int IDebugProgram2.EnumCodePaths(string pszHint, IDebugCodeContext2 pStart, IDebugStackFrame2 pFrame, int fSource, out IEnumCodePaths2 ppEnum, out IDebugCodeContext2 ppSafety)
    {
      Debug.WriteLine("AD7ProgramNode: Entering EnumCodePaths");
      throw new NotImplementedException();
    }

    int IDebugProgram2.EnumModules(out IEnumDebugModules2 ppEnum)
    {
      Debug.WriteLine("AD7ProgramNode: Entering EnumModules");
      throw new NotImplementedException();
    }

    int IDebugProgram2.EnumThreads(out IEnumDebugThreads2 ppEnum)
    {
      Debug.WriteLine("AD7ProgramNode: Entering EnumThreads");
      ppEnum = this;
      return VSConstants.S_OK;
    }

    int IDebugProgram2.Execute()
    {
      Debug.WriteLine("AD7ProgramNode: Entering Execute");      
      DebuggerManager.Instance.SteppingType = SteppingTypeEnum.None;
      DebuggerManager.Instance.Run();
      return VSConstants.S_OK;
    }

    int IDebugProgram2.GetDebugProperty(out IDebugProperty2 ppProperty)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetDebugProperty");
      throw new NotImplementedException();
    }

    int IDebugProgram2.GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 pCodeContext, out IDebugDisassemblyStream2 ppDisassemblyStream)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetDisassemblyStream");
      throw new NotImplementedException();
    }

    int IDebugProgram2.GetENCUpdate(out object ppUpdate)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetENCUpdate");
      throw new NotImplementedException();
    }

    int IDebugProgram2.GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetEngineInfo");
      pbstrEngine = AD7Guids.EngineName;
      pguidEngine = AD7Guids.EngineGuid;
      return VSConstants.S_OK;
    }

    int IDebugProgram2.GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetMemoryBytes");
      throw new NotImplementedException();
    }

    int IDebugProgram2.GetName(out string pbstrName)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetName");
      pbstrName = AD7Guids.EngineName;
      return VSConstants.S_OK;
    }

    int IDebugProgram2.GetProcess(out IDebugProcess2 ppProcess)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetProcess");
      ppProcess = Process;
      return VSConstants.S_OK;
    }

    int IDebugProgram2.GetProgramId(out Guid pguidProgramId)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetProgramId");
      pguidProgramId = Id;
      return VSConstants.S_OK;
    }

    int IDebugProgram2.Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Step");
      if (sk == enum_STEPKIND.STEP_BACKWARDS)
        return VSConstants.E_NOTIMPL;
      
      Thread thread = new Thread(new ThreadStart(() => { 
        // Just to ensure main method returns before running thread.
        Thread.Sleep(10);
        SteppingTypeEnum stepping = SteppingTypeEnum.None;
        switch (sk)
        {
          case enum_STEPKIND.STEP_INTO:
            stepping = SteppingTypeEnum.StepInto;
            break;
          case enum_STEPKIND.STEP_OVER:
            stepping = SteppingTypeEnum.StepOver;
            break;
          case enum_STEPKIND.STEP_OUT:
            stepping = SteppingTypeEnum.StepOut;
            break;
        }
        DebuggerManager.Instance.SteppingType = stepping;
        DebuggerManager.Instance.Run();
        //Debugger.Step();
      }));
      thread.Start();
      return VSConstants.S_OK;
    }

    int IDebugProgram2.Terminate()
    {
      Debug.WriteLine("AD7ProgramNode: Entering Terminate");
      //DebuggerManager.Instance.Debugger.RestoreRoutinesBackup();
      return VSConstants.S_OK;
    }

    int IDebugProgram2.WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
    {
      Debug.WriteLine("AD7ProgramNode: Entering WriteDump");
      throw new NotImplementedException();
    }
    
    #endregion

    #region IDebugProgramNodeAttach2 Members

    int IDebugProgramNodeAttach2.OnAttach(ref Guid guidProgramId)
    {
      Debug.WriteLine("AD7ProgramNode: Entering OnAttach");
      return VSConstants.S_OK;
    }

    #endregion

    #region IDebugEngineProgram2 Members

    int IDebugEngineProgram2.Stop()
    {
      Debug.WriteLine("AD7ProgramNode: Entering Stop");
      return VSConstants.S_OK;
    }

    int IDebugEngineProgram2.WatchForExpressionEvaluationOnThread(IDebugProgram2 pOriginatingProgram, uint dwTid, uint dwEvalFlags, IDebugEventCallback2 pExprCallback, int fWatch)
    {
      Debug.WriteLine("AD7ProgramNode: Entering WatchForExpressionEvaluationOnThread");
      throw new NotImplementedException();
    }

    int IDebugEngineProgram2.WatchForThreadStep(IDebugProgram2 pOriginatingProgram, uint dwTid, int fWatch, uint dwFrame)
    {
      Debug.WriteLine("AD7ProgramNode: Entering WatchForThreadStep");
      return VSConstants.S_OK;
    }

    #endregion

    #region IDebugThread2 Members

    int IDebugThread2.CanSetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
    {
      Debug.WriteLine("AD7ProgramNode: Entering CanSetNextStatement");
      return VSConstants.S_OK;
    }

    int IDebugThread2.EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 ppEnum)
    {
      // TODO: Get the real callstack here.
      Debug.WriteLine("AD7ProgramNode: Entering EnumFrameInfo");
      ppEnum = new AD7StackFrameCollection(this);
      return VSConstants.S_OK;
    }

    int IDebugThread2.GetLogicalThread(IDebugStackFrame2 pStackFrame, out IDebugLogicalThread2 ppLogicalThread)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetLogicalThread");
      throw new NotImplementedException();
    }

    int IDebugThread2.GetName(out string pbstrName)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetName");
      pbstrName = AD7Guids.EngineName;
      return VSConstants.S_OK;
    }

    int IDebugThread2.GetProgram(out IDebugProgram2 ppProgram)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetProgram");
      ppProgram = this;
      return VSConstants.S_OK;
    }

    int IDebugThread2.GetThreadId(out uint pdwThreadId)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetThreadId");
      pdwThreadId = 0;
      return VSConstants.S_OK;
    }

    int IDebugThread2.GetThreadProperties(enum_THREADPROPERTY_FIELDS dwFields, THREADPROPERTIES[] ptp)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetThreadProperties");
      if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_ID) != 0)
      {
        ptp[0].dwThreadId = 0;
        ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_ID;
      }

      if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_NAME) != 0)
      {
        ptp[0].bstrName = "Thread";
        ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_NAME;
      }

      if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0)
      {
        ptp[0].dwThreadState = (int)enum_THREADSTATE.THREADSTATE_STOPPED;
        ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
      }

      return VSConstants.S_OK;
    }

    int IDebugThread2.Resume(out uint pdwSuspendCount)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Resume");
      throw new NotImplementedException();
    }

    int IDebugThread2.SetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
    {
      //TODO set next line number
      Debug.WriteLine("AD7ProgramNode: Entering SetNextStatement");
      return VSConstants.S_OK;
    }

    int IDebugThread2.SetThreadName(string pszName)
    {
      Debug.WriteLine("AD7ProgramNode: Entering SetThreadName");
      throw new NotImplementedException();
    }

    int IDebugThread2.Suspend(out uint pdwSuspendCount)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Suspend");
      pdwSuspendCount = 0;
      return VSConstants.S_OK;
    }

    #endregion

    #region IEnumDebugThreads2 Members

    int IEnumDebugThreads2.Clone(out IEnumDebugThreads2 ppEnum)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Clone");
      throw new NotImplementedException();
    }

    int IEnumDebugThreads2.GetCount(out uint pcelt)
    {
      Debug.WriteLine("AD7ProgramNode: Entering GetCount");
      pcelt = 1;
      return VSConstants.S_OK;
    }

    int IEnumDebugThreads2.Next(uint celt, IDebugThread2[] rgelt, ref uint pceltFetched)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Next");
      rgelt[0] = this;
      pceltFetched = 1;
      return VSConstants.S_OK;
    }

    int IEnumDebugThreads2.Reset()
    {
      Debug.WriteLine("AD7ProgramNode: Entering Reset");
      return VSConstants.S_OK;
    }

    int IEnumDebugThreads2.Skip(uint celt)
    {
      Debug.WriteLine("AD7ProgramNode: Entering Skip");
      throw new NotImplementedException();
    }

    #endregion
  }
}
