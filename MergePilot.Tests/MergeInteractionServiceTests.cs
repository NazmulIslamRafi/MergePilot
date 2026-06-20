using System.Collections.Generic;
using Xunit;

namespace MergePilot.Tests
{
    public class MergeInteractionServiceTests
    {
        [Fact]
        public void ConfirmMissingTargetBranch_UsesWarningConfirmation()
        {
            var dialogs = new FakeUserDialogService { NextYesNoResult = true };
            var service = new MergeInteractionService(dialogs, new FakeRepositoryFolderOpener(), _ => { });

            var result = service.ConfirmMissingTargetBranch(
                new MissingTargetBranchContext("C:\\repo", "origin", "develop"));

            Assert.True(result);
            Assert.Single(dialogs.YesNoPrompts);
            Assert.Equal("Target branch not found", dialogs.YesNoPrompts[0].Title);
            Assert.Equal(UserDialogIcon.Warning, dialogs.YesNoPrompts[0].Icon);
            Assert.Contains("develop", dialogs.YesNoPrompts[0].Message);
        }

        [Fact]
        public void ResolveMergeConflict_WhenUserDeclinesManualResolution_Aborts()
        {
            var dialogs = new FakeUserDialogService { NextYesNoResult = false };
            var opener = new FakeRepositoryFolderOpener();
            var service = new MergeInteractionService(dialogs, opener, _ => { });

            var result = service.ResolveMergeConflict(
                new MergeConflictContext("C:\\repo", "feature/a", "main", "conflict"));

            Assert.Equal(MergeConflictResolution.Abort, result);
            Assert.False(opener.WasCalled);
            Assert.Empty(dialogs.OkCancelPrompts);
        }

        [Fact]
        public void ResolveMergeConflict_WhenUserResolvesAndConfirms_ReturnsContinue()
        {
            var dialogs = new FakeUserDialogService
            {
                NextYesNoResult = true,
                NextOkCancelResult = true
            };
            var opener = new FakeRepositoryFolderOpener { OpenResult = true };
            var service = new MergeInteractionService(dialogs, opener, _ => { });

            var result = service.ResolveMergeConflict(
                new MergeConflictContext("C:\\repo", "feature/a", "main", "conflict"));

            Assert.Equal(MergeConflictResolution.ContinueAfterManualResolution, result);
            Assert.True(opener.WasCalled);
            Assert.Single(dialogs.OkCancelPrompts);
        }

        [Fact]
        public void ResolveMergeConflict_WhenFolderOpenFails_LogsErrorAndStillAsksForResolution()
        {
            var errors = new List<string>();
            var dialogs = new FakeUserDialogService
            {
                NextYesNoResult = true,
                NextOkCancelResult = false
            };
            var opener = new FakeRepositoryFolderOpener { OpenResult = false };
            var service = new MergeInteractionService(dialogs, opener, errors.Add);

            var result = service.ResolveMergeConflict(
                new MergeConflictContext("C:\\repo", "feature/a", "main", "conflict"));

            Assert.Equal(MergeConflictResolution.Abort, result);
            Assert.Single(errors);
            Assert.Contains("C:\\repo", errors[0]);
            Assert.Single(dialogs.OkCancelPrompts);
        }

        private sealed class FakeUserDialogService : IUserDialogService
        {
            public bool NextYesNoResult { get; set; }
            public bool NextOkCancelResult { get; set; }
            public List<DialogPrompt> YesNoPrompts { get; } = new();
            public List<DialogPrompt> OkCancelPrompts { get; } = new();

            public void ShowInformation(string message, string title) { }
            public void ShowWarning(string message, string title) { }
            public void ShowError(string message, string title) { }

            public bool ConfirmYesNo(string message, string title, UserDialogIcon icon = UserDialogIcon.Question)
            {
                YesNoPrompts.Add(new DialogPrompt(message, title, icon));
                return NextYesNoResult;
            }

            public bool ConfirmOkCancel(string message, string title, UserDialogIcon icon = UserDialogIcon.Question)
            {
                OkCancelPrompts.Add(new DialogPrompt(message, title, icon));
                return NextOkCancelResult;
            }
        }

        private sealed class FakeRepositoryFolderOpener : IRepositoryFolderOpener
        {
            public bool OpenResult { get; set; }
            public bool WasCalled { get; private set; }

            public bool TryOpen(string repositoryPath)
            {
                WasCalled = true;
                return OpenResult;
            }
        }

        private record DialogPrompt(string Message, string Title, UserDialogIcon Icon);
    }
}
