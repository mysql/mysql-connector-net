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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;

namespace MySql.EntityFrameworkCore.Internal
{
  internal class MySQLRetryingExecutionStrategy : ExecutionStrategy
  {
    private readonly ICollection<int> _additionalErrorNumbers;

    /// <summary>
    ///   Creates a new instance of <see cref="MySQLRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="context"> The context on which the operations will be invoked. </param>
    /// <remarks>
    ///   The default retry limit is 6, which means that the total amount of time spent before failing is about a minute.
    /// </remarks>
    public MySQLRetryingExecutionStrategy(
      [NotNull] DbContext context)
      : this(context, DefaultMaxRetryCount)
    {
    }

    /// <summary>
    ///   Creates a new instance of <see cref="MySQLRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    public MySQLRetryingExecutionStrategy(
      [NotNull] ExecutionStrategyDependencies dependencies)
      : this(dependencies, DefaultMaxRetryCount)
    {
    }

    /// <summary>
    ///   Creates a new instance of <see cref="MySQLRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="context"> The context used to invoke the operations. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    public MySQLRetryingExecutionStrategy(
      [NotNull] DbContext context,
      int maxRetryCount)
      : this(context, maxRetryCount, DefaultMaxDelay, errorNumbersToAdd: null)
    {
    }

    /// <summary>
    ///   Creates a new instance of <see cref="MySQLRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    public MySQLRetryingExecutionStrategy(
      [NotNull] ExecutionStrategyDependencies dependencies,
      int maxRetryCount)
      : this(dependencies, maxRetryCount, DefaultMaxDelay, errorNumbersToAdd: null)
    {
    }

    /// <summary>
    ///   Creates a new instance of <see cref="MySQLRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="context"> The context used to invoke the operations. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    /// <param name="maxRetryDelay"> The maximum delay between retries. </param>
    /// <param name="errorNumbersToAdd"> Additional SQL error numbers that should be considered transient. </param>
    public MySQLRetryingExecutionStrategy(
      [NotNull] DbContext context,
      int maxRetryCount,
      TimeSpan maxRetryDelay,
      ICollection<int>? errorNumbersToAdd)
      : base(context,
        maxRetryCount,
        maxRetryDelay)
      => _additionalErrorNumbers = errorNumbersToAdd!;

    /// <summary>
    ///   Creates a new instance of <see cref="MySQLRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    /// <param name="maxRetryDelay"> The maximum delay between retries. </param>
    /// <param name="errorNumbersToAdd"> Additional SQL error numbers that should be considered transient. </param>
    public MySQLRetryingExecutionStrategy(
      [NotNull] ExecutionStrategyDependencies dependencies,
      int maxRetryCount,
      TimeSpan maxRetryDelay,
      ICollection<int>? errorNumbersToAdd)
      : base(dependencies, maxRetryCount, maxRetryDelay)
      => _additionalErrorNumbers = errorNumbersToAdd!;

    protected override bool ShouldRetryOn(Exception exception)
      => exception is MySqlException mySqlException &&
         _additionalErrorNumbers?.Contains(mySqlException.Number) == true
         || MySQLTransientExceptionDetector.ShouldRetryOn(exception);
  }
}
