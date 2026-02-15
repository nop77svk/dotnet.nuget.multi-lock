namespace NoP77svk.Threading;

using System;
using System.Threading.Tasks;

public interface IFullAsyncMultiLock<TKey>
{
    ValueTask TryAcquireLockAsync(TKey key);
    ValueTask<bool> TryAcquireLockAsync(TKey key, TimeSpan? lockAcquireTimeout);
    ValueTask ReleaseLockAsync(TKey key);
    ValueTask<IAsyncDisposable> AcquireAutoReleaseLockAsync(TKey key);
    ValueTask<IAsyncDisposable> AcquireAutoReleaseLockAsync(TKey key, TimeSpan? lockAcquireTimeout);
}
