using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlakeId.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakeId.Tests
{
    [TestClass]
    public class IdTests
    {
        [TestMethod]
        public void Id_Create()
        {
            Id id = Id.Create();

            Assert.IsTrue(id > 1);
        }

        [TestMethod]
        public void Id_CreateFromTimeStamp()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long timeStamp = now.ToUnixTimeMilliseconds();
            Id id = Id.Create(timeStamp);

            Assert.AreEqual(timeStamp, id.ToUnixTimeMilliseconds());
        }

        [TestMethod]
        public void Id_CreateFromTimeStamp_RecentTimeStamp()
        {
            Assert.ThrowsException<ArgumentException>(
                () => Id.Create(new DateTimeOffset(2010, 1, 1, 0, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()));
        }

        [TestMethod]
        public void Id_CreateManyFast()
        {
            Id[] ids = Enumerable.Range(0, 1000).Select(_ => Id.Create()).ToArray();

            foreach (Id id in ids)
            {
                Assert.IsTrue(ids.Count(i => i == id) == 1);
            }
        }

        [TestMethod]
        public async Task Id_CreateManyDelayed()
        {
            List<Id> ids = new List<Id>();

            for (int i = 0; i < 100; i++)
            {
                ids.Add(Id.Create());
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            }

            foreach (Id id in ids)
            {
                Assert.IsTrue(ids.Count(i => i == id) == 1);
            }
        }

        [TestMethod]
        public void Id_Equality()
        {
            // This test should never fail so long as Id is a struct.
            Id left = new Id(5956206959003041793);
            Id right = new Id(5956206959003041793);

            Assert.AreEqual(left, right);
        }

        [TestMethod]
        public void Id_Sortable()
        {
            // The sequence in which Ids are generated should be equal to a set of sorted Ids.
            Id[] ids = Enumerable.Range(0, 1000).Select(_ => Id.Create()).ToArray();
            Id[] sorted = ids.OrderBy(i => i).ToArray();

            Assert.IsTrue(ids.SequenceEqual(sorted));
        }

        [TestMethod]
        public void Id_ToString()
        {
            long id = Id.Create();

            Assert.AreEqual(id.ToString(), id.ToString());
        }
    }
}
