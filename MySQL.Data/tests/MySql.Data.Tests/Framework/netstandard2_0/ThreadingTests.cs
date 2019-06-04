// Copyright © 2013, 2016, Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Threading;
using System.Collections;

namespace MySql.Data.MySqlClient.Tests
{
  public class ThreadingTests : TestBase
  {
    public ThreadingTests(TestFixture fixture) : base(fixture)
    {
      TableCache.DumpCache();
    }

    private void MultipleThreadsWorker(object ev)
    {
      (ev as ManualResetEvent).WaitOne();

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #17106 MySql.Data.MySqlClient.CharSetMap.GetEncoding thread synchronization issue
    /// </summary>
    [Fact]
    public void MultipleThreads()
    {
      GenericListener myListener = new GenericListener();
      ManualResetEvent ev = new ManualResetEvent(false);
      ArrayList threads = new ArrayList();
      System.Diagnostics.Trace.Listeners.Add(myListener);

      for (int i = 0; i < 20; i++)
      {
        ParameterizedThreadStart ts = new ParameterizedThreadStart(MultipleThreadsWorker);
        Thread t = new Thread(ts);
        threads.Add(t);
        t.Start(ev);
      }
      // now let the threads go
      ev.Set();

      // wait for the threads to end
      int x = 0;
      while (x < threads.Count)
      {
        while ((threads[x] as Thread).IsAlive)
          Thread.Sleep(50);
        x++;
      }
    }

    private Exception lastException;

    /// <summary>
    /// Bug #54012  	MySql Connector/NET is not hardened to deal with 
    /// ThreadAbortException
    /// </summary>
    private void HardenedThreadAbortExceptionWorker()
    {
      try
      {
        using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
        {
          c.Open();
          MySqlCommand cmd = new MySqlCommand(
              "SELECT BENCHMARK(10000000000,ENCODE('hello','goodbye'))",
              c);
          // ThreadAbortException is not delivered, when thread is 
          // stuck in system call. To shorten test time, set command 
          // timeout to a small value. Note .shortening command timeout
          // means we could actually have timeout exception here too, 
          // but it seems like CLR delivers ThreadAbortException, if 
          // the  thread was aborted.
          cmd.CommandTimeout = 2;
          cmd.ExecuteNonQuery();
        }
      }
      catch (Exception e)
      {
        lastException = e;
      }
    }

    [Fact(Skip="Fix This")]
    public void HardenedThreadAbortException()
    {
      Thread t = new Thread(new ThreadStart(HardenedThreadAbortExceptionWorker));
      t.Name = "Execute Query";
      t.Start();
      Thread.Sleep(500);
      t.Abort();
      t.Join();

      Assert.NotNull(lastException);

      if (lastException is MySqlException)
      {
        // In some runs the ThreadAbortException comes as an inner exception
        lastException = lastException.InnerException;
      }

      //Assert.InstanceOf(typeof(ThreadAbortException), lastException);
      Assert.IsType<ThreadAbortException>(lastException);
    }
  }
}
