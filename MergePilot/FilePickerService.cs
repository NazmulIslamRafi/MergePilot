using System.Windows;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace MergePilot
{
    public interface IFilePickerService
    {
        string? SelectFolder(string description, bool useDescriptionForTitle = false, bool showNewFolderButton = true);
        string? SelectSaveFile(SaveFilePickerOptions options, Window? owner = null);
    }

    public record SaveFilePickerOptions(string Filter, string DefaultExt, string FileName);

    public sealed class WpfFilePickerService : IFilePickerService
    {
        public string? SelectFolder(string description, bool useDescriptionForTitle = false, bool showNewFolderButton = true)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = useDescriptionForTitle,
                ShowNewFolderButton = showNewFolderButton
            };

            return dialog.ShowDialog() == FormsDialogResult.OK
                ? dialog.SelectedPath
                : null;
        }

        public string? SelectSaveFile(SaveFilePickerOptions options, Window? owner = null)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = options.Filter,
                DefaultExt = options.DefaultExt,
                FileName = options.FileName
            };

            var result = owner == null
                ? dialog.ShowDialog()
                : dialog.ShowDialog(owner);

            return result == true
                ? dialog.FileName
                : null;
        }
    }
}
