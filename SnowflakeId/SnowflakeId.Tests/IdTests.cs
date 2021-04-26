using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeId.Tests
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
        public void Id_CreateMany()
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
            var ids = Enumerable.Range(0, 1000).Select(_ => Id.Create()).ToArray();
            var sorted = ids.OrderBy(i => i).ToArray();
            
            Assert.IsTrue(ids.SequenceEqual(sorted));
        }
    }
}
