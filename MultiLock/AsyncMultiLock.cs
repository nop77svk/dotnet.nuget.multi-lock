namespace NoP77svk.Threading;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class AsyncMultiLock<TKey>
{
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new();

    public TimeSpan? LockAcquireTimeout { get; init; } = null;

    public SemaphoreSlim this[TKey key] => GetLock(key);

    public void ReleaseLock(TKey key)
    {
        _locks.TryRemove(key, out var lockObject);
        lockObject.Release();
    }

    public IDisposable AcquireAutoReleaseLock(TKey key, TimeSpan? lockAcquireTimeout)
    {
        SemaphoreSlim lockObject = GetLock(key);

        if (lockAcquireTimeout is null)
        {
            lockObject.Wait();
        }
        else if (!lockObject.Wait(lockAcquireTimeout ?? TimeSpan.Zero))
        {
            throw new TimeoutException($"Cannot acquire lock on key {key}");
        }

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, lockObject);

        return new LockAutoRelease(() => ReleaseLock(key));
    }

    public IDisposable AcquireAutoReleaseLock(TKey key, int? lockAcquireTimeoutMilliseconds)
        => AcquireAutoReleaseLock(key, lockAcquireTimeoutMilliseconds?.MillisecondsToTimeSpan());

    public IDisposable AcquireAutoReleaseLock(TKey key)
        => AcquireAutoReleaseLock(key, LockAcquireTimeout);

    public async Task<IDisposable> AcquireAutoReleaseLockAsync(TKey key, TimeSpan? lockAcquireTimeout)
    {
        SemaphoreSlim lockObject = GetLock(key);

        if (lockAcquireTimeout is null)
        {
            await lockObject.WaitAsync();
        }
        else if (!await lockObject.WaitAsync(lockAcquireTimeout ?? TimeSpan.Zero))
        {
            throw new TimeoutException($"Cannot acquire lock on key {key}");
        }

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, lockObject);

        return new LockAutoRelease(() => ReleaseLock(key));
    }

    public async Task<IDisposable> AcquireAutoReleaseLockAsync(TKey key, int? lockAcquireTimeoutMilliseconds)
        => await AcquireAutoReleaseLockAsync(key, lockAcquireTimeoutMilliseconds?.MillisecondsToTimeSpan());

    public async Task<IDisposable> AcquireAutoReleaseLockAsync(TKey key)
        => await AcquireAutoReleaseLockAsync(key, LockAcquireTimeout);

    private SemaphoreSlim GetLock(TKey key) => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1));

    private class LockAutoRelease : IDisposable
    {
        private readonly Action _releaseAction;
        private bool disposedValue;

        public LockAutoRelease(Action releaseAction) => _releaseAction = releaseAction;

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _releaseAction?.Invoke();
                }

                disposedValue = true;
            }
        }
    }
}
