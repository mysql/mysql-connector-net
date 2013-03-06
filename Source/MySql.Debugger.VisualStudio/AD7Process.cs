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
  public class AD7Process : IDebugProcess3
  {
    private IDebugPort2 _port;

    public Guid Id { get; set; }
    public AD7ProgramNode Node { get; set; }

    public AD7Process(IDebugPort2 debugPort)
    {
      Id = Guid.NewGuid();
      _port = debugPort;
      Node = new AD7ProgramNode(this);
    }

    #region IDebugProcess3 Members

    public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
    {
      Debug.WriteLine("AD7Process: Attach");
      return VSConstants.E_NOTIMPL;
    }

    public int CanDetach()
    {
      Debug.WriteLine("AD7Process: CanDetach");
      return VSConstants.E_NOTIMPL;
    }

    public int CauseBreak()
    {
      Debug.WriteLine("AD7Process: CauseBreak");
      return VSConstants.E_NOTIMPL;
    }

    public int Continue(IDebugThread2 pThread)
    {
      Debug.WriteLine("AD7Process: Continue");
      return VSConstants.E_NOTIMPL;
    }

    public int Detach()
    {
      Debug.WriteLine("AD7Process: Detach");
      return VSConstants.E_NOTIMPL;
    }

    public int DisableENC(EncUnavailableReason reason)
    {
      throw new NotImplementedException();
    }

    public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
    {
      Debug.WriteLine("AD7Process: EnumPrograms");
      ppEnum = null;
      return VSConstants.E_NOTIMPL;
    }

    public int EnumThreads(out IEnumDebugThreads2 ppEnum)
    {
      Debug.WriteLine("AD7Process: EnumThreads");
      ppEnum = null;
      return VSConstants.E_NOTIMPL;
    }

    public int Execute(IDebugThread2 pThread)
    {
      Debug.WriteLine("AD7Process: Execute");
      return VSConstants.E_NOTIMPL;
    }

    public int GetAttachedSessionName(out string pbstrSessionName)
    {
      throw new NotImplementedException();
    }

    public int GetDebugReason(enum_DEBUG_REASON[] pReason)
    {
      throw new NotImplementedException();
    }

    public int GetENCAvailableState(EncUnavailableReason[] pReason)
    {
      throw new NotImplementedException();
    }

    public int GetEngineFilter(GUID_ARRAY[] pEngineArray)
    {
      throw new NotImplementedException();
    }

    public int GetHostingProcessLanguage(out Guid pguidLang)
    {
      throw new NotImplementedException();
    }

    public int GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
    {
      throw new NotImplementedException();
    }

    public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
    {
      throw new NotImplementedException();
    }

    public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
    {
      pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
      pProcessId[0].guidProcessId = Id;
      return VSConstants.S_OK;
    }

    public int GetPort(out IDebugPort2 ppPort)
    {
      ppPort = _port;
      return VSConstants.S_OK;
    }

    public int GetProcessId(out Guid pguidProcessId)
    {
      pguidProcessId = Id;
      return VSConstants.S_OK;
    }

    public int GetServer(out IDebugCoreServer2 ppServer)
    {
      throw new NotImplementedException();
    }

    public int SetHostingProcessLanguage(ref Guid guidLang)
    {
      throw new NotImplementedException();
    }

    public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
    {
      Debug.WriteLine("AD7Process: Step");
      
      return VSConstants.S_OK;
    }

    public int Terminate()
    {
      Debug.WriteLine("AD7Process: Terminate");
      return VSConstants.E_NOTIMPL;
    }

    #endregion
  }
}
