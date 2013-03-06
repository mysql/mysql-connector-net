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
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.Drawing;

namespace MySql.Data.VisualStudio.Editors 
{

  /// <summary>
  /// This class serves as 
  /// a) Command broker (subscribing once to the mappable keys of Visual Studio, instead of many times
  /// Solving a bug of backspace affecting the wrong mysql editor window when more than one is open).
  /// b) A repository to serve the connections of each SqlEditor for the Intellisense classifiers.
  /// </summary>
  internal class EditorBroker : IOleCommandTarget 
  {
    private Dictionary<string, VSCodeEditorWindow> dic = new Dictionary<string, VSCodeEditorWindow>();
    private EnvDTE.DTE Dte;
    private uint CmdTargetCookie;
    internal static EditorBroker Broker { get; private set; }        

    private EditorBroker(ServiceBroker sb)
    {
      // Register priority command target, this dispatches mappable keys like Enter, Backspace, Arrows, etc.
      int hr = sb.VsRegisterPriorityCommandTarget.RegisterPriorityCommandTarget(
        0, (IOleCommandTarget)this, out this.CmdTargetCookie);

      if (hr != VSConstants.S_OK)
        Marshal.ThrowExceptionForHR(hr);
      this.Dte = (EnvDTE.DTE)sb.Site.GetService(typeof(EnvDTE.DTE));
    }

    // this method must be externally synchronized
    internal static void CreateSingleton(ServiceBroker sb)
    {
      if (Broker != null)
        throw new InvalidOperationException( "The singleton broker has alreaby been created." );
      Broker = new EditorBroker(sb);
    }

    internal static void RegisterEditor( VSCodeEditorWindow editor )
    {
      Broker.dic.Add(editor.Parent.SqlEditor.Pane.DocumentPath, editor);
    }

    internal static void UnregisterEditor(VSCodeEditorWindow editor)
    {
      Broker.dic.Remove( editor.Parent.SqlEditor.Pane.DocumentPath );
    }

    /// <summary>
    /// Returns the DbConnection associated with the current mysql editor.
    /// </summary>
    /// <returns></returns>
    internal DbConnection GetCurrentConnection()
    {
      VSCodeEditorWindow editor;
      dic.TryGetValue(Dte.ActiveDocument.FullName, out editor);
      // Null here means No connection opened for the current mysql editor, or current active window not a mysql editor.
      if (editor == null) return null;
      else
      {
        return editor.Parent.SqlEditor.Connection;
      }
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
      VSCodeEditorWindow editor;
      if (Dte.ActiveDocument == null) 
        return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
      if (dic.TryGetValue(Dte.ActiveDocument.FullName, out editor))
        return ((IOleCommandTarget)editor).Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      else
        return ( int )Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
      VSCodeEditorWindow editor;
      if (Dte.ActiveDocument == null)
        return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
      if (dic.TryGetValue(Dte.ActiveDocument.FullName, out editor))
        return ((IOleCommandTarget)editor).QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
      else
        return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
    }
  }

  internal class VSCodeEditorWindow : NativeWindow,
      System.Windows.Forms.IMessageFilter, IOleCommandTarget, IDisposable
  {
    ServiceBroker services;
    VSCodeEditor coreEditor;
    private IOleCommandTarget cmdTarget;
    private uint cmdTargetCookie;
    internal VSCodeEditorUserControl Parent { get; set; }

    public VSCodeEditorWindow(ServiceBroker sb, UserControl parent)
    {
      Parent = (VSCodeEditorUserControl)parent;
      services = sb;
      coreEditor = new VSCodeEditor(parent.Handle, services);

      //Create window            
      IVsCodeWindow win = coreEditor.CodeWindow;
      cmdTarget = win as IOleCommandTarget;

      IVsTextView textView;
      int hr = win.GetPrimaryView(out textView);
      if (hr != VSConstants.S_OK)
        Marshal.ThrowExceptionForHR(hr);

      // assign the window handle
      IntPtr commandHwnd = textView.GetWindowHandle();
      AssignHandle(commandHwnd);

      //// Register priority command target, this dispatches mappable keys like Enter, Backspace, Arrows, etc.
      //hr = services.VsRegisterPriorityCommandTarget.RegisterPriorityCommandTarget(
      //    0, (IOleCommandTarget)CommandBroker.Broker, out cmdTargetCookie);

      //if (hr != VSConstants.S_OK)
      //  Marshal.ThrowExceptionForHR(hr);

      lock (typeof(EditorBroker))
      {
        if (EditorBroker.Broker == null) 
        {
          EditorBroker.CreateSingleton(services);
        }
        EditorBroker.RegisterEditor(this);
      }
      //Add message filter
      //Application.AddMessageFilter((System.Windows.Forms.IMessageFilter)this);
    }

    public VSCodeEditor CoreEditor
    {
      get { return coreEditor; }
    }

    public void SetWindowPos(Rectangle r)
    {
      if (coreEditor == null) return;
      NativeMethods.SetWindowPos(coreEditor.Hwnd, IntPtr.Zero, r.Left, r.Top, r.Width, r.Height, 0x4);
    }

    public void SetFocus()
    {
      if (coreEditor == null) return;
      NativeMethods.SetFocus(coreEditor.Hwnd);
    }

    public override void DestroyHandle()
    {
      ReleaseHandle();
    }

    #region IDisposable Members

    public void Dispose()
    {
      //Remove message filter
      //Application.RemoveMessageFilter((System.Windows.Forms.IMessageFilter)this);
      EditorBroker.UnregisterEditor(this);
      if (services != null)
      {
        // Remove this object from the list of the priority command targets.
        if (cmdTargetCookie != 0)
        {
          IVsRegisterPriorityCommandTarget register =
              services.VsRegisterPriorityCommandTarget;
          if (null != register)
          {
            int hr = register.UnregisterPriorityCommandTarget(cmdTargetCookie);
            if (hr != VSConstants.S_OK)
              Marshal.ThrowExceptionForHR(hr);
          }
          cmdTargetCookie = 0;
        }
        services = null;
      }
      if (coreEditor != null)
      {
        IVsCodeWindow win = coreEditor.CodeWindow;
        win.Close();
        coreEditor = null;
      }
    }

    #endregion

    #region IMessageFilter Members

    /// <summary>
    /// Filters out a message before it is dispatched
    /// </summary>
    /// <param name="m">The message to be dispatched. You cannot modify this message.</param>
    /// <returns>True to filter the message and stop it from being dispatched; false to allow the message to continue to the next filter or control.</returns>
    public bool PreFilterMessage(ref Message m)
    {
      //IVsFilterKeys2 performs advanced keyboard message translation
      IVsFilterKeys2 filterKeys2 = services.VsFilterKeys2;

      MSG[] messages = new MSG[1];
      messages[0].hwnd = m.HWnd;
      messages[0].lParam = m.LParam;
      messages[0].wParam = m.WParam;
      messages[0].message = (uint)m.Msg;

      Guid cmdGuid;
      uint cmdCode;
      int cmdTranslated;
      int keyComboStarts;

      int hr = filterKeys2.TranslateAcceleratorEx(messages,
          (uint)__VSTRANSACCELEXFLAGS.VSTAEXF_UseTextEditorKBScope //Translates keys using TextEditor key bindings. Equivalent to passing CMDUIGUID_TextEditor, CMDSETID_StandardCommandSet97, and guidKeyDupe for scopes and the VSTAEXF_IgnoreActiveKBScopes flag. 
          | (uint)__VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState,  //By default this function cannot be called when the shell is in a modal state, since command routing is inherently dangerous. However if you must access this in a modal state, specify this flag, but keep in mind that many commands will cause unpredictable behavior if fired. 
          0,
          null,
          out cmdGuid,
          out cmdCode,
          out cmdTranslated,
          out keyComboStarts);

      if (hr != VSConstants.S_OK)
        return false;

      return cmdTranslated != 0;
    }

    #endregion

    #region IOleCommandTarget Members

    /// <summary>
    /// Executes a specified command or displays help for a command.
    /// </summary>
    /// <param name="pguidCmdGroup">Pointer to command group</param>
    /// <param name="nCmdID">Identifier of command to execute</param>
    /// <param name="nCmdexecopt">Options for executing the command</param>
    /// <param name="pvaIn">Pointer to input arguments</param>
    /// <param name="pvaOut">Pointer to command output</param>
    /// <returns></returns>
    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {      
      return cmdTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
    }


    /// <summary>
    /// Queries the object for the status of one or more commands generated by user interface events.
    /// </summary>
    /// <param name="pguidCmdGroup">Pointer to command group</param>
    /// <param name="cCmds">Number of commands in prgCmds array</param>
    /// <param name="prgCmds">Array of commands</param>
    /// <param name="pCmdText">Pointer to name or status of command</param>
    /// <returns></returns>
    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
      return cmdTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    #endregion
  }

  internal static class NativeMethods
  {
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);
  }
}
