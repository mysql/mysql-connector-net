// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLQueryStringFactory : IRelationalQueryStringFactory
  {
    private static readonly Lazy<Regex> _limitExpressionParameterRegex = new Lazy<Regex>(
      () => new Regex($@"(?<=\W)LIMIT\s+(?:(?<leading_offset>@?\w+),\s*)?(?<row_count>@?\w+)(?:\s*OFFSET\s*(?<trailing_offset>@?\w+))?",
        RegexOptions.Singleline | RegexOptions.IgnoreCase));

    private static readonly Lazy<Regex> _extractParameterRegex = new Lazy<Regex>(() => new Regex(@"@\w+"));

    private readonly IRelationalTypeMappingSource _typeMapper;

    public MySQLQueryStringFactory([NotNull] IRelationalTypeMappingSource typeMapper)
    {
      _typeMapper = typeMapper;
    }

    public virtual string Create(DbCommand command)
    {
      if (command.Parameters.Count == 0)
      {
        return command.CommandText;
      }

      PrepareCommand(command);

      var builder = new StringBuilder();
      foreach (DbParameter parameter in command.Parameters)
      {
        builder
          .Append("SET ")
          .Append(parameter.ParameterName)
          .Append(" = ")
          .Append(GetParameterValue(parameter))
          .AppendLine(";");
      }

      return builder
        .AppendLine()
        .Append(command.CommandText).ToString();
    }

    private string? GetParameterValue(DbParameter parameter)
    {
      var typeMapping = _typeMapper.FindMapping(parameter.Value!.GetType());

      return (parameter.Value == DBNull.Value || parameter.Value == null)
        ? "NULL" : typeMapping != null
        ? typeMapping.GenerateSqlLiteral(parameter.Value) : parameter.Value.ToString();
    }

    protected virtual void PrepareCommand(DbCommand command)
    {
      var stringParameterNames = command.Parameters.Cast<DbParameter>()
        .Where(p => p.Value is string)
        .Select(p => p.ParameterName)
        .ToList();

      if (!command.CommandText.Contains("LIMIT", StringComparison.OrdinalIgnoreCase) &&
          !stringParameterNames.Any())
        return;

      var matches = _limitExpressionParameterRegex.Value.Matches(command.CommandText);
      if (matches.Count <= 0 && !stringParameterNames.Any())
        return;

      var limitGroupsWithParameter = matches.SelectMany(m => m.Groups["row_count"].Captures)
        .Concat(matches.SelectMany(m => m.Groups["leading_offset"].Captures))
        .Concat(matches.SelectMany(m => m.Groups["trailing_offset"].Captures))
        .ToList();

      if (!limitGroupsWithParameter.Any() && !stringParameterNames.Any())
        return;

      var parser = new MySQLCommandParser(command.CommandText);
      var parameterPositions = parser.GetStateIndices('@');
      var parameters = parameterPositions
        .Select(i => new
        {
          Index = i,
          ParameterName = _extractParameterRegex.Value.Match(command.CommandText.Substring(i)).Value,
        })
        .Where(t => !string.IsNullOrEmpty(t.ParameterName)).ToList();

      var validParameters = (limitGroupsWithParameter.Where(c => parameterPositions.Contains(c.Index) &&
        command.Parameters.Contains(c.Value)).Select(c => new { Index = c.Index, ParameterName = c.Value }))
        .Concat(stringParameterNames.SelectMany(s => parameters.Where(p => p.ParameterName == s))).Distinct()
        .OrderByDescending(c => c.Index).ToList();

      foreach (var validParameter in validParameters)
      {
        var parameterIndex = validParameter.Index;
        var parameterName = validParameter.ParameterName;

        parameters.RemoveAt(
          parameters.FindIndex(
            t => t.Index == parameterIndex &&
            t.ParameterName == parameterName));

        var parameter = command.Parameters[parameterName];
        var parameterValue = GetParameterValue(parameter);

        command.CommandText = command.CommandText.Substring(0, parameterIndex) +
          parameterValue +
          command.CommandText.Substring(parameterIndex + validParameter.ParameterName.Length);
      }

      foreach (var parameterName in validParameters
        .Select(c => c.ParameterName).Distinct()
        .Where(s => parameters.FindIndex(t => t.ParameterName == s) == -1))
      {
        command.Parameters.RemoveAt(parameterName);
      }
    }
  }
}
