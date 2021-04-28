using System;

namespace SnowflakeId.Extensions
{
    public static class IdExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this Id id)
        {
            long timestamp = id >> 22;
            DateTimeOffset epoch = MonotonicTimer.Epoch;

            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp + epoch.ToUnixTimeMilliseconds());
        }
    }
}
