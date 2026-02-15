namespace NoP77svk.Threading;

using System;
using System.Collections.Concurrent;

public class GenericSyncMultiLock<TKey, TLock>
    : ISyncMultiLock<TKey, TLock>
{
    private readonly ConcurrentDictionary<TKey, TLock> _locks = new();

    private readonly Func<TKey, TLock> _lockInstanceSelector;
    private readonly Func<TLock, bool> _lockAcquirer;
    private readonly Func<TLock, TimeSpan, bool> _lockWithTimeoutAcquirer;
    private readonly Action<TLock> _lockReleaser;

    protected GenericSyncMultiLock(Func<TKey, TLock> lockInstanceCreator, Func<TLock, bool> lockAcquirer, Func<TLock, TimeSpan, bool> lockWithTimeoutAcquirer, Action<TLock> lockReleaser)
    {
        _lockInstanceSelector = lockInstanceCreator;
        _lockAcquirer = lockAcquirer;
        _lockWithTimeoutAcquirer = lockWithTimeoutAcquirer;
        _lockReleaser = lockReleaser;
    }

    public TLock this[TKey key] => GetLock(key);

    public bool TryAcquireLock(TKey key, TimeSpan? lockAcquireTimeout, out TLock lockObject)
    {
        lockObject = GetLock(key);
        bool lockAcquired = lockAcquireTimeout is null
            ? _lockAcquirer(lockObject)
            : _lockWithTimeoutAcquirer(lockObject, (TimeSpan)lockAcquireTimeout);

        // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
        _locks.TryAdd(key, lockObject);

        return lockAcquired;
    }

    public bool TryAcquireLock(TKey key, int? lockAcquireTimeoutMilliseconds, out TLock lockObject)
        => TryAcquireLock(key, lockAcquireTimeoutMilliseconds?.MillisecondsToTimeSpan(), out lockObject);

    public void ReleaseLock(TKey key)
    {
        // note: We must first remove the lock from the collection, then release it, so that other threads may get the chance of adding it again after this lock release.
        _locks.TryRemove(key, out TLock lockObject);
        _lockReleaser(lockObject);
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

    public bool TryExecuteUnderLock(TKey key, int? lockAcquireTimeoutMilliseconds, Action lockedCode)
        => TryExecuteUnderLock(key, lockAcquireTimeoutMilliseconds?.MillisecondsToTimeSpan(), lockedCode);

    public IDisposable AcquireAutoReleaseLock(TKey key, TimeSpan? lockAcquireTimeout)
    {
        if (!TryAcquireLock(key, lockAcquireTimeout, out TLock _))
        {
            throw new TimeoutException($"Failed to acquire lock on key {key}");
        }

        return new LockAutoRelease(() => ReleaseLock(key));
    }

    public IDisposable AcquireAutoReleaseLock(TKey key, int? lockAcquireTimeout)
        => AcquireAutoReleaseLock(key, lockAcquireTimeout?.MillisecondsToTimeSpan());

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
