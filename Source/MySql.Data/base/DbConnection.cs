using System;

namespace MySql.Data.MySqlClient
{
    public abstract class DbConnection : IDisposable
    {
      public void Dispose()
      {
        throw new NotImplementedException();
      }

      protected virtual void Dispose(bool disposing)
      {
      }
    }

    public enum ConnectionState
    {
      Closed,
      Open,
      Connecting,
      Executing,
      Fetching,
      Broken
    }
}
