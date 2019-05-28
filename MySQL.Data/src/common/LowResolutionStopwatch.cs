// Copyright (c) 2009, 2019, Oracle and/or its affiliates. All rights reserved.
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
      long elapsed;

      // Calculate time different, handle possible overflow
      if (now < _startTime)
      {
        if (now < 0)
          elapsed = 1 + ((long)Int32.MaxValue - _startTime) + (now - (long)Int32.MinValue);
        else
          elapsed = Int32.MaxValue - _startTime + now;
      }
      else
        elapsed = now - _startTime;

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
