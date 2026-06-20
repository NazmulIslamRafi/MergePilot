# MergePilot Current Project Status

**Date:** 2026-06-06  
**Purpose:** Current source-of-truth status after reviewing the older analysis and phase documents.

## Summary

The early analysis documents are useful historical planning material, but several items are now stale. The project has moved beyond the original baseline: Phase 1 is complete, the Phase 2 service/MVVM extraction roadmap is complete for the current requirements, and repeatable performance/log profiling now has automated and integrated smoke coverage. Remaining MainWindow code is treated as the WPF view boundary for event handlers and timer callbacks.

## Document Accuracy

| Area | Older Docs Say | Current Status |
|------|----------------|----------------|
| Input validation | Missing / create `InputValidator.cs` | Implemented. `InputValidator.cs` exists and dialogs/services use validation for repositories, branches, and merge operations. |
| Test project | Missing / 0% coverage | Test project exists with unit tests for validators, settings, services, and ViewModels. |
| Settings validation | Missing | Implemented: `Validate()`, `LoadWithFallback()`, `SaveAtomically()`, and validation result type. |
| MVVM | Future recommendation | Implemented for the current roadmap scope. ViewModels, workflow commands, and service boundaries own application behavior; remaining code-behind is view-specific WPF event/timer coordination. |
| Branch caching | Future recommendation | Implemented in `RepositoryService` with cache status and aggregate cache metrics. |
| Repository management service | Future recommendation | Implemented: `RepositoryConfigurationService` owns persisted repository/branch mutations. |
| Repository manager dialog MVVM | Dialog click handlers | Implemented: `RepositoryManagerViewModel` owns dialog collections, commands, repository-name projection, and refresh-button state. |
| Repository manager modal workflow | Code-behind only | Implemented: `RepositoryManagerWorkflowService` owns repository manager dialog launch and settings reload. |
| Inline repository editor | Code-behind only | Implemented: `InlineRepositoryEditorWorkflowService` owns prompt/mutation orchestration; `InlineRepositoryEditorService` owns selected-repository resolution, missing-git detection, and add/update/remove delegation; `MainWindowViewModel` owns bound editor fields and add/update mode. |
| Selection state | Code-behind only | Implemented: `BranchSelectionState`, `RepositorySelectionState`, and `RepositorySelectionItem` own selected branch/repository state outside visual-tree scans. |
| Branch catalog assembly | Code-behind only | Implemented: `BranchCatalogService` owns initial branch catalog, repository-specific custom branches, and recent branch persistence; `BranchCatalogCoordinator` applies catalogs to selection state. |
| Branch dropdown projection | Code-behind only | Implemented: `BranchDropdownProjectionService` owns source/target hierarchical flattening, and `MainWindowViewModel` owns the bound dropdown collections. |
| Branch tree tri-state logic | Code-behind only | Implemented: `BranchTreeSelectionService` owns BranchItem checked-state and group tri-state calculations. |
| Batch request preparation | Code-behind only | Implemented: `BatchOperationRequestFactory` owns merge/pull request preparation and last branch persistence. |
| Batch workflow orchestration | Code-behind only | Implemented: `BatchOperationWorkflowRunner` owns merge/pull request creation plus batch service invocation. |
| Logging service | Future recommendation | Implemented: `LogBufferService` owns log queueing, retained buffers, export text, trimming, and file streaming. |
| Log file export/copy | Code-behind only | Implemented: `LogExportService` owns save-file picker coordination and export file writing; `LogClipboardService` owns copy-to-clipboard export text. |
| Log rendering | Code-behind only | Implemented adapter: `RichTextBoxLogRenderer` owns RichTextBox coloring, line culling, and smooth scrolling; `LogLineClassifier` owns classification rules; `LogDisplayCullPolicy` owns tested culling thresholds. |
| MainWindow shortcuts | Code-behind only | Implemented: `MainWindowShortcutService` owns action/log shortcut registration and Shift+E preview-key handling. |
| MainWindow closing state | Code-behind only | Implemented: `MainWindowClosingService` owns close-time settings persistence and log buffer disposal. |
| MainWindow event hooks | Code-behind only | Implemented: `MainWindowEventHookService` owns branch dropdown and preview-key hook/unhook coordination. |
| MainWindow timers | Code-behind only | Implemented: `MainWindowTimerService` owns DispatcherTimer creation and shutdown. |
| Git operation safety | Future recommendation | Improved: `GitCommandSafety` validates branch/remote names before guarded Git helper command construction, and `CommandResult` carries structured error kinds. |
| Custom exceptions | Future recommendation | Addressed with structured `GitCommandErrorKind` classification while preserving existing result-record workflows. |
| Build warnings | Should be zero | Complete. Build passes with 0 warnings and 0 errors. |
| Documentation | Needs XML docs | Current roadmap/status/performance docs are updated. Additional XML-doc polish is optional maintenance. |

## Completed

- `InputValidator.cs` exists with branch, repository, repository-name, and merge-operation validation.
- `AddBranchDialog` and `RepositoryConfigurationService` validate custom branch names before saving them to settings.
- `AddRepositoryDialog.xaml.cs` uses input validation.
- Repository path validation now matches the active configuration service requirement: an existing local folder with a `.git` marker, without rejecting valid workspaces outside the user profile.
- `MERGEPILOT_SETTINGS_PATH` can point the app at a generated profiling settings file without overwriting the active user settings file.
- `MergePilot.Tests` exists and runs under xUnit.
- `RepositoryService` exists with async operations, progress events, and branch cache support.
- `RepositoryService` now supports injectable branch providers for tests and reports branch cache hits, misses, hit rate, cached repository count, and cached branch count.
- `PerformanceScaleSmokeTests` cover 10,000-branch dropdown projection, 5,000-line log buffering/trimming, large log stream/export separation, aggregate cache metrics, local-Git cache metrics, and repeated local-Git branch refresh deduplication.
- Branch dropdowns and the repository list now enable WPF recycling virtualization hints for large bound collections.
- `XamlRegressionTests` keep the WPF virtualization hints and command-bound button entry points covered against regressions.
- `tools/Prepare-ManualProfilingFixture.ps1` creates repeatable local Git profiling fixtures with generated settings, prints a non-destructive `MERGEPILOT_SETTINGS_PATH` launch path, and has been smoke-tested with a small fixture without applying settings.
- `tools/Run-WpfDropdownSmoke.ps1` automates non-destructive WPF dropdown smoke profiling and has been smoke-tested with a small fixture.
- `tools/Run-WpfPullLogSmoke.ps1` automates a non-destructive WPF pull/log smoke run against generated settings, selected repository/branch UI Automation, streamed log verification, and optional repeated Pull runs with minimum stream-line thresholds.
- Integrated WPF dropdown smoke testing with a generated 1,000-branch fixture passed through UI Automation: Source dropdown expanded/collapsed in 665ms, Target dropdown expanded/collapsed in 631ms, and the app exited cleanly.
- Integrated WPF pull/log smoke testing with a generated 3-branch fixture passed through UI Automation: Pull ran for `feature/team-00/area-00/branch-0000`, streamed 26 log lines, and the app exited cleanly.
- Integrated long-log WPF pull/log smoke testing with a generated 3-branch fixture passed through UI Automation: Pull ran 80 times for `feature/team-00/area-00/branch-0000`, streamed 2,080 log lines across 80 summaries, and the app exited cleanly.
- `BatchPullService` owns the multi-repository and multi-branch pull/fetch workflow.
- `BatchMergeService` owns the multi-repository and multi-branch merge workflow, using callbacks for missing-target confirmation and manual conflict resolution.
- `RepositoryConfigurationService` owns add/update/remove operations for saved repositories and custom branches, plus branch refresh persistence.
- `InlineRepositoryEditorService` owns inline repository editor selection resolution, missing-git confirmation detection, and add/update/remove delegation through `RepositoryConfigurationService`.
- `InlineRepositoryEditorWorkflowService` owns inline repository editor prompts, missing-git confirmation flow, and mutation result handling.
- `MainWindowViewModel` owns inline repository editor name/path state and add/update mode, with MainWindow inline editor text boxes and buttons bound to those properties.
- `RepositoryManagerViewModel` owns RepositoryManager dialog repository/branch collections, commands, repository-name projection, and refresh-branch UI state.
- `RepositoryManagerWorkflowService` owns launching the repository manager dialog and reloading settings after it closes.
- `BranchSelectionState` owns selected source/target branches and branch catalogs outside WPF controls.
- Merge/pull workflows now read selected branches directly from `BranchSelectionState` instead of a ComboBox-facing helper.
- `RepositorySelectionState` owns selected repository paths outside WPF visual-tree scans.
- `RepositorySelectionCoordinator` owns repository checkbox/list selection mutation and returns branch-load/clear intent to MainWindow.
- `RepositoryListVisualState` owns WPF repository-list selection reads, reselects, scrolling, and single-selection event hookup.
- `BranchCatalogService` owns startup branch catalog assembly, selected-repository custom branch lookup, and max-100 recent branch persistence.
- `BranchCatalogCoordinator` owns applying initial, repository-specific, recent, and cleared branch catalogs to `BranchSelectionState`.
- Repository uncheck now clears stale branch selections along with branch catalogs, and workflow command availability is recalculated after repository selection changes.
- `BranchDropdownProjectionService` owns flattened source/target dropdown projection from branch catalogs, including large-catalog coverage.
- `MainWindowViewModel.ReplaceBranchDropdownItems()` updates the bound branch dropdown collections; `MainWindow.xaml.cs` no longer mutates `SourceBranchBox.Items` or `TargetBranchBox.Items` directly.
- `BranchTreeSelectionService` owns pure branch tree checked-state application and group tri-state calculations.
- Branch dropdown checkbox state now updates through bound `BranchItem.IsChecked` models instead of branch dropdown visual-tree checkbox synchronization.
- `BranchDropdownSelectionCoordinator` owns branch checkbox toggle coordination, selected branch tracking updates, group child propagation, ancestor tri-state updates, and dropdown checked-state restore.
- `BranchDropdownVisualState` owns WPF branch dropdown role resolution and item-reference lookup for branch checkbox/dropdown events.
- `BatchOperationRequestFactory` owns merge/pull request creation and last source/target branch persistence.
- `BatchOperationWorkflowRunner` owns production merge/pull request creation and batch service invocation, with tests proving selected repository/branch propagation and cancellation token pass-through.
- `RefreshWorkflowService` owns MainWindow refresh workflow state reset for branch selections, branch catalogs, selected repositories, empty branch-list output, and branch text preservation decisions.
- `RefreshWorkflowVisualState` owns the WPF list/ComboBox reset and branch text restore effects used by the refresh workflow.
- `LogBufferService` owns log queueing, master buffers, trimming, export text, and stream-to-file lifecycle.
- `LogBufferService` normalizes queued entries with trailing newlines so batched output, streaming, export, and rendering keep log entries separated.
- `LogExportService` owns save-file picker coordination and export file writing, with tests for selected-file output, default picker options, and cancellation.
- `LogClipboardService` owns copying exported log text to clipboard through an adapter, with tests for output/error section content.
- `RichTextBoxLogRenderer` owns WPF log rendering, visible-line culling, and smooth scrolling.
- `RichTextBoxLogRenderer` caches the RichTextBox `ScrollViewer` after first lookup to avoid repeated visual-tree traversal on smooth-scroll ticks.
- `RichTextBoxLogRendererTests` exercise WPF RichTextBox append, classifier brush selection, culling, large-log visible paragraph bounding, and clear behavior on an STA thread.
- `LogLineClassifier` owns test-covered log severity/category rules.
- `LogDisplayCullPolicy` owns test-covered visible log line culling thresholds, preserving the default 2,000-line behavior while avoiding over-culling for smaller configured limits.
- `GitCommandSafety` centralizes branch/remote argument validation for guarded Git helper workflows.
- `GitCommandErrorClassifier` classifies Git command failures into validation, repository, authentication, remote, timeout, conflict, network, and unknown categories on `CommandResult`.
- `ViewModels/ViewModelBase.cs`, `ViewModels/RelayCommand.cs`, and `ViewModels/MainWindowViewModel.cs` exist.
- Main action buttons, inline repository editor buttons, and action shortcuts now route through `MainWindowViewModel` workflow commands backed by `MainWindowWorkflowActions`.
- Log save/copy/clear shortcuts now route through `MainWindowViewModel` workflow commands backed by `MainWindowWorkflowActions`.
- `MainWindowShortcutService` owns action/log shortcut registration plus Shift+E preview-key handling, with tests for ViewModel and fallback command paths.
- `MainWindowClosingService` owns close-time settings persistence and log buffer disposal.
- `MainWindowEventHookService` owns branch dropdown and preview-key hook/unhook coordination, with tests for duplicate-safe dropdown hooking and null-safe operations.
- `MainWindowTimerService` owns log flush and smooth-scroll DispatcherTimer creation and shutdown.
- Synchronous RelayCommand implementations now guard `Execute` with `CanExecute`, preventing disabled workflows from running through direct command invocation.
- Merge/pull workflow button availability now flows through tested `WorkflowAvailabilityService` rules, `MainWindowViewModel.UpdateWorkflowAvailability()`, and command `CanExecute` instead of direct MainWindow button `IsEnabled` mutation; merge availability requires a selected repository, exactly one source branch, target branches, and no source-target self-merge.
- MainWindow XAML now binds its button entry points, repository list, and branch collections to the ViewModel.
- Dynamic repository checkboxes are now generated by a bound `ItemsControl` using `RepositorySelectionItem` instead of code-behind-created `CheckBox` instances.
- MainWindow, RepositoryManager, AddRepositoryDialog, and AddBranchDialog XAML button entry points are command-bound; no XAML `Click=` handlers remain.
- Message boxes, folder picker, and save-file picker calls are centralized behind `IUserDialogService` and `IFilePickerService` adapters.
- MainWindow and RepositoryManager UI-thread dispatch calls are centralized through local `InvokeOnUiThread` helpers instead of scattered direct `Dispatcher.Invoke` calls.
- App startup is explicit and non-blocking: `StartupUri` is removed, settings are primed asynchronously, exactly one MainWindow is shown, and `Thread.Sleep` is gone.
- MainWindow now disposes merge/pull operation cancellation token sources and stops/unhooks its timers and dropdown/key events during window close.
- One-way WPF display converters now return `Binding.DoNothing` on `ConvertBack` instead of throwing `NotImplementedException`.
- Obsolete MainWindow log-search/filter helpers, old client/environment stubs, unused visual-tree helpers, stale target-validation helper, and unreferenced legacy log toggle/checkbox handlers have been removed.
- `ButtonProgressScope` centralizes temporary button disabling and indeterminate progress restoration for MainWindow operations, restoring the button's original enabled state by default so command-driven availability is preserved.
- `BatchOperationCallbackFactory` centralizes merge/pull callback construction and UI dispatch, while `MergeInteractionService` owns missing-target and merge-conflict prompts/folder opening through `RepositoryFolderOpener`.
- App settings now include validation, fallback loading, and atomic saving.

## Final Boundaries And Optional Follow-Up

1. **MVVM migration boundary**
   - `MainWindow.xaml.cs` still owns view-specific WPF event receipt and timer callback methods; this is the current view boundary, not unfinished business.
   - Pull/fetch is now extracted to `BatchPullService`, and selection state is now backed by testable state classes.
   - Merge is now extracted to `BatchMergeService`, and selected repository/branch state is no longer raw code-behind collections.
   - Merge/pull request preparation and batch service invocation are extracted, and the main buttons/shortcuts now enter through ViewModel workflow commands; callback wiring is centralized in `BatchOperationCallbackFactory` and `MergeInteractionService`, and button progress lifecycle is centralized in `ButtonProgressScope`.
   - Branch catalog/recent-branch behavior, catalog state application, dropdown projection, bound branch collection replacement, branch checkbox model synchronization, branch dropdown selection coordination, and branch dropdown visual role/item lookup are extracted; MainWindow still receives the WPF checkbox/dropdown events.
   - Branch tree checked-state calculations are extracted and branch/repository checkbox visual-tree synchronization has been removed.
   - The MainWindow button entry points and merge/pull workflow availability route through ViewModel workflow commands that delegate to existing production workflows to preserve multi-repository and multi-branch behavior; availability now requires both selected repositories and valid branch selections.
   - MainWindow repository list display, dynamic repository checkbox generation, and repository checkbox selection state are ViewModel-bound; checked/unchecked and list-selection events route through `RepositorySelectionCoordinator`, with WPF repository-list visual reads/effects isolated in `RepositoryListVisualState`.
   - `ManageRepositoriesCommand` and `ManageRepositoriesWorkflowCommand` now both invoke the configured production repository-management workflow.
   - Inline repository editor prompt/mutation orchestration is extracted to `InlineRepositoryEditorWorkflowService`; lower-level decision logic is extracted to `InlineRepositoryEditorService`; inline editor fields and add/update button mode are ViewModel-bound.
   - Refresh workflow state reset and branch text preservation decisions are extracted to `RefreshWorkflowService`; WPF list/ComboBox reselect and text effects are extracted to `RefreshWorkflowVisualState`.
   - `RepositoryManager` and the add/edit dialogs now bind buttons to commands; repository manager dialog launch/reload is extracted to `RepositoryManagerWorkflowService`; message-box and file-picker calls are centralized behind adapters.

2. **Log rendering cleanup**
   - Log buffering/export text/streaming is extracted to `LogBufferService`.
   - Save-file export coordination and file writing are extracted to `LogExportService`.
   - Log buffer entries are normalized with trailing line separators to avoid merged batched output.
   - WPF-specific RichTextBox coloring, visible-line trimming, and smooth-scroll rendering are now in `RichTextBoxLogRenderer`; smooth-scroll lookup now caches the `ScrollViewer`, and culling thresholds are extracted to `LogDisplayCullPolicy`.
   - Longer-running integrated app log profiling passed against the generated local fixture; real organization repository/remote smoke is optional release validation.

3. **Warning hygiene**
   - Build is currently warning-free.
   - Keep it that way during future maintenance.

4. **Git operation safety**
   - New branch/remote command construction should continue using guarded helper methods during future maintenance.
   - Structured Git command error classification is implemented on `CommandResult` to preserve existing non-throwing workflows.

5. **Performance phase**
   - Log rendering and line trimming are tuned in the extracted renderer.
   - Branch cache status and aggregate cache metrics are implemented in `RepositoryService`.
   - Branch dropdowns and the repository list have WPF recycling virtualization hints enabled.
   - Automated scale smoke tests cover large branch catalogs, large log buffers, large log stream/export separation, multi-repository cache metrics, local-Git cache metrics, and repeated local-Git refresh deduplication without timing-sensitive assertions.
   - A repeatable local Git profiling fixture script exists for manual app profiling.
   - Integrated large-branch dropdown smoke profiling, pull/log streaming smoke profiling, and long-log repeated pull smoke profiling have been run; generated-fixture performance coverage is complete for the current phase.

## Optional Next Order

1. Treat remaining MainWindow WPF event handlers and timer callbacks as the current view boundary unless a future change can extract them without obscuring behavior.
2. Run optional release smoke against real organization repositories/remotes and the OS save dialog if the release process requires it.
3. Retire or archive historical phase documents if the team wants a smaller documentation set.

## Latest Compatibility Note

MainWindow buttons and keyboard shortcuts now enter through `MainWindowViewModel` workflow commands, which delegate to the existing production workflows for now. This is intentional: the current single-repository `MainWindowViewModel` operations do not yet replace the multi-repository and multi-branch checkbox selections tracked by `MainWindow.xaml.cs`. `RepositoryManager` now has its own ViewModel-backed collections and command entry points; repository manager dialog launch/reload is centralized in `RepositoryManagerWorkflowService`; and the add/edit dialogs expose command-bound save/cancel/browse actions. Message-box, folder-picker, and save-file picker calls are centralized behind adapters.

The pull/fetch workflow has been moved into `BatchPullService`, with callback hooks for output, error reporting, and recent-branch updates. The merge workflow has been moved into `BatchMergeService`, with callback hooks for output, error reporting, target-branch confirmation, and manual conflict resolution. Merge/pull callback construction and UI dispatch are centralized in `BatchOperationCallbackFactory`, with prompt and folder-opening behavior owned by `MergeInteractionService` and `RepositoryFolderOpener`. Repository/branch selection state is now backed by `RepositorySelectionState`, `RepositorySelectionItem`, and `BranchSelectionState`; merge/pull workflows read selected branches directly from `BranchSelectionState`; repository selection mutation now routes through `RepositorySelectionCoordinator`; repository-list visual reads/effects now route through `RepositoryListVisualState`; inline repository editor decisions now route through `InlineRepositoryEditorService`; inline repository editor prompt/mutation orchestration now routes through `InlineRepositoryEditorWorkflowService`; inline repository editor fields and mode now flow through `MainWindowViewModel`; merge/pull workflow availability now flows through `WorkflowAvailabilityService` and `MainWindowViewModel` command availability; refresh workflow state reset now routes through `RefreshWorkflowService`; refresh WPF visual reset effects now route through `RefreshWorkflowVisualState`; repository list and selectable repository replacement now flow through `MainWindowViewModel`; branch catalog behavior is now backed by `BranchCatalogService` and `BranchCatalogCoordinator`; branch dropdown projection is now backed by `BranchDropdownProjectionService`; branch dropdown collection replacement now flows through `MainWindowViewModel`; branch tree tri-state logic is now backed by `BranchTreeSelectionService`; branch dropdown selection coordination is now backed by `BranchDropdownSelectionCoordinator`; branch dropdown visual role/item lookup is now backed by `BranchDropdownVisualState`; branch dropdown checkbox state now updates through bound `BranchItem` models; operation request preparation and batch service invocation are now backed by `BatchOperationWorkflowRunner`.

The log pipeline is now split into buffering (`LogBufferService`), file export (`LogExportService`), clipboard copy (`LogClipboardService`), classification (`LogLineClassifier`), culling policy (`LogDisplayCullPolicy`), and WPF rendering (`RichTextBoxLogRenderer`). `MainWindow.xaml.cs` now coordinates these components instead of directly owning queue storage, export text, stream file handles, export file writing, clipboard writes, color classification, line culling, and smooth-scroll internals.

## Latest Verification

- `dotnet build MergePilot.sln --no-restore` passes with 0 warnings and 0 errors.
- `dotnet test MergePilot.sln --no-restore` passes.

## Notes On Older Docs

The following documents should be treated as historical planning docs, not current instructions:

- `DO_THIS_NOW.md`
- `PHASE_1_START.md`
- `START_HERE.md`
- `README_ANALYSIS.md`
- `ANALYSIS_SUMMARY.md`
- `ANALYSIS_COMPLETE.md`
- `DETAILED_ANALYSIS.md`
- `IMPLEMENTATION_GUIDE.md`
- `PHASE_1_DETAILED_TASKS.md`
- `DOCUMENTATION_INDEX_ANALYSIS.md`

They still contain useful ideas, but they describe the project before the current Phase 1 and Phase 2 implementation work.
