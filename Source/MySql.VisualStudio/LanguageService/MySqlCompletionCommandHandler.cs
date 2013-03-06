// Copyright © 2012, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using System.Runtime.InteropServices;

namespace MySql.Data.VisualStudio
{
  internal class MySqlCompletionCommandHandler : IOleCommandTarget
  {
    private IOleCommandTarget m_nextCommandHandler;
    private ITextView m_textView;
    private MySqlCompletionHandlerProvider m_provider;
    private ICompletionSession m_session;

    internal MySqlCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, MySqlCompletionHandlerProvider provider)
    {
        this.m_textView = textView;
        this.m_provider = provider;

        //add the command to the command chain
        textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
      if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
      {
        for (int i = 0; i < cCmds; i++)
        {        
          OLECMD cmd = prgCmds[i];
          switch ((VSConstants.VSStd2KCmdID)cmd.cmdID)
          {
            case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
            case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
            //case VSConstants.VSStd2KCmdID.COMPLETEWORD:
            //case VSConstants.VSStd2KCmdID.PARAMINFO:
            //case VSConstants.VSStd2KCmdID.QUICKINFO:
              prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
              return VSConstants.S_OK;
          }
        }
      }
      return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
      if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
      {
        return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }
      //make a copy of this so we can look at it after forwarding some commands
      uint commandID = nCmdID;
      char typedChar = char.MinValue;
      //make sure the input is a char before getting it
      if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
      {
        typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
      }

      //check for a commit character
      if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
          || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
          || (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar)))
      {
        //check for a a selection
        if (m_session != null && !m_session.IsDismissed)
        {
          //if the selection is fully selected, commit the current session
          if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected)
          {
            m_session.Commit();
            //also, don't add the character to the buffer
            return VSConstants.S_OK;
          }
          else
          {
            //if there is no selection, dismiss the session
            m_session.Dismiss();
          }
        }
      }
      else if ( 
        (nCmdID == (uint)VSConstants.VSStd2KCmdID.SHOWMEMBERLIST) ||
        (nCmdID == (uint)VSConstants.VSStd2KCmdID.AUTOCOMPLETE))        
      {
        if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
        {
          this.TriggerCompletion();
          // Can still be null, if there is no database connection
          if (m_session != null)
          {
            if (!m_session.IsStarted)
            {
              m_session.Start();
            }
          }
          return VSConstants.S_OK;
        }
      }
      
      //pass along the command so the char is added to the buffer
      int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      bool handled = false;
      
      if ( !typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar) )         
      {
        if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
        {
          this.TriggerCompletion();
          // Can still be null, if there is no database connection
          if (m_session != null)
          {
            m_session.Filter();
          }
        }
        else    //the completion session is already active, so just filter
        {
          m_session.Filter();
        }
        handled = true;
      }
      else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE   //redo the filter if there is a deletion
          || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE)
      {
        if (m_session != null && !m_session.IsDismissed)
        {
          m_session.Filter();
          handled = true;
        }
      }
      if (handled) return VSConstants.S_OK;
      return retVal;
    }

    private bool TriggerCompletion()
    {
      //the caret must be in a non-projection location 
      SnapshotPoint? caretPoint =
      m_textView.Caret.Position.Point.GetPoint(
      textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
      if (!caretPoint.HasValue)
      {
        return false;
      }

      m_session = m_provider.CompletionBroker.CreateCompletionSession
   (m_textView,
          caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
          true);

      //subscribe to the Dismissed event on the session 
      m_session.Dismissed += this.OnSessionDismissed;
      // Can still be null, if there is no database connection
      if (m_session != null)
      {
        m_session.Start();
      }

      return true;
    }

    private void OnSessionDismissed(object sender, EventArgs e)
    {
      m_session.Dismissed -= this.OnSessionDismissed;
      m_session = null;
    }
  }
}
