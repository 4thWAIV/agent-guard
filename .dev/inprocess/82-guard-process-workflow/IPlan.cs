using System.Collections.Generic;

public enum ExpectedOutcome
{
    Pass,
    Fail,
    Unknown
}

public sealed record PlannedCase
{
    // Identifies this case in execution and evaluation results.
    public required string Id { get; init; }

    // Markdown describing what to exercise and what to check.
    public required string Description { get; init; }

    public required ExpectedOutcome Expectation { get; init; }

    // Why this outcome is expected for the pre-implementation code.
    public required string Reason { get; init; }

    // Optional certified human input carried into independent evaluation.
    public CertifiedResults<string>? HumanInput { get; init; }
}

public interface IPlan
{
    IReadOnlyList<PlannedCase> Cases { get; }
}
