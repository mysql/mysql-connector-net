// Copyright (c) 2013 Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Spatial;
using System.Data.Entity.Infrastructure;
using MySql.Data.MySqlClient;

/*
 * This file implement the retry policy for MySql server and several backoff algorithms for different transient errors,
 * these algorithms include recommendations gathered on input from MySql Support team.
 * */

namespace MySql.Data.Entity
{
  /// <summary>
  /// An execution strategy tailored for handling MySql Server transient errors.
  /// </summary>
  public class MySqlExecutionStrategy : DbExecutionStrategy
  {
    /// <summary>
    /// Different back off algorithms used for different errors.
    /// </summary>
    private static Dictionary<int, BackoffAlgorithm> errorsToRetryOn = new Dictionary<int, BackoffAlgorithm>();

    static MySqlExecutionStrategy()
    {
      errorsToRetryOn.Add( 1040, new BackoffAlgorithmErr1040() );  // Too many connections.
      errorsToRetryOn.Add( 1205, new BackoffAlgorithmErr1205() );  // Lock wait timeout exceeded; try restarting transaction.
      errorsToRetryOn.Add( 1213, new BackoffAlgorithmErr1213() );  // Deadlock found when trying to get lock; try restarting transaction.
      errorsToRetryOn.Add( 1614, new BackoffAlgorithmErr1614() );  // Transaction branch was rolled back: deadlock was detected.
      errorsToRetryOn.Add( 2006, new BackoffAlgorithmErr2006() );  // Server has gone away.
      errorsToRetryOn.Add( 2013, new BackoffAlgorithmErr2013() );  // Lost connection to MySQL server during query.
      // TODO: Add MySql Cluster (Ndb) errors when Connector/NET correctly reports Cluster errors...
      // ...
    }

    public MySqlExecutionStrategy()
    {
      // A new strategy instance will just reset all the backoff logic.
      foreach (BackoffAlgorithm ba in errorsToRetryOn.Values)
      {
        ba.Reset();
      }
    }

    protected override TimeSpan? GetNextDelay(Exception lastException)
    {
      MySqlTrace.LogInformation(1, "Re");
      MySqlException myex = lastException as MySqlException;
      BackoffAlgorithm algorithm = null;
      if (!errorsToRetryOn.TryGetValue(myex.Number, out algorithm))
      {
        // This must never happen
        throw new InvalidOperationException( string.Format( "Trying to retry for non transient exception number: {0}, message: {1}", myex.Number, myex.Message ));
      }
      TimeSpan? ts = algorithm.GetNextDelay();
      if( ts != null )
        MySqlTrace.LogInformation(1, string.Format( "Retrying query for exception {0}", myex ));
      return ts;
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
      if (exception == null)
        return false;

      MySqlException myex = exception as MySqlException;
      BackoffAlgorithm algorithm = null;
      if (myex != null && errorsToRetryOn.TryGetValue(myex.Number, out algorithm))
        return true;
      else
        return false;
    }
  }

  /// <summary>
  /// The base class for backoff algorithms.
  /// </summary>
  /// <remarks>Different transient error conditions require different approaches.</remarks>
  public abstract class BackoffAlgorithm
  {
    //protected static Random RandomGen = new Random();
    protected const int DEFAULT_MAX_RETRIES = 3;
    protected static readonly TimeSpan DEFAULT_MAX_DELAY = TimeSpan.FromSeconds(60);
    protected TimeSpan _maxDelay;
    protected int _maxRetries = 1;
    protected int _totalRetries;

    public BackoffAlgorithm() : this( DEFAULT_MAX_RETRIES, DEFAULT_MAX_DELAY )
    {
    }

    public BackoffAlgorithm( int maxRetries, TimeSpan maxDelay )
    {
      _maxRetries = maxRetries;
      _maxDelay = maxDelay;
      Reset();
    }

    /// <summary>
    /// The default implementation is an exponential delay backoff.
    /// </summary>
    /// <returns></returns>
    public virtual TimeSpan? GetNextDelay()
    {
      double delay = ( ( Math.Pow( 2d, ++_totalRetries ) - 1d ) / 2d );
      if( _totalRetries > _maxRetries ) return null;
      delay = Math.Min(_maxDelay.TotalSeconds, delay );
      return TimeSpan.FromSeconds( delay );
    }

    /// <summary>
    /// Resets a backoff algorithm, so they can be reused.
    /// </summary>
    public virtual void Reset()
    {
      _totalRetries = 0;
    }
  }

  /// <summary>
  /// Back-off algorithm customized for the MySql error code 1040 - Too many connections.
  /// </summary>
  public class BackoffAlgorithmErr1040 : BackoffAlgorithm
  {
    public BackoffAlgorithmErr1040() : base( BackoffAlgorithm.DEFAULT_MAX_RETRIES * 2, TimeSpan.FromSeconds( 100 ) )
    {
    }

    public override TimeSpan? GetNextDelay()
    {
      // Do twice the exponential step delay
      _totalRetries++;
      return base.GetNextDelay();
    }
  }

  /// <summary>
  /// Back-off algorithm for the Mysql error code 1614 - Transaction branch was rolled back: deadlock was detected.
  /// </summary>
  public class BackoffAlgorithmErr1614 : BackoffAlgorithm
  {
    public BackoffAlgorithmErr1614()
      : base()
    {
    }
  }

  /// <summary>
  /// Back-off algorithm customized for the MySql error code 1205 - Lock wait timeout exceeded; try restarting transaction.
  /// </summary>
  public class BackoffAlgorithmErr1205 : BackoffAlgorithm
  {
    public BackoffAlgorithmErr1205()
      : base(BackoffAlgorithm.DEFAULT_MAX_RETRIES * 2, BackoffAlgorithm.DEFAULT_MAX_DELAY)
    {
    }
  }

  /// <summary>
  /// Back-off algorithm customized for MySql error code 1213 - Deadlock found when trying to get lock; try restarting transaction.
  /// </summary>
  public class BackoffAlgorithmErr1213 : BackoffAlgorithm
  {
    public BackoffAlgorithmErr1213()
      : base(BackoffAlgorithm.DEFAULT_MAX_RETRIES * 2, BackoffAlgorithm.DEFAULT_MAX_DELAY)
    {
    }
  }

  /// <summary>
  /// Back-off algorithm customized for MySql error code 2006 - MySQL server has gone away.
  /// </summary>
  public class BackoffAlgorithmErr2006 : BackoffAlgorithm
  {
    public BackoffAlgorithmErr2006()
    {
    }
  }

  /// <summary>
  /// Back-off algorithm customized for MySql error code 2013 - Lost connection to MySQL server during query.
  /// </summary>
  public class BackoffAlgorithmErr2013 : BackoffAlgorithm
  {
    public BackoffAlgorithmErr2013()
    {
    }

    public override TimeSpan? GetNextDelay()
    {
      // for error code 2013, wait at least 10 seconds.
      TimeSpan? delay = base.GetNextDelay();
      if (delay == null) return delay;
      else if (delay.Value.TotalSeconds < 10)
        return delay.Value.Add(TimeSpan.FromSeconds(10));
      else
      {
        return delay;
      }
    }
  }

  /// <summary>
  /// Back-off algorithm customized for MySql Cluster (NDB) errors.
  /// </summary>
  public class BackoffAlgorithmNdb : BackoffAlgorithm
  {
    public BackoffAlgorithmNdb()
      : base()
    {
    }
  }
}