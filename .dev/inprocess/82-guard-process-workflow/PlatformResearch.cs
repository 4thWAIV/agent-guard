// Design draft, not an implemented Guard tool or RefLib MCP schema.
// External research is separate from project prior art.
using System;
using System.Threading;
using System.Threading.Tasks;

// Stored inside Workflow. Read capability, target platforms, scope and exclusions
// from this exact Conversation version; do not copy them into the assignment.
public sealed record PlatformResearchAssignment(
    string Role,
    VersionedFileReference Conversation,
    string CapabilityId)
    : AgentAssignment(Role);

public abstract record PlatformResearchRequest(
    AssignmentReference Assignment);

public sealed record DocumentationSearchRequest(
    AssignmentReference Assignment,
    string Query,
    string[] Domains)
    : PlatformResearchRequest(Assignment);

public sealed record SdkSearchRequest(
    AssignmentReference Assignment,
    string SearchRoot,
    string Pattern,
    TextMatchKind MatchKind)
    : PlatformResearchRequest(Assignment);

// RefLib supplies the reference-library backend. Guard selects the backend.
public sealed record ListReferenceCorporaRequest(
    AssignmentReference Assignment)
    : PlatformResearchRequest(Assignment);

public sealed record ReferenceLibrarySearchRequest(
    AssignmentReference Assignment,
    string CorpusId,
    string Query)
    : PlatformResearchRequest(Assignment);

public abstract record ResearchSource;

public sealed record WebResearchSource(Uri Url) : ResearchSource;

public sealed record LocalResearchSource(string Path) : ResearchSource;

public sealed record ReferenceLibrarySource(
    string CorpusId,
    string SectionId) : ResearchSource;

public sealed record ReadResearchSourceRequest(
    AssignmentReference Assignment,
    ResearchSource Source)
    : PlatformResearchRequest(Assignment);

public interface IPlatformResearch
{
    Task<ResearchReceipt> SearchDocumentationAsync(
        DocumentationSearchRequest request,
        CancellationToken cancellationToken);

    Task<ResearchReceipt> SearchSdkAsync(
        SdkSearchRequest request,
        CancellationToken cancellationToken);

    Task<ResearchReceipt> ListCorporaAsync(
        ListReferenceCorporaRequest request,
        CancellationToken cancellationToken);

    Task<ResearchReceipt> SearchReferenceLibraryAsync(
        ReferenceLibrarySearchRequest request,
        CancellationToken cancellationToken);

    Task<ResearchReceipt> ReadSourceAsync(
        ReadResearchSourceRequest request,
        CancellationToken cancellationToken);
}

public sealed record ResearchReceipt(VersionedFileReference Capture);

// Guard writes captures; the research agent supplies its interpretation separately.
// References retain each request and response once, not embedded copies.
public sealed record PlatformResearchCapture(
    VersionedFileReference Request,
    string Backend,
    VersionedFileReference ExecutedRequest,
    ResearchOutcome Outcome);

public abstract record ResearchOutcome;

public sealed record ResearchCompleted(
    VersionedFileReference Response) : ResearchOutcome;

public sealed record ResearchFailed(
    string Error,
    VersionedFileReference? Response) : ResearchOutcome;
