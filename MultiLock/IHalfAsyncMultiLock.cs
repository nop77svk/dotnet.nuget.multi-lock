namespace NoP77svk.Threading;

using System;
using System.Threading.Tasks;

public interface IHalfAsyncMultiLock<TKey>
{
    ValueTask TryAcquireLockAsync(TKey key);
    ValueTask<bool> TryAcquireLockAsync(TKey key, TimeSpan lockAcquireTimeout);
    void ReleaseLock(TKey key);
    ValueTask<IDisposable> AcquireAutoReleaseLockAsync(TKey key);
    ValueTask<IDisposable> AcquireAutoReleaseLockAsync(TKey key, TimeSpan lockAcquireTimeout);
}
