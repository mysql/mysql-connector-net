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
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.Windows.Forms;

namespace MySql.Debugger.VisualStudio
{
  [ComVisible(true)]
  [Guid(AD7Guids.CLSIDString)]
  public class AD7Engine : IDebugEngine2, IDebugEngineLaunch2
  {
    private AD7ProgramNode _node;
    private AD7Events _events;
    private AD7Breakpoint _breakpoint;

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern IntPtr GetForegroundWindow();

    #region IDebugEngine2 Members

    int IDebugEngine2.Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms, IDebugEventCallback2 pCallback, enum_ATTACH_REASON dwReason)
    {
      Debug.WriteLine("AD7Engine Attach");
      Guid id;

      if (( DebuggerManager.Instance != null ) && ( DebuggerManager.Instance.Debugger.IsRunning))
      {
        // If already running, abort.
        MessageBox.Show("Cannot start MySql Debugger. A MySql Debug session is already running", "Error");
        return HRESULT.E_ATTACH_FAILED_ABORT_SILENTLY;
      }

      rgpPrograms[0].GetProgramId(out id);
      if (id == Guid.Empty)
      {
        return VSConstants.E_NOTIMPL;
      }

      _events = new AD7Events(this, pCallback);

      try
      {
        DebuggerManager.Init(_events, _node, _breakpoint);
      }
      catch (Exception ex)
      {
        MessageBox.Show(_node.ParentWindow, ex.GetBaseException().Message, "Debugger Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return HRESULT.E_ATTACH_FAILED_ABORT_SILENTLY;
      }

      System.Threading.Thread thread = new System.Threading.Thread(() =>
      {
        DebuggerManager debugger = DebuggerManager.Instance;
        _node.Debugger = debugger;
        debugger.SteppingType = SteppingTypeEnum.StepInto;
        debugger.Breakpoint = new AD7Breakpoint(_node, _events);
        debugger.OnEndDebugger += () => { _events.ProgramDestroyed(_node); };
        debugger.Debugger.RestoreAtExit = true;
        debugger.Run();
      });
      thread.SetApartmentState(System.Threading.ApartmentState.STA);
      thread.Start();

      _node.Id = id;
      _events.EngineCreated();
      _events.ProgramCreated(_node);
      _events.EngineLoaded();
      _events.DebugEntryPoint();

      return VSConstants.S_OK;
    }

    int IDebugEngine2.CauseBreak()
    {
      Debug.WriteLine("AD7Engine CauseBreak");
      return ((IDebugProgram2)this).CauseBreak();
    }

    int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 pEvent)
    {
      Debug.WriteLine("AD7Engine ContinueFromSynchronousEvent");
      return VSConstants.S_OK;
    }

    int IDebugEngine2.CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
    {
      Debug.WriteLine("AD7Engine CreatePendingBreakpoint");
      _breakpoint = new AD7Breakpoint(_node, _events, pBPRequest);
      ppPendingBP = _breakpoint;
      //_events.Breakpoint(_node, _breakpoint);

      return VSConstants.S_OK;
    }

    int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
    {
      Debug.WriteLine("AD7Engine DestroyProgram");
      return (HRESULT.E_PROGRAM_DESTROY_PENDING);
    }

    int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 ppEnum)
    {
      Debug.WriteLine("AD7Engine EnumPrograms");
      throw new NotImplementedException();
    }

    int IDebugEngine2.GetEngineId(out Guid pguidEngine)
    {
      Debug.WriteLine("AD7Engine GetEngineId");
      pguidEngine = AD7Guids.EngineGuid;
      return VSConstants.S_OK;
    }

    int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
    {
      Debug.WriteLine("AD7Engine RemoveAllSetExceptions");
      throw new NotImplementedException();
    }

    int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
    {
      Debug.WriteLine("AD7Engine RemoveSetException");
      throw new NotImplementedException();
    }

    int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
    {
      Debug.WriteLine("AD7Engine SetException");
      throw new NotImplementedException();
    }

    int IDebugEngine2.SetLocale(ushort wLangID)
    {
      Debug.WriteLine("AD7Engine SetLocale");
      return VSConstants.S_OK;
    }

    int IDebugEngine2.SetMetric(string pszMetric, object varValue)
    {
      Debug.WriteLine("AD7Engine SetMetric");
      throw new NotImplementedException();
    }

    int IDebugEngine2.SetRegistryRoot(string pszRegistryRoot)
    {
      Debug.WriteLine("AD7Engine SetRegistryRoot");
      return VSConstants.S_OK;
    }

    #endregion


    #region IDebugEngineLaunch2 Members

    int IDebugEngineLaunch2.CanTerminateProcess(IDebugProcess2 pProcess)
    {
      Debug.WriteLine("AD7Engine CanTerminateProcess");
      return VSConstants.S_OK;
    }

    int IDebugEngineLaunch2.LaunchSuspended(string pszServer, IDebugPort2 pPort, string pszExe, string pszArgs, string pszDir, string bstrEnv, string pszOptions, enum_LAUNCH_FLAGS dwLaunchFlags, uint hStdInput, uint hStdOutput, uint hStdError, IDebugEventCallback2 pCallback, out IDebugProcess2 ppProcess)
    {
      Debug.WriteLine("AD7Engine LaunchSuspended");
      ppProcess = new AD7Process(pPort);
      _node = (ppProcess as AD7Process).Node;
      _node.FileName = pszExe;
      _node.ConnectionString = pszArgs;
      _events = new AD7Events(this, pCallback);

      // Gets active window handler to use in modals
      IntPtr handler = GetForegroundWindow();
      _node.ParentWindow = new System.Windows.Forms.NativeWindow();
      _node.ParentWindow.AssignHandle(handler);

      return VSConstants.S_OK;
    }

    int IDebugEngineLaunch2.ResumeProcess(IDebugProcess2 pProcess)
    {
      Debug.WriteLine("AD7Engine ResumeProcess");
      if (pProcess is AD7Process)
      {
        IDebugPort2 port;
        pProcess.GetPort(out port);

        var defaultPort = (IDebugDefaultPort2)port;
        IDebugPortNotify2 notify;

        defaultPort.GetPortNotify(out notify);

        AD7ProgramNode node = (pProcess as AD7Process).Node;
        int result = notify.AddProgramNode(node);
        if (node.Debugger == null)
          return VSConstants.E_ABORT;

        return VSConstants.S_OK;
      }

      return VSConstants.E_UNEXPECTED;
    }

    int IDebugEngineLaunch2.TerminateProcess(IDebugProcess2 pProcess)
    {
      _node.Debugger.DoEndProgram();
      Debug.WriteLine("AD7Engine TerminateProcess");

      IDebugPort2 port;
      pProcess.GetPort(out port);

      var defaultPort = (IDebugDefaultPort2)port;
      IDebugPortNotify2 notify;

      defaultPort.GetPortNotify(out notify);

      notify.RemoveProgramNode(_node);
      
      return VSConstants.S_OK;
    }

    #endregion
  }
}
