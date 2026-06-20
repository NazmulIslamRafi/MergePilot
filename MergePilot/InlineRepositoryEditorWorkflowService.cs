using System;

namespace MergePilot
{
    /// <summary>
    /// Coordinates inline repository editor prompts and mutations without depending on WPF controls.
    /// </summary>
    public class InlineRepositoryEditorWorkflowService
    {
        private readonly InlineRepositoryEditorService _editorService;
        private readonly IUserDialogService _dialogs;

        public InlineRepositoryEditorWorkflowService(
            InlineRepositoryEditorService editorService,
            IUserDialogService dialogs)
        {
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        }

        public InlineRepositoryEditWorkflowResult BeginEdit(
            AppSettings settings,
            int selectedIndex,
            AppSettings.RepositoryEntry? selectedRepository)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (selectedIndex < 0)
            {
                _dialogs.ShowInformation("Select a repository to edit.", "No selection");
                return InlineRepositoryEditWorkflowResult.None;
            }

            var selection = _editorService.ResolveSelectedRepository(
                settings,
                selectedIndex,
                selectedRepository);
            if (!selection.HasRepository)
            {
                _dialogs.ShowWarning("Selected repository was not found.", "Invalid selection");
                return InlineRepositoryEditWorkflowResult.None;
            }

            return new InlineRepositoryEditWorkflowResult(true, selection.Repository, selection.Index);
        }

        public InlineRepositoryMutationWorkflowResult AddRepository(
            AppSettings settings,
            string? name,
            string? path)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
            {
                _dialogs.ShowWarning("Repo name and path are required.", "Invalid input");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            var allowMissingGitRepository = ConfirmMissingGitIfNeeded(path);
            if (allowMissingGitRepository == null)
                return InlineRepositoryMutationWorkflowResult.NoChange;

            var result = _editorService.AddRepository(
                settings,
                name.Trim(),
                path.Trim(),
                allowMissingGitRepository.Value);
            if (!result.IsSuccess)
            {
                _dialogs.ShowWarning(result.Message, "Invalid input");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            return new InlineRepositoryMutationWorkflowResult(true, result.Index);
        }

        public InlineRepositoryMutationWorkflowResult UpdateRepository(
            AppSettings settings,
            int selectedIndex,
            AppSettings.RepositoryEntry? selectedRepository,
            string? name,
            string? path)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (selectedIndex < 0)
            {
                _dialogs.ShowInformation("Select a repository to update.", "No selection");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            var selection = _editorService.ResolveSelectedRepository(
                settings,
                selectedIndex,
                selectedRepository);
            if (!selection.HasRepository)
            {
                _dialogs.ShowWarning("Selected repository was not found.", "Invalid selection");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
            {
                _dialogs.ShowWarning("Name and path are required.", "Invalid input");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            var allowMissingGitRepository = ConfirmMissingGitIfNeeded(path);
            if (allowMissingGitRepository == null)
                return InlineRepositoryMutationWorkflowResult.NoChange;

            var result = _editorService.UpdateRepository(
                settings,
                selection.Index,
                name.Trim(),
                path.Trim(),
                allowMissingGitRepository.Value);
            if (!result.IsSuccess)
            {
                _dialogs.ShowWarning(result.Message, "Invalid input");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            return new InlineRepositoryMutationWorkflowResult(true, selection.Index);
        }

        public InlineRepositoryMutationWorkflowResult RemoveRepository(
            AppSettings settings,
            int selectedIndex,
            AppSettings.RepositoryEntry? selectedRepository)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (selectedIndex < 0)
            {
                _dialogs.ShowInformation("Select a repository to remove.", "No selection");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            var selection = _editorService.ResolveSelectedRepository(
                settings,
                selectedIndex,
                selectedRepository);
            if (!selection.HasRepository)
            {
                _dialogs.ShowWarning("Selected repository was not found.", "Invalid selection");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            if (!_dialogs.ConfirmYesNo($"Remove '{selection.Repository?.Name}'?", "Confirm remove"))
                return InlineRepositoryMutationWorkflowResult.NoChange;

            var result = _editorService.RemoveRepository(settings, selection.Index);
            if (!result.IsSuccess)
            {
                _dialogs.ShowWarning(result.Message, "Invalid selection");
                return InlineRepositoryMutationWorkflowResult.NoChange;
            }

            return new InlineRepositoryMutationWorkflowResult(true, selection.Index);
        }

        private bool? ConfirmMissingGitIfNeeded(string path)
        {
            if (!_editorService.RequiresMissingGitConfirmation(path))
                return false;

            return _dialogs.ConfirmYesNo(
                "The selected path does not contain a .git folder. Add anyway?",
                "Git folder not found");
        }
    }

    public readonly record struct InlineRepositoryEditWorkflowResult(
        bool ShouldEdit,
        AppSettings.RepositoryEntry? Repository,
        int Index)
    {
        public static InlineRepositoryEditWorkflowResult None { get; } = new(false, null, -1);
    }

    public readonly record struct InlineRepositoryMutationWorkflowResult(
        bool ShouldRefresh,
        int Index)
    {
        public static InlineRepositoryMutationWorkflowResult NoChange { get; } = new(false, -1);
    }
}
