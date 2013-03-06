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
 * This file contains data view support entity implementation.
 */

using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents an implementation of data view support that returns
  /// the stream of XML containing the data view support elements.
  /// </summary>
  internal class MySqlDataViewSupport : DataViewSupport
  {
    /// <summary>
    /// Constructor does nothing.  All the work happens in GetDataViews
    /// </summary>
    public MySqlDataViewSupport()
      : base()
    {
    }

    public override Stream GetDataViews()
    {
      string xmlName = "MySql.Data.VisualStudio.DDEX.MySqlDataViewSupport.xml";
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      Stream stream = executingAssembly.GetManifestResourceStream(xmlName);
      StreamReader reader = new StreamReader(stream);
      string xml = reader.ReadToEnd();
      reader.Close();

      // if we are running under VS 2008 then we need to switch out a couple
      // of command handlers
      DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;
      if (dte != null && dte.Version.StartsWith("9", StringComparison.Ordinal))
        xml = xml.Replace("Microsoft.VisualStudio.DataTools.DBPackage.VDT_OLEDB_CommandHandler_TableTools",
            "884DD964-5327-461f-9F06-6484DD540F8F");

      MemoryStream ms = new MemoryStream(xml.Length);
      StreamWriter writer = new StreamWriter(ms);
      writer.Write(xml);
      writer.Flush();
      ms.Position = 0;
      return ms;
    }
  }
}
