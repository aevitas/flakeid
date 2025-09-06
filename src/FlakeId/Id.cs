using System;
using System.Diagnostics;
using System.Threading;
using FlakeId.Extensions;

namespace FlakeId
{
    /// <summary>
    ///     Represents a unique, K-ordered, sortable identifier.
    /// </summary>
    [DebuggerDisplay("{_value}")]
    public struct Id : IComparable<Id>
    {
        // This implementation of Snowflake ID is based on the specification as published by Discord:
        // https://discord.com/developers/docs/reference
        //
        // Every Snowflake fits in a 64-bit integer, consisting of various components that make it unique across generations.
        // The layout of the components that comprise a snowflake can be expressed as:
        //
        // Timestamp                                   Thread Proc  Increment
        // 111111111111111111111111111111111111111111  11111  11111 111111111111
        // 64                                          22     17    12          0
        //
        // The Timestamp component is represented as the milliseconds since the first second of 2015.
        // Since we're using all 64 bits available, this epoch can be any point in time, as long as it's in the past.
        // If the epoch is set to a point in time in the future, it may result in negative snowflakes being generated.
        //
        // Where the original Discord reference mentions worker ID and process ID, we substitute these with the
        // thread and process ID respectively, as the combination of these two provide sufficient uniqueness, and they are
        // the closest we can get to the original specification within the .NET ecosystem.
        //
        // The Increment component is a monotonically incrementing number, which is incremented every time a snowflake is generated.
        // This is in contrast with some other flake-ish implementations, which only increment the counter any time a snowflake is
        // generated twice at the exact same instant in time. We believe Discord's implementation is more correct here,
        // as even two snowflakes that are generated at the exact same point in time will not be identical, because of their increments.
        //
        // This implementation is optimised for high-throughput applications, while providing IDs that are roughly sortable, and
        // with a very high degree of uniqueness.

        private long _value;

        private static long s_prevId;

        // Calling Process.GetCurrentProcess() is a very slow operation, as it has to query the operating system.
        // Because it's highly unlikely the process ID will change (if at all possible) during our run time, we'll cache it.
        private static int? s_processId;

        internal const int TimestampBits = 42;
        internal const int ThreadIdBits = 5;
        internal const int ProcessIdBits = 5;
        internal const int IncrementBits = 12;

        internal const long TimestampMask = (1L << TimestampBits) - 1;
        internal const int ThreadIdMask = (1 << ThreadIdBits) - 1;
        internal const int ProcessIdMask = (1 << ProcessIdBits) - 1;
        internal const int IncrementMask = (1 << IncrementBits) - 1;

        public Id(long value) => _value = value;

        /// <summary>
        ///     Creates a new, unique ID.
        /// </summary>
        /// <returns></returns>
        public static Id Create()
        {
            Id id = new Id();

            id.CreateInternal();

            return id;
        }

        /// <summary>
        ///     Creates a new ID based on the provided timestamp in milliseconds.
        ///     When using this overload, make sure you take the timezone of the provided timestamp into consideration.
        /// </summary>
        /// <param name="timeStampMs"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Timestamps can not be negative</exception>
        public static Id Create(long timeStampMs)
        {
            if (timeStampMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeStampMs));
            }

            Id id = new Id();
            long relativeTimeStamp = timeStampMs - MonotonicTimer.Epoch.ToUnixTimeMilliseconds();

            if (relativeTimeStamp < 0)
            {
                throw new ArgumentException(
                    "Specified timestamp would result in a negative ID (it's before instance epoch)");
            }

            id.CreateInternal(relativeTimeStamp);

            return id;
        }

        /// <summary>
        ///     Attempts to parse an ID from the specified <see cref="long" /> value. This method will return false if the
        ///     specified value doesn't match the shape of a snowflake ID.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool TryParse(long value, out Id id)
        {
            Id input = new Id(value);

            if (!input.IsSnowflake())
            {
                id = default;
                return false;
            }

            id = input;
            return true;
        }

        /// <summary>
        ///     Parses an ID from the specified <see cref="long" /> value, and throws an exception if the shape of the value
        ///     doesn't match that of a valid ID.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static Id Parse(long value)
        {
            Id id = new Id(value);

            if (!id.IsSnowflake())
            {
                throw new FormatException("The specified value is not a valid snowflake");
            }

            return id;
        }

        private void CreateInternal(long timeStampMs = 0)
        {
            if (s_processId is null)
            {
                s_processId = Process.GetCurrentProcess().Id & ProcessIdMask;
            }

            int processId = s_processId.Value;
            SpinWait spinner = default;

            while (true)
            {
                long prev = Interlocked.Read(ref s_prevId);

                long lastTimestamp = (prev >> (ThreadIdBits + ProcessIdBits + IncrementBits));
                long currentTimestamp = timeStampMs == 0 ? MonotonicTimer.ElapsedMilliseconds : timeStampMs;

                if (currentTimestamp < lastTimestamp)
                {
                    throw new InvalidOperationException(
                        "Clock shifted backwards; can't reliably generate ID");
                }

                int increment;
                if (currentTimestamp == lastTimestamp)
                {
                    increment = (int)(prev & IncrementMask) + 1;

                    // Increment overflows, wait for the next ms to avoid it being 0 for ages
                    if (increment > IncrementMask)
                    {
                        spinner.SpinOnce();
                        continue;
                    }
                }
                else
                {
                    increment = 0;
                }

                int threadId = Environment.CurrentManagedThreadId & ThreadIdMask;

                long newValue = (currentTimestamp << (ThreadIdBits + ProcessIdBits + IncrementBits))
                                | (long)(threadId << (ProcessIdBits + IncrementBits))
                                | (long)(processId << IncrementBits)
                                | (long)increment;

                // Atomically update the last value. If the original value was changed by another
                // thread, this will fail and we'll loop again.
                if (Interlocked.CompareExchange(ref s_prevId, newValue, prev) == prev)
                {
                    _value = newValue;
                    break;
                }
            }
        }

        public override string ToString() => _value.ToString();

        public static implicit operator long(Id id) => id._value;

        public static bool operator ==(Id left, Id right) => left._value == right._value;

        public static bool operator !=(Id left, Id right) => !(left == right);

        public int CompareTo(Id other) => _value.CompareTo(other._value);

        public bool Equals(Id other) => _value == other._value;

        public override bool Equals(object obj) => obj is Id other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();
    }
}
