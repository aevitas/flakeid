using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakeId.Tests
{
    [TestClass]
    public class ProcessIdTests
    {
        private static readonly FieldInfo s_processIdField =
            typeof(Id).GetField("s_processId", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo s_prevIdField =
            typeof(Id).GetField("s_prevId", BindingFlags.NonPublic | BindingFlags.Static);

        private int? _originalProcessId;
        private string _originalEnvValue;

        [TestInitialize]
        public void TestInitialize()
        {
            // Save the current configured process ID and environment variable so we can restore them after the test.
            _originalProcessId = Id.ProcessId;
            _originalEnvValue = Environment.GetEnvironmentVariable(Id.ProcessIdEnvironmentVariable);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore the original configured process ID and environment variable.
            Id.ProcessId = _originalProcessId;
            Environment.SetEnvironmentVariable(Id.ProcessIdEnvironmentVariable, _originalEnvValue);

            // Reset the cached process ID and prev ID to ensure a clean state for other tests.
            Assert.IsNotNull(s_processIdField, "Could not find the private static field 's_processId' for testing.");
            s_processIdField.SetValue(null, null);

            Assert.IsNotNull(s_prevIdField, "Could not find the private static field 's_prevId' for testing.");
            s_prevIdField.SetValue(null, 0L);
        }

        [TestMethod]
        public void ProcessId_WhenSet_IsUsedInIdGeneration()
        {
            // Set a specific process ID that is unlikely to be the OS-assigned one.
            Id.ProcessId = 7;

            long idValue = Id.Create();

            int processId = (int)((idValue >> Id.IncrementBits) & Id.ProcessIdMask);
            Assert.AreEqual(7, processId);
        }

        [TestMethod]
        public void ProcessId_WhenSetToNull_FallsBackToOsProcessId()
        {
            // First, set a custom process ID.
            Id.ProcessId = 15;
            long idWithCustom = Id.Create();
            int customProcessId = (int)((idWithCustom >> Id.IncrementBits) & Id.ProcessIdMask);
            Assert.AreEqual(15, customProcessId);

            // Now, clear it and verify the OS process ID is used.
            Id.ProcessId = null;
            long idWithOs = Id.Create();
            int osProcessId = (int)((idWithOs >> Id.IncrementBits) & Id.ProcessIdMask);

            // The OS process ID should be different from our custom value (in most cases).
            Assert.AreNotEqual(15, osProcessId);
        }

        [TestMethod]
        public void ProcessId_WhenSetOutOfRange_IsMaskedCorrectly()
        {
            // Set a process ID that exceeds the 5-bit range (0-31).
            // 100 & 31 = 4
            Id.ProcessId = 100;

            long idValue = Id.Create();

            int processId = (int)((idValue >> Id.IncrementBits) & Id.ProcessIdMask);
            Assert.AreEqual(4, processId);
        }

        [TestMethod]
        public void ProcessId_WhenSetViaEnvironmentVariable_IsUsedInIdGeneration()
        {
            // Set the environment variable.
            Environment.SetEnvironmentVariable(Id.ProcessIdEnvironmentVariable, "12");

            // Clear the cached process ID to force re-evaluation.
            s_processIdField.SetValue(null, null);
            // Clear the configured process ID so the env var takes effect.
            Id.ProcessId = null;

            long idValue = Id.Create();

            int processId = (int)((idValue >> Id.IncrementBits) & Id.ProcessIdMask);
            Assert.AreEqual(12, processId);
        }

        [TestMethod]
        public void ProcessId_WhenSetViaProperty_TakesPrecedenceOverEnvironmentVariable()
        {
            // Set the environment variable to one value.
            Environment.SetEnvironmentVariable(Id.ProcessIdEnvironmentVariable, "5");

            // Set the property to a different value.
            Id.ProcessId = 20;
            // Clear the cached process ID to force re-evaluation.
            s_processIdField.SetValue(null, null);

            long idValue = Id.Create();

            int processId = (int)((idValue >> Id.IncrementBits) & Id.ProcessIdMask);
            Assert.AreEqual(20, processId);
        }

        [TestMethod]
        public void ProcessId_Get_ReturnsConfiguredValue()
        {
            Id.ProcessId = 10;
            Assert.AreEqual(10, Id.ProcessId);

            Id.ProcessId = null;
            Assert.IsNull(Id.ProcessId);
        }
    }
}
