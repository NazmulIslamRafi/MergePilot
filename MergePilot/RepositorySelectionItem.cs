using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MergePilot
{
    /// <summary>
    /// Bindable repository checkbox item used by MainWindow.
    /// </summary>
    public class RepositorySelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public RepositorySelectionItem(AppSettings.RepositoryEntry repository)
        {
            Repository = repository;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppSettings.RepositoryEntry Repository { get; }

        public string DisplayName => string.IsNullOrWhiteSpace(Repository.Name)
            ? Repository.Path ?? string.Empty
            : Repository.Name;

        public string Path => Repository.Path ?? string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
