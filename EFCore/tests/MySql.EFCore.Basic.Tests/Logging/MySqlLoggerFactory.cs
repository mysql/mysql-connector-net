// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
#if !NETCORE10
using System.Runtime.Remoting.Messaging;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Basic.Tests.Logging
{
    public class MySqlLoggerFactory : ILoggerFactory
    {
        private static MySqlLogger _logger;
        private static MySqlLogger Logger => LazyInitializer.EnsureInitialized(ref _logger);

        public static IReadOnlyList<string> SqlStatements
        {
            get
            {
                return Logger.MySqlLoggerData.mySqlStatements;
            }
        } 

        public static IReadOnlyList<DbCommandLogData> CommandLogData => Logger.MySqlLoggerData.logData;

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return Logger;
        }

        public void Dispose()
        {            
        }

        public static string Log => Logger.MySqlLoggerData.output;
        public static string Sql => string.Join(Environment.NewLine, Logger.MySqlLoggerData.mySqlStatements);
        
        public static void Reset() => Logger.ResetLoggerData();


        private class MySqlLogger : ILogger
        {

#if NETCORE10            
            private readonly static AsyncLocal<MySqlLoggerData> loggerData = new AsyncLocal<MySqlLoggerData>();
#else
            private const string contextName = "_LogMySQL";
#endif

            public MySqlLoggerData MySqlLoggerData
            {
                get
                {
#if NETCORE10
                    var lgData = loggerData.Value;
#else
                    var lgData = (MySqlLoggerData)CallContext.LogicalGetData(contextName);
#endif
                    return lgData ?? CreateLoggerData();
                }
            }

            private static MySqlLoggerData CreateLoggerData()
            {
                var lgData = new MySqlLoggerData();
#if NETCORE10
                loggerData.Value = lgData;
#else
                CallContext.LogicalSetData(contextName, lgData);
#endif
                return lgData;

            }


            public IDisposable BeginScope<TState>(TState state)
            {                
                return MySqlLoggerData.stmt;
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {

                var format = formatter(state, exception)?.Trim();
                if (format != null)
                {
                    var mysqlLoggerData = MySqlLoggerData;
                    var cmdLog = state as DbCommandLogData;

                    if (cmdLog != null)
                    {
                        var parameters = "";

                        if (cmdLog.Parameters.Any())
                        {
                            parameters = string.Join(Environment.NewLine , cmdLog.Parameters.Select(p => $"{p.Name}: {p.Value.ToString()}"));
                        }

                        mysqlLoggerData.mySqlStatements.Add(parameters + cmdLog.CommandText);

                        mysqlLoggerData.logData.Add(cmdLog);
                    }

                    else
                    {
                        mysqlLoggerData.stmt.AppendLine(format);
                    }

                    mysqlLoggerData.xUnitOutputHelper?.WriteLine(format + Environment.NewLine);
                }
            }

            internal void ResetLoggerData()
            {
#if NETCORE10
                loggerData.Value = null;
#else
                CallContext.LogicalSetData(contextName, null);
#endif
            }
        }


        private class MySqlLoggerData
        {
            public string output => stmt.ToString();

#if !NETCORE10
            [NonSerialized]
#endif
            public readonly HelperDisposable stmt = new HelperDisposable(new StringBuilder(""));
            public readonly List<string> mySqlStatements = new List<string>();
#if !NETCORE10
            [NonSerialized]
#endif
            public readonly List<DbCommandLogData> logData = new List<DbCommandLogData>();

#if !NETCORE10
            [NonSerialized]
#endif
            public ITestOutputHelper xUnitOutputHelper;
        }

        private class HelperDisposable : IDisposable
        {
            private readonly StringBuilder _stringBuilder;

            public HelperDisposable(StringBuilder stringBuilder)
            {
                _stringBuilder = stringBuilder;                
            }            

            public void Dispose()
            {
                _stringBuilder.Append("");
            }

            public override string ToString() => _stringBuilder.ToString();

            public HelperDisposable AppendLine(string stringBuilder)
            {
                _stringBuilder.AppendLine(stringBuilder);
                return this;
            }
        }
    }
}
