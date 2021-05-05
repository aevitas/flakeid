using System;

namespace FlakeId.Extensions
{
    public static class IdExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this Id id)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(id.ToUnixTimeMilliseconds());
        }

        public static long ToUnixTimeMilliseconds(this Id id)
        {
            long timestamp = id >> 22;

            return MonotonicTimer.Epoch.ToUnixTimeMilliseconds() + timestamp;
        }
    }
}
