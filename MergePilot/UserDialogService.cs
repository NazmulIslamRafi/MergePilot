using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MergePilot
{
    public interface IUserDialogService
    {
        void ShowInformation(string message, string title);
        void ShowWarning(string message, string title);
        void ShowError(string message, string title);
        bool ConfirmYesNo(string message, string title, UserDialogIcon icon = UserDialogIcon.Question);
        bool ConfirmOkCancel(string message, string title, UserDialogIcon icon = UserDialogIcon.Question);
    }

    public enum UserDialogIcon
    {
        Question,
        Information,
        Warning
    }

    public sealed class WpfUserDialogService : IUserDialogService
    {
        public void ShowInformation(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ConfirmYesNo(string message, string title, UserDialogIcon icon = UserDialogIcon.Question)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, ToMessageBoxImage(icon)) == MessageBoxResult.Yes;
        }

        public bool ConfirmOkCancel(string message, string title, UserDialogIcon icon = UserDialogIcon.Question)
        {
            return MessageBox.Show(message, title, MessageBoxButton.OKCancel, ToMessageBoxImage(icon)) == MessageBoxResult.OK;
        }

        private static MessageBoxImage ToMessageBoxImage(UserDialogIcon icon)
        {
            return icon switch
            {
                UserDialogIcon.Information => MessageBoxImage.Information,
                UserDialogIcon.Warning => MessageBoxImage.Warning,
                _ => MessageBoxImage.Question
            };
        }
    }
}
