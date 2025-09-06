using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakeId.Tests;

[TestClass]
public class ConcurrencyTests
{
    private const int ThreadIdBits = 5;
    private const int ProcessIdBits = 5;
    private const int IncrementBits = 12;
    private const int TimestampShift = ThreadIdBits + ProcessIdBits + IncrementBits;
    private const int IncrementMask = (1 << IncrementBits) - 1;

    private static readonly FieldInfo s_prevIdField =
        typeof(Id).GetField("s_prevId", BindingFlags.NonPublic | BindingFlags.Static);

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsNotNull(s_prevIdField, "Could not find the private static field 's_prevId' for testing.");
        s_prevIdField.SetValue(null, 0L);
    }

    [TestMethod]
    public void Create_ShouldGenerateNonZeroId()
    {
        long id = Id.Create();

        Assert.AreNotEqual(0L, id);
    }

    [TestMethod]
    public void Create_ShouldGenerateMonotonicallyIncreasingIds()
    {
        const int idCount = 500_000;
        long[] ids = new long[idCount];

        for (int i = 0; i < idCount; i++)
        {
            ids[i] = Id.Create();
        }

        for (int i = 1; i < idCount; i++)
        {
            Assert.IsTrue(ids[i] > ids[i - 1], $"ID at index {i} was not greater than the previous one.");
        }
    }

    [TestMethod]
    public void Create_ShouldGenerateUniqueIds_WhenCalledConcurrently()
    {
        const int idsPerTask = 100_000;
        int taskCount = Environment.ProcessorCount;
        int totalIds = idsPerTask * taskCount;
        var generatedIds = new ConcurrentBag<long>();

        var tasks = new List<Task>();

        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < idsPerTask; j++)
                {
                    generatedIds.Add(Id.Create());
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.AreEqual(totalIds, generatedIds.Count, "The number of generated IDs does not match the expected total.");

        var distinctIds = new HashSet<long>(generatedIds);
        Assert.AreEqual(totalIds, distinctIds.Count, "Duplicate IDs were found when generating concurrently.");
    }

    [TestMethod]
    public void Create_ShouldThrow_WhenClockMovesBackwards()
    {
        Assert.IsNotNull(s_prevIdField, "Could not find the private static field 's_prevId' for testing.");

        long baseId = Id.Create();
        long baseTimestamp = baseId >> TimestampShift;

        long futureTimestamp = baseTimestamp + 5000;
        long futureId = futureTimestamp << TimestampShift;
        s_prevIdField.SetValue(null, futureId);

        var ex = Assert.ThrowsException<InvalidOperationException>(() => Id.Create());
        StringAssert.Contains(ex.Message, "Clock shifted backwards");
    }

    [TestMethod]
    public void Increment_ShouldResetToZero_OnNewMillisecond()
    {Assert.IsNotNull(s_prevIdField, "Could not find the private static field 's_prevId' for testing.");

        long baseId = Id.Create();
        long baseTimestamp = baseId >> TimestampShift;

        // create a fake "previous ID" that is at the same timestamp but has a high increment.
        // we preserve the other parts of the ID (thread, process) to make the state realistic.
        const int highIncrement = 4000;
        long otherParts = baseId & ~((1L << 42) - 1); // masks out the timestamp
        long prevId = otherParts | (baseTimestamp << TimestampShift) | highIncrement;

        s_prevIdField.SetValue(null, prevId);

        Thread.Sleep(5);

        long newIdValue = Id.Create();

        long newTimestamp = newIdValue >> TimestampShift;
        int newIncrement = (int)(newIdValue & IncrementMask);

        Assert.IsTrue(newTimestamp > baseTimestamp, "Timestamp should have advanced.");
        Assert.AreEqual(0, newIncrement, "Increment should reset to 0 on a new millisecond.");
    }
}
