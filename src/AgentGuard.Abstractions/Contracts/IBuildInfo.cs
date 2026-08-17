// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned read of the running build's version metadata — the three already-stamped version attributes on a
/// guard assembly. It only reads: the values are computed and stamped one way by the build, so it neither falls
/// back nor recomputes, and it reads from a guard assembly rather than the ambient entry assembly (the test host
/// under <c>dotnet test</c>).
/// </summary>
public interface IBuildInfo
{
    /// <summary>
    /// Gets the semantic version — the assembly's informational version.
    /// </summary>
    string SemVer { get; }

    /// <summary>
    /// Gets the assembly version.
    /// </summary>
    string AssemblyVersion { get; }

    /// <summary>
    /// Gets the file version.
    /// </summary>
    string FileVersion { get; }
}
