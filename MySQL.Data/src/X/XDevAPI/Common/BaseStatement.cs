// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI.Relational;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Base abstract class for API statement.
  /// </summary>
  /// <typeparam name="TResult"></typeparam>
  public abstract class BaseStatement<TResult> where TResult : BaseResult
  {
    /// <summary>
    /// Initializes a new instance of the BaseStatement class based on the specified session.
    /// </summary>
    /// <param name="session">The session where the statement will be executed.</param>
    public BaseStatement(BaseSession session)
    {
      Session = session;
    }

    /// <summary>
    /// Gets the <see cref="Session"/> that owns the statement.
    /// </summary>
    public BaseSession Session { get; private set;  }

    /// <summary>
    /// Executes the base statements. This method is intended to be defined by child classes.
    /// </summary>
    /// <returns>A result object containing the details of the execution.</returns>
    public abstract TResult Execute();

    /// <summary>
    /// Executes a statement asynchronously.
    /// </summary>
    /// <returns>A result object containing the details of the execution.</returns>
    public async Task<TResult> ExecuteAsync()
    {
      return await Task.Factory.StartNew<TResult>(() =>
      {
        var result = Execute();
        if (result is BufferingResult<DbDoc>)
        {
          (result as BufferingResult<DbDoc>).FetchAll();
        }
        else if(result is BufferingResult<Row>)
        {
          (result as BufferingResult<Row>).FetchAll();
        }
        return result;
      },
        CancellationToken.None,
        TaskCreationOptions.None,
        Session._scheduler);
    }
  }
}
