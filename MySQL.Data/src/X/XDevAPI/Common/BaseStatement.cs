// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
        Session.scheduler);
    }
  }
}
