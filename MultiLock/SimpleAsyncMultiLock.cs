namespace NoP77svk.Threading;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public partial class SimpleAsyncMultiLock<TKey>
    : IFullAsyncMultiLock<TKey>
{
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new();

    public TimeSpan? LockAcquireTimeout { get; init; } = null;

    public async ValueTask TryAcquireLockAsync(TKey key)
    {
        SemaphoreSlim semaphore = GetOrCreateSemaphore(key);
        await semaphore.WaitAsync();

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, semaphore);
    }

    public async ValueTask<bool> TryAcquireLockAsync(TKey key, TimeSpan? lockAcquireTimeout)
    {
        SemaphoreSlim semaphore = GetOrCreateSemaphore(key);
        bool lockAcquired = await semaphore.WaitAsync(lockAcquireTimeout ?? TimeSpan.Zero);

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, semaphore);

        return lockAcquired;
    }

    public async ValueTask<bool> AcquireLockAsync(TKey key, int? lockAcquireTimeout)
        => await TryAcquireLockAsync(key, lockAcquireTimeout?.MillisecondsToTimeSpan());

    public async ValueTask ReleaseLockAsync(TKey key)
    {
        _locks.TryRemove(key, out var semaphore);
        semaphore.Release();
    }

    public async ValueTask<IAsyncDisposable> AcquireAutoReleaseLockAsync(TKey key)
    {
        await TryAcquireLockAsync(key);
        return new AsyncAutoDisposer(async () => await ReleaseLockAsync(key));
    }

    public async ValueTask<IAsyncDisposable> AcquireAutoReleaseLockAsync(TKey key, TimeSpan? lockAcquireTimeout)
    {
        lockAcquireTimeout = lockAcquireTimeout ?? lockAcquireTimeout;
        if (!await TryAcquireLockAsync(key, lockAcquireTimeout))
        {
            throw new TimeoutException($"Failed to acquire lock on key {key} in {lockAcquireTimeout}");
        }

        return new AsyncAutoDisposer(async () => await ReleaseLockAsync(key));
    }

    public async ValueTask<IAsyncDisposable> AcquireAutoReleaseLockAsync(TKey key, int? lockAcquireTimeoutMilliseconds)
        => await AcquireAutoReleaseLockAsync(key, lockAcquireTimeoutMilliseconds?.MillisecondsToTimeSpan());

    private SemaphoreSlim GetOrCreateSemaphore(TKey key)
        => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1));
}
