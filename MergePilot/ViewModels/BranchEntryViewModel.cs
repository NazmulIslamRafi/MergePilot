using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// Display wrapper for BranchEntry that adds transient UI state (last commit info).
    /// </summary>
    public class BranchEntryViewModel : INotifyPropertyChanged
    {
        private string _lastCommitInfo = "—";

        public AppSettings.BranchEntry Entry { get; }

        public string? BranchName => Entry.BranchName;
        public string? Repository => Entry.Repository;
        public bool IsManual => Entry.IsManual;

        public string LastCommitInfo
        {
            get => _lastCommitInfo;
            set
            {
                if (_lastCommitInfo != value)
                {
                    _lastCommitInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BranchEntryViewModel(AppSettings.BranchEntry entry)
        {
            Entry = entry;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
