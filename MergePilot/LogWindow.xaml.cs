using MahApps.Metro.Controls;
using System.Windows;

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
    }
}
