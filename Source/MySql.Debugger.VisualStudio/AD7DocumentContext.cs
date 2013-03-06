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
  public class AD7DocumentContext : IDebugDocumentContext2, IDebugCodeContext2, IEnumDebugCodeContexts2, IDebugDocument2
  {
    private string _fileName;
    private TEXT_POSITION _beginPosition;
    private TEXT_POSITION _endPosition;
    private int _lineNumber;
    private RoutineScope _rs;

    public int LineNumber { get; set; }

    public AD7DocumentContext(string fileName, int lineNumber, TEXT_POSITION beginPosition, TEXT_POSITION endPosition, RoutineScope rs)
    {
      Debug.WriteLine("AD7DocumentContext: ctor");
      _fileName = fileName;
      _beginPosition = beginPosition;
      _endPosition = endPosition;
      _lineNumber = lineNumber;
      _rs = rs;
    }

    #region IDebugDocumentContext2 Members

    int IDebugDocumentContext2.Compare(enum_DOCCONTEXT_COMPARE Compare, IDebugDocumentContext2[] rgpDocContextSet, uint dwDocContextSetLen, out uint pdwDocContext)
    {
      Debug.WriteLine("AD7DocumentContext: Compare");
      throw new NotImplementedException();
    }

    int IDebugDocumentContext2.EnumCodeContexts(out IEnumDebugCodeContexts2 ppEnumCodeCxts)
    {
      Debug.WriteLine("AD7DocumentContext: EnumCodeContexts");
      ppEnumCodeCxts = this;
      return VSConstants.S_OK;
    }

    int IDebugDocumentContext2.GetDocument(out IDebugDocument2 ppDocument)
    {
      Debug.WriteLine("AD7DocumentContext: GetDocument");
      ppDocument = this;
      return VSConstants.S_OK;
    }

    int IDebugDocumentContext2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
    {
      Debug.WriteLine("AD7DocumentContext: GetLanguageInfo");
      pbstrLanguage = AD7Guids.LanguageName;
      pguidLanguage = Guid.Empty;
      return VSConstants.S_OK;
    }

    int IDebugDocumentContext2.GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
    {
      Debug.WriteLine("AD7DocumentContext: GetName");
      pbstrFileName = _fileName;
      return VSConstants.S_OK;
    }

    int IDebugDocumentContext2.GetSourceRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
    {
      Debug.WriteLine("AD7DocumentContext: GetSourceRange");
      return VSConstants.S_OK;
    }

    int IDebugDocumentContext2.GetStatementRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
    {
      Debug.WriteLine("AD7DocumentContext: GetStatementRange");
      pBegPosition[0] = _beginPosition; //TODO default(TEXT_POSITION);
      pEndPosition[0] = _endPosition; //TODO default(TEXT_POSITION);

      return VSConstants.S_OK;
    }

    int IDebugDocumentContext2.Seek(int nCount, out IDebugDocumentContext2 ppDocContext)
    {
      Debug.WriteLine("AD7DocumentContext: Seek");
      throw new NotImplementedException();
    }

    #endregion

    #region IDebugCodeContext2 Members

    int IDebugCodeContext2.Add(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
    {
      Debug.WriteLine("AD7DocumentContext: Add");
      throw new NotImplementedException();
    }

    int IDebugCodeContext2.Compare(enum_CONTEXT_COMPARE Compare, IDebugMemoryContext2[] rgpMemoryContextSet, uint dwMemoryContextSetLen, out uint pdwMemoryContext)
    {
      Debug.WriteLine("AD7DocumentContext: Compare");
      throw new NotImplementedException();
    }

    int IDebugCodeContext2.GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt)
    {
      Debug.WriteLine("AD7DocumentContext: GetDocumentContext");
      ppSrcCxt = this;
      return VSConstants.S_OK;
    }

    int IDebugCodeContext2.GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo)
    {
      Debug.WriteLine("AD7DocumentContext: GetInfo");
      pinfo[0].dwFields = 0;

      if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION) != 0)
      {
        pinfo[0].bstrFunction = _rs.OwningRoutine.Name;
        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION;
      }

      if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL) != 0)
      {
        pinfo[0].bstrModuleUrl = _fileName;
        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL;
      }

      if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS) != 0)
      {
        pinfo[0].bstrAddress = LineNumber.ToString();
        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS;
      }

      return VSConstants.S_OK;
    }

    int IDebugCodeContext2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
    {
      Debug.WriteLine("AD7DocumentContext: GetLanguageInfo");
      pbstrLanguage = AD7Guids.LanguageName;
      pguidLanguage = Guid.Empty;
      return VSConstants.S_OK;
    }

    int IDebugCodeContext2.GetName(out string pbstrName)
    {
      Debug.WriteLine("AD7DocumentContext: GetName");
      pbstrName = "TestName";
      return VSConstants.S_OK;
    }

    int IDebugCodeContext2.Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
    {
      Debug.WriteLine("AD7DocumentContext: Subtract");
      throw new NotImplementedException();
    }

    #endregion

    #region IDebugMemoryContext2 Members

    int IDebugMemoryContext2.Add(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
    {
      Debug.WriteLine("AD7DocumentContext: Add");
      throw new NotImplementedException();
    }

    int IDebugMemoryContext2.Compare(enum_CONTEXT_COMPARE Compare, IDebugMemoryContext2[] rgpMemoryContextSet, uint dwMemoryContextSetLen, out uint pdwMemoryContext)
    {
      Debug.WriteLine("AD7DocumentContext: Compare");
      throw new NotImplementedException();
    }

    int IDebugMemoryContext2.GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo)
    {
      Debug.WriteLine("AD7DocumentContext: GetInfo");
      throw new NotImplementedException();
    }

    int IDebugMemoryContext2.GetName(out string pbstrName)
    {
      Debug.WriteLine("AD7DocumentContext: GetName");
      throw new NotImplementedException();
    }

    int IDebugMemoryContext2.Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
    {
      Debug.WriteLine("AD7DocumentContext: Subtract");
      throw new NotImplementedException();
    }

    #endregion

    #region IEnumDebugCodeContexts2 Members

    int IEnumDebugCodeContexts2.Clone(out IEnumDebugCodeContexts2 ppEnum)
    {
      Debug.WriteLine("AD7DocumentContext: Clone");
      throw new NotImplementedException();
    }

    int IEnumDebugCodeContexts2.GetCount(out uint pcelt)
    {
      Debug.WriteLine("AD7DocumentContext: GetCount");
      pcelt = 1;
      return VSConstants.S_OK;
    }

    int IEnumDebugCodeContexts2.Next(uint celt, IDebugCodeContext2[] rgelt, ref uint pceltFetched)
    {
      Debug.WriteLine("AD7DocumentContext: Next");
      if (celt == 1)
      {
        rgelt[0] = this;
        pceltFetched = 1;
      }
      return VSConstants.S_OK;
    }

    int IEnumDebugCodeContexts2.Reset()
    {
      Debug.WriteLine("AD7DocumentContext: Reset");
      return VSConstants.S_OK;
    }

    int IEnumDebugCodeContexts2.Skip(uint celt)
    {
      Debug.WriteLine("AD7DocumentContext: Skip");
      return VSConstants.S_OK;
    }

    #endregion

    #region IDebugDocument2 Members

    int IDebugDocument2.GetDocumentClassId(out Guid pclsid)
    {
      throw new NotImplementedException();
    }

    int IDebugDocument2.GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
    {
      Debug.WriteLine("AD7DocumentContext: GetName");
      pbstrFileName = _fileName;
      return VSConstants.S_OK;
    }

    #endregion
  }
}
