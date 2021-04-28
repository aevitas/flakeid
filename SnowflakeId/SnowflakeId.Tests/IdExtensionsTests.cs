using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SnowflakeId.Extensions;

namespace SnowflakeId.Tests
{
    [TestClass]
    public class IdExtensionsTests
    {
        [TestMethod]
        public void Id_ToDateTimeOffset()
        {
            var id = Id.Create();
            var timeStamp = id.ToDateTimeOffset();
            var now = DateTimeOffset.Now;
            var delta = now - timeStamp;

            Assert.IsTrue(delta.Seconds <= 1);
        }
    }
}
