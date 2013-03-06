// Copyright © 2004, 2012, Oracle and/or its affiliates. All rights reserved.
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
using System.IO;

namespace MySql.Debugger
{
  /// <summary>
  /// The scope of a routine, if recursivity was enabled, there could be many RoutineScopes for a single Routine.
  /// </summary>
  public class RoutineScope
  {
    /// <summary>
    /// Reference to Routine Metadata.
    /// </summary>
    public RoutineInfo OwningRoutine;

    /// <summary>
    /// A reference to a filename, in case the routine belongs to one.
    /// </summary>
    /// <remarks>This is of utility to debugger's clients, not for the core debugger itself.</remarks>
    private string _fileName;
    public string FileName { 
      get { return _fileName; }
      set { _fileName = value; }
    }

    public string GetFileName()
    {
      if (string.IsNullOrEmpty(_fileName))
      {
        string routineName = OwningRoutine.FullName;
        string path = Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MySqlDebuggerCache" );
        if (!Directory.Exists(path))
        {
          Directory.CreateDirectory(path);
        }
        path = Path.Combine( path, routineName );
        _fileName = string.Format( "{0}.mysql", path );
        string fileContent = null;
        if (File.Exists(_fileName))
          fileContent = File.ReadAllText(_fileName);
        if (fileContent == null 
          || !fileContent.Equals(OwningRoutine.SourceCode))
          File.WriteAllText(_fileName, OwningRoutine.SourceCode);
      }
      return _fileName;
    }

    /// <summary>
    /// The dictionary of variables for the given scope (stack frame).
    /// </summary>
    public Dictionary<string, StoreType> Variables;

    public Breakpoint CurrentPosition { get; set; }
  }
}
