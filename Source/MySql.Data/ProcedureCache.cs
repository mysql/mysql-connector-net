// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient.Properties;
using System.Diagnostics;
using System.Text;
using System.Globalization;

namespace MySql.Data.MySqlClient
{
  internal class ProcedureCache
  {
    private Hashtable procHash;
    private Queue<int> hashQueue;
    private int maxSize;

    public ProcedureCache(int size)
    {
      maxSize = size;
      hashQueue = new Queue<int>(maxSize);
      procHash = new Hashtable(maxSize);
    }

    public DataSet GetProcedure(MySqlConnection conn, string spName, string cacheKey)
    {
      DataSet ds = null;

      if (cacheKey != null)
      {
        int hash = cacheKey.GetHashCode();

        lock (procHash.SyncRoot)
        {
          ds = (DataSet)procHash[hash];
        }
      }
      if (ds == null)
      {
        ds = AddNew(conn, spName);
#if !CF
        conn.PerfMonitor.AddHardProcedureQuery();
#endif
        if (conn.Settings.Logging)
          MySqlTrace.LogInformation(conn.ServerThread,
            String.Format(Resources.HardProcQuery, spName));
      }
      else
      {
#if !CF
        conn.PerfMonitor.AddSoftProcedureQuery();
#endif
        if (conn.Settings.Logging)
          MySqlTrace.LogInformation(conn.ServerThread,
            String.Format(Resources.SoftProcQuery, spName));
      }
      return ds;
    }

    internal string GetCacheKey(string spName, DataSet procData)
    {
      string retValue = String.Empty;
      StringBuilder key = new StringBuilder(spName);
      key.Append("(");
      string delimiter = "";
      if (procData.Tables.Contains("Procedure Parameters"))
      {
        foreach (DataRow row in procData.Tables["Procedure Parameters"].Rows)
        {
          if (row["ORDINAL_POSITION"].Equals(0))
            retValue = "?=";
          else
          {
            key.AppendFormat(CultureInfo.InvariantCulture, "{0}?", delimiter);
            delimiter = ",";
          }
        }
      }
      key.Append(")");
      return retValue + key.ToString();
    }

    private DataSet AddNew(MySqlConnection connection, string spName)
    {
      DataSet procData = GetProcData(connection, spName);
      if (maxSize > 0)
      {
        string cacheKey = GetCacheKey(spName, procData);
        int hash = cacheKey.GetHashCode();
        lock (procHash.SyncRoot)
        {
          if (procHash.Keys.Count >= maxSize)
            TrimHash();
          if (!procHash.ContainsKey(hash))
          {
            procHash[hash] = procData;
            hashQueue.Enqueue(hash);
          }
        }
      }
      return procData;
    }

    private void TrimHash()
    {
      int oldestHash = hashQueue.Dequeue();
      procHash.Remove(oldestHash);
    }

    private static DataSet GetProcData(MySqlConnection connection, string spName)
    {
      string schema = String.Empty;
      string name = spName;

      int dotIndex = spName.IndexOf(".");
      if (dotIndex != -1)
      {
        schema = spName.Substring(0, dotIndex);
        name = spName.Substring(dotIndex + 1, spName.Length - dotIndex - 1);
      }

      string[] restrictions = new string[4];
      restrictions[1] = schema.Length > 0 ? schema : connection.CurrentDatabase();
      restrictions[2] = name;
      DataTable procTable = connection.GetSchema("procedures", restrictions);
      if (procTable.Rows.Count > 1)
        throw new MySqlException(Resources.ProcAndFuncSameName);
      if (procTable.Rows.Count == 0)
        throw new MySqlException(String.Format(Resources.InvalidProcName, name, schema));

      DataSet ds = new DataSet();
      ds.Tables.Add(procTable);

      // we don't use GetSchema here because that would cause another
      // query of procedures and we don't need that since we already
      // know the procedure we care about.
      ISSchemaProvider isp = new ISSchemaProvider(connection);
      string[] rest = isp.CleanRestrictions(restrictions);
      DataTable parametersTable = isp.GetProcedureParameters(rest, procTable);
      ds.Tables.Add(parametersTable);

      return ds;
    }
  }
}