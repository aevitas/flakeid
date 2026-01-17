using System;
using System.Text;

namespace FlakeId.Extensions
{
    public static class IdExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this Id id) =>
            DateTimeOffset.FromUnixTimeMilliseconds(id.ToUnixTimeMilliseconds());

        private const int TimestampOffset = Id.IncrementBits + Id.ProcessIdBits + Id.ThreadIdBits;
        private const int ThreadOffset = Id.IncrementBits + Id.ProcessIdBits;
        private const int ProcessOffset = Id.IncrementBits;

        /// <summary>
        ///     Returns the timestamp component of the ID in UNIX timestamp format.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static long ToUnixTimeMilliseconds(this Id id)
        {
            long timestamp = id >> TimestampOffset;

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
            long timestamp = id >> TimestampOffset;
            long increment = id & Id.IncrementMask;

            return timestamp > 0 && increment >= 0;
        }

        /// <summary>
        ///     Returns the specified ID as a base 64 encoded string, useful when exposing 64 bit IDs to Node or v8 applications.
        ///     This method uses the legacy implementation, which first converts the ID to a decimal string and then Base64-encodes it.
        ///     For improved performance, shorter output, and standard Base64 encoding, use <see cref="ToBase64String(Id)"/> instead.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Obsolete(
            "This legacy method produces longer, non-standard Base64 IDs. Prefer ToBase64String() for compact and standard IDs."
        )]
        public static string ToStringIdentifier(this Id id)
        {
            string identifier = id.ToString();

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(identifier));
        }

        /// <summary>
        ///     Returns the specified ID as a URL-safe Base64 encoded string, useful when exposing 64 bit IDs to web applications.
        ///     This method directly encodes the 64-bit value as Base64, making it more efficient.
        ///     The result is URL-safe (uses '-' and '_' instead of '+' and '/') and has no padding.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string ToBase64String(this Id id)
        {
            long value = id;
            byte[] bytes = BitConverter.GetBytes(value);

            // convert bytes to a base64 string. remove padding and replace symbols to ensure result is URL safe
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>
        ///     Parses a Base64-encoded string back to an ID. This method can handle both the new URL-safe format
        ///     (from <see cref="ToBase64String(Id)"/>) and the legacy format (from <see cref="ToStringIdentifier(Id)"/>).
        /// </summary>
        /// <param name="base64String">The Base64-encoded string to parse</param>
        /// <returns>The parsed ID</returns>
        /// <exception cref="ArgumentException">Thrown when the string cannot be parsed as a valid ID</exception>
        public static Id FromBase64String(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                throw new ArgumentException(
                    "Base64 string cannot be null or empty",
                    nameof(base64String)
                );

            try
            {
                // try parse the new url-safe format first
                if (TryParseFromUrlSafeBase64(base64String, out Id id))
                    return id;

                // fall back to legacy format
                if (TryParseFromLegacyBase64(base64String, out id))
                    return id;

                // neither method could parse the base 64 string, throw exception
                throw new ArgumentException("Invalid Base64 string format", nameof(base64String));
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new ArgumentException(
                    "Invalid Base64 string format",
                    nameof(base64String),
                    ex
                );
            }
        }

        /// <summary>
        ///     Tries to parse a URL-safe Base64 string into an <see cref="Id"/>.
        ///     Returns true if parsing succeeds; otherwise returns false and sets <paramref name="id"/> to <see cref="default(Id)"/>.
        /// </summary>
        /// <param name="base64String">The URL-safe Base64 string to parse.</param>
        /// <param name="id">The resulting <see cref="Id"/> if successful; otherwise <see cref="default(Id)"/>.</param>
        /// <returns>True if the string was a valid 64-bit ID; false otherwise.</returns>
        private static bool TryParseFromUrlSafeBase64(string base64String, out Id id)
        {
            id = default;

            try
            {
                // restore url safe characters and padding
                string standardBase64 = base64String.Replace('-', '+').Replace('_', '/');

                // add padding to make the string length a multiple of 4 (needed for base64)
                switch (standardBase64.Length % 4)
                {
                    case 2: standardBase64 += "=="; break;
                    case 3: standardBase64 += "="; break;
                }

                byte[] bytes = Convert.FromBase64String(standardBase64);

                // check the decoded byte array is exactly 8 bytes (64 bits),
                // otherwise it is not a valid 64-bit ID
                if (bytes.Length != 8)
                    return false;

                // decode the 8 bytes into a 64-bit long representing the original Id value,
                // then create a new Id instance from that value.
                long value = BitConverter.ToInt64(bytes, 0);
                id = new Id(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Attempts to parse a legacy Base64-encoded string (from <see cref="Id.ToStringIdentifier"/>)
        ///     into an <see cref="Id"/>. Returns true if parsing succeeds; otherwise returns false
        ///     and sets <paramref name="id"/> to <see cref="default(Id)"/>.
        /// </summary>
        /// <param name="base64String">The legacy Base64 string to parse.</param>
        /// <param name="id">The resulting <see cref="Id"/> if successful; otherwise <see cref="default(Id)"/>.</param>
        /// <returns>True if the string was successfully parsed into a valid 64-bit ID; false otherwise.</returns>
        private static bool TryParseFromLegacyBase64(string base64String, out Id id)
        {
            id = default;

            try
            {
                // decode the Base64 string into a UTF-8 byte array
                byte[] bytes = Convert.FromBase64String(base64String);

                // convert the byte array into a decimal string representation of the Id
                string decimalString = Encoding.UTF8.GetString(bytes);

                // try to parse the decimal string into a 64-bit long
                if (long.TryParse(decimalString, out long value))
                {
                    // parsed - create a new Id instance
                    id = new Id(value);
                    return true;
                }

                // failed - string doesn't represent valid long Id
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Returns an ID that is valid for the specified timestamp.
        ///     Note that consecutive calls with the same timestamp will yield different IDs, as the other components of the ID will still differ.
        ///     In other words, the time component of the ID is guaranteed to be equal to the specified timestamp.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static Id FromDateTimeOffset(this Id id, DateTimeOffset timeStamp)
        {
            return Id.Create(timeStamp.ToUnixTimeMilliseconds());
        }
    }
}
