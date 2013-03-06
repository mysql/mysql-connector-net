// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace MySql.Data.VisualStudio.Editors
{
  internal class VSCodeEditor
  {
    private ServiceBroker services;
    private IVsCodeWindow codeWindow;
    private IntPtr parentHandle;
    private IntPtr hwnd;
    private bool noContent;
    private IVsTextBuffer textBuffer;

    public VSCodeEditor()
    {
      hwnd = IntPtr.Zero;
      parentHandle = IntPtr.Zero;
    }

    public VSCodeEditor(IntPtr parent, ServiceBroker services)
      : this()
    {
      parentHandle = parent;
      this.services = services;
      CreateCodeWindow();
    }

    public VSCodeEditor(IOleServiceProvider psp)
      : this()
    {
      services = new ServiceBroker(psp);
      CreateCodeWindow();
    }

    public IntPtr Hwnd
    {
      get { return hwnd; }
    }

    public IVsCodeWindow CodeWindow
    {
      get { return codeWindow; }
    }

    public bool Dirty
    {
      get
      {
        uint flags;
        textBuffer.GetStateFlags(out flags);
        return (flags & (uint)BUFFERSTATEFLAGS.BSF_MODIFIED) != 0;
      }
      set
      {
        uint flags;
        textBuffer.GetStateFlags(out flags);
        if (value)
          flags |= (uint)BUFFERSTATEFLAGS.BSF_MODIFIED;
        else
          flags &= ~(uint)BUFFERSTATEFLAGS.BSF_MODIFIED;
        textBuffer.SetStateFlags(flags);
      }
    }

    public string Text
    {
      get
      {
        IVsTextLines lines = (IVsTextLines)textBuffer;
        int lineCount, lineLength;
        string text;
        lines.GetLineCount(out lineCount);
        lines.GetLengthOfLine(lineCount - 1, out lineLength);
        lines.GetLineText(0, 0, lineCount - 1, lineLength, out text);
        return text;
      }
      set
      {
        if (noContent)
          Initialize(value);
        else
          Replace(value);
        noContent = false;
      }
    }

    private void Initialize(string value)
    {
      IVsTextLines lines = (IVsTextLines)textBuffer;
      lines.InitializeContent(value, value.Length);
    }

    private void Replace(string value)
    {
      int endLine, endCol;
      IVsTextLines lines = (IVsTextLines)textBuffer;
      lines.GetLastLineIndex(out endLine, out endCol);

      IntPtr pText = Marshal.StringToCoTaskMemAuto(value);

      try
      {
        lines.ReplaceLines(0, 0, endLine, endCol, pText, value.Length, null);
      }
      finally
      {
        Marshal.FreeCoTaskMem(pText);
      }
    }

    private void CreateCodeWindow()
    {
      // create code window
      Guid guidVsCodeWindow = typeof(VsCodeWindowClass).GUID;
      codeWindow = services.CreateObject(services.LocalRegistry, guidVsCodeWindow, typeof(IVsCodeWindow).GUID) as IVsCodeWindow;

      CustomizeCodeWindow();

      // set buffer
      Guid guidVsTextBuffer = typeof(VsTextBufferClass).GUID;
      textBuffer = services.CreateObject(services.LocalRegistry, guidVsTextBuffer,
          typeof(IVsTextBuffer).GUID) as IVsTextBuffer;
      textBuffer.InitializeContent("ed", 2);

      Guid langSvc = new Guid(MySqlLanguageService.IID);

      int hr = textBuffer.SetLanguageServiceID(ref langSvc);
      if (hr != VSConstants.S_OK)
        Marshal.ThrowExceptionForHR(hr);

      hr = codeWindow.SetBuffer(textBuffer as IVsTextLines);
      if (hr != VSConstants.S_OK)
        Marshal.ThrowExceptionForHR(hr);

      // this is necessary for the adapters to work in VS2010
      Initialize(String.Empty);

      // create pane window
      IVsWindowPane windowPane = codeWindow as IVsWindowPane;
      hr = windowPane.SetSite(services.IOleServiceProvider);
      if (hr != VSConstants.S_OK)
        Marshal.ThrowExceptionForHR(hr);
      if (parentHandle != IntPtr.Zero)
      {
        hr = windowPane.CreatePaneWindow(parentHandle, 0, 0, 100, 100, out hwnd);
        if (hr != VSConstants.S_OK)
          Marshal.ThrowExceptionForHR(hr);
      }
    }

    private void CustomizeCodeWindow()
    {
      //// initialize code window
      //INITVIEW[] initView = new INITVIEW[1];
      //initView[0].fSelectionMargin = 0;
      //initView[0].IndentStyle = vsIndentStyle.vsIndentStyleSmart;
      //initView[0].fWidgetMargin = 0;
      //initView[0].fVirtualSpace = 0;
      //initView[0].fDragDropMove = 1;
      //initView[0].fVisibleWhitespace = 0;

      //IVsCodeWindowEx codeWindowEx = codeWindow as IVsCodeWindowEx;
      //int hr = codeWindowEx.Initialize((uint)_codewindowbehaviorflags.CWB_DISABLEDROPDOWNBAR,
      //    //(uint)_codewindowbehaviorflags.CWB_DISABLESPLITTER,
      //    0, null, null,
      //    (int)TextViewInitFlags.VIF_SET_WIDGET_MARGIN |
      //    (int)TextViewInitFlags.VIF_SET_SELECTION_MARGIN |
      //    (int)TextViewInitFlags2.VIF_ACTIVEINMODALSTATE |
      //    (int)TextViewInitFlags2.VIF_SUPPRESSBORDER |
      //    (int)TextViewInitFlags2.VIF_SUPPRESS_STATUS_BAR_UPDATE |
      //    (int)TextViewInitFlags2.VIF_SUPPRESSTRACKCHANGES,
      //    initView);

      //if (hr != VSConstants.S_OK)
      //    Marshal.ThrowExceptionForHR(hr);
    }
  }
}
