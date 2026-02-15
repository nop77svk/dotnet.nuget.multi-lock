namespace NoP77svk.Threading;

using System;
using System.Threading;

public class SimpleSyncMultiLock<TKey, TLock>
    : GenericSyncMultiLock<TKey, TLock>
{
    public TimeSpan? LockAcquireTimeout { get; init; } = null;

    public SimpleSyncMultiLock(Func<TKey, TLock> lockInstanceCreator)
        : base(
            lockInstanceCreator: lockInstanceCreator,
            lockAcquirer: lockOobject => Monitor.TryEnter(lockOobject),
            lockWithTimeoutAcquirer: (lockObject, lockAcquireTimeout) => Monitor.TryEnter(lockObject, lockAcquireTimeout),
            lockReleaser: lockObject => Monitor.Exit(lockObject)
        )
    {
    }

    public bool TryAcquireLock(TKey key, out TLock lockObject)
        => TryAcquireLock(key, LockAcquireTimeout, out lockObject);

    public bool TryExecuteUnderLock(TKey key, Action lockedCode)
        => TryExecuteUnderLock(key, LockAcquireTimeout, lockedCode);

    public IDisposable AcquireAutoReleaseLock(TKey key)
        => AcquireAutoReleaseLock(key, LockAcquireTimeout);
}

public class SimpleSyncMultiLock<TKey>
    : SimpleSyncMultiLock<TKey, object>
{
    public SimpleSyncMultiLock()
        : base(key => new object())
    {
    }
}
