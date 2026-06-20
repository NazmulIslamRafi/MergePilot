using System.Collections.Generic;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchSelectionStateTests
    {
        [Fact]
        public void SetBranchSelected_TracksSourceAndTargetIndependently()
        {
            var state = new BranchSelectionState();

            state.SetBranchSelected(BranchSelectionRole.Source, "main", true);
            state.SetBranchSelected(BranchSelectionRole.Target, "develop", true);

            Assert.Contains("main", state.SelectedSourceBranches);
            Assert.DoesNotContain("main", state.SelectedTargetBranches);
            Assert.Contains("develop", state.SelectedTargetBranches);
        }

        [Fact]
        public void SetBranchesSelected_RemovesBranchesWhenUnchecked()
        {
            var state = new BranchSelectionState();
            state.SetBranchesSelected(BranchSelectionRole.Source, new[] { "main", "develop" }, true);

            state.SetBranchesSelected(BranchSelectionRole.Source, new[] { "main" }, false);

            Assert.DoesNotContain("main", state.SelectedSourceBranches);
            Assert.Contains("develop", state.SelectedSourceBranches);
        }

        [Fact]
        public void GetGroupCheckedState_ReturnsTrueFalseOrNull()
        {
            var state = new BranchSelectionState();
            var leaves = new List<string> { "feature/a", "feature/b" };

            Assert.False(state.GetGroupCheckedState(BranchSelectionRole.Source, leaves));

            state.SetBranchSelected(BranchSelectionRole.Source, "feature/a", true);
            Assert.Null(state.GetGroupCheckedState(BranchSelectionRole.Source, leaves));

            state.SetBranchSelected(BranchSelectionRole.Source, "feature/b", true);
            Assert.True(state.GetGroupCheckedState(BranchSelectionRole.Source, leaves));
        }

        [Fact]
        public void AddBranchToCatalogs_DeduplicatesCaseInsensitively()
        {
            var state = new BranchSelectionState();

            state.AddBranchToCatalogs("Main");
            state.AddBranchToCatalogs("main");

            Assert.Single(state.SourceBranchCatalog);
            Assert.Single(state.TargetBranchCatalog);
        }

        [Fact]
        public void ClearSelectionsAndCatalogs_RemovesTrackedValues()
        {
            var state = new BranchSelectionState();
            state.SetBranchSelected(BranchSelectionRole.Source, "main", true);
            state.AddBranchToCatalogs("main");

            state.ClearSelections();
            state.ClearBranchCatalogs();

            Assert.Empty(state.SelectedSourceBranches);
            Assert.Empty(state.SourceBranchCatalog);
        }
    }
}
