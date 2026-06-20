using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MergePilot.Tests
{
    public class InlineRepositoryEditorWorkflowServiceTests : IDisposable
    {
        private readonly string _testDirectory;

        public InlineRepositoryEditorWorkflowServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.InlineRepositoryWorkflow", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            AppSettings.SettingsPathOverride = Path.Combine(_testDirectory, "settings.json");
        }

        public void Dispose()
        {
            AppSettings.SettingsPathOverride = null;

            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }

        [Fact]
        public void BeginEdit_WithValidSelection_ReturnsRepository()
        {
            var settings = CreateSettings();
            var dialogs = new FakeUserDialogService();
            var service = CreateService(dialogs);

            var result = service.BeginEdit(settings, 0, settings.Repositories[0]);

            Assert.True(result.ShouldEdit);
            Assert.Equal("Repo", result.Repository?.Name);
            Assert.Equal(0, result.Index);
            Assert.Empty(dialogs.Messages);
        }

        [Fact]
        public void BeginEdit_WithNoSelection_ShowsInformation()
        {
            var settings = CreateSettings();
            var dialogs = new FakeUserDialogService();
            var service = CreateService(dialogs);

            var result = service.BeginEdit(settings, -1, null);

            Assert.False(result.ShouldEdit);
            Assert.Contains(dialogs.Messages, message => message.Contains("Select a repository to edit."));
        }

        [Fact]
        public void AddRepository_WithMissingGitAndConfirmation_AddsRepository()
        {
            var settings = new AppSettings();
            var dialogs = new FakeUserDialogService { ConfirmYesNoResult = true };
            var service = CreateService(dialogs);

            var result = service.AddRepository(settings, " Repo ", " C:\\missing ");

            Assert.True(result.ShouldRefresh);
            Assert.Single(settings.Repositories);
            Assert.Equal("Repo", settings.Repositories[0].Name);
            Assert.Equal("C:\\missing", settings.Repositories[0].Path);
            Assert.Contains(dialogs.Messages, message => message.Contains("Git folder not found"));
        }

        [Fact]
        public void AddRepository_WhenMissingGitConfirmationDeclined_DoesNotMutateSettings()
        {
            var settings = new AppSettings();
            var dialogs = new FakeUserDialogService { ConfirmYesNoResult = false };
            var service = CreateService(dialogs);

            var result = service.AddRepository(settings, "Repo", "C:\\missing");

            Assert.False(result.ShouldRefresh);
            Assert.Empty(settings.Repositories);
        }

        [Fact]
        public void AddRepository_WithBlankInput_ShowsWarning()
        {
            var settings = new AppSettings();
            var dialogs = new FakeUserDialogService();
            var service = CreateService(dialogs);

            var result = service.AddRepository(settings, "", "C:\\repo");

            Assert.False(result.ShouldRefresh);
            Assert.Contains(dialogs.Messages, message => message.Contains("Repo name and path are required."));
        }

        [Fact]
        public void UpdateRepository_WithValidSelection_UpdatesAndReturnsResolvedIndex()
        {
            var settings = CreateSettings();
            var dialogs = new FakeUserDialogService { ConfirmYesNoResult = true };
            var service = CreateService(dialogs);

            var result = service.UpdateRepository(
                settings,
                selectedIndex: 0,
                selectedRepository: settings.Repositories[0],
                name: "Renamed",
                path: "C:\\renamed");

            Assert.True(result.ShouldRefresh);
            Assert.Equal(0, result.Index);
            Assert.Equal("Renamed", settings.Repositories[0].Name);
            Assert.Equal("C:\\renamed", settings.Repositories[0].Path);
        }

        [Fact]
        public void UpdateRepository_WithNoSelection_ShowsInformation()
        {
            var settings = CreateSettings();
            var dialogs = new FakeUserDialogService();
            var service = CreateService(dialogs);

            var result = service.UpdateRepository(settings, -1, null, "Repo", "C:\\repo");

            Assert.False(result.ShouldRefresh);
            Assert.Contains(dialogs.Messages, message => message.Contains("Select a repository to update."));
        }

        [Fact]
        public void RemoveRepository_WithConfirmation_RemovesRepository()
        {
            var settings = CreateSettings();
            var dialogs = new FakeUserDialogService { ConfirmYesNoResult = true };
            var service = CreateService(dialogs);

            var result = service.RemoveRepository(settings, 0, settings.Repositories[0]);

            Assert.True(result.ShouldRefresh);
            Assert.Empty(settings.Repositories);
            Assert.Contains(dialogs.Messages, message => message.Contains("Confirm remove"));
        }

        [Fact]
        public void RemoveRepository_WhenConfirmationDeclined_DoesNotMutateSettings()
        {
            var settings = CreateSettings();
            var dialogs = new FakeUserDialogService { ConfirmYesNoResult = false };
            var service = CreateService(dialogs);

            var result = service.RemoveRepository(settings, 0, settings.Repositories[0]);

            Assert.False(result.ShouldRefresh);
            Assert.Single(settings.Repositories);
        }

        private InlineRepositoryEditorWorkflowService CreateService(FakeUserDialogService dialogs)
        {
            return new InlineRepositoryEditorWorkflowService(
                new InlineRepositoryEditorService(new RepositoryConfigurationService()),
                dialogs);
        }

        private static AppSettings CreateSettings()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "Repo",
                Path = "C:\\repo"
            });
            return settings;
        }

        private sealed class FakeUserDialogService : IUserDialogService
        {
            public List<string> Messages { get; } = new();
            public bool ConfirmYesNoResult { get; init; } = true;

            public void ShowInformation(string message, string title)
            {
                Messages.Add($"{title}: {message}");
            }

            public void ShowWarning(string message, string title)
            {
                Messages.Add($"{title}: {message}");
            }

            public void ShowError(string message, string title)
            {
                Messages.Add($"{title}: {message}");
            }

            public bool ConfirmYesNo(string message, string title, UserDialogIcon icon = UserDialogIcon.Question)
            {
                Messages.Add($"{title}: {message}");
                return ConfirmYesNoResult;
            }

            public bool ConfirmOkCancel(string message, string title, UserDialogIcon icon = UserDialogIcon.Question)
            {
                Messages.Add($"{title}: {message}");
                return true;
            }
        }
    }
}
