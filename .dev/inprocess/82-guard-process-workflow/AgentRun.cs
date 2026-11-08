using System;

// Agent-run tracking draft. Timing is approved for agent runs only.
// The execution mechanism records the timestamps, not the agent.
public sealed record AgentRun(
    // Key in the containing StageAttempt.Assignments dictionary.
    Guid AssignmentId,
    AgentRunStatus Status,
    string? ResultPath,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);

public enum AgentRunStatus
{
    Running,
    Completed,
    Interrupted,
    Failed
}
