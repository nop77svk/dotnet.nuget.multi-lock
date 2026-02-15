namespace NoP77svk.Threading;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using NoP77svk.Threading.Infrastructure;

public class MultiSemaphoreSlim<TKey>
    : IHalfAsyncMultiLock<TKey>
{
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new();

    public async ValueTask TryAcquireLockAsync(TKey key)
    {
        SemaphoreSlim semaphore = GetOrCreateSemaphore(key);
        await semaphore.WaitAsync();

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, semaphore);
    }

    public async ValueTask<bool> TryAcquireLockAsync(TKey key, TimeSpan lockAcquireTimeout)
    {
        SemaphoreSlim semaphore = GetOrCreateSemaphore(key);
        bool lockAcquired = await semaphore.WaitAsync(lockAcquireTimeout);

        if (lockAcquired)
        {
            // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
            _locks.TryAdd(key, semaphore);
        }

        return lockAcquired;
    }

    public async ValueTask<bool> TryAcquireLockAsync(TKey key, int lockAcquireTimeout)
        => await TryAcquireLockAsync(key, lockAcquireTimeout.MillisecondsToTimeSpan());

    public void ReleaseLock(TKey key)
    {
        _locks.TryRemove(key, out var semaphore);
        semaphore.Release();
    }

    public async ValueTask<IDisposable> AcquireAutoReleaseLockAsync(TKey key)
    {
        await TryAcquireLockAsync(key);
        return new AutoDisposer(() => ReleaseLock(key));
    }

    public async ValueTask<IDisposable> AcquireAutoReleaseLockAsync(TKey key, TimeSpan lockAcquireTimeout)
    {
        if (!await TryAcquireLockAsync(key, lockAcquireTimeout))
        {
            throw new TimeoutException($"Failed to acquire lock on key {key} in {lockAcquireTimeout}");
        }

        return new AutoDisposer(() => ReleaseLock(key));
    }

    public async ValueTask<IDisposable> AcquireAutoReleaseLockAsync(TKey key, int lockAcquireTimeoutMilliseconds)
        => await AcquireAutoReleaseLockAsync(key, lockAcquireTimeoutMilliseconds.MillisecondsToTimeSpan());

    private SemaphoreSlim GetOrCreateSemaphore(TKey key)
        => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1));
}
