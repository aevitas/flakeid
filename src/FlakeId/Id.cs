using System;\r
using System.Diagnostics;\r
using System.Threading;\r
using FlakeId.Extensions;\r
using System.Text.Json.Serialization;\r
\r
namespace FlakeId\r
{\r
    /// <summary>\r
    ///     Represents a unique, K-ordered, sortable identifier.\r
    /// </summary>\r
    [DebuggerDisplay("{_value}")]\r
    [JsonConverter(typeof(FlakeIdJsonConverter))]\r
    public struct Id : IComparable<Id>, IEquatable<Id>\r
    {\r
        // This implementation of Snowflake ID is based on the specification as published by Discord:\r
        // https://discord.com/developers/docs/reference\r
        //\r
        // Every Snowflake fits in a 64-bit integer, consisting of various components that make it unique across generations.\r
        // The layout of the components that comprise a snowflake can be expressed as:\r
        //\r
        // Timestamp                                   Thread Proc  Increment\r
        // 111111111111111111111111111111111111111111  11111  11111 111111111111\r
        // 64                                          22     17    12          0\r
        //\r
        // The Timestamp component is represented as the milliseconds since the first second of 2015.\r
        // Since we're using all 64 bits available, this epoch can be any point in time, as long as it's in the past.\r
        // If the epoch is set to a point in time in the future, it may result in negative snowflakes being generated.\r
        //\r
        // Where the original Discord reference mentions worker ID and process ID, we substitute these with the\r
        // thread and process ID respectively, as the combination of these two provide sufficient uniqueness, and they are\r
        // the closest we can get to the original specification within the .NET ecosystem.\r
        //\r
        // The Increment component is a monotonically incrementing number, which is incremented every time a snowflake is generated.\r
        // This is in contrast with some other flake-ish implementations, which only increment the counter any time a snowflake is\r
        // generated twice at the exact same instant in time. We believe Discord's implementation is more correct here,\r
        // as even two snowflakes that are generated at the exact same point in time will not be identical, because of their increments.\r
        //\r
        // This implementation is optimised for high-throughput applications, while providing IDs that are roughly sortable, and\r
        // with a very high degree of uniqueness.\r
\r
        private long _value;\r
\r
        private static long s_prevId;\r
\r
        // Calling Process.GetCurrentProcess() is a very slow operation, as it has to query the operating system.\r
        // Because it's highly unlikely the process ID will change (if at all possible) during our run time, we'll cache it.\r
        private static int? s_processId;\r
\r
        // The configured process ID, if set by the user through the ProcessId property. This takes precedence over\r
        // the OS-assigned process ID, which is useful in containerized environments (e.g., Docker) where the\r
        // process ID is always 1.\r
        private static int? s_configuredProcessId;\r
\r
        // The name of the environment variable that can be used to configure the process ID for Snowflake generation.\r
        // This is useful in containerized environments (e.g., Docker) where the process ID is always 1.\r
        internal const string ProcessIdEnvironmentVariable = "FLAKEID_PROCESS_ID";\r
\r
        internal const int TimestampBits = 42;\r
        internal const int ThreadIdBits = 5;\r
        internal const int ProcessIdBits = 5;\r
        internal const int IncrementBits = 12;\r
\r
        internal const long TimestampMask = (1L << TimestampBits) - 1;\r
        internal const int ThreadIdMask = (1 << ThreadIdBits) - 1;\r
        internal const int ProcessIdMask = (1 << ProcessIdBits) - 1;\r
        internal const int IncrementMask = (1 << IncrementBits) - 1;\r
\r
        public Id(long value) => _value = value;\r
\r
        /// <summary>\r
        ///     Gets or sets the process ID component used in Snowflake generation.\r
        ///     When set, this value is used instead of the OS-assigned process ID,\r
        ///     which is useful in containerized environments (e.g., Docker) where\r
        ///     the process ID is always 1.\r
        ///     The value is masked to <see cref="ProcessIdBits" /> bits (5 bits, range 0-31).\r
        ///     Set to null to revert to automatic process ID detection.\r
        /// </summary>\r
        /// <remarks>\r
        ///     The precedence for process ID resolution is:\r
        ///     1. The value set through this property.\r
        ///     2. The <c>FLAKEID_PROCESS_ID</c> environment variable.\r
        ///     3. The OS-assigned process ID.\r
        /// </remarks>\r
        public static int? ProcessId\r
        {\r
            get => s_configuredProcessId;\r
            set\r
            {\r
                s_configuredProcessId = value;\r
                // Reset the cached process ID so it is re-evaluated on the next call to CreateInternal.\r
                s_processId = null;\r
            }\r
        }\r
\r
        /// <summary>\r
        ///     Creates a new, unique ID.\r
        /// </summary>\r
        /// <returns></returns>\r
        public static Id Create()\r
        {\r
            Id id = new Id();\r
\r
            id.CreateInternal();\r
\r
            return id;\r
        }\r
\r
        /// <summary>\r
        ///     Creates a new ID based on the provided timestamp in milliseconds.\r
        ///     When using this overload, make sure you take the timezone of the provided timestamp into consideration.\r
        /// </summary>\r
        /// <param name="timeStampMs"></param>\r
        /// <returns></returns>\r
        /// <exception cref="ArgumentOutOfRangeException">Timestamps can not be negative</exception>\r
        public static Id Create(long timeStampMs)\r
        {\r
            if (timeStampMs < 0)\r
            {\r
                throw new ArgumentOutOfRangeException(nameof(timeStampMs));\r
            }\r
\r
            Id id = new Id();\r
            long relativeTimeStamp = timeStampMs - MonotonicTimer.Epoch.ToUnixTimeMilliseconds();\r
\r
            if (relativeTimeStamp < 0)\r
            {\r
                throw new ArgumentException(\r
                    "Specified timestamp would result in a negative ID (it's before instance epoch)");\r
            }\r
\r
            id.CreateInternal(relativeTimeStamp);\r
\r
            return id;\r
        }\r
\r
        /// <summary>\r
        ///     Attempts to parse an ID from the specified <see cref="long" /> value. This method will return false if the\r
        ///     specified value doesn't match the shape of a snowflake ID.\r
        /// </summary>\r
        /// <param name="value"></param>\r
        /// <param name="id"></param>\r
        /// <returns></returns>\r
        public static bool TryParse(long value, out Id id)\r
        {\r
            Id input = new Id(value);\r
\r
            if (!input.IsSnowflake())\r
            {\r
                id = default;\r
                return false;\r
            }\r
\r
            id = input;\r
            return true;\r
        }\r
\r
        /// <summary>\r
        ///     Parses an ID from the specified <see cref="long" /> value, and throws an exception if the shape of the value\r
        ///     doesn't match that of a valid ID.\r
        /// </summary>\r
        /// <param name="value"></param>\r
        /// <returns></returns>\r
        /// <exception cref="FormatException"></exception>\r
        public static Id Parse(long value)\r
        {\r
            Id id = new Id(value);\r
\r
            if (!id.IsSnowflake())\r
            {\r
                throw new FormatException("The specified value is not a valid snowflake");\r
            }\r
\r
            return id;\r
        }\r
\r
        private void CreateInternal(long timeStampMs = 0)\r
        {\r
            if (s_processId is null)\r
            {\r
                s_processId = ResolveProcessId();\r
            }\r
\r
            int processId = s_processId.Value;\r
            SpinWait spinner = default;\r
\r
            while (true)\r
            {\r
                long prev = Interlocked.Read(ref s_prevId);\r
\r
                long lastTimestamp = (prev >> (ThreadIdBits + ProcessIdBits + IncrementBits));\r
                long currentTimestamp = timeStampMs == 0 ? MonotonicTimer.ElapsedMilliseconds : timeStampMs;\r
\r
                if (currentTimestamp < lastTimestamp)\r
                {\r
                    throw new InvalidOperationException(\r
                        "Clock shifted backwards; can't reliably generate ID");\r
                }\r
\r
                int increment;\r
                if (currentTimestamp == lastTimestamp)\r
                {\r
                    increment = (int)(prev & IncrementMask) + 1;\r
\r
                    // Increment overflows, wait for the next ms to avoid it being 0 for ages\r
                    if (increment > IncrementMask)\r
                    {\r
                        spinner.SpinOnce();\r
                        continue;\r
                    }\r
                }\r
                else\r
                {\r
                    increment = 0;\r
                }\r
\r
                int threadId = Environment.CurrentManagedThreadId & ThreadIdMask;\r
\r
                long newValue = (currentTimestamp << (ThreadIdBits + ProcessIdBits + IncrementBits))\r
                                | (long)(threadId << (ProcessIdBits + IncrementBits))\r
                                | (long)(processId << IncrementBits)\r
                                | (long)increment;\r
\r
                // Atomically update the last value. If the original value was changed by another\r
                // thread, this will fail and we'll loop again.\r
                if (Interlocked.CompareExchange(ref s_prevId, newValue, prev) == prev)\r
                {\r
                    _value = newValue;\r
                    break;\r
                }\r
            }\r
        }\r
\r
        /// <summary>\r
        ///     Resolves the process ID to use for Snowflake generation. The resolution order is:\r
        ///     1. The value set through <see cref="ProcessId" />.\r
        ///     2. The <c>FLAKEID_PROCESS_ID</c> environment variable.\r
        ///     3. The OS-assigned process ID.\r
        ///     The result is masked to <see cref="ProcessIdBits" /> bits.\r
        /// </summary>\r
        /// <returns>The resolved and masked process ID.</returns>\r
        private static int ResolveProcessId()\r
        {\r
            // Use the explicitly configured process ID, if set.\r
            if (s_configuredProcessId.HasValue)\r
            {\r
                return s_configuredProcessId.Value & ProcessIdMask;\r
            }\r
\r
            // Check for an environment variable override. This is useful in containerized\r
            // environments (e.g., Docker) where the process ID is always 1.\r
            string envValue = Environment.GetEnvironmentVariable(ProcessIdEnvironmentVariable);\r
            if (int.TryParse(envValue, out int envProcessId))\r
            {\r
                return envProcessId & ProcessIdMask;\r
            }\r
\r
            // Fall back to the OS-assigned process ID.\r
            return Process.GetCurrentProcess().Id & ProcessIdMask;\r
        }\r
\r
        public override string ToString() => _value.ToString();\r
\r
        public static implicit operator long(Id id) => id._value;\r
\r
        public static bool operator ==(Id left, Id right) => left._value == right._value;\r
\r
        public static bool operator !=(Id left, Id right) => !(left == right);\r
\r
        public int CompareTo(Id other) => _value.CompareTo(other._value);\r
\r
        public bool Equals(Id other) => _value == other._value;\r
\r
        public override bool Equals(object obj) => obj is Id other && Equals(other);\r
\r
        public override int GetHashCode() => _value.GetHashCode();\r
    }\r
}\r
