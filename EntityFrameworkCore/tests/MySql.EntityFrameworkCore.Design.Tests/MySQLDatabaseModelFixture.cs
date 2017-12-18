// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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


using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using MySql.Data.EntityFrameworkCore.Design.Internal;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using MySql.Data.EntityFrameworkCore.Tests;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using MySql.Data.EntityFrameworkCore.Scaffolding.Internal;

namespace MySql.EntityFrameworkCore.Design.Tests
{
    public class MySQLDatabaseModelFixture : IDisposable
    {
        public MySQLTestStore teststore { get; set; }
        public string dbName { get; set; }

        public MySQLDatabaseModelFixture()
        {            
        }

        public DatabaseModel CreateModel(string sql, TableSelectionSet selection, ILogger logger = null, bool executeScript=false)
        {
            if (executeScript)
                MySQLTestStore.ExecuteScript(sql);
            else
                MySQLTestStore.Execute(sql);

            return new MySQLDatabaseModelFactory(new MyTestLoggerFactory(logger).CreateLogger<MySQLDatabaseModelFactory>()).
                   Create(MySQLTestStore.rootConnectionString + ";database=" + dbName + ";", selection ?? TableSelectionSet.All);
        }

        public void Dispose()
        {
            MySQLTestStore.Execute("drop database " + dbName + ";");
        }

        class MyTestLoggerFactory : ILoggerFactory
        {
            readonly ILogger _logger;

            public MyTestLoggerFactory(ILogger logger)
            {
                _logger = logger ?? new MyTestLogger();
            }

            public void AddProvider(ILoggerProvider provider)
            {
            }

            public ILogger CreateLogger(string categoryName) => _logger;

            public void Dispose()
            {
            }
        }
        public class MyTestLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                => Items.Add(new { logLevel, eventId, state, exception });

            public bool IsEnabled(LogLevel logLevel) => true;

            public ICollection<dynamic> Items = new List<dynamic>();
        }
    }
}
