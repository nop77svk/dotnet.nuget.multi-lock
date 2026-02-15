namespace NoP77svk.Threading;

using System;

internal class AutoDisposer : IDisposable
{
    private readonly Action _releaseAction;
    private bool disposedValue;

    public AutoDisposer(Action releaseAction) => _releaseAction = releaseAction;

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
