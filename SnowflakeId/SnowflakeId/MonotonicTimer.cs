using System;
using System.Diagnostics;

namespace SnowflakeId
{
    internal static class MonotonicTimer
    {
        private static long? _epoch;
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public static long ElapsedMilliseconds => _epoch ??= GetEpoch() + _stopwatch.ElapsedMilliseconds;

        private static long GetEpoch()
        {
            // 1420070400000
            DateTimeOffset epoch = new DateTimeOffset(2015, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
            TimeSpan deltaNow = DateTimeOffset.UtcNow - epoch;

            return epoch.ToUnixTimeMilliseconds() + (long) deltaNow.TotalMilliseconds;
        }
    }
}
