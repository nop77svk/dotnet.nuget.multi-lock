namespace NoP77svk.Threading;

using System;

public interface ISyncMultiLock<in TKey, TLock>
{
    TLock this[TKey key] { get; }
    bool TryAcquireLock(TKey key, TimeSpan? lockAcquireTimeout, out TLock lockObject);
    void ReleaseLock(TKey key);
    bool TryExecuteUnderLock(TKey key, TimeSpan? lockAcquireTimeout, Action lockedCode);
    IDisposable AcquireAutoReleaseLock(TKey key, TimeSpan? lockAcquireTimeout);
}
