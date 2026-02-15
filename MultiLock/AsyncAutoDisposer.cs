namespace NoP77svk.Threading;

using System;
using System.Threading.Tasks;

internal class AsyncAutoDisposer : IAsyncDisposable
{
    private readonly Func<Task> _releaseActionAsync;
    private bool disposedValue;

    public AsyncAutoDisposer(Func<Task> releaseActionAsync) => _releaseActionAsync = releaseActionAsync;

    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                await _releaseActionAsync();
            }

            disposedValue = true;
        }
    }
}
