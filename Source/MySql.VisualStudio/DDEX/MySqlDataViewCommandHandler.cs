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

/*
 * This file contains implementation of data view commands handler.
 */

using System;
using Microsoft.VisualStudio.Data;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents a custom data view command handler. 
  /// </summary>
  public class MySqlDataViewCommandHandler : DataViewCommandHandler
  {
    //		private NodeIdMapper nodeMapper;

    /*		private NodeIdMapper NodeMapper
        {
          get
          {
            if (nodeMapper == null)
              nodeMapper = new NodeIdMapper(DataViewHierarchyAccessor);
            return nodeMapper;
          }
        }*/

    /// <summary>
    /// This method supplies information about a command's status. At this point all 
    /// commands are supported and visible.
    /// </summary>
    /// <param name="itemIds">
    /// Array of identifiers for the items in the data view hierarchy on which this 
    /// command should be invoked.
    /// </param>
    /// <param name="command">
    /// The OleCommand object representing the command to invoke.
    /// </param>
    /// <param name="textType">
    /// The OleCommandTextType object instance for the specified command.
    /// </param>
    /// <param name="status">
    /// The OleCommandStatus object instance for the specified command.
    /// </param>
    /// <returns>
    /// Returns an OleCommandStatus object instance representing the status returned by the specified commands. /// 
    /// </returns>        
    public override OleCommandStatus GetCommandStatus(int[] itemIds, OleCommand command,
        OleCommandTextType textType, OleCommandStatus status)
    {
      if (command == null)
        throw new ArgumentNullException("command");
      if (status == null)
        throw new ArgumentNullException("status");

      OleCommandStatus result = new OleCommandStatus();

      /*			string[] selTypes = DataViewHierarchyAccessor.GetChildSelectionTypes(itemIds[0]);
            string type = DataViewHierarchyAccessor.GetObjectType(itemIds[0]);
            string[] staticNodes = DataViewHierarchyAccessor.GetChildStaticNodeIds(itemIds[0]);
            string path = DataViewHierarchyAccessor.GetNodePath(itemIds[0]);
            object[] identifier = DataViewHierarchyAccessor.GetObjectIdentifier(itemIds[0]);

            object v1 = DataViewHierarchyAccessor.GetProperty(itemIds[0], (int)__VSHPROPID.VSHPROPID_IsHiddenItem);
            object v2 = DataViewHierarchyAccessor.GetProperty(itemIds[0], (int)__VSHPROPID.VSHPROPID_Parent);
      */

      //BaseNode node = NodeMapper[itemIds[0]];

      result.Enabled = true;
      result.Visible = true;
      result.Supported = true;

      if (command.CommandId == PkgCmdIDList.cmdDebugProcedure)
        result.Visible = DataViewHierarchyAccessor.GetNodeId(itemIds[0]).Equals("StoredProcedure", StringComparison.OrdinalIgnoreCase);

      return result;
      /*            // Get command handler instance
                  ICommand commandHandler = CommandFactory.Instance.CreateCommandHandler(command.GroupGuid, command.CommandId);
                  if (commandHandler == null)
                      return base.GetCommandStatus(itemIds, command, textType, status);
                  result.Supported = true;

                  // Determine visibility
                  if (!commandHandler.GetIsVisible(Hierarchy, itemIds))
                  {
                      result.Visible = false;
                      return result;
                  }
                  result.Visible = true;
                  result.Enabled = true;

                  // TODO: Find out why this doesn't work at all!
                  // Localize text if possible
                  string localizedText = commandHandler.GetText(Hierarchy, itemIds);
                  if (!String.IsNullOrEmpty(localizedText))
                      result.Text = localizedText;
            
                  return result;*/
    }

    /// <summary>
    /// Executes a specified command, potentially based on parameters passed in 
    /// from the data view support XML.
    /// </summary>
    /// <param name="itemIds">
    /// Array of identifiers for the items in the data view hierarchy on which this 
    /// command should be invoked.
    /// </param>
    /// <param name="command">
    /// The OleCommand object representing the command to invoke.
    /// </param>
    /// <param name="executionOption">
    /// Any OleCommandExecutionOption object instance representing options on the 
    /// invoked command.
    /// NOT USED.
    /// </param>
    /// <param name="arguments">
    /// An object representing arguments to the command.
    /// NOT USED.
    /// </param>
    /// <returns>
    /// Returns an object instance representing the value returned by the specified 
    /// command, which is typically nothing. 
    /// </returns>
    public override object[] ExecuteCommand(int[] itemIds, OleCommand command,
        OleCommandExecutionOption executionOption, object arguments)
    {
      if (command == null)
        throw new ArgumentNullException("command");

      if (!HandleStaticCommand(command.CommandId))
      {
        BaseNode node = MakeNewNode(itemIds[0]);
        node.ExecuteCommand(command.CommandId);
      }
      return null;
    }

    /// <summary>
    /// Handle any of the static commands
    /// </summary>
    /// <param name="commandId"></param>
    /// <returns></returns>
    private bool HandleStaticCommand(int commandId)
    {
      switch ((uint)commandId)
      {
        case PkgCmdIDList.cmdCreateTable:
        case PkgCmdIDList.cmdidGlobalCreateTable:
          TableNode.CreateNew(DataViewHierarchyAccessor);
          return true;
        case PkgCmdIDList.cmdCreateProcedure:
        case PkgCmdIDList.cmdidGlobalCreateProcedure:
          StoredProcedureNode.CreateNew(DataViewHierarchyAccessor, false);
          return true;
        case PkgCmdIDList.cmdCreateFunction:
        case PkgCmdIDList.cmdidGlobalCreateFunction:
          StoredProcedureNode.CreateNew(DataViewHierarchyAccessor, true);
          return true;
        case PkgCmdIDList.cmdCreateView:
        case PkgCmdIDList.cmdidGlobalCreateView:
          ViewNode.CreateNew(DataViewHierarchyAccessor);
          return true;
        case PkgCmdIDList.cmdCreateUDF:
        case PkgCmdIDList.cmdidGlobalCreateUDF:
          UDFNode.CreateNew(DataViewHierarchyAccessor);
          return true;
      }
      return false;
    }

    /// <summary>
    /// Executes a specified command, potentially based on parameters passed in 
    /// from the data view support XML.
    /// </summary>
    /// <param name="itemId">
    /// Identifier of hierarchy item for which command should be executed.
    /// </param>
    /// <param name="command">
    /// The OleCommand object representing the command to invoke.
    /// </param>
    /// <param name="executionOption">
    /// Any OleCommandExecutionOption object instance representing options on the 
    /// invoked command.
    /// NOT USED.
    /// </param>
    /// <param name="arguments">
    /// An object representing arguments to the command.
    /// NOT USED.
    /// </param>
    /// <returns>
    /// Returns an object instance representing the value returned by the specified 
    /// command, which is typically nothing. 
    /// </returns>
    public override object ExecuteCommand(int itemId, OleCommand command, OleCommandExecutionOption executionOption, object arguments)
    {
      return ExecuteCommand(new int[] { itemId }, command, executionOption, arguments);
    }

    private BaseNode MakeNewNode(int id)
    {
      string nodeId = DataViewHierarchyAccessor.GetNodeId(id);
      nodeId = nodeId.ToLowerInvariant();

      BaseNode newNode = null;
      switch (nodeId)
      {
        case "table":
          newNode = new TableNode(DataViewHierarchyAccessor, id);
          break;
        case "storedprocedure":
          newNode = new StoredProcedureNode(DataViewHierarchyAccessor, id, false);
          break;
        case "storedfunction":
          newNode = new StoredProcedureNode(DataViewHierarchyAccessor, id, true);
          break;
        case "view":
          newNode = new ViewNode(DataViewHierarchyAccessor, id);
          break;
        case "udf":
          newNode = new UDFNode(DataViewHierarchyAccessor, id);
          break;
        case "trigger":
          newNode = new TriggerNode(DataViewHierarchyAccessor, id);
          break;
        default:
          throw new NotSupportedException("Node type not supported");
      }
      Debug.Assert(newNode != null);
      return newNode;
    }

    /*        #region Hierarchy accessor mediator
            /// <summary>
            /// Returns hierarchy accessor mediator instance. Create instance at the first call.
            /// </summary>
            protected ServerExplorerFacade Hierarchy
            {
                get
                {
                    // Check if not created yet
                    if (hierarchyRef == null)
                    {
                        Debug.Assert(DataViewHierarchyAccessor != null, "Hierarchy accessor is not initialized!");
                        hierarchyRef = new ServerExplorerFacade(DataViewHierarchyAccessor);
                    }
                    // Here must be already created
                    Debug.Assert(hierarchyRef != null, "Failed to initialize hierarchy accessor mediator!");
                    return hierarchyRef;
                }
            }

            /// <summary>
            /// Used to store hierarchy accessor mediator instance
            /// </summary>
            private ServerExplorerFacade hierarchyRef; 
            #endregion*/
  }
}
