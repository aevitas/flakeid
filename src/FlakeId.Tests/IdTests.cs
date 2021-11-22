using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakeId.Tests
{
    [TestClass]
    public class IdTests
    {
        [TestMethod]
        public void Id_Create()
        {
            var id = Id.Create();
            
            Assert.IsTrue(id > 1);
        }

        [TestMethod]
        public void Id_CreateManyFast()
        {
            var ids = Enumerable.Range(0, 1000).Select(_ => Id.Create()).ToArray();

            foreach (var id in ids)
                Assert.IsTrue(ids.Count(i => i == id) == 1);
        }

        [TestMethod]
        public async Task Id_CreateManyDelayed()
        {
            List<Id> ids = new();

            for (int i = 0; i < 100; i++)
            {
                ids.Add(Id.Create());
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
            
            foreach (var id in ids)
                Assert.IsTrue(ids.Count(i => i == id) == 1);
        }

        [TestMethod]
        public void Id_Equality()
        {
            // This test should never fail so long as Id is a struct.
            var left = new Id(5956206959003041793);
            var right = new Id(5956206959003041793);
            
            Assert.AreEqual(left, right);
        }

        [TestMethod]
        public void Id_Sortable()
        {
            // The sequence in which Ids are generated should be equal to a set of sorted Ids.
            var ids = Enumerable.Range(0, 1000).Select(_ => Id.Create()).ToArray();
            var sorted = ids.OrderBy(i => i).ToArray();
            
            Assert.IsTrue(ids.SequenceEqual(sorted));
        }

        [TestMethod]
        public void Id_Parse_Invalid()
        {
            const long value = 10;

            Assert.ThrowsException<FormatException>(() => Id.Parse(value));
        }

        [TestMethod]
        public void Id_Parse()
        {
            long id = Id.Create();

            Id.Parse(id);
        }

        [TestMethod]
        public void Id_TryParse_Invalid()
        {
            const long value = 10;

            bool parse = Id.TryParse(value, out _);

            Assert.IsFalse(parse);
        }

        [TestMethod]
        public void Id_TryParse()
        {
            long id = Id.Create();

            bool parse = Id.TryParse(id, out var parsed);

            Assert.IsTrue(parse);
            Assert.AreEqual(id, parsed);
        }

        [TestMethod]
        public void Id_ToString()
        {
            long id = Id.Create();

            Assert.AreEqual(((long)id).ToString(), id.ToString());
        }
    }
}
