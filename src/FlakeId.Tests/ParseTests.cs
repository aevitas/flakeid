using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakeId.Tests
{
    [TestClass]
    public class ParseTests
    {


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

            bool parse = Id.TryParse(id, out Id parsed);

            Assert.IsTrue(parse);
            Assert.AreEqual(id, parsed);
        }

        [TestMethod]
        public void Id_TryParse_Many()
        {
            List<Id> ids = Enumerable.Range(0, 100_000).Select(_ => Id.Create()).ToList();
            List<Id> problematic = new List<Id>();

            bool failed = false;
            foreach (var id in ids)
            {
                if (!Id.TryParse(id, out Id parsed))
                {
                    Debug.WriteLine(id);
                    problematic.Add(id);
                    failed = true;
                }
            }

            Assert.IsFalse(failed);
        }

        [TestMethod]
        public void Id_TryParse_Problematic()
        {
            Id id = Id.Parse(1108047973760811023);
        }

        public class IdJson
        {
            public required Id Id { get; set; }
        }

        [TestMethod]
        public void Id_JsonConverter_Read()
        {
            const string json = "{\"Id\":1108047973760811023}";

            var obj = JsonSerializer.Deserialize<IdJson>(json);

            Assert.IsNotNull(obj);
            Assert.AreEqual(1108047973760811023, obj.Id);
        }

        [TestMethod]
        public void Id_JsonConverter_Write()
        {
            var obj = new IdJson
            {
                Id = Id.Parse(1108047973760811023)
            };

            string json = JsonSerializer.Serialize(obj);

            Assert.AreEqual("{\"Id\":1108047973760811023}", json);
        }
    }
}
