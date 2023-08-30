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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLCommandParser
  {
    public virtual string SqlFragment { get; }
    public virtual char[] States { get; }

    public MySQLCommandParser(string sqlFragment)
    {
      SqlFragment = sqlFragment ?? throw new ArgumentNullException(nameof(sqlFragment));
      States = new char[sqlFragment.Length];

      Parse();
    }

    public virtual IReadOnlyList<int> GetStateIndices(char state, int start = 0, int length = -1)
    {
      if (start < 0 || start >= States.Length)
        throw new ArgumentOutOfRangeException(nameof(start));

      if (length < 0 || length > 0 && start + length > States.Length)
        length = States.Length - start;

      var stateIndices = new List<int>();
      char? lastState = null;

      for (var i = start; i < length; i++)
      {
        var currentState = States[i];

        if (currentState == state && currentState != lastState)
          stateIndices.Add(i);

        lastState = currentState;
      }

      return stateIndices.AsReadOnly();
    }

    public virtual IReadOnlyList<int> GetStateIndices(char[] states, int start = 0, int length = -1)
    {
      if (states == null)
        throw new ArgumentNullException(nameof(states));

      if (states.Length <= 0)
        throw new ArgumentOutOfRangeException(nameof(states));

      if (start < 0 || start >= States.Length)
        throw new ArgumentOutOfRangeException(nameof(start));

      if (length < 0 || length > 0 && start + length > States.Length)
        length = States.Length - start;

      var stateIndices = new List<int>();
      char? lastState = null;

      for (var i = start; i < length; i++)
      {
        var currentState = States[i];

        if (currentState != lastState && states.Contains(currentState))
          stateIndices.Add(i);

        lastState = currentState;
      }

      return stateIndices.AsReadOnly();
    }

    protected virtual void Parse()
    {
      var state = '\0';
      var lastChar = '\0';
      var secondTolastChar = '\0';

      for (var i = 0; i < SqlFragment.Length; i++)
      {
        var c = SqlFragment[i];
        var skipProcessing = false;

        if (state == '\'')
        {
          if (c == '\'')
          {
            if (lastChar == '\'')
              lastChar = '\0';
            else
              lastChar = '\'';
          }
          else if (lastChar == '\'')
          {
            state = '\0';
            lastChar = '\0';
            States[i - 1] = state;
          }
        }

        if (state == '"')
        {
          if (c == '"')
          {
            if (lastChar == '"')
              lastChar = '\0';
            else
              lastChar = '"';
          }
          else if (lastChar == '"')
          {
            state = '\0';
            lastChar = '\0';
            States[i - 1] = state;
          }
        }

        if (state == '`')
        {
          if (c == '`')
          {
            if (lastChar == '`')
              lastChar = '\0';
            else
              lastChar = '`';
          }
          else if (lastChar == '`')
          {
            state = '\0';
            lastChar = '\0';
            States[i - 1] = state;
          }
        }

        if (state == '/')
        {
          if (lastChar == '/')
          {
            Debug.Assert(c == '*');
            lastChar = '\0';
          }
          else if (lastChar == '*')
          {
            if (c == '/')
            {
              lastChar = '\0';
              state = '\0';
              skipProcessing = true;
            }
            else
              lastChar = '\0';
          }
          else if (c == '*')
          {
            Debug.Assert(lastChar == '\0');
            lastChar = '*';
          }
          else
            lastChar = '\0';
        }

        if (state == '-')
        {
          if (lastChar == '-')
          {
            if (c == '-' && secondTolastChar == '\0')
              secondTolastChar = '-';
            else if (secondTolastChar == '-' && (c == ' ' || c == '\t'))
            {
              lastChar = '\0';
              secondTolastChar = '\0';
            }
            else
            {
              state = '\0';
              lastChar = '\0';
              States[i - 1] = state;

              if (secondTolastChar == '-')
              {
                secondTolastChar = '\0';
                States[i - 2] = state;
              }
            }
          }
          else
          {
            Debug.Assert(lastChar == '\0');
            Debug.Assert(secondTolastChar == '\0');

            if (c == '\r' || c == '\n')
            {
              state = '\0';
              skipProcessing = true;
            }
          }
        }

        if (state == '#')
        {
          if (c == '\r' || c == '\n')
          {
            state = '\0';
            skipProcessing = true;
          }
        }

        if (state == '\0' && !skipProcessing)
        {
          if (c == '"')
            state = '"';
          else if (c == '\'')
            state = '\'';
          else if (c == '`')
            state = '`';
          else if (c == '/')
          {
            state = '/';
            lastChar = '/';
          }
          else if (c == '-')
          {
            state = '-';
            lastChar = '-';
          }
          else if (c == '#')
            state = '#';
          else if (c == '@')
            state = '@';
          else if (c == ';')
            States[i] = ';';
        }
        else if (state == '@')
        {
          if (c == '@')
            States[i - 1] = '$';

          state = '\0';
        }

        if (state != '\0')
          States[i] = state;
      }

      if (state == '\'' && lastChar == '\'' ||
          state == '"' && lastChar == '"' ||
          state == '`' && lastChar == '`' ||
          state == '/' && lastChar == '/' ||
          state == '-' && lastChar == '-')
      {
        state = '\0';
        lastChar = '\0';
        States[States.Length - 1] = state;
      }
    }
  }
}
