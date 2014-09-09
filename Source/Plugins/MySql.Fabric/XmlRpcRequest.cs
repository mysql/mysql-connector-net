// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.Text;
using System.Xml;

namespace MySql.Fabric
{
  internal static class XmlRpcRequest
  {
    public static string GetResquestMessage(string methodName, params object[] parameters)
    {
      StringBuilder xmlMessage = new StringBuilder();
      XmlWriterSettings settings = new XmlWriterSettings();
      settings.OmitXmlDeclaration = true;
      XmlWriter xmlWriter = XmlWriter.Create(xmlMessage, settings);

      xmlMessage.Append(@"<?xml version=""1.0""?>");
      xmlWriter.WriteStartDocument();
      xmlWriter.WriteStartElement("methodCall");
      xmlWriter.WriteElementString("methodName", methodName);
      if (parameters.Length > 0)
      {
        xmlWriter.WriteStartElement("params");
        xmlWriter.WriteStartElement("param");
        WriteXmlParameters(parameters, ref xmlWriter);
        xmlWriter.WriteEndElement(); // ends param
        xmlWriter.WriteEndElement(); // ends params
      }
      xmlWriter.WriteEndElement(); // ends methodCall
      xmlWriter.WriteEndDocument(); // ends document
      xmlWriter.Flush();

      return xmlMessage.ToString();
    }

    private static void WriteXmlParameters(object[] parameters, ref XmlWriter xmlWriter)
    {
      foreach (var param in parameters)
      {
        xmlWriter.WriteStartElement("value");
        string type = null, value = null;
        if (param is int)
        {
          type = "int";
          value = ((int)param).ToString();
        }
        else if (param is bool)
        {
          type = "boolean";
          value = (bool)param ? "1" : "0";
        }
        else if (param is float || param is double)
        {
          type = "double";
          value = ((double)param).ToString();
        }
        else if (param is DateTime)
        {
          type = "dateTime.iso8601";
          value = ((DateTime)param).ToString("s");
        }
        else if (param is byte[])
        {
          type = "base64";
          value = Convert.ToBase64String((byte[])param);
        }
        else if (param is ICollection)
        {
          object[] values = new object[((ICollection)param).Count];
          ((ICollection)param).CopyTo(values, 0);
          xmlWriter.WriteStartElement("array");
          xmlWriter.WriteStartElement("data");
          WriteXmlParameters(values, ref xmlWriter);
          xmlWriter.WriteEndElement(); // ends data
          xmlWriter.WriteEndElement(); // ends array
        }
        else
        {
          type = "string";
          value = param.ToString();
        }

        if (!string.IsNullOrEmpty(type))
          xmlWriter.WriteElementString(type, value);
        xmlWriter.WriteEndElement(); // ends value
      }
    }

    public static string ParseResponse(string xml)
    {
      XmlDocument xmlDocument = new XmlDocument();
      xmlDocument.LoadXml(xml);
      XmlNode methodResponseNode = xmlDocument.SelectSingleNode("methodResponse");
      if (methodResponseNode == null)
        throw new XmlException("methodResponse");
      if (methodResponseNode.FirstChild.Name == "params")
      {
        XmlNode xmlNodeValue = methodResponseNode.SelectSingleNode("params/param/value");
        return xmlNodeValue.FirstChild.InnerText;
      }
      else if (methodResponseNode.FirstChild.Name == "fault")
      {
        int faultCode = int.Parse(methodResponseNode.SelectSingleNode(@"fault/value/struct/member[name='faultCode']/value/int").InnerText);
        string faultString = methodResponseNode.SelectSingleNode(@"fault/value/struct/member[name='faultString']/value/string").InnerText;
        throw new MySqlFabricException(faultString);
      }
      else 
      {
        throw new InvalidOperationException(methodResponseNode.FirstChild.Name);
      }
    }
  }
}
