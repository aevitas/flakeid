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
        public async Task Id_CreateMany()
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
    }
}
