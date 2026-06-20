using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace MergePilot
{
    /// <summary>
    /// Buffers log output, trims retained text, and optionally streams drained logs to a file.
    /// </summary>
    public sealed class LogBufferService : IDisposable
    {
        public const int DefaultMaxChars = 200_000;

        private readonly ConcurrentQueue<string> _outputQueue = new();
        private readonly ConcurrentQueue<string> _errorQueue = new();
        private readonly StringBuilder _outputMaster = new();
        private readonly StringBuilder _errorMaster = new();
        private StreamWriter? _streamWriter;

        public int MaxChars { get; set; } = DefaultMaxChars;
        public bool IsStreaming => _streamWriter != null;
        public string OutputText => _outputMaster.ToString();
        public string ErrorText => _errorMaster.ToString();

        public void AppendOutput(string text, bool status = false, DateTime? timestamp = null)
        {
            var line = status
                ? text
                : $"[{(timestamp ?? DateTime.Now):yyyy-MM-dd HH:mm:ss}] {text}";

            var normalizedLine = EnsureTrailingNewLine(line);
            _outputQueue.Enqueue(normalizedLine);
            _outputMaster.Append(normalizedLine);
        }

        public void AppendError(string text, DateTime? timestamp = null)
        {
            var line = $"[{(timestamp ?? DateTime.Now):yyyy-MM-dd HH:mm:ss}] {text}";
            var normalizedLine = EnsureTrailingNewLine(line);
            _errorQueue.Enqueue(normalizedLine);
            _errorMaster.Append(normalizedLine);
        }

        public LogFlushBatch Drain()
        {
            var output = DrainQueue(_outputQueue);
            var error = DrainQueue(_errorQueue);
            var combined = output + error;

            TrimMasters();

            if (combined.Length > 0 && _streamWriter != null)
            {
                _streamWriter.Write(combined);
                _streamWriter.Flush();
            }

            return new LogFlushBatch(output, error);
        }

        public void Clear()
        {
            DrainQueue(_outputQueue);
            DrainQueue(_errorQueue);
            _outputMaster.Clear();
            _errorMaster.Clear();
        }

        public string BuildExport(string sectionSeparator)
        {
            var separator = string.IsNullOrEmpty(sectionSeparator)
                ? "==============================================="
                : sectionSeparator;

            var combined = new StringBuilder();
            combined.Append(separator + "\n");
            combined.Append("--- OUTPUT ---\n");
            combined.Append(separator + "\n");
            combined.Append(OutputText);
            combined.Append(separator + "\n");
            combined.Append("--- ERRORS ---\n");
            combined.Append(separator + "\n");
            combined.Append(ErrorText);
            return combined.ToString();
        }

        public void StartStreaming(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Log stream path cannot be empty.", nameof(path));

            StopStreaming();

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _streamWriter = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }

        public void StopStreaming()
        {
            _streamWriter?.Flush();
            _streamWriter?.Dispose();
            _streamWriter = null;
        }

        public void Dispose()
        {
            StopStreaming();
        }

        private void TrimMasters()
        {
            var max = MaxChars > 0 ? MaxChars : DefaultMaxChars;

            if (_outputMaster.Length > max)
                _outputMaster.Remove(0, _outputMaster.Length - max);

            if (_errorMaster.Length > max)
                _errorMaster.Remove(0, _errorMaster.Length - max);
        }

        private static string DrainQueue(ConcurrentQueue<string> queue)
        {
            var builder = new StringBuilder();
            while (queue.TryDequeue(out var item))
            {
                builder.Append(item);
            }

            return builder.ToString();
        }

        private static string EnsureTrailingNewLine(string text)
        {
            return text.EndsWith('\n')
                ? text
                : text + "\n";
        }
    }

    public record LogFlushBatch(string OutputText, string ErrorText)
    {
        public string CombinedText => OutputText + ErrorText;
        public bool HasText => OutputText.Length > 0 || ErrorText.Length > 0;
    }
}
