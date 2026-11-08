using System.Collections.Generic;

public enum IssueStatus
{
    Opened,
    Closed,
    Investigating
}

public enum ProposedDecisionState
{
    Proposed,
    Reviewing,
    Accepted,
    Rejected
}

public sealed record Issue
{
    // Markdown describing the issue to resolve.
    public required string Description { get; init; }
    public required IssueStatus Status { get; init; }
}

public sealed record ProposedDecision
{
    // Markdown describing the proposed decision.
    public required string Description { get; init; }
    public required ProposedDecisionState State { get; init; }
}

public sealed record InvestigationArea
{
    public required string Id { get; init; }

    // Markdown describing what to investigate.
    public required string Focus { get; init; }
}

public sealed record Capability
{
    public required string Id { get; init; }

    // Markdown describing the functionality to search for.
    public required string Description { get; init; }
}

public sealed record TargetPlatform(
    string Id,
    string Description);

public interface IConversation
{
    // Markdown describing the work this workflow will do.
    string Description { get; }

    // Markdown defining the portion of the issue this workflow covers.
    string Scope { get; }

    // Markdown defining work outside this workflow.
    string Exclusions { get; }

    IReadOnlyList<Issue> Issues { get; }

    IReadOnlyList<ProposedDecision> ProposedDecisions { get; }

    // Focus is Markdown describing each investigation assignment.
    IReadOnlyList<InvestigationArea> InvestigationAreas { get; }

    // Description is Markdown describing each capability to search for.
    IReadOnlyList<Capability> Capabilities { get; }

    // One workflow-wide platform selection, shared by all capability research.
    IReadOnlyList<TargetPlatform> TargetPlatforms { get; }

    // Path to the serialized workflow file.
    string WorkflowPath { get; }
}
