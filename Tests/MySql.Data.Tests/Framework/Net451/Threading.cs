// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using System.Data;
using NUnit.Framework;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Collections.Specialized;

namespace MySql.Data.MySqlClient.Tests
{
  public class GenericListener : TraceListener
  {
    StringCollection strings;
    StringBuilder partial;

    public GenericListener()
    {
      strings = new StringCollection();
      partial = new StringBuilder();
    }

    public StringCollection Strings
    {
      get { return strings; }
    }

    public int Find(string sToFind)
    {
      int count = 0;
      foreach (string s in strings)
        if (s.IndexOf(sToFind) != -1)
          count++;
      return count;
    }

    public void Clear()
    {
      partial.Remove(0, partial.Length);
      strings.Clear();
    }

    public override void Write(string message)
    {
      partial.Append(message);
    }

    public override void WriteLine(string message)
    {
      Write(message);
      strings.Add(partial.ToString());
      partial.Remove(0, partial.Length);
    }

    public int CountLinesContaining(string text)
    {
      int count = 0;
      foreach (string s in strings)
        if (s.Contains(text)) count++;
      return count;
    }
  }

  /// <summary>
  /// Summary description for ConnectionTests.
  /// </summary>
  [TestFixture]
  public class ThreadingTests : BaseTest
  {
    private void MultipleThreadsWorker(object ev)
    {
      (ev as ManualResetEvent).WaitOne();

      using (MySqlConnection c = new MySqlConnection(GetConnectionString(true)))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #17106 MySql.Data.MySqlClient.CharSetMap.GetEncoding thread synchronization issue
    /// </summary>
    [Test]
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

    private volatile Exception lastException;

    /// <summary>
    /// Bug #54012  	MySql Connector/NET is not hardened to deal with 
    /// ThreadAbortException
    /// </summary>
    private void HardenedThreadAbortExceptionWorker()
    {
      try
      {
        using (MySqlConnection c = new MySqlConnection(GetConnectionString(true)))
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

    [Test]
    public void HardenedThreadAbortException()
    {
      Thread t = new Thread(new ThreadStart(HardenedThreadAbortExceptionWorker));
      t.Name = "Execute Query";
      t.Start();
      Thread.Sleep(500);
      t.Abort();
      t.Join();

      Assert.IsNotNull(lastException);

      if (lastException is MySqlException)
      {
        // In some runs the ThreadAbortException comes as an inner exception
        lastException = lastException.InnerException;
      }

      Assert.IsInstanceOf(typeof(ThreadAbortException), lastException);
    }
    
    /// <summary>
    /// Test fix for bug Connection String Cache corrupted when using Multithreading + MySqlScript
    /// (http://bugs.mysql.com/bug.php?id=68217).
    /// </summary>
    [Test]
    [Timeout(100000)]
    public void MultipleThreadsMysqlScript()
    {
      //GenericListener myListener = new GenericListener();
      ManualResetEvent ev = new ManualResetEvent(false);
      ArrayList threads = new ArrayList();
      //System.Diagnostics.Trace.Listeners.Add(myListener);
      lastException = null;
      for (int i = 0; i < 20; i++)
      {
        ParameterizedThreadStart ts = new ParameterizedThreadStart(MultipleThreadsWorkerScript);
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
        while ((threads[x] as Thread).IsAlive && lastException == null)
          Thread.Sleep(50);
        x++;
      }
      if (lastException != null)
        Assert.Fail("At least one of the threads got \"{0}\"", lastException.Message);
    }

    private void MultipleThreadsWorkerScript(object ev)
    {
      MySqlScript script = new MySqlScript();
      script.Query = "show variables like '%ssl%'";
      (ev as ManualResetEvent).WaitOne();
      string strConn = GetConnectionString(true) + ";Pooling=true;";
      MySqlConnection c = new MySqlConnection(strConn);
      
      int result;
      script.Connection = c;
      try
      {
        for (int i = 0; i < 10; i++)
        {
          c.Open();
          result = script.Execute();
          c.Close();
        }
      }
      catch (InvalidOperationException ioe)
      {
        //Assert.AreEqual("Collection was modified; enumeration operation may not execute.", ioe.Message );
        //Assert.Fail();
        lastException = ioe;
      }
      catch (Exception e)
      {
        lastException = e;
      }
    }
  }
}
