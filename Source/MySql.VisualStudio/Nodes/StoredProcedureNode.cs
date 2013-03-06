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
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


namespace MySql.Data.VisualStudio
{
  class StoredProcedureNode : DocumentNode, IVsTextBufferProvider
  {
    private string sql_mode;
    private bool isFunction;
    private VSCodeEditor editor;

    public StoredProcedureNode(DataViewHierarchyAccessor hierarchyAccessor, int id, bool isFunc) :
      base(hierarchyAccessor, id)
    {
      NodeId = isFunc ? "StoredFunction" : "StoredProcedure";
      isFunction = isFunc;
      NameIndex = 3;
      editor = new VSCodeEditor((IOleServiceProvider)hierarchyAccessor.ServiceProvider);
      DocumentNode.RegisterNode(this);
    }

    #region Properties

    public override string SchemaCollection
    {
      get { return "procedures"; }
    }

    public override bool Dirty
    {
      get { return editor.Dirty; }
      protected set { editor.Dirty = value; }
    }

    private bool IsFunction
    {
      get { return NodeId.ToLowerInvariant() == "storedfunction"; }
    }

    #endregion

    public static void CreateNew(DataViewHierarchyAccessor HierarchyAccessor, bool isFunc)
    {
      StoredProcedureNode node = new StoredProcedureNode(HierarchyAccessor, 0, isFunc);
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

    private string GetDropSQL(string procName)
    {
      procName = procName.Trim('`');
      return String.Format("DROP {0} `{1}`.`{2}`",
          IsFunction ? "FUNCTION" : "PROCEDURE", Database, procName);
    }

    private string GetNewRoutineText()
    {
      StringBuilder sb = new StringBuilder("CREATE ");
      sb.AppendFormat("{0} {1}()\r\n", isFunction ? "FUNCTION" : "PROCEDURE", Name);
      sb.Append("/*\r\n(\r\n");
      sb.Append("parameter1 INT\r\nOUT parameter2 datatype\r\n");
      sb.Append(")\r\n*/\r\n");
      if (isFunction)
        sb.Append("RETURNS /* datatype */\r\n");
      sb.Append("BEGIN\r\n");
      if (isFunction)
        sb.Append("RETURN /* return value */\r\n");
      sb.Append("END");
      return sb.ToString();
    }

    protected override void Load()
    {
      if (IsNew)
        editor.Text = GetNewRoutineText();
      else
      {
        try
        {
          string sql = GetStoredProcedureBody(String.Format(
              "SHOW CREATE {0} `{1}`.`{2}`",
              IsFunction ? "FUNCTION" : "PROCEDURE", Database, Name), out sql_mode);
          editor.Text = sql;
          Dirty = false;
        }
        catch (Exception ex)
        {
          MessageBox.Show("Unable to load the stored procedure for editing");
        }
      }
    }

    private string GetStoredProcedureBody(string sql, out string sql_mode)
    {
      string body = null;

      DbConnection conn = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
      try
      {

        DbCommand cmd = MySqlProviderObjectFactory.Factory.CreateCommand();
        cmd.Connection = conn;
        cmd.CommandText = sql;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          sql_mode = reader.GetString(1);
          body = reader.GetString(2);
        }
        return body;
      }
      finally
      {
        HierarchyAccessor.Connection.UnlockProviderObject();
      }
    }

    /// <summary>
    /// We override save here so we can change the sql from create to alter on
    /// first save
    /// </summary>
    /// <returns></returns>
    protected override bool Save()
    {
      // since MySQL doesn't support altering the body of a proc we have
      // to do some "magic"

      try
      {
        string sql = editor.Text.Trim();
        if (!IsNew)
        {
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
