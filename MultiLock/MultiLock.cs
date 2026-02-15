namespace NoP77svk.Threading;

using System;
using System.Collections.Concurrent;
using System.Threading;

public class MultiLock<TKey, TLock>
    : ISyncMultiLock<TKey>
{
    private readonly ConcurrentDictionary<TKey, TLock> _locks = new();

    private readonly Func<TKey, TLock> _lockInstanceSelector;
    private readonly Func<TLock, bool> _lockAcquirer;
    private readonly Func<TLock, TimeSpan, bool> _lockWithTimeoutAcquirer;
    private readonly Action<TLock> _lockReleaser;

    public MultiLock(Func<TKey, TLock> lockInstanceCreator)
        : this(
            lockInstanceCreator: lockInstanceCreator,
            lockAcquirer: lockOobject => Monitor.TryEnter(lockOobject),
            lockWithTimeoutAcquirer: (lockObject, lockAcquireTimeout) => Monitor.TryEnter(lockObject, lockAcquireTimeout),
            lockReleaser: lockObject => Monitor.Exit(lockObject)
        )
    {
    }

    protected MultiLock(Func<TKey, TLock> lockInstanceCreator, Func<TLock, bool> lockAcquirer, Func<TLock, TimeSpan, bool> lockWithTimeoutAcquirer, Action<TLock> lockReleaser)
    {
        _lockInstanceSelector = lockInstanceCreator;
        _lockAcquirer = lockAcquirer;
        _lockWithTimeoutAcquirer = lockWithTimeoutAcquirer;
        _lockReleaser = lockReleaser;
    }

    public bool TryAcquireLock(TKey key)
    {
        TLock lockObject = GetOrCreateLockObject(key);
        bool lockAcquired = _lockAcquirer(lockObject);

        if (lockAcquired)
        {
            // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
            _locks.TryAdd(key, lockObject);
        }

        return lockAcquired;
    }

    public bool TryAcquireLock(TKey key, TimeSpan? lockAcquireTimeout)
    {
        TLock lockObject = GetOrCreateLockObject(key);
        bool lockAcquired = _lockWithTimeoutAcquirer(lockObject, lockAcquireTimeout ?? lockAcquireTimeout ?? TimeSpan.Zero);

        if (lockAcquired)
        {
            // note: Let's add the lock again, since another thread may just have removed it from the collection upon lock release.
            _locks.TryAdd(key, lockObject);
        }

        return lockAcquired;
    }

    public bool TryAcquireLock(TKey key, int? lockAcquireTimeoutMilliseconds)
        => TryAcquireLock(key, lockAcquireTimeoutMilliseconds?.MillisecondsToTimeSpan());

    public void ReleaseLock(TKey key)
    {
        // note: We must first remove the lock from the collection, then release it, so that other threads may get the chance of adding it again after this lock release.
        _locks.TryRemove(key, out TLock lockObject);
        _lockReleaser(lockObject);
    }

    public IDisposable AcquireAutoReleaseLock(TKey key)
    {
        if (!TryAcquireLock(key))
        {
            throw new TimeoutException($"Failed to acquire lock on key {key}");
        }

        return new AutoDisposer(() => ReleaseLock(key));
    }

    public IDisposable AcquireAutoReleaseLock(TKey key, TimeSpan? lockAcquireTimeout)
    {
        lockAcquireTimeout = lockAcquireTimeout ?? lockAcquireTimeout;
        if (!TryAcquireLock(key, lockAcquireTimeout))
        {
            throw new TimeoutException($"Failed to acquire lock on key {key} in {lockAcquireTimeout}");
        }

        return new AutoDisposer(() => ReleaseLock(key));
    }

    public IDisposable AcquireAutoReleaseLock(TKey key, int? lockAcquireTimeout)
        => AcquireAutoReleaseLock(key, lockAcquireTimeout?.MillisecondsToTimeSpan());

    private TLock GetOrCreateLockObject(TKey key) => _locks.GetOrAdd(key, _lockInstanceSelector);
}

public class MultiLock<TKey>
    : MultiLock<TKey, object>
{
    public MultiLock()
        : base(_ => new object())
    {
    }
}
