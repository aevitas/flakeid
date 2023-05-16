using System;
using System.Text;

namespace FlakeId.Extensions
{
    public static class IdExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this Id id) =>
            DateTimeOffset.FromUnixTimeMilliseconds(id.ToUnixTimeMilliseconds());

        public static long ToUnixTimeMilliseconds(this Id id)
        {
            long timestamp = id >> 22;

            return MonotonicTimer.Epoch.ToUnixTimeMilliseconds() + timestamp;
        }

        public static bool IsSnowflake(this Id id)
        {
            // There's no way to guarantee the specified value is a snowflake.
            // The closest we can get is by decomposing its components, and ensuring all of them are set
            // to values that would be valid for a snowflake.
            long timestamp = id >> 22;
            long thread = (id >> 17) & 0b11111;
            long process = (id >> 12) & 0b11111;
            long increment = id & 0b111111111111;

            return timestamp > 0 && thread > 0 && process > 0 && increment >= 0;
        }

        public static string ToStringIdentifier(this Id id)
        {
            string identifier = id.ToString();

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(identifier));
        }
    }
}
