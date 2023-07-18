using System;
using System.Threading;

namespace MySql.Data.MySqlClient;

internal class Releaser : IDisposable
{
    private readonly SemaphoreSlim semaphoreSlim;

    public Releaser(SemaphoreSlim semaphoreSlim)
    {
        this.semaphoreSlim = semaphoreSlim;
    }

    public void Dispose()
    {
        semaphoreSlim.Release();
    }
}