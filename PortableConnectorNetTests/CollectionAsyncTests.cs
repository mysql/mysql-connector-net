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

using MySql.XDevAPI;
using MySql.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests
{
  public class CollectionAsyncTests : BaseTest, IDisposable
  {
    Collection coll;

    public CollectionAsyncTests()
    {
      coll = CreateCollection("test");
    }

    [Fact]
    public void CollectionInsert()
    {
      List<Task<UpdateResult>> tasksList = new List<Task<UpdateResult>>();

      for (int i = 1; i <= 200; i++)
      {
        tasksList.Add(coll.Add(string.Format(@"{{ ""_id"": {0}, ""foo"": {0} }}", i)).ExecuteAsync());
      }

      Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2));
      foreach(Task<UpdateResult> task in tasksList)
      {
        Assert.True(task.Result.Succeeded);
      }
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          coll.Drop();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~CollectionAsyncTests() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion


  }
}
