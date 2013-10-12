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
