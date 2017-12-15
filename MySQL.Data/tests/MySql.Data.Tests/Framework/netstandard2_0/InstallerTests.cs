// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using System.Xml;

namespace MySql.Data.MySqlClient.Tests
{
  public class InstallerTests 
  {   
    private void CreateFiles()
    {
      //make a local copy of the machine.config file
      object installRoot = Registry.GetValue(
       @"HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\",
       "InstallRoot", null);
      if (installRoot == null)
        throw new Exception("Unable to retrieve install root for .NET framework");

      string[] dirs = new string[2] { "v2.0.50727", "v4.0.30319" };

      foreach (string frameworkDir in dirs)
      {
        string path = installRoot.ToString() + frameworkDir;
        string configPath = String.Format(@"{0}\CONFIG", path);

        if (!Directory.Exists(configPath))
        {
          throw new Exception("Unable to get config .NET framework path");
        }

        if (!Directory.Exists(Environment.CurrentDirectory + "\\" + frameworkDir + @"\CONFIG"))
        {
          Directory.CreateDirectory(Environment.CurrentDirectory + "\\" + frameworkDir + @"\CONFIG");
        }

        File.Copy(configPath + @"\machine.config", Environment.CurrentDirectory + "\\" + frameworkDir + @"\CONFIG" + @"\machine.config", true);
      }
    
    }


    [Fact]
    public void CanAddAssemblyBinding()
    {
      CreateFiles();
      CustomInstaller.UpdateMachineConfigs(Environment.CurrentDirectory + "\\", true);
      DeleteFiles();
    }

    [Fact]
    public void CanRemoveAssemblyBinding()
    {
      CreateFiles();
      CustomInstaller.UpdateMachineConfigs(Environment.CurrentDirectory + "\\", true);

      CustomInstaller.UpdateMachineConfigs(Environment.CurrentDirectory + "\\", false);
      
      string[] dirs = new string[2] { "v2.0.50727", "v4.0.30319" };

      foreach (string frameworkDir in dirs)
      {
        var configFile = Environment.CurrentDirectory + "\\" + frameworkDir + @"\CONFIG" + @"\machine.config";

        StreamReader sr = new StreamReader(configFile);
        string configXML = sr.ReadToEnd();
        sr.Close();

        // load the XML into the XmlDocument
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(configXML);

        XmlNodeList nodesDependantAssembly = doc.GetElementsByTagName("assemblyBinding")[0].ChildNodes;
        if (nodesDependantAssembly != null)
        {
          int nodesCount = nodesDependantAssembly.Count;
          for (int i = 0; i < nodesCount; i++)
          {
            if (nodesDependantAssembly[0].ChildNodes[0].Attributes[0].Name == "name"
               &&
               nodesDependantAssembly[0].ChildNodes[0].Attributes[0].Value.Contains("MySql"))
            {              
              Assert.True(true, "Error when removing assembly binding redirection");
            }
          }
        }        
      }

      DeleteFiles();
    }

    public void DeleteFiles()
    {

     string[] dirs = new string[2] { "v2.0.50727", "v4.0.30319" };

     foreach (string frameworkDir in dirs)
     {
       Directory.Delete(Environment.CurrentDirectory + "\\" + frameworkDir, true);
     }

    }
  }
}
