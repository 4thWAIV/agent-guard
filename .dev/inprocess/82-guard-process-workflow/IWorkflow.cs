using System;
using System.Collections.Generic;

public interface IWorkflow
{
    // Supply IssueId or nonempty IssueText, never both.
    int? IssueId { get; }

    // Null when IssueId is supplied; otherwise AgentGuard creates the issue from this text.
    string? IssueText { get; }

    // Assigned by AgentGuard and retained in server persistent state.
    Guid WorkflowId { get; }

    string ProjectPath { get; }

    string WorkingFolder { get; }

    // Path to the serialized conversation file.
    string ConversationPath { get; }

    WorkflowLevel Level { get; }

    // Stages contain attempts, their assignments, and agent runs in this Workflow document.
    IReadOnlyDictionary<string, StageExecution> Stages { get; }
}
