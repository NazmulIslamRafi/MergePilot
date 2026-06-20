using System;
using System.IO;
using Xunit;

namespace MergePilot.Tests
{
    public class LogBufferServiceTests : IDisposable
    {
        private readonly string _tempDirectory;

        public LogBufferServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.LogBuffer", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }

        [Fact]
        public void AppendOutput_WithStatus_DoesNotAddTimestamp()
        {
            var service = new LogBufferService();

            service.AppendOutput("STATUS", status: true);
            var batch = service.Drain();

            Assert.Equal("STATUS\n", batch.OutputText);
            Assert.Equal("STATUS\n", batch.CombinedText);
        }

        [Fact]
        public void AppendOutputAndError_AddTimestampAndDrainInOutputThenErrorOrder()
        {
            var service = new LogBufferService();
            var timestamp = new DateTime(2026, 6, 2, 10, 30, 0);

            service.AppendOutput("hello", timestamp: timestamp);
            service.AppendError("bad", timestamp: timestamp);
            var batch = service.Drain();

            Assert.Equal("[2026-06-02 10:30:00] hello\n", batch.OutputText);
            Assert.Equal("[2026-06-02 10:30:00] bad\n", batch.ErrorText);
            Assert.Equal(batch.OutputText + batch.ErrorText, batch.CombinedText);
        }

        [Fact]
        public void Drain_WithMultipleQueuedEntries_SeparatesEntriesByLine()
        {
            var service = new LogBufferService();

            service.AppendOutput("one", status: true);
            service.AppendOutput("two", status: true);
            var batch = service.Drain();

            Assert.Equal("one\ntwo\n", batch.OutputText);
        }

        [Fact]
        public void Drain_TrimsMasterBuffersToMaxChars()
        {
            var service = new LogBufferService { MaxChars = 5 };

            service.AppendOutput("1234567890", status: true);
            service.AppendError("abcdefghij", timestamp: new DateTime(2026, 6, 2, 1, 2, 3));
            service.Drain();

            Assert.Equal("7890\n", service.OutputText);
            Assert.Equal("ghij\n", service.ErrorText);
        }

        [Fact]
        public void Clear_RemovesQueuedAndMasterText()
        {
            var service = new LogBufferService();
            service.AppendOutput("hello", status: true);
            service.AppendError("bad");

            service.Clear();
            var batch = service.Drain();

            Assert.False(batch.HasText);
            Assert.Empty(service.OutputText);
            Assert.Empty(service.ErrorText);
        }

        [Fact]
        public void BuildExport_IncludesOutputAndErrorSections()
        {
            var service = new LogBufferService();
            service.AppendOutput("out", status: true);
            service.AppendError("err", timestamp: new DateTime(2026, 6, 2, 1, 2, 3));

            var export = service.BuildExport("==");

            Assert.Contains("--- OUTPUT ---", export);
            Assert.Contains("out", export);
            Assert.Contains("--- ERRORS ---", export);
            Assert.Contains("err", export);
        }

        [Fact]
        public void StartStreaming_WritesDrainedLogsToFile()
        {
            using var service = new LogBufferService();
            var path = Path.Combine(_tempDirectory, "logs", "mergepilot.log");

            service.StartStreaming(path);
            service.AppendOutput("streamed", status: true);
            service.Drain();

            Assert.True(service.IsStreaming);
            Assert.Equal("streamed\n", ReadAllTextShared(path));
        }

        private static string ReadAllTextShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        [Fact]
        public void StopStreaming_DisablesFurtherWrites()
        {
            using var service = new LogBufferService();
            var path = Path.Combine(_tempDirectory, "mergepilot.log");

            service.StartStreaming(path);
            service.AppendOutput("one", status: true);
            service.Drain();
            service.StopStreaming();
            service.AppendOutput("two", status: true);
            service.Drain();

            Assert.False(service.IsStreaming);
            Assert.Equal("one\n", File.ReadAllText(path));
        }
    }
}
