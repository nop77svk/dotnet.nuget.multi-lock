namespace NoP77svk.Threading;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class MultiLock<TKey, TLock>
{
    private readonly ConcurrentDictionary<TKey, TLock> _locks = new();
    private readonly Func<TKey, TLock> _lockInstanceSelector;

    public TimeSpan? LockAcquireTimeout { get; init; } = null;

    public MultiLock(Func<TKey, TLock> lockInstanceSelector)
    {
        _lockInstanceSelector = lockInstanceSelector;
    }

    public TLock this[TKey key] => GetLock(key);

    public bool TryAcquireLock(TKey key, TimeSpan? lockAcquireTimeout, out TLock lockObject)
    {
        lockObject = GetLock(key);

        bool lockAcquired = lockAcquireTimeout is null
            ? Monitor.TryEnter(lockObject)
            : Monitor.TryEnter(lockObject, (TimeSpan)lockAcquireTimeout);

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, lockObject);

        return lockAcquired;
    }

    public bool TryAcquireLock(TKey key, int? lockAcquireTimeoutMilliseconds, out TLock lockObject)
        => TryAcquireLock(key, TimeSpan.FromMilliseconds(lockAcquireTimeoutMilliseconds ?? 0), out lockObject);

    public bool TryAcquireLock(TKey key, out TLock lockObject)
        => TryAcquireLock(key, LockAcquireTimeout, out lockObject);

    public void ReleaseLock(TKey key)
    {
        // note: We must first remove the lock from the collection, then release it, so that other threads may get the chance of adding it again after this lock release.
        _locks.TryRemove(key, out TLock lockObject);
        Monitor.Exit(lockObject);
    }

    public bool TryExecuteUnderLock(TKey key, TimeSpan? lockAcquireTimeout, Action lockedCode)
    {
        bool isLockedUponKey = TryAcquireLock(key, lockAcquireTimeout, out var _);

        if (isLockedUponKey)
        {
            try
            {
                lockedCode?.Invoke();
            }
            finally
            {
                ReleaseLock(key);
            }
        }

        return isLockedUponKey;
    }

    public bool ExecuteUnderLock(TKey key, Action lockedCode)
        => TryExecuteUnderLock(key, LockAcquireTimeout, lockedCode);

    public async Task<bool> ExecuteUnderLockAsync(TKey key, TimeSpan? lockAcquireTimeout, Func<Task> lockedCode)
    {
        bool isLockedUponKey = TryAcquireLock(key, lockAcquireTimeout, out TLock _);

        if (isLockedUponKey)
        {
            try
            {
                await lockedCode();
            }
            finally
            {
                ReleaseLock(key);
            }
        }

        return isLockedUponKey;
    }

    public async Task<bool> ExecuteUnderLockAsync(TKey key, Func<Task> lockedCode)
        => await ExecuteUnderLockAsync(key, LockAcquireTimeout, lockedCode);

    public IDisposable AcquireAutoReleaseLock(TKey key, TimeSpan? lockAcquireTimeout)
    {
        if (!TryAcquireLock(key, lockAcquireTimeout, out var _))
        {
            throw new TimeoutException($"Failed to acquire lock on key {key}");
        }

        return new LockAutoRelease(() => ReleaseLock(key));
    }

    public IDisposable AcquireAutoReleaseLock(TKey key, int? lockAcquireTimeout)
        => AcquireAutoReleaseLock(key, lockAcquireTimeout is null ? null : TimeSpan.FromMilliseconds(lockAcquireTimeout ?? 0));

    public IDisposable AcquireAutoReleaseLock(TKey key)
        => AcquireAutoReleaseLock(key, LockAcquireTimeout);

    private TLock GetLock(TKey key) => _locks.GetOrAdd(key, _lockInstanceSelector);

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

public class MultiLock<TKey>
    : MultiLock<TKey, object>
{
    public MultiLock()
        : base(key => new object())
    {
    }
}
