# MergePilot Performance Notes

**Date:** 2026-06-06

## Implemented Improvements

- Branch dropdown projection is isolated in `BranchDropdownProjectionService` and covered with a 10,000-branch catalog test.
- `PerformanceScaleSmokeTests` cover 10,000-branch dropdown projection, 5,000-line log buffering/trimming, large log stream/export separation, aggregate cache metrics across multiple cached repositories, local-Git cache metrics, and repeated local-Git branch refresh deduplication.
- `BranchOrganizer.FlattenBranches()` now uses a shared accumulator instead of allocating nested intermediate lists.
- Branch dropdown and repository list UI data now flow through ViewModel-bound collections instead of direct `Items` mutation.
- Branch dropdowns and the repository list now enable WPF recycling virtualization hints for large bound collections.
- `XamlRegressionTests` assert the virtualization hints remain present in `MainWindow.xaml`.
- Inline repository editor name/path fields and add/update button mode are ViewModel-bound instead of code-behind-mutated controls.
- Merge/pull workflow button availability now uses tested `WorkflowAvailabilityService` rules and ViewModel command `CanExecute` state instead of direct button mutation.
- Refresh workflow state reset and branch text preservation decisions are isolated in `RefreshWorkflowService`; WPF list/ComboBox reset and branch text restore effects are isolated in `RefreshWorkflowVisualState`.
- Repository checkbox/list selection mutation is isolated in `RepositorySelectionCoordinator`.
- Repository-list WPF selection reads, scrolling, reselects, and single-selection event hookup are isolated in `RepositoryListVisualState`.
- Branch catalog mutation is isolated in `BranchCatalogCoordinator`, keeping startup, selected-repository, and recent-branch catalog updates out of MainWindow.
- Branch dropdown checkbox state now updates through bound `BranchItem.IsChecked` models instead of branch dropdown visual-tree checkbox synchronization.
- Branch checkbox toggle coordination and dropdown checked-state restore are isolated in `BranchDropdownSelectionCoordinator`.
- Branch dropdown WPF role resolution and item-reference lookup are isolated in `BranchDropdownVisualState`.
- `LogBufferService` normalizes appended entries with trailing line separators so batched logs render, stream, and export as distinct lines.
- `LogExportService` owns save-file picker coordination and export file writing; tests cover selected-file output, default picker options, and cancellation.
- `LogClipboardService` owns copying exported log text to clipboard through an adapter; tests cover copied output/error sections.
- `LogDisplayCullPolicy` isolates and tests visible log line culling thresholds used by `RichTextBoxLogRenderer`.
- `RichTextBoxLogRendererTests` cover actual WPF RichTextBox append, cull, large-log visible paragraph bounding, classifier brush, and clear behavior on an STA thread.
- `RichTextBoxLogRenderer` caches the RichTextBox `ScrollViewer` after first lookup instead of walking the visual tree on every smooth-scroll tick.
- `RepositoryService` exposes branch cache status plus aggregate cache metrics: hits, misses, hit rate, cached repository count, and cached branch count.
- `tools/Prepare-ManualProfilingFixture.ps1` creates local Git repositories, local bare remotes, many remote branch refs, and a generated profile settings file for repeatable manual profiling; `MERGEPILOT_SETTINGS_PATH` allows non-destructive app runs against that generated file.
- `tools/Run-WpfDropdownSmoke.ps1` automates a non-destructive WPF dropdown smoke run against generated settings by launching the app, expanding/collapsing both branch dropdowns through UI Automation, and closing the app.
- `tools/Run-WpfPullLogSmoke.ps1` automates a non-destructive WPF pull/log smoke run against generated settings by launching the app, selecting a generated repository and branch through UI Automation, running Pull, verifying streamed log content, and closing the app. It also supports repeated Pull runs and minimum stream-line thresholds for longer log profiling.
- App startup no longer blocks the UI thread with a fixed sleep, and `StartupUri` is removed so startup creates one explicit MainWindow.

## Profiling Checklist

- Automated scale smoke tests, STA renderer tests, and WPF smoke scripts now cover the non-app data/rendering paths plus selected integrated app paths, including local-Git refresh/cache behavior and streamed logs after a Pull operation.
- Prepare repeatable local profiling data with `tools/Prepare-ManualProfilingFixture.ps1`; prefer `MERGEPILOT_SETTINGS_PATH` for non-destructive runs, and use `-ApplySettings` only when ready to back up and replace the active `%LOCALAPPDATA%\MergePilot\settings.json`.
- Completed: loaded a generated repository set with 1,000 branches and opened both branch dropdowns through UI Automation.
- Completed: let logs grow beyond `RichTextBoxLogRenderer.DefaultMaxDisplayedLines` through the WPF pull/log smoke; the app closed cleanly after 80 Pull runs and 2,080 streamed log lines.
- Confirm custom `RichTextBoxLogRenderer.MaxDisplayedLines` values retain a small hysteresis window instead of over-culling.
- Optional: run repeated branch refreshes against real remote repositories and inspect `RepositoryService.GetCacheMetrics()` during a debug session.
- Export content and file writing are service-tested through `LogExportService`; run the OS save dialog manually only if the platform dialog itself needs release smoke coverage.

## Fixture Script Smoke Test

- `tools/Prepare-ManualProfilingFixture.ps1 -RepositoryCount 1 -BranchCount 3` was smoke-tested successfully without `-ApplySettings`; it created a temporary local Git repository, local bare remote, and `settings.profile.json`.
- `tools/Run-WpfDropdownSmoke.ps1 -RepositoryCount 1 -BranchCount 3` was smoke-tested successfully; Source dropdown expand/collapse took 612ms, Target dropdown expand/collapse took 550ms, and the app exited with code 0.
- Integrated WPF dropdown smoke test was run with a generated `RepositoryCount=1`, `BranchCount=1000` fixture through `MERGEPILOT_SETTINGS_PATH`; MergePilot opened, the Source dropdown expanded/collapsed in 665ms, the Target dropdown expanded/collapsed in 631ms, and the app closed cleanly with exit code 0.
- `tools/Run-WpfPullLogSmoke.ps1 -BranchCount 3` was smoke-tested successfully on 2026-06-06; it selected `ProfileRepo01` and `feature/team-00/area-00/branch-0000`, ran Pull, verified one summary with 26 non-empty streamed log lines, and exited with code 0.
- `tools/Run-WpfPullLogSmoke.ps1 -BranchCount 3 -RepeatCount 80 -MinimumStreamLines 2000 -OperationTimeoutSeconds 180` passed on 2026-06-06; it selected the generated repository and branch, ran Pull 80 times, verified 80 summaries with 2,080 streamed log lines, and exited with code 0.
- Automated local-Git smoke tests now cover repeated `RepositoryService` cache use and repeated `RepositoryConfigurationService.RefreshBranchesFromRepositoriesAsync()` deduplication against generated bare remotes.

## Remaining Watch Points

- Repository checkbox selection now uses bindable `RepositorySelectionItem` models and coordinator-owned state mutation; repository-list visual state is isolated in `RepositoryListVisualState`.
- MainWindow still owns view-specific WPF event receipt and timer callback methods; callback wiring is centralized in `BatchOperationCallbackFactory`/`MergeInteractionService`, branch dropdown event context is centralized in `BranchDropdownVisualState`, keyboard shortcut handling is centralized in `MainWindowShortcutService`, event hook/unhook wiring is centralized in `MainWindowEventHookService`, timer creation/shutdown is centralized in `MainWindowTimerService`, close-time state persistence is centralized in `MainWindowClosingService`, and button progress lifecycle is centralized in `ButtonProgressScope`, which now preserves the original command-driven enabled state by default.
- Remaining release smoke, if desired, should use real organization repositories/remotes and the OS save dialog; the repeatable generated-fixture coverage is complete for the current performance work.
