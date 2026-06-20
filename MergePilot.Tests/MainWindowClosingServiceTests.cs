using System;
using System.IO;
using Xunit;

namespace MergePilot.Tests
{
    public class MainWindowClosingServiceTests : IDisposable
    {
        private readonly string _settingsDirectory;

        public MainWindowClosingServiceTests()
        {
            _settingsDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.MainWindowClosing", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_settingsDirectory);
            AppSettings.SettingsPathOverride = Path.Combine(_settingsDirectory, "settings.json");
        }

        public void Dispose()
        {
            AppSettings.SettingsPathOverride = null;

            if (Directory.Exists(_settingsDirectory))
                Directory.Delete(_settingsDirectory, recursive: true);
        }

        [Fact]
        public void SaveAndDispose_PersistsLogFontSizeAndDisposesLogBuffer()
        {
            var settings = new AppSettings();
            var disposable = new FakeDisposable();
            var service = new MainWindowClosingService();

            service.SaveAndDispose(settings, 17.5, disposable);

            Assert.True(disposable.IsDisposed);
            var saved = AppSettings.Load();
            Assert.Equal(17.5, saved.LogFontSize);
        }

        [Fact]
        public void SaveAndDispose_RequiresDependencies()
        {
            var service = new MainWindowClosingService();
            var settings = new AppSettings();
            var disposable = new FakeDisposable();

            Assert.Throws<ArgumentNullException>(() => service.SaveAndDispose(null!, 12, disposable));
            Assert.Throws<ArgumentNullException>(() => service.SaveAndDispose(settings, 12, null!));
        }

        private sealed class FakeDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
