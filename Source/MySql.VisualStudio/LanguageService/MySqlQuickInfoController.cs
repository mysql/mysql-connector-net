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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MySql.Data.VisualStudio.LanguageService
{
  internal class MySqlQuickInfoController : IIntellisenseController
  {
    private ITextView m_textView;
    private IList<ITextBuffer> m_subjectBuffers;
    private MySqlQuickInfoControllerProvider m_provider;
    private IQuickInfoSession m_session;

    internal MySqlQuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, MySqlQuickInfoControllerProvider provider)
    {
      m_textView = textView;
      m_subjectBuffers = subjectBuffers;
      m_provider = provider;

      m_textView.MouseHover += this.OnTextViewMouseHover;
    }

    private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
    {
      //find the mouse position by mapping down to the subject buffer
      SnapshotPoint? point = m_textView.BufferGraph.MapDownToFirstMatch
        (new SnapshotPoint(m_textView.TextSnapshot, e.Position),
        PointTrackingMode.Positive,
        snapshot => m_subjectBuffers.Contains(snapshot.TextBuffer),
        PositionAffinity.Predecessor);

      if (point != null)
      {
        ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position,
        PointTrackingMode.Positive);

        if (!m_provider.QuickInfoBroker.IsQuickInfoActive(m_textView))
        {
          m_session = m_provider.QuickInfoBroker.TriggerQuickInfo(m_textView, triggerPoint, true);
        }
      }
    }

    public void Detach(ITextView textView)
    {
      if (m_textView == textView)
      {
        m_textView.MouseHover -= this.OnTextViewMouseHover;
        m_textView = null;
      }
    }

    public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
    {
    }

    public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
    {
    }
  }
}
