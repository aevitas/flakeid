using System;
using System.Linq;
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

        [TestMethod]
        public void Id_ToBase64String_ProducesValidString()
        {
            Id id = Id.Create();
            string base64 = id.ToBase64String();

            Assert.IsNotNull(base64);
            Assert.IsTrue(base64.Length > 0);
            Assert.IsFalse(base64.Contains('+'));
            Assert.IsFalse(base64.Contains('/'));
            Assert.IsFalse(base64.Contains('='));
        }

        [TestMethod]
        public void Id_ToBase64String_IsUrlSafe()
        {
            Id id = Id.Create();
            string base64 = id.ToBase64String();

            Assert.IsTrue(base64.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        }

        [TestMethod]
        public void Id_ToBase64String_And_FromBase64String_RoundTrip()
        {
            Id originalId = Id.Create();
            string base64 = originalId.ToBase64String();
            Id parsedId = IdExtensions.FromBase64String(base64);

            Assert.AreEqual(originalId, parsedId);
        }

        [TestMethod]
        public void Id_FromBase64String_HandlesLegacyToStringIdentifierFormat()
        {
            Id originalId = Id.Create();
            string legacyBase64 = originalId.ToStringIdentifier();
            Id parsedId = IdExtensions.FromBase64String(legacyBase64);

            Assert.AreEqual(originalId, parsedId);
        }

        [TestMethod]
        public void Id_FromBase64String_HandlesNewFormat()
        {
            Id originalId = Id.Create();
            string newBase64 = originalId.ToBase64String();
            Id parsedId = IdExtensions.FromBase64String(newBase64);

            Assert.AreEqual(originalId, parsedId);
        }

        [TestMethod]
        public void Id_FromBase64String_ThrowsOnInvalidInput()
        {
            Assert.ThrowsException<ArgumentException>(() => IdExtensions.FromBase64String(null));
            Assert.ThrowsException<ArgumentException>(() => IdExtensions.FromBase64String(""));
            Assert.ThrowsException<ArgumentException>(() =>
                IdExtensions.FromBase64String("invalid")
            );
            Assert.ThrowsException<ArgumentException>(() =>
                IdExtensions.FromBase64String("not-base64!")
            );
        }

        [TestMethod]
        public void Id_ToBase64String_IsShorterThanToStringIdentifier()
        {
            Id id = Id.Create();

            string newFormat = id.ToBase64String();
            string legacyFormat = id.ToStringIdentifier();

            Assert.IsTrue(newFormat.Length <= legacyFormat.Length);
        }

        [TestMethod]
        public void Id_ToBase64String_WithSpecificValues()
        {
            Id id1 = new Id(1234567890123456789L);
            Id id2 = new Id(0L);
            Id id3 = new Id(-1L);

            string base64_1 = id1.ToBase64String();
            string base64_2 = id2.ToBase64String();
            string base64_3 = id3.ToBase64String();

            Assert.IsNotNull(base64_1);
            Assert.IsNotNull(base64_2);
            Assert.IsNotNull(base64_3);

            Assert.AreEqual(id1, IdExtensions.FromBase64String(base64_1));
            Assert.AreEqual(id2, IdExtensions.FromBase64String(base64_2));
            Assert.AreEqual(id3, IdExtensions.FromBase64String(base64_3));
        }

        [TestMethod]
        public void Id_ToStringIdentifier_HasObsoleteAttribute()
        {
            var method = typeof(IdExtensions).GetMethod("ToStringIdentifier");
            var obsoleteAttribute = method.GetCustomAttributes(typeof(ObsoleteAttribute), false);

            Assert.IsTrue(obsoleteAttribute.Length > 0);
        }
    }
}
