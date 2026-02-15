namespace NoP77svk.Threading;

using System;

public interface ISyncMultiLock<in TKey>
{
    bool TryAcquireLock(TKey key);
    bool TryAcquireLock(TKey key, TimeSpan? lockAcquireTimeout);
    void ReleaseLock(TKey key);
    IDisposable AcquireAutoReleaseLock(TKey key);
    IDisposable AcquireAutoReleaseLock(TKey key, TimeSpan? lockAcquireTimeout);
}
