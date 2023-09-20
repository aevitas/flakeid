using System;
using System.Text;

namespace FlakeId.Extensions
{
    public static class IdExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this Id id) =>
            DateTimeOffset.FromUnixTimeMilliseconds(id.ToUnixTimeMilliseconds());

        /// <summary>
        ///     Returns the timestamp component of the ID in UNIX timestamp format.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static long ToUnixTimeMilliseconds(this Id id)
        {
            long timestamp = id >> 22;

            return MonotonicTimer.Epoch.ToUnixTimeMilliseconds() + timestamp;
        }

        /// <summary>
        ///     Returns a value indicating whether or not the specified ID is likely to be a snowflake, i.e. it contains all the
        ///     components that comprise a valid snowflake.
        ///     Note that the value returned does not guarantee the specified value is a snowflake; it merely indicated whether the
        ///     specified ID _could_ be one.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     Returns the specified ID as a base 64 encoded string, useful when exposing 64 bit IDs to Node or v8 applications.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string ToStringIdentifier(this Id id)
        {
            string identifier = id.ToString();

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(identifier));
        }
    }
}
