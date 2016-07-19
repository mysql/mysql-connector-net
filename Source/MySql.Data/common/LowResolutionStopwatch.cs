// Copyright © 2009, 2016 Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.Common
{
  /// <summary>
  /// This class is modeled after .NET Stopwatch. It provides better
  /// performance (no system calls).It is however less precise than
  /// .NET Stopwatch, measuring in milliseconds. It is adequate to use
  /// when high-precision is not required (e.g for measuring IO timeouts),
  /// but not for other tasks.
  /// </summary>
  internal class LowResolutionStopwatch
  {
    long _startTime;
    public static readonly long Frequency = 1000; // measure in milliseconds
    public static readonly bool IsHighResolution = false;

    public LowResolutionStopwatch()
    {
      ElapsedMilliseconds = 0;
    }
    public long ElapsedMilliseconds { get; private set; }

    public void Start()
    {
      _startTime = Environment.TickCount;
    }

    public void Stop()
    {
      long now = Environment.TickCount;
      // Calculate time different, handle possible overflow
      long elapsed = (now < _startTime) ? Int32.MaxValue - _startTime + now : now - _startTime;
      ElapsedMilliseconds += elapsed;
    }

    public void Reset()
    {
      ElapsedMilliseconds = 0;
      _startTime = 0;
    }

    public TimeSpan Elapsed => new TimeSpan(0, 0, 0, 0, (int)ElapsedMilliseconds);

    public static LowResolutionStopwatch StartNew()
    {
      LowResolutionStopwatch sw = new LowResolutionStopwatch();
      sw.Start();
      return sw;
    }

    public static long GetTimestamp()
    {
      return Environment.TickCount;
    }

    bool IsRunning()
    {
      return (_startTime != 0);
    }
  }
}
