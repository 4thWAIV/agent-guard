// Draft for review. Prior art covers this project's own code only.
// Input versions use VersionedFileReference; certification and Guard operations remain open.
// Input ownership: see conversation.md, "Prior-art input ownership".
// Assignments are nested in Workflow; instructions remain in Conversation.

using System.Threading;
using System.Threading.Tasks;

public abstract record PriorArtSearchRequest(
    AssignmentReference Assignment);

public sealed record StructuralSearchRequest(
    AssignmentReference Assignment,
    string Query)
    : PriorArtSearchRequest(Assignment);

public sealed record TextSearchRequest(
    AssignmentReference Assignment,
    string Pattern,
    TextMatchKind MatchKind)
    : PriorArtSearchRequest(Assignment);

public enum TextMatchKind
{
    Literal,
    RegularExpression
}

public interface IPriorArtSearch
{
    Task<SearchReceipt> SearchAsync(
        PriorArtSearchRequest request,
        CancellationToken cancellationToken);
}

public sealed record SearchReceipt(
    string CapturePath);

public sealed record PriorArtSearchCapture(
    // Path to the saved request; do not embed a second copy.
    string RequestPath,
    ExecutedCommand Command,
    SearchExecutionResult Result);

public sealed record ExecutedCommand(
    string Executable,
    string[] Arguments,
    string WorkingDirectory);

public sealed record SearchExecutionResult(
    SearchStatus Status,
    int? ExitCode,
    string? ExecutionError,
    string StandardOutput,
    string StandardError);

// Stored inside StageAttempt.Assignments in Workflow, not a separate input file.
// CapabilityId selects IConversation.Capabilities[].Id in the pinned Conversation.
// Scope, exclusions, and capability description are read from that same version.
public sealed record PriorArtSearchAssignment(
    string Role,
    VersionedFileReference Conversation,
    string CapabilityId) : AgentAssignment(Role);

// Stored inside Workflow; the judge reads the exact saved search result.
// Its Assignment reference leads back to the original search instructions.
public sealed record PriorArtEvaluationAssignment(
    string Role,
    VersionedFileReference SearchResult) : AgentAssignment(Role);

// Produced by the codebase-search agent and stored through Guard.
public sealed record PriorArtSearchResult(
    AssignmentReference Assignment,
    SearchExecution[] Searches,
    CodeCandidate[] Candidates);

public sealed record SearchExecution(
    string Id,
    // Execution details belong to the tool-written capture, not this agent result.
    string CapturePath);

public enum SearchStatus
{
    Completed,
    Failed
}

public sealed record CodeCandidate(
    string Id,
    string File,
    string Symbol,
    // References SearchExecution.Id values in the same search result.
    string[] FoundBySearchIds);

// Produced by the reuse-judge agent and stored through Guard.
public sealed record PriorArtEvaluationResult(
    AssignmentReference Assignment,
    CandidateEvaluation[] Evaluations,
    ReuseDecision Decision);

public sealed record CandidateEvaluation(
    // References CodeCandidate.Id in the supplied search result.
    string CandidateId,
    string ProvidedBehavior,
    string[] MissingBehavior,
    string[] Limitations,
    EvaluationEvidence[] Evidence);

public sealed record EvaluationEvidence(
    // Retained source inspection or execution result.
    string CapturePath,
    // Relevant lines or result entry within that capture.
    string Location,
    // The agent's explanation, distinguished from the captured evidence.
    string Supports);

public abstract record ReuseDecision
{
    public abstract ReuseApproach Approach { get; }
}

public abstract record ResolvedReuseDecision : ReuseDecision
{
    // References the candidates supporting this determination.
    public required string[] CandidateIds { get; init; }
    public required string Reason { get; init; }
    public required string[] RequiredChanges { get; init; }
}

public sealed record ReuseExistingDecision : ResolvedReuseDecision
{
    public override ReuseApproach Approach => ReuseApproach.Reuse;
}

public sealed record ExtendExistingDecision : ResolvedReuseDecision
{
    public override ReuseApproach Approach => ReuseApproach.Extend;
}

public sealed record ExtractExistingDecision : ResolvedReuseDecision
{
    public override ReuseApproach Approach => ReuseApproach.Extract;
}

public sealed record NewImplementationDecision : ResolvedReuseDecision
{
    public override ReuseApproach Approach => ReuseApproach.New;
}

// Guard requires at least one nonempty question.
public sealed record UnresolvedReuseDecision(
    string[] Questions) : ReuseDecision
{
    public override ReuseApproach Approach => ReuseApproach.Unresolved;
}

public enum ReuseApproach
{
    Reuse,
    Extend,
    Extract,
    New,
    Unresolved
}

// Guard creates the ledger by linking saved inputs and outputs.
// Neither agent nor the workflow script rewrites the results into this file.
public sealed record PriorArtLedger(
    PriorArtLedgerEntry[] Entries);

public sealed record PriorArtLedgerEntry(
    VersionedFileReference SearchResult,
    VersionedFileReference EvaluationResult);
