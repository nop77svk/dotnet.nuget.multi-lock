namespace NoP77svk.Threading;

using System;

internal static class MultiLockHelpers
{
    internal static TimeSpan MillisecondsToTimeSpan(this double milliseconds)
        => TimeSpan.FromMilliseconds(milliseconds);

    internal static TimeSpan MillisecondsToTimeSpan(this int milliseconds)
        => TimeSpan.FromMilliseconds(milliseconds);
}
