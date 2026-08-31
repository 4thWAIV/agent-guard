// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// A direct test of the owned per-verb prompt resource (<c>PresenceDialogText.For</c>, os-dialog-text-owned-resource),
/// independent of the approval-gate call site. The gate test proves only that the gate passes
/// <c>PresenceDialogText.For(verb)</c> to the boundary — it would still pass if every verb produced the same or an empty
/// string. This pins what the resource must actually return: each setup verb (<see cref="SetupVerb.Install"/> /
/// <see cref="SetupVerb.Init"/> / <see cref="SetupVerb.Remove"/>) yields its OWN reviewed, non-empty prompt string, so
/// the three actions cannot share or blank out a dialog.
/// </summary>
public sealed class PresenceDialogTextTests
{
    [Fact]
    public void For_YieldsAReviewedNonEmptyString_ForEachVerb()
    {
        PresenceDialogText.For(SetupVerb.Install).Should().NotBeNullOrWhiteSpace();
        PresenceDialogText.For(SetupVerb.Init).Should().NotBeNullOrWhiteSpace();
        PresenceDialogText.For(SetupVerb.Remove).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void For_YieldsADistinctStringPerVerb_SoTheThreeActionsNeverShareAPrompt()
    {
        string install = PresenceDialogText.For(SetupVerb.Install);
        string init = PresenceDialogText.For(SetupVerb.Init);
        string remove = PresenceDialogText.For(SetupVerb.Remove);

        new[] { install, init, remove }.Should().OnlyHaveUniqueItems();
    }
}
