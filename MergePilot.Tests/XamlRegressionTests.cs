using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MergePilot.Tests
{
    public class XamlRegressionTests
    {
        [Fact]
        public void MainWindowXaml_KeepsVirtualizationHintsForLargeBoundLists()
        {
            var xaml = ReadProjectFile("MergePilot", "MainWindow.xaml");

            Assert.Contains("VirtualizingPanel.IsVirtualizing=\"True\"", xaml);
            Assert.Contains("VirtualizingPanel.VirtualizationMode=\"Recycling\"", xaml);
            Assert.Contains("<VirtualizingStackPanel", xaml);
            Assert.True(
                CountOccurrences(xaml, "ScrollViewer.CanContentScroll=\"True\"") >= 2,
                "Expected virtualization-friendly scrolling on branch dropdowns and repository list.");
        }

        [Fact]
        public void MainWindowXaml_ExposesBranchDropdownAutomationIdsForProfiling()
        {
            var xaml = ReadProjectFile("MergePilot", "MainWindow.xaml");

            Assert.Contains("AutomationProperties.AutomationId=\"SourceBranchBox\"", xaml);
            Assert.Contains("AutomationProperties.AutomationId=\"TargetBranchBox\"", xaml);
            Assert.Contains("AutomationProperties.Name=\"Source Branches\"", xaml);
            Assert.Contains("AutomationProperties.Name=\"Target Branches\"", xaml);
        }

        [Fact]
        public void MainWindowXaml_ExposesActionAndLogAutomationIdsForSmokeProfiling()
        {
            var xaml = ReadProjectFile("MergePilot", "MainWindow.xaml");

            Assert.Contains("AutomationProperties.AutomationId=\"MergeButton\"", xaml);
            Assert.Contains("AutomationProperties.AutomationId=\"PullButton\"", xaml);
            Assert.Contains("AutomationProperties.AutomationId=\"RefreshButton\"", xaml);
            Assert.Contains("AutomationProperties.AutomationId=\"OutputLogBox\"", xaml);
            Assert.Contains("AutomationProperties.Name=\"{Binding FullName}\"", xaml);
            Assert.Contains("AutomationProperties.Name=\"{Binding DisplayName}\"", xaml);
        }

        [Fact]
        public void WindowXaml_UsesCommandBindingsInsteadOfButtonClickHandlers()
        {
            var files = new[]
            {
                ReadProjectFile("MergePilot", "MainWindow.xaml"),
                ReadProjectFile("MergePilot", "RepositoryManager.xaml"),
                ReadProjectFile("MergePilot", "AddRepositoryDialog.xaml"),
                ReadProjectFile("MergePilot", "AddBranchDialog.xaml")
            };

            foreach (var xaml in files)
            {
                Assert.DoesNotContain("Click=", xaml);
            }

            Assert.All(files, xaml => Assert.Contains("Command=", xaml));
        }

        [Fact]
        public void AppStartup_IsExplicitAndDoesNotBlockUiThreadWithSleep()
        {
            var appXaml = ReadProjectFile("MergePilot", "App.xaml");
            var appCode = ReadProjectFile("MergePilot", "App.xaml.cs");

            Assert.DoesNotContain("StartupUri=", appXaml);
            Assert.DoesNotContain("Thread.Sleep", appCode);
            Assert.Contains("mainWindow.Show();", appCode);
        }

        private static string ReadProjectFile(params string[] pathParts)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(new[] { directory.FullName }.Concat(pathParts).ToArray());
                if (File.Exists(candidate))
                    return File.ReadAllText(candidate);

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not locate project file: {Path.Combine(pathParts)}");
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var startIndex = 0;
            while (true)
            {
                var index = text.IndexOf(value, startIndex, StringComparison.Ordinal);
                if (index < 0)
                    return count;

                count++;
                startIndex = index + value.Length;
            }
        }
    }
}
