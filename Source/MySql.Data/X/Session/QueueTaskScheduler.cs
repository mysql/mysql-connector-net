// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Session
{
  /// <summary>
  /// Implementation class for object that manages low-level work of queuing tasks onto threads.
  /// </summary>  
  internal class QueueTaskScheduler : TaskScheduler
  {
    private readonly object lockObject = new object();

    protected override IEnumerable<Task> GetScheduledTasks()
    {
      throw new NotImplementedException();
    }

    protected override void QueueTask(Task task)
    {
      ThreadPool.QueueUserWorkItem(_ =>
      {
        lock (lockObject)
        {
          System.Threading.Thread.CurrentThread.Name = "mysqlx";
          base.TryExecuteTask(task);
        }
      });
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      throw new NotImplementedException();
    }

  }
}
