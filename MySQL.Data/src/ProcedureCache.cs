// Copyright © 2004, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal class ProcedureCacheEntry
  {
    public MySqlSchemaCollection procedure;
    public MySqlSchemaCollection parameters;
  }

  internal class ProcedureCache
  {
    private readonly Dictionary<int, ProcedureCacheEntry> _procHash;
    private readonly Queue<int> _hashQueue;
    private readonly int _maxSize;

    public ProcedureCache(int size)
    {
      _maxSize = size;
      _hashQueue = new Queue<int>(_maxSize);
      _procHash = new Dictionary<int, ProcedureCacheEntry>(_maxSize);
    }

    public async Task<ProcedureCacheEntry> GetProcedureAsync(MySqlConnection conn, string spName, string cacheKey, bool execAsync)
    {
      ProcedureCacheEntry proc = null;

      if (cacheKey != null)
      {
        int hash = cacheKey.GetHashCode();

        lock (_procHash)
        {
          _procHash.TryGetValue(hash, out proc);
        }
      }
      if (proc == null)
      {
        proc = await AddNewAsync(conn, spName, execAsync).ConfigureAwait(false);
        conn.PerfMonitor.AddHardProcedureQuery();
        if (conn.Settings.Logging)
          MySqlTrace.LogInformation(conn.ServerThread,
            String.Format(Resources.HardProcQuery, spName));
      }
      else
      {
        conn.PerfMonitor.AddSoftProcedureQuery();
        if (conn.Settings.Logging)
          MySqlTrace.LogInformation(conn.ServerThread,
            String.Format(Resources.SoftProcQuery, spName));
      }
      return proc;
    }

    internal string GetCacheKey(string spName, ProcedureCacheEntry proc)
    {
      string retValue = String.Empty;
      StringBuilder key = new StringBuilder(spName);
      key.Append("(");
      string delimiter = "";
      if (proc.parameters != null)
      {
        foreach (MySqlSchemaRow row in proc.parameters.Rows)
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

    private async Task<ProcedureCacheEntry> AddNewAsync(MySqlConnection connection, string spName, bool execAsync)
    {
      ProcedureCacheEntry procData = await GetProcDataAsync(connection, spName, execAsync).ConfigureAwait(false);

      if (_maxSize <= 0) return procData;

      string cacheKey = GetCacheKey(spName, procData);
      int hash = cacheKey.GetHashCode();
      lock (_procHash)
      {
        if (_procHash.Keys.Count >= _maxSize)
          TrimHash();
        if (!_procHash.ContainsKey(hash))
        {
          _procHash[hash] = procData;
          _hashQueue.Enqueue(hash);
        }
      }
      return procData;
    }

    private void TrimHash()
    {
      int oldestHash = _hashQueue.Dequeue();
      _procHash.Remove(oldestHash);
    }

    private static async Task<ProcedureCacheEntry> GetProcDataAsync(MySqlConnection connection, string spName, bool execAsync)
    {
      SplitSchemaAndEntity(spName, out string schema, out string entity);

      string[] restrictions = new string[4];
      restrictions[1] = string.IsNullOrEmpty(schema) ? connection.CurrentDatabase() : Utils.UnquoteString(schema);
      restrictions[2] = Utils.UnquoteString(entity);
      MySqlSchemaCollection proc = connection.GetSchemaCollection("procedures", restrictions);
      if (proc.Rows.Count > 1)
        throw new MySqlException(Resources.ProcAndFuncSameName);
      if (proc.Rows.Count == 0)
      {
        string msg = string.Format(Resources.InvalidProcName, entity, schema) + " " +
        string.Format(Resources.ExecuteProcedureUnauthorized, connection.Settings.UserID, connection.Settings.Server);
        throw new MySqlException(msg);
      }

      ProcedureCacheEntry entry = new ProcedureCacheEntry();
      entry.procedure = proc;

      // we don't use GetSchema here because that would cause another
      // query of procedures and we don't need that since we already
      // know the procedure we care about.
      ISSchemaProvider isp = new ISSchemaProvider(connection);
      string[] rest = isp.CleanRestrictions(restrictions);
      MySqlSchemaCollection parameters = await isp.GetProcedureParametersAsync(rest, proc, execAsync).ConfigureAwait(false);
      entry.parameters = parameters;

      return entry;
    }

    /// <summary>
    /// Splits the schema and the entity from a syntactically correct "spName"; 
    /// if there's no schema, then schema will be an empty string.
    /// </summary>
    /// <param name="spName">string to inspect.</param>
    /// <param name="schema">The schema.</param>
    /// <param name="entity">The entity.</param>
    private static void SplitSchemaAndEntity(string spName, out string schema, out string entity)
    {
      int dotIndex = ExtractDotIndex(spName);

      if (dotIndex != -1)
      {
        schema = spName.Substring(0, dotIndex);
        entity = spName.Substring(dotIndex + 1);
      }
      else
      {
        schema = string.Empty;
        entity = spName;
      }
    }

    /// <summary>
    /// Obtains the dot index that separates the schema from the entity if there's one; 
    /// otherwise, returns -1. It expects a syntactically correct "spName".
    /// </summary>
    /// <param name="spName">string to inspect.</param>
    /// <param name="dotIndex">Value of the dot index.</param>
    /// <returns>The dot index.</returns>
    private static int ExtractDotIndex(string spName, int dotIndex = -1)
    {
      int backticks, _dotIndexTemp;
      _dotIndexTemp = spName.IndexOf('.'); // looks for a '.' in the string passed as argument
      string subString;

      if (_dotIndexTemp != -1)
      {
        subString = spName.Substring(_dotIndexTemp + 1);  // gets a substring from the found '.' to the end of the string
        backticks = subString.Count(c => c == '`'); // counts backticks in the substring

        // if the count of backticks in the substring is an odd number,
        // that means that this '.' is part of the schema or entity and will continue looking;
        // otherwise, returns the index.
        if (backticks % 2 == 0)
          dotIndex = dotIndex == -1 ? _dotIndexTemp : dotIndex + _dotIndexTemp;
        else
          dotIndex = ExtractDotIndex(subString, _dotIndexTemp + 1);
      }
      else if (_dotIndexTemp == -1 && dotIndex != -1)
        dotIndex = -1;

      return dotIndex;
    }

    internal void Clear()
    {
      _procHash.Clear();
      _hashQueue.Clear();
    }
  }
}
