using System.Collections.Generic;
using Xunit;

namespace MergePilot.Tests
{
    public class BatchOperationCallbackFactoryTests
    {
        [Fact]
        public void CreateMergeCallbacks_DispatchesOutputErrorAndInteractions()
        {
            var invoked = 0;
            var output = new List<(string Message, bool Status)>();
            var errors = new List<string>();
            var interactions = new FakeMergeInteractionService
            {
                MissingTargetResult = true,
                ConflictResult = MergeConflictResolution.ContinueAfterManualResolution
            };
            var factory = new BatchOperationCallbackFactory(
                action =>
                {
                    invoked++;
                    action();
                },
                (message, status) => output.Add((message, status)),
                errors.Add,
                _ => { },
                interactions);

            var callbacks = factory.CreateMergeCallbacks();

            callbacks.WriteOutput("out", status: true);
            callbacks.WriteError("err");
            var missingResult = callbacks.ConfirmMissingTarget(new MissingTargetBranchContext("repo", "origin", "main"));
            var conflictResult = callbacks.ResolveConflict(new MergeConflictContext("repo", "source", "target", "message"));

            Assert.Equal(4, invoked);
            Assert.Equal(("out", true), output[0]);
            Assert.Equal("err", errors[0]);
            Assert.True(missingResult);
            Assert.Equal(MergeConflictResolution.ContinueAfterManualResolution, conflictResult);
            Assert.True(interactions.MissingTargetWasCalled);
            Assert.True(interactions.ConflictWasCalled);
        }

        [Fact]
        public void CreatePullCallbacks_DispatchesOutputErrorAndBranchUpdated()
        {
            var invoked = 0;
            var output = new List<(string Message, bool Status)>();
            var errors = new List<string>();
            var updatedBranches = new List<string>();
            var factory = new BatchOperationCallbackFactory(
                action =>
                {
                    invoked++;
                    action();
                },
                (message, status) => output.Add((message, status)),
                errors.Add,
                updatedBranches.Add,
                new FakeMergeInteractionService());

            var callbacks = factory.CreatePullCallbacks();

            callbacks.WriteOutput("out", status: true);
            callbacks.WriteError("err");
            callbacks.NotifyBranchUpdated("main");

            Assert.Equal(3, invoked);
            Assert.Equal(("out", true), output[0]);
            Assert.Equal("err", errors[0]);
            Assert.Equal("main", updatedBranches[0]);
        }

        private sealed class FakeMergeInteractionService : IMergeInteractionService
        {
            public bool MissingTargetResult { get; set; }
            public MergeConflictResolution ConflictResult { get; set; }
            public bool MissingTargetWasCalled { get; private set; }
            public bool ConflictWasCalled { get; private set; }

            public bool ConfirmMissingTargetBranch(MissingTargetBranchContext context)
            {
                MissingTargetWasCalled = true;
                return MissingTargetResult;
            }

            public MergeConflictResolution ResolveMergeConflict(MergeConflictContext context)
            {
                ConflictWasCalled = true;
                return ConflictResult;
            }
        }
    }
}
