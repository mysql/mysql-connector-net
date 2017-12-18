// Copyright © 2004, 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Web;
using MySql.Data.MySqlClient;
using MySql.Web.Common;
using MySql.Web.Properties;


namespace MySql.Web.SiteMap
{
  /// <summary>
  /// SiteMap provider backed by MySql database.
  /// </summary>
  public class MySqlSiteMapProvider : StaticSiteMapProvider
  {
    private readonly object _lockObject = new object();
    private string _connStr;
    private SiteMapNode _rootNode;
    private Dictionary<int, SiteMapNode> _nodes = new Dictionary<int, SiteMapNode>();
    private bool writeExceptionsToEventLog;
    string eventSource = "MySQLSiteMap";
    string eventLog = "Application";
    string exceptionMessage = "An exception occurred. Please check the event log.";

    internal bool WriteExceptionsToEventLog { get { return writeExceptionsToEventLog; } }

    public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
    {
      base.Initialize(name, config);

      if (config == null)
        throw new ArgumentException(Resources.SiteMapConnectionStringMissing);

      _connStr = ConfigUtility.GetConnectionString(config);
      if (string.IsNullOrEmpty(_connStr)) 
        throw new ArgumentException(Resources.SiteMapConnectionStringMissing);

      writeExceptionsToEventLog = false;
      if (config["writeExceptionsToEventLog"] != null)
      {
        writeExceptionsToEventLog = (config["writeExceptionsToEventLog"].ToUpper() == "TRUE");
      }

      SchemaManager.CheckSchema(_connStr, config);
    }

    public override SiteMapNode BuildSiteMap()
    {
      lock (_lockObject)
      {
        if (_rootNode != null) return _rootNode;
        string sql = "select Id, Title, Description, Url, Roles, ParentId from my_aspnet_sitemap";
        MySqlConnection conn = new MySqlConnection(_connStr);
        try
        {
          conn.Open();
          MySqlCommand cmd = new MySqlCommand(sql, conn);
          using (MySqlDataReader r = cmd.ExecuteReader())
          {
            int IdFld = r.GetOrdinal("Id");
            int TitleFld = r.GetOrdinal("Title");
            int DescFld = r.GetOrdinal("Description");
            int UrlFld = r.GetOrdinal("Url");
            int RolesFld = r.GetOrdinal("Roles");
            int ParentIdFld = r.GetOrdinal("ParentId");

            while (r.Read())
            {
              int IdVal;
              string TitleVal;
              string DescVal;
              string UrlVal;
              string RolesVal;
              int ParentIdVal;

              LoadValue<int>(r, IdFld, out IdVal);
              LoadValue<string>(r, TitleFld, out TitleVal);
              LoadValue<string>(r, DescFld, out DescVal);
              LoadValue<string>(r, UrlFld, out UrlVal);
              LoadValue<string>(r, RolesFld, out RolesVal);
              LoadValue<int>(r, ParentIdFld, out  ParentIdVal);

              SiteMapNode node = new SiteMapNode(this, IdVal.ToString(), UrlVal, TitleVal, DescVal);
              _nodes.Add(IdVal, node);
              if (ParentIdVal != 0)
              {
                SiteMapNode parentNode = _nodes[ParentIdVal];
                AddNode(node, parentNode);
              }
              else
              {
                AddNode(node);
              }
              if (ParentIdVal == 0)
              {
                _rootNode = node;
              }
            }
          }
        }
        catch (MySqlException ex) {
          HandleMySqlException(ex, "BuildSiteMap");
        } finally {
          if ((conn.State & ConnectionState.Open) != 0) conn.Close();
        }
        return _rootNode;
      }
    }

    private void LoadValue<T>(MySqlDataReader r, int fldNum, out T val)
    {
      if (r.IsDBNull(fldNum))
        val = default(T);
      else
      {
        val = (T)r.GetValue(fldNum);
      }
    }

    public override SiteMapNode FindSiteMapNodeFromKey(string key)
    {
      if (string.IsNullOrEmpty(key)) return null;
      else
      {
        int idKey = Convert.ToInt32(key);
        SiteMapNode node;
        _nodes.TryGetValue(idKey, out node);
        return node;
      }
    }

    /// <summary>
    /// Handles MySql exception.
    /// If WriteExceptionsToEventLog is set, will write exception info
    /// to event log. 
    /// It throws provider exception (original exception is stored as inner exception)
    /// </summary>
    /// <param name="e">exception</param>
    /// <param name="action"> name of the function that throwed the exception</param>
    private void HandleMySqlException(MySqlException e, string action)
    {
      if (WriteExceptionsToEventLog)
      {
        using (EventLog log = new EventLog())
        {
          log.Source = eventSource;
          log.Log = eventLog;

          string message = "An exception occurred communicating with the data source.\n\n";
          message += "Action: " + action;
          message += "Exception: " + e.ToString();
          log.WriteEntry(message);
        }
      }
      throw new ProviderException(exceptionMessage, e);
    }

    protected override SiteMapNode GetRootNodeCore()
    {
      if (_rootNode == null)
        BuildSiteMap();
        
      return _rootNode;
    }
  }
}
