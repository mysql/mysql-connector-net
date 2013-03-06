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
  public class AD7StackFrame : IDebugStackFrame2
  {
    private AD7ProgramNode _node;
    internal AD7ProgramNode Node { get { return _node; } }
    internal RoutineScope _rs;

    private AD7DocumentContext _docContext;

    public AD7StackFrame(AD7ProgramNode node, RoutineScope rs)
    {
      Debug.WriteLine("AD7StackFrame: ctor");
      _rs = rs;
      _node = node;
      //Breakpoint bp = DebuggerManager.Instance.CurrentBreakpoint.CoreBreakpoint;
      Breakpoint bp = rs.CurrentPosition;
      TEXT_POSITION pos = new TEXT_POSITION() { dwLine = (uint)( bp.StartLine - 1), dwColumn = ( uint )( bp.StartColumn ) };
      TEXT_POSITION endPos = new TEXT_POSITION();
      endPos.dwLine = ( uint )( bp.EndLine - 1 );
      endPos.dwColumn = ( uint )( bp.EndColumn );
      _docContext = new AD7DocumentContext( _rs.GetFileName(), -1, pos, endPos, rs);
      _node.FileName = _node.Debugger.Debugger.CurrentScope.FileName;
    }

    #region IDebugStackFrame2 Members

    int IDebugStackFrame2.EnumProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, uint nRadix, ref Guid guidFilter, uint dwTimeout, out uint pcelt, out IEnumDebugPropertyInfo2 ppEnum)
    {
      Debug.WriteLine("AD7StackFrame: EnumProperties");
      pcelt = 0;
      ppEnum = new AD7PropertyCollection(_node, _rs);
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetCodeContext(out IDebugCodeContext2 ppCodeCxt)
    {
      Debug.WriteLine("AD7StackFrame: GetCodeContext");
      ppCodeCxt = _docContext;
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetDebugProperty(out IDebugProperty2 ppProperty)
    {
      Debug.WriteLine("AD7StackFrame: GetDebugProperty");
      ppProperty = new AD7Property(_node);
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetDocumentContext(out IDebugDocumentContext2 ppCxt)
    {
      Debug.WriteLine("AD7StackFrame: GetDocumentContext");
      ppCxt = _docContext;
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetExpressionContext(out IDebugExpressionContext2 ppExprCxt)
    {
      Debug.WriteLine("AD7StackFrame: GetExpressionContext");
      // Add expression context to evaluate watches.
      ppExprCxt = new AD7DebugExpressionContext(this);
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, FRAMEINFO[] pFrameInfo)
    {
      // TODO: Update with actual stack frame.
      Debug.WriteLine("AD7StackFrame: GetInfo");
      var frameInfo = pFrameInfo[0];

      if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME) != 0)
      {
        frameInfo.m_bstrFuncName = _rs.OwningRoutine.Name;
        frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME;
      }

      if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
      {
        frameInfo.m_bstrModule = "Module";
        frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE;
      }

      if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FRAME) != 0)
      {
        frameInfo.m_pFrame = this;
        frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FRAME;
      }

      if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO) != 0)
      {
        frameInfo.m_fHasDebugInfo = 1;
        frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO;
      }

      if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_STALECODE) != 0)
      {
        frameInfo.m_fStaleCode = 0;
        frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STALECODE;
      }

      if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_LANGUAGE) != 0)
      {
        frameInfo.m_bstrLanguage = AD7Guids.LanguageName;
        frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_LANGUAGE;
      }

      pFrameInfo[0] = frameInfo;

      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
    {
      Debug.WriteLine("AD7StackFrame: GetLanguageInfo");
      pbstrLanguage = AD7Guids.LanguageName;
      pguidLanguage = Guid.Empty;
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetName(out string pbstrName)
    {
      Debug.WriteLine("AD7StackFrame: GetName");
      pbstrName = "Test StackFrame";
      return VSConstants.S_OK;
    }

    int IDebugStackFrame2.GetPhysicalStackRange(out ulong paddrMin, out ulong paddrMax)
    {
      Debug.WriteLine("AD7StackFrame: GetPhysicalStackRange");
      throw new NotImplementedException();
    }

    int IDebugStackFrame2.GetThread(out IDebugThread2 ppThread)
    {
      Debug.WriteLine("AD7StackFrame: GetThread");
      throw new NotImplementedException();
    }

    #endregion
  }

  public class AD7StackFrameCollection : List<AD7StackFrame>, IEnumDebugFrameInfo2
  {
    private AD7ProgramNode _node;

    public AD7StackFrameCollection(AD7ProgramNode node)
    {
      Debug.WriteLine("AD7StackFrameCollection: ctor");
      _node = node;
      IEnumerable<RoutineScope> frames = node.Debugger.Debugger.GetScopes();
      //this.Add(new AD7StackFrame(node));
      foreach (RoutineScope rs in frames)
      {
        if (rs.OwningRoutine.Name == "<main>") continue;
        this.Add(new AD7StackFrame(node, rs));
      }
    }

    #region IEnumDebugFrameInfo2 Members

    int IEnumDebugFrameInfo2.Clone(out IEnumDebugFrameInfo2 ppEnum)
    {
      Debug.WriteLine("AD7StackFrameCollection: Clone");
      throw new NotImplementedException();
    }

    int IEnumDebugFrameInfo2.GetCount(out uint pcelt)
    {
      Debug.WriteLine("AD7StackFrameCollection: GetCount");
      //pcelt = 1;
      pcelt = (uint)this.Count;
      return VSConstants.S_OK;
    }

    private int _nextElement = 0;

    int IEnumDebugFrameInfo2.Next(uint celt, FRAMEINFO[] rgelt, ref uint pceltFetched)
    {
      Debug.WriteLine("AD7StackFrameCollection: Next");
      if (_nextElement == this.Count)
      {
        pceltFetched = 0;
        return VSConstants.S_OK;
      }
      int _inext = 0;
      int max = Math.Min( ( int )celt + _nextElement, this.Count);
      while (_inext + _nextElement < max)
      {
        rgelt[ _inext ].m_dwValidFields = (enum_FRAMEINFO_FLAGS.FIF_LANGUAGE
          | enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO | enum_FRAMEINFO_FLAGS.FIF_STALECODE
          | enum_FRAMEINFO_FLAGS.FIF_FRAME | enum_FRAMEINFO_FLAGS.FIF_FUNCNAME
          | enum_FRAMEINFO_FLAGS.FIF_MODULE);
        rgelt[ _inext ].m_fHasDebugInfo = 1;
        rgelt[ _inext ].m_fStaleCode = 0;
        rgelt[ _inext ].m_bstrLanguage = AD7Guids.LanguageName;
        rgelt[_inext].m_bstrFuncName = this[_inext + _nextElement]._rs.OwningRoutine.Name;
        rgelt[_inext].m_pFrame = this[_inext + _nextElement];
        _inext++;
      }
      
      //TODO implement??? rgelt[0].m_pModule = _node;
      pceltFetched = ( uint )( max - _nextElement );
      _nextElement += _inext;
      return VSConstants.S_OK;
    }

    int IEnumDebugFrameInfo2.Reset()
    {
      Debug.WriteLine("AD7StackFrameCollection: Reset");
      _nextElement = 0;
      return VSConstants.S_OK;
    }

    int IEnumDebugFrameInfo2.Skip(uint celt)
    {
      Debug.WriteLine("AD7StackFrameCollection: Skip");
      _nextElement += (int)celt;
      throw new NotImplementedException();
    }

    #endregion
  }
}
