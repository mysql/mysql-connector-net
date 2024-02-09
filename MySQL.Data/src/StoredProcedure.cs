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

using MySql.Data.Common;
using MySql.Data.Types;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for StoredProcedure.
  /// </summary>
  internal class StoredProcedure : PreparableStatement
  {
    private string _outSelect;
    private string resolvedCommandText;

    public StoredProcedure(MySqlCommand cmd, string text)
      : base(cmd, text)
    {
    }

    private MySqlParameter GetReturnParameter()
    {
      return Parameters?.Cast<MySqlParameter>().FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
    }

    public override string ResolvedCommandText
    {
      get { return resolvedCommandText; }
    }

    internal string GetCacheKey(string spName)
    {
      string retValue = String.Empty;
      StringBuilder key = new StringBuilder(spName);
      key.Append("(");
      string delimiter = "";
      foreach (MySqlParameter p in command.Parameters)
      {
        if (p.Direction == ParameterDirection.ReturnValue)
          retValue = "?=";
        else
        {
          key.AppendFormat(CultureInfo.InvariantCulture, "{0}?", delimiter);
          delimiter = ",";
        }
      }
      key.Append(")");
      return retValue + key.ToString();
    }

    private async Task<ProcedureCacheEntry> GetParametersAsync(string procName, bool execAsync)
    {
      string procCacheKey = GetCacheKey(procName);
      ProcedureCacheEntry entry = await Connection.ProcedureCache.GetProcedureAsync(Connection, procName, procCacheKey, execAsync).ConfigureAwait(false);
      return entry;
    }

    public static string GetFlags(string dtd)
    {
      int x = dtd.Length - 1;
      while (x > 0 && (Char.IsLetterOrDigit(dtd[x]) || dtd[x] == ' '))
        x--;
      string dtdSubstring = dtd.Substring(x);
      return StringUtility.ToUpperInvariant(dtdSubstring);
    }

    internal static string FixProcedureName(string spName)
    {
      if (IsSyntacticallyCorrect(spName)) return spName;

      return $"`{spName.Replace("`", "``")}`";
    }

    /// <summary>
    /// Verify if the string passed as argument is syntactically correct.
    /// </summary>
    /// <param name="spName">String to be analyzed</param>
    /// <returns>true if is correct; otherwise, false.</returns>
    internal static bool IsSyntacticallyCorrect(string spName)
    {
      const char backtick = '`', dot = '.';

      char[] spNameArray = spName.ToArray();
      bool quoted = spName.StartsWith("`");
      bool splittingDot = false;

      for (int i = 1; i < spNameArray.Length; i++)
      {
        if (spNameArray[i] == backtick)
        {
          // We are in quoted mode.
          if (quoted)
          {
            // we are not in the last char of the string.
            if (i < spNameArray.Length - 1)
            {
              // Get the next char.
              char nextChar = spNameArray[i + 1];

              // If the next char are neither a dot nor a backtick,
              // it means the input string is not well quoted and exits the loop.
              if (nextChar != dot && nextChar != backtick)
                return false;

              // If the next char is a backtick, move forward 2 positions.
              if (nextChar == backtick)
                i++;

              // If the next char is a dot, that means we are not in quoted mode anymore.
              if (nextChar == dot)
              {
                quoted = false;
                splittingDot = true;
                i++;
              }
            }
          }
          // Not quoted mode
          else
          {
            // If the previous char is not a dot or the string does not end with a backtick,
            // it means the input string is not well quoted and exits the loop;
            // otherwise, enter quoted mode.
            if (spNameArray[i - 1] != dot || !spName.EndsWith("`"))
              return false;

            quoted = true;
          }
        }
        else if (spNameArray[i] == dot && !quoted)
          if (splittingDot)
            return false;
          else
            splittingDot = true;
      }

      // If we reach to the very last char of the string, it means the string is well written.
      return true;
    }

    private MySqlParameter GetAndFixParameter(string spName, MySqlSchemaRow param, bool realAsFloat, MySqlParameter returnParameter)
    {
      string mode = (string)param["PARAMETER_MODE"];
      string pName = (string)param["PARAMETER_NAME"];
      string datatype = (string)param["DATA_TYPE"];
      bool unsigned = GetFlags(param["DTD_IDENTIFIER"].ToString()).IndexOf("UNSIGNED") != -1;

      if (Convert.ToUInt64(param["ORDINAL_POSITION"]) == 0)
      {
        if (returnParameter == null)
          throw new InvalidOperationException(String.Format(Resources.RoutineRequiresReturnParameter, spName));
        pName = returnParameter.ParameterName;
      }

      // make sure the parameters given to us have an appropriate type set if it's not already
      MySqlParameter p = command.Parameters.GetParameterFlexible(pName, true);
      if (!p.TypeHasBeenSet)
        p.MySqlDbType = MetaData.NameToType(datatype, unsigned, realAsFloat, Connection);

      return p;
    }

    private async Task<MySqlParameterCollection> CheckParametersAsync(string spName, bool execAsync)
    {
      MySqlParameterCollection newParms = new MySqlParameterCollection(command);
      MySqlParameter returnParameter = GetReturnParameter();

      ProcedureCacheEntry entry = await GetParametersAsync(spName, execAsync).ConfigureAwait(false);
      if (entry.procedure == null || entry.procedure.Rows.Count == 0)
        throw new InvalidOperationException(String.Format(Resources.RoutineNotFound, spName));

      bool realAsFloat = entry.procedure.Rows[0]["SQL_MODE"].ToString().IndexOf("REAL_AS_FLOAT") != -1;

      foreach (MySqlSchemaRow param in entry.parameters.Rows)
        newParms.Add(GetAndFixParameter(spName, param, realAsFloat, returnParameter));
      return newParms;
    }

    public override void Resolve(bool preparing)
    {
      // check to see if we are already resolved
      if (ResolvedCommandText != null) return;

      ServerProvidingOutputParameters = Driver.SupportsOutputParameters && preparing;

      // first retrieve the procedure definition from our
      // procedure cache
      string spName = commandText;
      spName = FixProcedureName(spName);

      MySqlParameter returnParameter = GetReturnParameter();

      MySqlParameterCollection parms = command.Connection.Settings.CheckParameters ?
          CheckParametersAsync(spName, false).GetAwaiter().GetResult() : Parameters;

      string setSql = SetUserVariables(parms, preparing);
      string callSql = CreateCallStatement(spName, returnParameter, parms);
      string outSql = CreateOutputSelect(parms, preparing);
      resolvedCommandText = String.Format("{0}{1}{2}", setSql, callSql, outSql);
    }

    private string SetUserVariables(MySqlParameterCollection parms, bool preparing)
    {
      StringBuilder setSql = new StringBuilder();

      if (ServerProvidingOutputParameters) return setSql.ToString();

      string delimiter = String.Empty;
      foreach (MySqlParameter p in parms)
      {
        if (p.Direction != ParameterDirection.InputOutput) continue;

        string pName = "@" + p.BaseName;
        string uName = "@" + ParameterPrefix + p.BaseName;
        string sql = String.Format("SET {0}={1}", uName, pName);

        if (command.Connection.Settings.AllowBatch && !preparing)
        {
          setSql.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", delimiter, sql);
          delimiter = "; ";
        }
        else
        {
          MySqlCommand cmd = new MySqlCommand(sql, command.Connection);
          cmd.Parameters.Add(p);
          cmd.ExecuteNonQuery();
        }
      }
      if (setSql.Length > 0)
        setSql.Append("; ");
      return setSql.ToString();
    }

    private string CreateCallStatement(string spName, MySqlParameter returnParameter, MySqlParameterCollection parms)
    {
      StringBuilder callSql = new StringBuilder();

      string delimiter = String.Empty;
      foreach (MySqlParameter p in parms)
      {
        if (p.Direction == ParameterDirection.ReturnValue) continue;

        string pName = "@" + p.BaseName;
        string uName = "@" + ParameterPrefix + p.BaseName;

        bool useRealVar = p.Direction == ParameterDirection.Input || ServerProvidingOutputParameters;
        callSql.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", delimiter, useRealVar ? pName : uName);
        delimiter = ", ";
      }

      if (returnParameter == null)
        return String.Format("CALL {0}({1})", spName, callSql.ToString());
      else
        return String.Format("SET @{0}{1}={2}({3})", ParameterPrefix, returnParameter.BaseName, spName, callSql.ToString());
    }

    private string CreateOutputSelect(MySqlParameterCollection parms, bool preparing)
    {
      StringBuilder outSql = new StringBuilder();

      string delimiter = String.Empty;
      foreach (MySqlParameter p in parms)
      {
        if (p.Direction == ParameterDirection.Input) continue;
        if ((p.Direction == ParameterDirection.InputOutput ||
            p.Direction == ParameterDirection.Output) &&
            ServerProvidingOutputParameters) continue;
        string pName = "@" + p.BaseName;
        string uName = "@" + ParameterPrefix + p.BaseName;

        outSql.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", delimiter, uName);
        delimiter = ", ";
      }

      if (outSql.Length == 0) return String.Empty;

      if (command.Connection.Settings.AllowBatch && !preparing)
        return String.Format(";SELECT {0}", outSql.ToString());

      _outSelect = String.Format("SELECT {0}", outSql.ToString());
      return String.Empty;
    }

    internal void ProcessOutputParameters(MySqlDataReader reader)
    {
      // We apparently need to always adjust our output types since the server
      // provided data types are not always right
      AdjustOutputTypes(reader);

      if ((reader.CommandBehavior & CommandBehavior.SchemaOnly) != 0)
        return;

      // now read the output parameters data row
      reader.Read();

      string prefix = "@" + StoredProcedure.ParameterPrefix;

      for (int i = 0; i < reader.FieldCount; i++)
      {
        string fieldName = reader.GetName(i);
        if (fieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          fieldName = fieldName.Remove(0, prefix.Length);
        MySqlParameter parameter = command.Parameters.GetParameterFlexible(fieldName, true);
        parameter.Value = reader.GetValue(i);
      }
    }

    private void AdjustOutputTypes(MySqlDataReader reader)
    {
      // since MySQL likes to return user variables as strings
      // we reset the types of the readers internal value objects
      // this will allow those value objects to parse the string based
      // return values
      for (int i = 0; i < reader.FieldCount; i++)
      {
        string fieldName = reader.GetName(i);
        if (fieldName.IndexOf(StoredProcedure.ParameterPrefix) != -1)
          fieldName = fieldName.Remove(0, StoredProcedure.ParameterPrefix.Length + 1);
        MySqlParameter parameter = command.Parameters.GetParameterFlexible(fieldName, true);

        IMySqlValue v = MySqlField.GetIMySqlValue(parameter.MySqlDbType);
        if (v is MySqlBit)
        {
          MySqlBit bit = (MySqlBit)v;
          bit.ReadAsString = true;
          reader.ResultSet.SetValueObject(i, bit);
        }
        else
          reader.ResultSet.SetValueObject(i, v);
      }
    }

    public override void Close(MySqlDataReader reader)
    {
      if (String.IsNullOrEmpty(_outSelect)) return;
      if ((reader.CommandBehavior & CommandBehavior.SchemaOnly) != 0) return;

      MySqlCommand cmd = new MySqlCommand(_outSelect, command.Connection);
      cmd.InternallyCreated = true;

      using (MySqlDataReader rdr = cmd.ExecuteReader(reader.CommandBehavior))
        ProcessOutputParameters(rdr);
    }
  }
}
