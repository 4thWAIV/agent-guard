// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using AgentGuard.CrossPlatform.MacOS;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The macOS native-ops pointed-integration test (contract-coverage-refactor acceptance #6: "The pointed-integration test
/// drives the real non-interactive native ops and is green locally and on the macOS CI leg (no prompt shown)"). It drives
/// the REAL <see cref="ObjCRuntime"/> — binding the live Objective-C runtime — through only its NON-interactive
/// operations: allocate a fresh <c>LAContext</c> and a reason <c>NSString</c>, run the <c>canEvaluatePolicy:error:</c>
/// capability probe, read an error code, build and free a reply block, and release the objects. It never calls
/// <c>Evaluate</c> (the one operation that shows the out-of-band dialog), so no prompt is shown and it is safe on a
/// headless CI runner. This is the real-boundary coverage a unit test over the fake cannot give.
///
/// Compiled only on the macOS CI leg (it constructs the internal macOS <see cref="ObjCRuntime"/>). Unlike the interactive
/// <c>PresenceNativeSmokeTests</c>, it is NOT behind the native-smoke opt-in, because it shows no prompt and must run on
/// every macOS build.
///
/// It is tagged <see cref="NativeSpecOwnerAttribute"/> for <see cref="IObjCRuntime"/> so the guardrail-1 convention test
/// (<see cref="NativeSpecCoverageConventionTests"/>) recognizes this as the macOS native-ops owner's spec test.
/// </summary>
[NativeSpecOwner(typeof(IObjCRuntime))]
public sealed class MacOsObjCRuntimeSpecTests
{
    [Fact]
    [Trait(NativeSpecOwnerAttribute.CategoryTraitName, NativeSpecOwnerAttribute.NativeSpecTraitValue)]
    public void RealObjCRuntime_RunsTheNonInteractiveOps_WithoutShowingAPrompt()
    {
        var objc = new ObjCRuntime();

        IntPtr context = objc.CreateContext();
        context.Should().NotBe(IntPtr.Zero, "a real LAContext is allocated");

        IntPtr reason = objc.CreateReasonString("AgentGuard native-ops spec");
        reason.Should().NotBe(IntPtr.Zero, "a real localizedReason NSString is allocated");

        // The capability probe is non-interactive. Two sides: either the device can authenticate its owner (no error), or
        // it cannot and the runtime writes a readable NSError whose -code the ops surface. Both are machine-independent
        // and neither shows a dialog.
        bool canEvaluate = objc.CanEvaluate(context, MacOsErrorCodeOutcomes.DeviceOwnerAuthentication, out IntPtr error);
        if (canEvaluate)
        {
            error.Should().Be(IntPtr.Zero, "a passing probe writes no NSError");
        }
        else
        {
            error.Should().NotBe(IntPtr.Zero, "a failing probe writes an NSError for the orchestrator to read");
            objc.GetErrorCode(error).Should().NotBe(0, "the NSError carries a non-zero LAError code");
        }

        // Block build (non-interactive): assemble a reply block over a stand-in completion handle and free it again. The
        // block is never invoked (that only happens on the interactive reply, which this test does not trigger).
        GCHandle standInCompletion = GCHandle.Alloc(new object());
        try
        {
            IntPtr replyBlock = objc.BuildReplyBlock(GCHandle.ToIntPtr(standInCompletion));
            replyBlock.Should().NotBe(IntPtr.Zero, "a real reply block is assembled");
            objc.FreeReplyBlock(replyBlock);
        }
        finally
        {
            standInCompletion.Free();
        }

        // Releasing the per-call handles must not throw on the real runtime.
        objc.Release(reason);
        objc.Release(context);
    }
}
