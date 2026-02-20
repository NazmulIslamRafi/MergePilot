using MahApps.Metro.Controls;
using System.Windows;
using System.IO;

namespace MergePilot
{
    public partial class LogWindow : MetroWindow
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        public void SetText(string text)
        {
            LogTextBox.Text = text;
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
        }

        public void AppendText(string text)
        {
            LogTextBox.AppendText(text);
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
        }

        public void ClearText()
        {
            LogTextBox.Clear();
        }

        public void TrimToMaxChars(int maxChars)
        {
            if (LogTextBox.Text.Length > maxChars)
            {
                LogTextBox.Text = LogTextBox.Text.Substring(LogTextBox.Text.Length - maxChars);
                LogTextBox.CaretIndex = LogTextBox.Text.Length;
                LogTextBox.ScrollToEnd();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = "log.txt"
            };

            if (dlg.ShowDialog(this) == true)
            {
                try
                {
                    System.IO.File.WriteAllText(dlg.FileName, LogTextBox.Text);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show(this, $"Failed to save log: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void StartStreamingToFile(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                var sw = new StreamWriter(fs) { AutoFlush = true };
                sw.WriteLine($"--- Log started: {DateTime.Now:O} ---");
                _streamWriter = sw;
            }
            catch { }
        }

        private StreamWriter? _streamWriter;

        public void StopStreamingToFile()
        {
            try
            {
                _streamWriter?.Flush();
                _streamWriter?.Dispose();
                _streamWriter = null;
            }
            catch { }
        }
    }
}
