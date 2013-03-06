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
using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MySql.Data.VisualStudio.LanguageService
{
  internal class MySqlQuickInfoSource : IQuickInfoSource
  {
    private MySqlQuickInfoSourceProvider m_provider;
    private ITextBuffer m_subjectBuffer;
    private Dictionary<string, string> m_dictionary;

    public MySqlQuickInfoSource(MySqlQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
    {
      m_provider = provider;
      m_subjectBuffer = subjectBuffer;
    }

    private void LoadDictionary( DbConnection con )
    {
      string sql = @"
        select table_name as object_name, 'table' as type from information_schema.tables where table_schema = '{0}'
        union all
        select table_name as object_name, 'view' as type from information_schema.views where table_schema = '{0}'
        union all
        select routine_name as object_name, routine_type as type from information_schema.routines where routine_schema = '{0}';
        ";
      DbCommand cmd = con.CreateCommand();
      cmd.CommandText = string.Format(sql, con.Database);
      m_dictionary = new Dictionary<string, string>();
      DbDataReader r = cmd.ExecuteReader();
      try
      {
        while (r.Read())
        {
          string objectName = r.GetString(0);
          string type = r.GetString( 1 ).ToLower();
          string description = type + " " + objectName;
          if (m_dictionary.ContainsKey(objectName))
          {
            if (string.Compare(type, "view", StringComparison.OrdinalIgnoreCase) == 0)
            {
              m_dictionary[objectName] = description;
            }
          }
          else
          {
            m_dictionary.Add(objectName, description);
          }
        }
      }
      finally
      {
        r.Close();
      }
    }

    public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
    {

      applicableToSpan = null;
      if (m_dictionary == null)
      {
        DbConnection connection = LanguageServiceUtil.GetConnection();
        if (connection == null || string.IsNullOrEmpty( connection.Database )) return;
        LoadDictionary(connection);
      }
      // Map the trigger point down to our buffer.
      SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_subjectBuffer.CurrentSnapshot);
      if (!subjectTriggerPoint.HasValue)
      {
        applicableToSpan = null;
        return;
      }

      ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
      SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

      //look for occurrences of our QuickInfo words in the span
      ITextStructureNavigator navigator =
        m_provider.NavigatorService.GetTextStructureNavigator(m_subjectBuffer);
      TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);
      string searchText = extent.Span.GetText();

      foreach (string key in m_dictionary.Keys)
      {        
        if( key == searchText )
        {
          int foundIndex = 0;
          int span = querySpan.Start.Add(foundIndex).Position;
          applicableToSpan = currentSnapshot.CreateTrackingSpan
              (
              span, 
              Math.Min( 
                span + currentSnapshot.Length - subjectTriggerPoint.Value.Position,
                currentSnapshot.Length - span ),
                SpanTrackingMode.EdgeInclusive
              );

          string value;
          m_dictionary.TryGetValue(key, out value);
          if (value != null)
            qiContent.Add(value);
          else
            qiContent.Add("");

          return;
        }
      }

      applicableToSpan = null;
    }

    private bool m_isDisposed;
    public void Dispose()
    {
      if (!m_isDisposed)
      {
        GC.SuppressFinalize(this);
        m_isDisposed = true;
      }
    }
  }
}
