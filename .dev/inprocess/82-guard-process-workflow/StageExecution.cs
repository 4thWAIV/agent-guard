using System;
using System.Collections.Generic;

public sealed record StageExecution(
    Guid? LatestAttemptId,
    IReadOnlyDictionary<Guid, StageAttempt> Attempts);

public sealed record StageAttempt(
    // Entries are serialized inside Workflow; the dictionary key is the assignment ID.
    IReadOnlyDictionary<Guid, AgentAssignment> Assignments,
    IReadOnlyDictionary<Guid, AgentRun> AgentRuns);

// Agent-specific records add typed input references, not copied instructions.
public abstract record AgentAssignment(string Role);

// Stored inside Workflow. Findings remain in the referenced result files.
public sealed record DesignAssignment(
    string Role,
    VersionedFileReference[] ExplorationResults,
    VersionedFileReference PriorArtLedger,
    VersionedFileReference[] PlatformResearchResults)
    : AgentAssignment(Role);

// Locates registered work from a separately stored request or result.
// WorkflowPath is a live locator, not a pinned copy of the changing Workflow document.
public sealed record AssignmentReference(
    string WorkflowPath,
    string Stage,
    Guid AttemptId,
    Guid AssignmentId);

// Identifies the exact bytes of an input artifact.
public sealed record VersionedFileReference(
    string Path,
    string GitBlobId);
