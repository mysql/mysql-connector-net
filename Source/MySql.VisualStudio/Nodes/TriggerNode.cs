// Copyright © 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using Microsoft.VisualStudio.Data;
using System.Windows.Forms;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;
using System.Data;
using MySql.Data.VisualStudio.Editors;
using MySql.Data.VisualStudio.Properties;
using MySql.Data.MySqlClient;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace MySql.Data.VisualStudio
{
  class TriggerNode : DocumentNode, IVsTextBufferProvider
  {
    private string sql_mode;
    private VSCodeEditor editor;
    string table;

    public TriggerNode(DataViewHierarchyAccessor hierarchyAccessor, int id) :
      base(hierarchyAccessor, id)
    {
      NodeId = "Trigger";
      NameIndex = 2;
      editor = new VSCodeEditor((IOleServiceProvider)hierarchyAccessor.ServiceProvider);
      DocumentNode.RegisterNode(this);
    }

    #region Properties

    public TableNode ParentTable;

    public override string SchemaCollection
    {
      get { return "triggers"; }
    }

    public override bool Dirty
    {
      get { return editor.Dirty; }
      protected set { editor.Dirty = value; }
    }

    #endregion

    public static void CreateNew(DataViewHierarchyAccessor HierarchyAccessor, TableNode parent)
    {
      TriggerNode node = new TriggerNode(HierarchyAccessor, 0);
      node.ParentTable = parent;
      node.Edit();
    }

    public override object GetEditor()
    {
      return editor;
    }

    public override string GetDropSQL()
    {
      return GetDropSQL(Name);
    }

    public override string GetSaveSql()
    {
      return editor.Text;
    }

    private string GetDropSQL(string triggerName)
    {
      triggerName = triggerName.Trim('`');
      return String.Format("DROP TRIGGER `{0}`.`{1}`", Database, triggerName);
    }

    private string GetNewTriggerText()
    {
      StringBuilder sb = new StringBuilder("CREATE TRIGGER ");
      sb.AppendFormat("{0}\r\n", Name);
      sb.AppendFormat("/* [BEFORE|AFTER] [INSERT|UPDATE|DELETE] */\r\n");
      sb.AppendFormat("ON {0}\r\n", ParentTable.Name);
      sb.Append("FOR EACH ROW\r\n");
      sb.Append("BEGIN\r\n");
      sb.Append("/* sql commands go here */\r\n");
      sb.Append("END");
      return sb.ToString();
    }

    protected override void Load()
    {
      if (IsNew)
      {
        editor.Text = GetNewTriggerText();
        sql_mode = string.Empty;
      }
      else
      {
        try
        {
          DataTable dt = GetDataTable(String.Format("SHOW CREATE TRIGGER `{0}`.`{1}`",
                  Database, Name));

          sql_mode = dt.Rows[0][1] as string;
          string sql = dt.Rows[0][2] as string;
          byte[] bytes = UTF8Encoding.UTF8.GetBytes(sql);
          editor.Text = ChangeSqlTypeTo(sql, "ALTER");
          Dirty = false;
          OnDataLoaded();
        }
        catch (Exception ex)
        {
          MessageBox.Show("Unable to load object with error: " + ex.Message);
        }
      }
      table = GetTargetedTable(editor.Text);
    }

    /// <summary>
    /// We override save here so we can change the sql from create to alter on
    /// first save
    /// </summary>
    /// <returns></returns>
    protected override bool Save()
    {
      try
      {
        string sql = editor.Text.Trim();
        if (!IsNew)
        {
          MakeSureWeAreNotChangingTables(sql);

          // first we need to check the syntax of our changes.  THis will throw
          // an exception if the syntax is bad
          CheckSyntax();

          sql = ChangeSqlTypeTo(editor.Text.Trim(), "CREATE");
          ExecuteSQL(GetDropSQL(Name));
        }
        ExecuteSQL(sql);
        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }
    }

    /// <summary>
    ///  This method will attempt to extract the table this trigger script is targeting 
    ///  and make sure it matches the table the trigger was originally created for.  We do 
    ///  this because we don't want the user using an 'ALTER' script to move a trigger to a 
    ///  different table
    /// </summary>
    private void MakeSureWeAreNotChangingTables(string sql)
    {
      string newTable = GetTargetedTable(sql);
      if (table != null && newTable != null &&
          newTable.ToLowerInvariant() != table.ToLowerInvariant())
        throw new InvalidOperationException(
            String.Format(Resources.AlterTriggerOnWrongTable, Name, newTable));
    }

    private string GetTargetedTable(string sql)
    {
      MySqlTokenizer tokenizer = new MySqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      tokenizer.AnsiQuotes = sql_mode.ToLowerInvariant().Contains("ansi_quotes");
      tokenizer.BackslashEscapes = !sql_mode.ToLowerInvariant().Contains("no_backslash_escapes");

      string token = null;
      while (token != "ON" || tokenizer.Quoted)
        token = tokenizer.NextToken();

      string tableName = tokenizer.NextToken();
      if (tokenizer.NextToken() == ".")
        tableName = tokenizer.NextToken();
      if (tableName.StartsWith("`", StringComparison.Ordinal))
        return tableName.Trim('`');
      if (tableName.StartsWith("\"", StringComparison.Ordinal) && tokenizer.AnsiQuotes)
        return tableName.Trim('"');
      return tableName;
    }

    private void CheckSyntax()
    {
      string sql = editor.Text.Trim();
      sql = ChangeSqlTypeTo(sql, "CREATE");
      try
      {
        ExecuteSQL(sql);
        sql = GetDropSQL(GetCurrentName());
        ExecuteSQL(sql);
      }
      catch (Exception ex)
      {
        if (ex.Message.Contains("syntax"))
          throw;
      }
    }

    private string ChangeSqlTypeTo(string sql, string type)
    {
      int index = sql.IndexOf(' ');
      string startingCommand = sql.Substring(0, index).ToUpperInvariant();
      if (startingCommand != "CREATE" && startingCommand != "ALTER")
        throw new Exception(Resources.UnableToExecuteProcScript);
      return type + sql.Substring(index);
    }

    protected override string GetCurrentName()
    {
      return LanguageServiceUtil.GetRoutineName(editor.Text);
    }

    #region IVsTextBufferProvider Members

    private IVsTextLines buffer;

    int IVsTextBufferProvider.GetTextBuffer(out IVsTextLines ppTextBuffer)
    {
      if (buffer == null)
      {
        Type bufferType = typeof(IVsTextLines);
        Guid riid = bufferType.GUID;
        Guid clsid = typeof(VsTextBufferClass).GUID;
        buffer = (IVsTextLines)MySqlDataProviderPackage.Instance.CreateInstance(
                             ref clsid, ref riid, typeof(object));
      }
      ppTextBuffer = buffer;
      return VSConstants.S_OK;
    }

    int IVsTextBufferProvider.LockTextBuffer(int fLock)
    {
      return VSConstants.S_OK;
    }

    int IVsTextBufferProvider.SetTextBuffer(IVsTextLines pTextBuffer)
    {
      return VSConstants.S_OK;
    }

    #endregion
  }
}
