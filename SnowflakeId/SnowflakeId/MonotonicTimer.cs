using System;
using System.Diagnostics;

namespace SnowflakeId
{
    internal static class MonotonicTimer
    {
        private static long? s_epoch;
        private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();

        public static long ElapsedMilliseconds => s_epoch ??= GetEpoch() + s_stopwatch.ElapsedMilliseconds;

        private static long GetEpoch()
        {
            // 1420070400000
            DateTimeOffset epoch = new DateTimeOffset(2015, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
            TimeSpan deltaNow = DateTimeOffset.UtcNow - epoch;

            return (long) deltaNow.TotalMilliseconds;
        }
    }
}
