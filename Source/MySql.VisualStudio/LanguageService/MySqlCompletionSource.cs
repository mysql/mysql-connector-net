// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using MySql.Parser;
using System.IO;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace MySql.Data.VisualStudio
{
  internal class MySqlCompletionSource : ICompletionSource
  {
    private MySqlCompletionSourceProvider m_sourceProvider;
    private ITextBuffer m_textBuffer;
    private List<Completion> m_compList;    

    public MySqlCompletionSource(MySqlCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
    {
      m_sourceProvider = sourceProvider;
      m_textBuffer = textBuffer;      
    }

    /// <summary>
    /// Removes a token using the enhanced token stream class.
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    private CommonTokenStream RemoveToken(string sql, SnapshotPoint snapPos)
    {
      Version ver = LanguageServiceUtil.GetVersion(LanguageServiceUtil.GetConnection().ServerVersion);
      TokenStreamRemovable tokens = LanguageServiceUtil.GetTokenStream(sql, ver);
      IToken tr = null;
      int position = snapPos.Position;
      tokens.Fill();
      if (!char.IsWhiteSpace(snapPos.GetChar()))
      {
        foreach (IToken t in tokens.GetTokens())
        {
          if ((t.StartIndex <= position) && (t.StopIndex >= position))
          {
            tr = t;
            break;
          }
        }
        tokens.Remove(tr);
      }
      return tokens;
    }    

    private ITree FindStmt(ITree t)
    {
      ITree treeStmt = null;      
      ITree child = null;
      for (int idx = 0; idx < t.ChildCount; idx++)
      {
        if (t.GetChild(idx).Text == "<EOF>") continue;
        child = t.GetChild(idx);
        if( child is CommonErrorNode ) continue;
        if ( (child.Text.Equals("create", StringComparison.OrdinalIgnoreCase)) ||
              (child.Text.Equals( "begin_end", StringComparison.OrdinalIgnoreCase ) ) )
        {
          treeStmt = FindStmt( child );
          if( treeStmt != null )
          {
            return treeStmt;
          }
        } else {
          if (child.TokenStartIndex == -1 || child.TokenStopIndex == -1) continue;
          if ((position >= tokens.Get(child.TokenStartIndex).StartIndex) &&
              ((position <= tokens.Get(child.TokenStopIndex).StopIndex) || 
               (( position - 1 ) <= tokens.Get(child.TokenStopIndex).StopIndex) ) )
          {
            treeStmt = child;
            break;
          }
        }
      }
      if (t.IsNil)
      {
        treeStmt = child;
      }
      return treeStmt;
    }

    private int position;
    private CommonTokenStream tokens;

    private void GetCompleteStatement(
      ITextSnapshot snapshot, SnapshotPoint snapPos, out StringBuilder sbErrors, out ITree treeStmt)
    {
      string sql = snapshot.GetText();
      treeStmt = null;
      sbErrors = new StringBuilder();
      position = snapPos.Position;
      tokens = RemoveToken(sql, snapPos);
      if (tokens.Count == 1 && tokens.Get(0).Type == MySQL51Lexer.EOF) return;
      MySQL51Parser.program_return r =
        LanguageServiceUtil.ParseSql(sql, false, out sbErrors, tokens);
      if (r == null) return;
      ITree t = r.Tree as ITree;
      treeStmt = t;
      // locate current statement's AST    
      if (t.IsNil)
      {
        ITree tmp = FindStmt(t);
        if (tmp != null) treeStmt = tmp;
      }
    }

    void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
    {
      DbConnection connection = LanguageServiceUtil.GetConnection();
      if( connection != null && !string.IsNullOrEmpty( connection.Database ) )      
      {
        if (session.TextView.Caret.Position.BufferPosition.Position == 0) return;
        SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
        ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
        TextExtent extent = navigator.GetExtentOfWord(currentPoint);
        ITrackingSpan span = currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        
        StringBuilder sbErrors;
        ITextSnapshot snapshot = currentPoint.Snapshot;
        ITextSnapshotLine line = currentPoint.GetContainingLine();
        int position = currentPoint.Position;
        // Get starting token
        ITree t;
        GetCompleteStatement(snapshot, currentPoint, out sbErrors, out t);
        if ( ( snapshot.Length == 0 ) || ( t == null )) return;
        
        string s = sbErrors.ToString();
        Match m = new Regex(@"Expected (?<item>.*)\.").Match( s );
        string expectedToken = "";
        if( m.Success )
        {
          expectedToken = m.Groups["item"].Value;
        }
        if (expectedToken == "table_factor" || 
          expectedToken == "simple_table_ref_no_alias_existing")
        {

          m_compList = new List<Completion>();
          DataTable schema = connection.GetSchema("Tables", new string[] { null, connection.Database });
          schema.Merge(connection.GetSchema("Views", new string[] { null, connection.Database }));
          string completionItem = null, completionItemUnq = null;

          foreach (DataRow row in schema.Rows)
          {
            completionItemUnq = row["TABLE_NAME"].ToString();
            completionItem = string.Format("`{0}`", row["TABLE_NAME"].ToString());
            m_compList.Add(new Completion(completionItemUnq, completionItem, completionItem, null, null));
          }          

          completionSets.Add(new CompletionSet(
            "MySqlTokens",    //the non-localized title of the tab
            "MySQL Tokens",    //the display title of the tab
            FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session),
            m_compList,
            null));
        }
        if (expectedToken == "proc_name")        
        {          
          m_compList = new List<Completion>();
          DataTable schema = connection.GetSchema("PROCEDURES WITH PARAMETERS", new string[] { null, connection.Database });
          DataView vi = schema.DefaultView;
          vi.Sort = "specific_name asc";
          string completionItem = null;
          string description = null;
          foreach (DataRowView row in vi)
          {
            if ("procedure".CompareTo(row["routine_type"].ToString().ToLower()) == 0)
            {
              completionItem = row["specific_name"].ToString();
              description = string.Format("procedure {0}.{1}({2})",
                row["routine_schema"], row["specific_name"], row["ParameterList"]);
              m_compList.Add(new Completion(completionItem, completionItem,
                description, null, null));
            }
          }

          completionSets.Add(new CompletionSet(
            "MySqlTokens",    //the non-localized title of the tab
            "MySQL Tokens",    //the display title of the tab
            FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session),
            m_compList,
            null));
        }
        else if (expectedToken == "column_name")
        {
          if (t != null)
          {
            if ((t.ChildCount != 0) ||
                ((t is CommonErrorNode) &&
                 ((t as CommonErrorNode).Text.Equals("SELECT",
                  StringComparison.CurrentCultureIgnoreCase))))
            {
              List<TableWithAlias> tables = new List<TableWithAlias>();
              ParserUtils.GetTables(t, tables);
              List<string> cols = GetColumns(connection, tables);
              CreateCompletionList(cols, session, completionSets);
            }
          }
        }
      }
    }

    private void CreateCompletionList(
      List<string> l, ICompletionSession session, IList<CompletionSet> completionSets)
    {
      m_compList = new List<Completion>();
      foreach (string c in l)
      {
        m_compList.Add(new Completion(c.Replace( "`", "" ), c, c, null, null));
      }
      completionSets.Add(new CompletionSet(
          "MySqlTokens",    //the non-localized title of the tab
          "MySQL Tokens",    //the display title of the tab
          FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session),
          m_compList,
          null));
    }

    private string BuildWhereGetColumns( string database, string sql, List<TableWithAlias> tables, out bool hasDbExplicit)
    {
      StringBuilder sb = new StringBuilder();
      string tableTemp = " and table_name = '{0}' ) or ";
      string schemaTemp = " ( table_schema = '{0}'";
      string defaultSchema = string.Format(schemaTemp, database);
      hasDbExplicit = false;
      if (tables.Count != 0)
      {
        foreach (TableWithAlias table in tables)
        {
          if (string.IsNullOrEmpty(table.Database))
          {
            sb.Append(defaultSchema).Append(string.Format(tableTemp, table.TableName));
          }
          else
          {
            hasDbExplicit = true;
            sb.Append(string.Format(schemaTemp, table.Database)).Append(string.Format(tableTemp, table.TableName));
          }
        }
        sb.Length = sb.Length - 4;
      }
      else
      {
        sb.Append(defaultSchema).Append(")");
      }
      return string.Format(sql, sb.ToString());
    }

    private Dictionary<string, List<string>> BuildColumnList(DbDataReader r, bool IncludeDb )
    {      
      Dictionary<string, List<string>> dicColumns = new Dictionary<string, List<string>>();
      List<string> cols = null;
      string prevTbl = "";
      while (r.Read())
      {
        string dbName = r.GetString(0)/*.ToLower() */;
        string tableName = r.GetString(1) /*.ToLower() */;
        string colName = r.GetString(2);
        string finalTableName = IncludeDb ? string.Format("{0}.{1}", dbName, tableName) :
          string.Format("{0}", tableName);

        if (prevTbl != finalTableName)
        {
          if (!string.IsNullOrEmpty(prevTbl))
          {
            dicColumns.Add(prevTbl, cols);
          }
          cols = new List<string>();
          prevTbl = finalTableName;
        }
        cols.Add(colName);
      }
      if (!string.IsNullOrEmpty(prevTbl))
      {
        dicColumns.Add(prevTbl, cols);
      }
      return dicColumns;
    }

    private List<string> GetColumns(DbConnection con, List<TableWithAlias> tables)
    {
      DbCommand cmd = con.CreateCommand();
      // information_schema.columns is available from MySql 5.0 and up.
      string sql =
        @"select table_schema, table_name, column_name from information_schema.columns 
          where ( 1 = 1 ) and ( {0} )
          order by table_schema, table_name, column_name";
      bool hasDbExplicit;
      cmd.CommandText = BuildWhereGetColumns( con.Database, sql, tables, out hasDbExplicit);      
      Dictionary<string, List<string>> dicColumns = null;
      DbDataReader r = cmd.ExecuteReader();
      try
      {
        dicColumns = BuildColumnList(r, tables.Count != 0 );
      }
      finally
      {
        r.Close();
      }
      List<string> columns = new List<string>();
      List<string> cols = new List<string>();
      if (tables.Count != 0)
      {
        foreach (TableWithAlias ta in tables)
        {
          string key = string.Format("{0}.{1}", 
            !string.IsNullOrEmpty( ta.Database )? ta.Database : con.Database.ToLower(), ta.TableName);
          // use db only if no alias defined and db was explicitely used.
          string tblTempl = (hasDbExplicit && string.IsNullOrEmpty(ta.Alias)) ? "`{0}`.`{1}`.`{2}`" : "`{1}`.`{2}`";
          dicColumns.TryGetValue(key, out cols);
          if (cols != null)
          {
            foreach (string col in cols)
            {
              columns.Add(string.Format(tblTempl, ta.Database,
                !string.IsNullOrEmpty(ta.Alias) ? ta.Alias : ta.TableName, col));
            }
          }
        }
      }
      else
      {
        string tblTempl = "`{0}`.`{1}`";
        foreach ( KeyValuePair<string, List<string>> kvp in dicColumns)
        {
          foreach (string s in kvp.Value)
          {
            columns.Add( string.Format( tblTempl, kvp.Key, s ));
          }
        }
      }
      return columns;
    }

    private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
    {
      SnapshotPoint? triggerPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot);

      ITextSnapshotLine line = triggerPoint.Value.GetContainingLine();
      SnapshotPoint start = triggerPoint.Value;

      while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
      {
        start -= 1;
      }
      ITextSnapshot snapshot = m_textBuffer.CurrentSnapshot;
      ITrackingSpan applicableTo = snapshot.CreateTrackingSpan( new SnapshotSpan(start, triggerPoint.Value), SpanTrackingMode.EdgeInclusive);
      return applicableTo;
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
