using System;
using System.IO;
using System.Windows;
using Xunit;

namespace MergePilot.Tests
{
    public class LogExportServiceTests : IDisposable
    {
        private readonly string _tempDirectory;

        public LogExportServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.LogExport", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }

        [Fact]
        public void SaveToSelectedFile_WritesExportTextToSelectedPath()
        {
            var path = Path.Combine(_tempDirectory, "export.txt");
            var picker = new StubFilePickerService(path);
            var logBuffer = new LogBufferService();
            logBuffer.AppendOutput("output-line", status: true);
            logBuffer.AppendError("error-line", new DateTime(2026, 6, 6, 8, 30, 0));
            var service = new LogExportService();

            var result = service.SaveToSelectedFile(logBuffer, picker, "==");

            Assert.True(result.IsSaved);
            Assert.Equal(path, result.Path);
            Assert.Equal(LogExportService.DefaultSaveOptions, picker.LastSaveOptions);
            var export = File.ReadAllText(path);
            Assert.Contains("--- OUTPUT ---", export);
            Assert.Contains("output-line\n", export);
            Assert.Contains("--- ERRORS ---", export);
            Assert.Contains("[2026-06-06 08:30:00] error-line\n", export);
        }

        [Fact]
        public void SaveToSelectedFile_WhenPickerCanceled_DoesNotWriteFile()
        {
            var picker = new StubFilePickerService(null);
            var logBuffer = new LogBufferService();
            logBuffer.AppendOutput("output-line", status: true);
            var service = new LogExportService();

            var result = service.SaveToSelectedFile(logBuffer, picker, "==");

            Assert.False(result.IsSaved);
            Assert.Null(result.Path);
            Assert.Equal(LogExportService.DefaultSaveOptions, picker.LastSaveOptions);
            Assert.Empty(Directory.GetFiles(_tempDirectory));
        }

        private sealed class StubFilePickerService : IFilePickerService
        {
            private readonly string? _savePath;

            public StubFilePickerService(string? savePath)
            {
                _savePath = savePath;
            }

            public SaveFilePickerOptions? LastSaveOptions { get; private set; }

            public string? SelectFolder(string description, bool useDescriptionForTitle = false, bool showNewFolderButton = true)
            {
                throw new NotSupportedException();
            }

            public string? SelectSaveFile(SaveFilePickerOptions options, Window? owner = null)
            {
                LastSaveOptions = options;
                return _savePath;
            }
        }
    }
}
