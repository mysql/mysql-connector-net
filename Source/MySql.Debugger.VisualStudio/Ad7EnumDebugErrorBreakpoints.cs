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
  public class Ad7EnumDebugErrorBreakpoints : IEnumDebugErrorBreakpoints2
  {
    private List<IDebugErrorBreakpoint2> _errors;
    private int _next = -1;

    public Ad7EnumDebugErrorBreakpoints()
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: ctor");
      _errors = new List<IDebugErrorBreakpoint2>();
      _next = 0;
    }

    protected Ad7EnumDebugErrorBreakpoints(Ad7EnumDebugErrorBreakpoints enumerator)
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: ctor clone");
      IEnumDebugErrorBreakpoints2 e = (IEnumDebugErrorBreakpoints2)enumerator;
      e.Reset();
      _errors = new List<IDebugErrorBreakpoint2>();
      uint cnt;
      e.GetCount(out cnt);
      for (int i = 0; i < cnt; i++)
      {
        IDebugErrorBreakpoint2[] err = new IDebugErrorBreakpoint2[ 1 ];
        uint fetched = 1;
        e.Next(1, err, ref fetched);
        _errors.Add(err[0]);
      }
    }

    internal void Add(IDebugErrorBreakpoint2 error)
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: Add");
      _errors.Add(error);
    }

    int IEnumDebugErrorBreakpoints2.Clone(out IEnumDebugErrorBreakpoints2 ppEnum)
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: Clone");
      ppEnum = new Ad7EnumDebugErrorBreakpoints( this );
      return VSConstants.S_OK;
    }

    int IEnumDebugErrorBreakpoints2.GetCount(out uint pcelt)
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: Count");
      pcelt = ( uint )_errors.Count;
      return VSConstants.S_OK;
    }

    int IEnumDebugErrorBreakpoints2.Next(
      uint celt, IDebugErrorBreakpoint2[] rgelt, ref uint pceltFetched)
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: Next");
      if (celt == 0) return VSConstants.E_UNEXPECTED;

      int inext = 0;
      int max = Math.Min((int)celt + _next, this._errors.Count);
      while (inext + _next < max)
      {
        rgelt[inext] = _errors[ ( int )inext];
        inext++;
      }
      pceltFetched = (uint)(max - _next);
      _next += inext;
      return VSConstants.S_OK;
    }

    int IEnumDebugErrorBreakpoints2.Reset()
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: Reset");
      _next = 0;
      return VSConstants.S_OK;
    }

    int IEnumDebugErrorBreakpoints2.Skip(uint celt)
    {
      Debug.WriteLine("Ad7EnumDebugErrorBreakpoints: Skip");
      _next += (int)celt;
      return VSConstants.S_OK;
    }
  }
}
