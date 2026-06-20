using System;
using System.IO;
using System.Windows;

namespace MergePilot
{
    /// <summary>
    /// Coordinates log export file selection and writing.
    /// </summary>
    public class LogExportService
    {
        public static readonly SaveFilePickerOptions DefaultSaveOptions = new(
            "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            "txt",
            "mergepilot_logs.txt");

        public LogExportResult SaveToSelectedFile(
            LogBufferService logBufferService,
            IFilePickerService filePickerService,
            string sectionSeparator,
            Window? owner = null)
        {
            ArgumentNullException.ThrowIfNull(logBufferService);
            ArgumentNullException.ThrowIfNull(filePickerService);

            var selectedPath = filePickerService.SelectSaveFile(DefaultSaveOptions, owner);
            if (string.IsNullOrWhiteSpace(selectedPath))
                return LogExportResult.Canceled();

            File.WriteAllText(selectedPath, logBufferService.BuildExport(sectionSeparator));
            return LogExportResult.Success(selectedPath);
        }
    }

    public readonly record struct LogExportResult(bool IsSaved, string? Path)
    {
        public static LogExportResult Success(string path) => new(true, path);
        public static LogExportResult Canceled() => new(false, null);
    }
}
