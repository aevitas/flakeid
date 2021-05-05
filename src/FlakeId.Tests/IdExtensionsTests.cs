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
            var id = Id.Create();
            var timeStamp = id.ToDateTimeOffset();
            var now = DateTimeOffset.Now;
            var delta = now - timeStamp;

            Assert.IsTrue(delta.Seconds <= 1);
        }

        [TestMethod]
        public void Id_ToUnixTimeMilliseconds()
        {
            var id = Id.Create();
            long timestamp = id.ToUnixTimeMilliseconds();
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            Assert.IsTrue(now - timestamp < 100);
        }
    }
}
