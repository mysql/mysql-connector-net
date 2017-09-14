// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.MySqlClient;
using MySql.Data.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
    internal class MySQLRelationalCommand : RelationalCommand
    {

        public MySQLRelationalCommand([NotNull] ISensitiveDataLogger logger,
            [NotNull] DiagnosticSource diagnosticSource,
            [NotNull] string commandText,
            [NotNull] IReadOnlyList<IRelationalParameter> parameters)
            : base(logger, diagnosticSource, commandText, parameters)
        {
        }

        protected override object Execute(IRelationalConnection connection, string executeMethod, IReadOnlyDictionary<string, object> parameterValues, bool closeConnection)
        {
            ThrowIf.Argument.IsNull(connection, nameof(connection));
            ThrowIf.Argument.IsNull(executeMethod, nameof(executeMethod));
            var dbCommand = CreateCommand(connection, parameterValues);
            object result = null;

            if (connection.DbConnection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            if (executeMethod.Equals(nameof(ExecuteReader)))
            {
                try
                {
                    result = new MySQLRelationalDataReader(connection, dbCommand, new MySQLDataReader(((MySqlCommand)dbCommand).ExecuteReader() as MySqlDataReader));
                    return result;
                }
                catch
                {
                    dbCommand.Dispose();
                    throw;
                }
            }

            return base.Execute(connection, executeMethod, parameterValues, closeConnection);
        }

        protected override async Task<object> ExecuteAsync(IRelationalConnection connection, string executeMethod, IReadOnlyDictionary<string, object> parameterValues, bool closeConnection, CancellationToken cancellationToken = default(CancellationToken))
        {

            ThrowIf.Argument.IsNull(connection, nameof(connection));
            ThrowIf.Argument.IsNull(executeMethod, nameof(executeMethod));

            var dbCommand = CreateCommand(connection, parameterValues);
            object result = null;

            await connection.OpenAsync(cancellationToken);

            if (executeMethod.Equals(nameof(ExecuteReader)))
            {
                try
                {
                    result = new RelationalDataReader(connection,
                                                       dbCommand,
                                                       new MySQLDataReader(await ((MySqlCommand)dbCommand).ExecuteReaderAsync(cancellationToken) as MySqlDataReader));
                    return result;
                }
                catch
                {
                    dbCommand.Dispose();
                    throw;
                }
            }

            return await base.ExecuteAsync(connection, executeMethod, parameterValues, closeConnection, cancellationToken);
        }


        private DbCommand CreateCommand(
           IRelationalConnection connection,
           IReadOnlyDictionary<string, object> parameterValues)
        {
            var command = connection.DbConnection.CreateCommand();
            ((MySqlCommand)command).InternallyCreated = true;
            command.CommandText = CommandText;

            if (connection.CurrentTransaction != null)
            {
                command.Transaction = connection.CurrentTransaction.GetDbTransaction();
            }

            if (connection.CommandTimeout != null)
            {
                command.CommandTimeout = (int)connection.CommandTimeout;
            }

            if (Parameters.Count > 0)
            {
                if (parameterValues == null)
                {
                    throw new InvalidOperationException(
                        RelationalStrings.MissingParameterValue(
                            Parameters[0].InvariantName));
                }

                foreach (var parameter in Parameters)
                {
                    object parameterValue;

                    if (parameterValues.TryGetValue(parameter.InvariantName, out parameterValue))
                    {
                        if (parameterValue != null && parameterValue.GetType().FullName.StartsWith("System.DateTimeOffset"))
                        {
                            DateTimeOffset dto = (DateTimeOffset)parameterValue;
                            DateTime dt = dto.DateTime;
                            
                            if (dt.Year < 1970)
                                 dt = new DateTime(1970, 1, 1, 0, 0, 1);
                            parameter.AddDbParameter(command, dt);
                        }
                        else
                        {
                            parameter.AddDbParameter(command, parameterValue);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            RelationalStrings.MissingParameterValue(parameter.InvariantName));
                    }
                }
            }

            return command;
        }
    }
}
