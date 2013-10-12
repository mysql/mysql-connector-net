// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
