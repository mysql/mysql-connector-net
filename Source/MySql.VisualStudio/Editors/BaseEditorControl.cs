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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Globalization;

namespace MySql.Data.VisualStudio.Editors
{
  public class BaseEditorControl : UserControl, IVsPersistDocData, IPersistFileFormat
  {
    private bool savingFile;
    private bool loadingFile;
    protected ServiceProvider serviceProvider;
    protected string fileName;

    #region IVsPersistDocData Members

    int IVsPersistDocData.Close()
    {
      return VSConstants.S_OK;
    }

    int IVsPersistDocData.GetGuidEditorType(out Guid pClassID)
    {
      throw new NotImplementedException();
    }

    int IVsPersistDocData.IsDocDataDirty(out int pfDirty)
    {
      return ((IPersistFileFormat)this).IsDirty(out pfDirty);
    }

    int IVsPersistDocData.IsDocDataReloadable(out int pfReloadable)
    {
      pfReloadable = 1;
      return VSConstants.S_OK;
    }

    int IVsPersistDocData.LoadDocData(string pszMkDocument)
    {
      return ((IPersistFileFormat)this).Load(pszMkDocument, 0, 0);
    }

    int IVsPersistDocData.OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
    {
      return VSConstants.S_OK;
    }

    int IVsPersistDocData.ReloadDocData(uint grfFlags)
    {
      return ((IPersistFileFormat)this).Load(null, grfFlags, 0);
    }

    int IVsPersistDocData.RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
    {
      fileName = pszMkDocumentNew;
      return VSConstants.S_OK;
    }

    private int QuerySave(out tagVSQuerySaveResult qsResult)
    {
      uint result;
      IVsQueryEditQuerySave2 querySave =
        (IVsQueryEditQuerySave2)serviceProvider.GetService(typeof(SVsQueryEditQuerySave));
      int hr = querySave.QuerySaveFile(fileName, 0, null, out result);
      qsResult = (tagVSQuerySaveResult)result;
      return hr;
    }

    int IVsPersistDocData.SaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int pfSaveCanceled)
    {
      pbstrMkDocumentNew = null;
      pfSaveCanceled = 0;
      int hr;

      IVsUIShell uiShell = (IVsUIShell)serviceProvider.GetService(typeof(IVsUIShell));

      switch (dwSave)
      {
        case VSSAVEFLAGS.VSSAVE_Save:
        case VSSAVEFLAGS.VSSAVE_SilentSave:
          {
            tagVSQuerySaveResult qsResult;
            hr = QuerySave(out qsResult);
            if (ErrorHandler.Failed(hr)) return hr;

            if (qsResult == tagVSQuerySaveResult.QSR_NoSave_Cancel)
              pfSaveCanceled = ~0;
            else if (qsResult == tagVSQuerySaveResult.QSR_SaveOK)
            {
              hr = uiShell.SaveDocDataToFile(dwSave, this, fileName,
                  out pbstrMkDocumentNew, out pfSaveCanceled);
              if (ErrorHandler.Failed(hr)) return hr;
            }
            else if (qsResult == tagVSQuerySaveResult.QSR_ForceSaveAs)
            {
              hr = uiShell.SaveDocDataToFile(VSSAVEFLAGS.VSSAVE_SaveAs, this, fileName,
                  out pbstrMkDocumentNew, out pfSaveCanceled);
              if (ErrorHandler.Failed(hr)) return hr;
            }
            break;
          }

        case VSSAVEFLAGS.VSSAVE_SaveAs:
        case VSSAVEFLAGS.VSSAVE_SaveCopyAs:
          {
            // --- Make sure the file name as the right extension
            if (String.Compare(".mysql", Path.GetExtension(fileName), true,
              CultureInfo.CurrentCulture) != 0)
              fileName += ".mysql";

            // --- Call the shell to do the save for us
            hr = uiShell.SaveDocDataToFile(dwSave, this, fileName,
              out pbstrMkDocumentNew, out pfSaveCanceled);
            if (ErrorHandler.Failed(hr)) return hr;
            break;
          }
        default:
          throw new ArgumentException("Unable to save file");
      }
      return VSConstants.S_OK;
    }

    int IVsPersistDocData.SetUntitledDocPath(string pszDocDataPath)
    {
      fileName = pszDocDataPath;
      return VSConstants.S_OK;
    }

    #endregion

    #region IPersistFileFormat Members

    int IPersistFileFormat.GetClassID(out Guid pClassID)
    {
      throw new NotImplementedException();
    }

    int IPersistFileFormat.GetCurFile(out string ppszFilename, out uint pnFormatIndex)
    {
      ppszFilename = fileName;
      pnFormatIndex = 0;
      return VSConstants.S_OK;
    }

    int IPersistFileFormat.GetFormatList(out string ppszFormatList)
    {
      ppszFormatList = GetFileFormatList();
      return VSConstants.S_OK;
    }

    int IPersistFileFormat.InitNew(uint nFormatIndex)
    {
      return VSConstants.S_OK;
    }

    int IPersistFileFormat.IsDirty(out int pfIsDirty)
    {
      pfIsDirty = IsDirty ? 1 : 0;
      return VSConstants.S_OK;
    }

    int IPersistFileFormat.Load(string pszFilename, uint grfMode, int fReadOnly)
    {
      // --- A valid file name is required.
      if ((pszFilename == null) && ((fileName == null) || (fileName.Length == 0)))
        throw new ArgumentNullException("pszFilename");

      loadingFile = true;
      int hr = VSConstants.S_OK;
      try
      {
        // --- If the new file name is null, then this operation is a reload
        bool isReload = false;
        if (pszFilename == null)
        {
          isReload = true;
        }
        // --- Set the new file name
        if (!isReload)
        {
          fileName = pszFilename;
        }
        // --- Load the file
        LoadFile(fileName);
        IsDirty = false;
        // --- Notify the load or reload
        //NotifyDocChanged();
      }
      finally
      {
        loadingFile = false;
      }
      return hr;
    }

    int IPersistFileFormat.Save(string pszFilename, int fRemember, uint nFormatIndex)
    {
      // --- switch into the NoScribble mode
      savingFile = true;
      try
      {
        // --- If file is null or same --> SAVE
        if (pszFilename == null || pszFilename == fileName)
        {
          SaveFile(fileName);
          IsDirty = false;
        }
        else
        {
          // --- If remember --> SaveAs
          if (fRemember != 0)
          {
            fileName = pszFilename;
            SaveFile(fileName);
            IsDirty = false;
          }
          else // --- Else, Save a Copy As
          {
            SaveFile(pszFilename);
          }
        }
      }
      finally
      {
        // --- Switch into the Normal mode
        savingFile = false;
      }
      return VSConstants.S_OK;
    }

    int IPersistFileFormat.SaveCompleted(string pszFilename)
    {
      if (savingFile)
        return VSConstants.S_FALSE;
      return VSConstants.S_OK;
    }

    #endregion

    #region IPersist Members

    public int GetClassID(out Guid pClassID)
    {
      throw new NotImplementedException();
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
    }

    #region Virtuals

    protected virtual string GetFileFormatList() { return null; }
    protected virtual void SaveFile(string fileName) { }
    protected virtual void LoadFile(string fileName) { }
    protected virtual bool IsDirty { get { return true; } set { } }

    #endregion

    #region IPersist Members

    int IPersist.GetClassID(out Guid pClassID)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
