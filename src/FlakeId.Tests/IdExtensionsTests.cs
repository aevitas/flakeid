using System;
using FlakeId.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakeId.Tests
{
    [TestClass]
    public class IdExtensionsTests
    {
        [TestMethod]
        public void Id_ToDateTimeOffset()
        {
            Id id = Id.Create();
            DateTimeOffset timeStamp = id.ToDateTimeOffset();
            DateTimeOffset now = DateTimeOffset.Now;
            TimeSpan delta = now - timeStamp;

            Assert.IsTrue(delta.Seconds <= 1);
        }

        [TestMethod]
        public void Id_ToUnixTimeMilliseconds()
        {
            Id id = Id.Create();
            long timestamp = id.ToUnixTimeMilliseconds();
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Assert.IsTrue(now - timestamp < 100);
        }

        [TestMethod]
        public void Id_IsValid()
        {
            Id id = Id.Create();
            bool isValid = id.IsSnowflake();

            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Id_ToStringIdentifier_ProducesValidId()
        {
            Id id = Id.Create();
            string s = id.ToStringIdentifier();

            Assert.AreNotEqual(default, s);
        }
    }
}
