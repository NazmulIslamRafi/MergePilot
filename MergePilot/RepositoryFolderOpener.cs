using System;
using System.Diagnostics;

namespace MergePilot
{
    public interface IRepositoryFolderOpener
    {
        bool TryOpen(string repositoryPath);
    }

    public sealed class ExplorerRepositoryFolderOpener : IRepositoryFolderOpener
    {
        public bool TryOpen(string repositoryPath)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = $"\"{repositoryPath}\"",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
