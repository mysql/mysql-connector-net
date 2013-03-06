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
using System.Windows.Forms;
using MySql.Data.VisualStudio.Properties;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Data;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.Data.Common;
using System.Resources;
using System.Globalization;
using System.Data;
using Microsoft.VisualStudio.Data.AdoDotNet;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using MySql.Data.VisualStudio.Editors;
using Microsoft.VisualStudio.Shell;

namespace MySql.Data.VisualStudio
{
  abstract class BaseNode
  {
    protected Guid editorGuid;
    protected Guid commandGroupGuid;
    protected string name;
    private static string defaultStorageEngine;
    public DataViewHierarchyAccessor HierarchyAccessor;
    public bool IsNew;

    public BaseNode(DataViewHierarchyAccessor hierarchyAccessor, int id)
    {
      HierarchyAccessor = hierarchyAccessor;
      ItemId = id;
      IsNew = ItemId == 0;

      // set the server and database from our active connection
      DbConnection conn = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
      Server = conn.DataSource;
      Database = conn.Database;
      HierarchyAccessor.Connection.UnlockProviderObject();

      if (!IsNew)
        Name = hierarchyAccessor.GetNodeName(id);
      else
        ItemId = hierarchyAccessor.CreateObjectNode();

      NameIndex = 2;
      commandGroupGuid = VSConstants.GUID_TextEditorFactory;
      editorGuid = Guid.Empty;
    }

    #region Properties

    public string Name
    {
      get
      {
        if (name == null)
          Name = GenerateUniqueName();
        return name;
      }
      protected set
      {
        name = value;
      }
    }

    private int _itemId;
    public int ItemId
    {
      get { return _itemId; }
      protected set { _itemId = value; }
    }

    private string _nodeId;
    public string NodeId
    {
      get { return _nodeId; }
      protected set { _nodeId = value; }
    }

    private string _server;
    public string Server
    {
      get { return _server; }
      private set { _server = value; }
    }

    private string _database;
    public string Database
    {
      get { return _database; }
      private set { _database = value; }
    }

    private bool _dirty;
    public virtual bool Dirty
    {
      get { return _dirty; }
      protected set { _dirty = value; }
    }

    protected internal string Moniker
    {
      get { return String.Format("mysql://{0}/{1}/{2}", Server, Database, Name); }
    }

    private int _nameIndex;
    public int NameIndex
    {
      get { return _nameIndex; }
      protected set { _nameIndex = value; }
    }

    public virtual string SchemaCollection
    {
      get { return NodeId + "s"; }
    }

    public virtual string LocalizedTypeString
    {
      get
      {
        return Resources.ResourceManager.GetString("Type_" + NodeId);
      }
    }

    public string DefaultStorageEngine
    {
      get
      {
        if (defaultStorageEngine == null)
        {
          DataTable dt = GetDataTable("SHOW VARIABLES LIKE 'storage_engine'");
          defaultStorageEngine = "MyISAM";
          if (dt != null && dt.Rows.Count == 1)
            defaultStorageEngine = (string)dt.Rows[0][1];
        }
        return defaultStorageEngine;
      }
    }

    #endregion

    #region Virtuals

    public virtual void ExecuteCommand(int command)
    {
      switch ((uint)command)
      {
        case 12291:  // design
        case PkgCmdIDList.cmdAlterTable:
        case PkgCmdIDList.cmdAlterTrigger:
        case PkgCmdIDList.cmdAlterProcedure:
        case PkgCmdIDList.cmdAlterView:
          Alter();
          break;

        case (uint)VSConstants.VSStd97CmdID.Delete:
          Drop();
          break;

        case PkgCmdIDList.cmdDebugProcedure:
          LaunchDebugger();
          break;
      }
    }


    public virtual void Edit()
    {
      if (!HierarchyAccessor.ActivateDocumentIfOpen(Moniker))
        OpenEditor();
    }

    public virtual object GetEditor()
    {
      throw new NotImplementedException();
    }

    public virtual void Alter()
    {
      Edit();
    }

    public virtual void LaunchDebugger()
    {
      throw new NotImplementedException();
    }

    public virtual string GetDropSQL()
    {
      return String.Format("DROP {0} `{1}`.`{2}`", NodeId, Database, Name);
    }

    private void Drop()
    {
      string typeString = LocalizedTypeString.ToLower(CultureInfo.CurrentCulture);

      DialogResult result = MessageBox.Show(String.Format(
          Resources.DropConfirmation, typeString, Name),
          String.Format(Resources.DropConfirmationCaption, typeString),
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question);
      if (result == DialogResult.No)
        throw new OperationCanceledException();

      string sql = GetDropSQL();
      try
      {
        ExecuteSQL(sql);

        // now we drop the node from the hierarchy
        HierarchyAccessor.DropObjectNode(ItemId);
      }
      catch (Exception ex)
      {
        MessageBox.Show(
            String.Format(Resources.ErrorAttemptingToDrop,
            LocalizedTypeString, Name, ex.Message), Resources.ErrorTitle,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    protected virtual string GenerateUniqueName()
    {
      DbConnection conn = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
      string[] restrictions = new string[2] { null, Database };
      try
      {
        DataTable dt = conn.GetSchema(SchemaCollection, restrictions);

        int uniqueIndex = 1;
        string objectName = String.Empty;

        foreach (DataRow row in dt.Rows)
        {
          objectName = String.Format("{0}{1}", NodeId, uniqueIndex).Replace(" ", "");
          if (row[NameIndex].ToString().ToLowerInvariant() == objectName.ToLowerInvariant())
            uniqueIndex++;
        }
        Name = String.Format("{0}{1}", NodeId, uniqueIndex).Replace(" ", "");
        HierarchyAccessor.SetProperty(ItemId, (int)__VSHPROPID.VSHPROPID_Name, Name);
        return Name;
      }
      finally
      {
        HierarchyAccessor.Connection.UnlockProviderObject();
      }
    }

    #endregion

    #region Private Methods

    private void OpenEditor()
    {
      IVsUIShellOpenDocument shell =
        MySqlDataProviderPackage.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

      IVsWindowFrame winFrame = null;

      object editor = GetEditor();
      object coreEditor = editor;
      editorGuid = editor.GetType().GUID;
      if (editor is VSCodeEditor)
        coreEditor = (editor as VSCodeEditor).CodeWindow;

      IntPtr viewPunk = Marshal.GetIUnknownForObject(coreEditor);
      IntPtr dataPunk = Marshal.GetIUnknownForObject(this);
      Guid viewGuid = VSConstants.LOGVIEWID_TextView;

      // Initialize IDE editor infrastracture
      int result = shell.InitializeEditorInstance(
        (uint)0,                // Initialization flags. We need default behavior here
        viewPunk,               // View object reference (should implement IVsWindowPane)
        dataPunk,               // Docuemnt object reference (should implement IVsPersistDocData)
        Moniker,                // Document moniker
        ref editorGuid,         // GUID of the editor type
        null,                   // Name of the physical view. We use default
        ref viewGuid,           // GUID identifying the logical view.
        null,                   // Initial caption defined by the document owner. Will be initialized by the editor later
        null,                   // Initial caption defined by the document editor. Will be initialized by the editor later
        // Pointer to the IVsUIHierarchy interface of the project that contains the document
        HierarchyAccessor.Hierarchy,
        (uint)ItemId,           // UI hierarchy item identifier of the document in the project system
        IntPtr.Zero,            // Pointer to the IUnknown interface of the document data object if the document data object already exists in the running document table
        // Project-specific service provider.
        HierarchyAccessor.ServiceProvider as Microsoft.VisualStudio.OLE.Interop.IServiceProvider,
        ref commandGroupGuid,   // Command UI GUID of the commands to display for this editor.
        out winFrame            // The window frame that contains the editor
        );

      Debug.Assert(winFrame != null &&
             ErrorHandler.Succeeded(result), "Failed to initialize editor");

      // if our editor is a text buffer then hook up our language service
      //if (editor is TextBufferEditor)
      //{
      //    // now we tell our text buffer what language service to use
      //    Guid langSvcGuid = typeof(MySqlLanguageService).GUID;
      //    (editor as TextBufferEditor).TextBuffer.SetLanguageServiceID(ref langSvcGuid);
      //}

      winFrame.Show();
    }

    protected void SaveNode()
    {
      string nodePath = String.Format("/Connection/{0}s/{1}", NodeId, Name);
      HierarchyAccessor.SetNodePath(ItemId, nodePath);
    }

    protected void ExecuteSQL(string sql)
    {
      DbConnection conn = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
      try
      {
        DbCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
      }
      finally
      {
        HierarchyAccessor.Connection.UnlockProviderObject();
      }
    }

    public DataTable GetSchema(string schemaName, string[] restrictions)
    {
      DbConnection conn = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
      try
      {
        return conn.GetSchema(schemaName, restrictions);
      }
      finally
      {
        HierarchyAccessor.Connection.UnlockProviderObject();
      }
    }

    public DataTable GetDataTable(string sql)
    {
      DbConnection conn = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
      try
      {
        DbDataAdapter da = MySqlProviderObjectFactory.Factory.CreateDataAdapter();
        DbCommand cmd = MySqlProviderObjectFactory.Factory.CreateCommand();
        cmd.Connection = conn;
        cmd.CommandText = sql;
        da.SelectCommand = cmd;
        DataTable dt = new DataTable();
        da.Fill(dt);
        return dt;
      }
      finally
      {
        HierarchyAccessor.Connection.UnlockProviderObject();
      }
    }

    /// <summary>
    /// Selects connection node in the Server Explorer window.
    /// </summary>
    private void SelectConnectionNode()
    {
      //MySqlDataProviderPackage package = MySqlDataProviderPackage.Instance;
      // Extracts connection mamanger global service
      DataExplorerConnectionManager manager = Package.GetGlobalService(typeof(DataExplorerConnectionManager))
          as DataExplorerConnectionManager;
      if (manager == null)
      {
        Debug.Fail("Failed to get connection manager!");
        return;
      }

      // Searches for connection using connection string for current connection
      DataExplorerConnection connection = manager.FindConnection(
          Guids.Provider, HierarchyAccessor.Connection.EncryptedConnectionString, true);
      if (connection == null)
      {
        Debug.Fail("Failed to find proper connection node!");
        return;
      }

      // Select connection node
      manager.SelectConnection(connection);
    }

    #endregion

    /// <summary>
    /// Refresh database node in server explorer
    /// </summary>
    public void Refresh()
    {
      SelectConnectionNode();
      IVsUIHierarchy hier = HierarchyAccessor.Hierarchy as IVsUIHierarchy;
      Guid g = VSConstants.GUID_VSStandardCommandSet97;
      hier.ExecCommand(VSConstants.VSITEMID_ROOT, ref g, (uint)VSConstants.VSStd97CmdID.Refresh,
          (uint)OleCommandExecutionOption.DoDefault, IntPtr.Zero, IntPtr.Zero);
    }
  }
}
