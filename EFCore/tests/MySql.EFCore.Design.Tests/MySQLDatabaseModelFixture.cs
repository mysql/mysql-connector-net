// Copyright © 2017, 2018, Oracle and/or its affiliates. All rights reserved.
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
  public partial class MySQLDatabaseModelFixture : IDisposable
  {
    public MySQLTestStore teststore { get; set; }
    public string dbName { get; set; }

    public MySQLDatabaseModelFixture()
    {
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
