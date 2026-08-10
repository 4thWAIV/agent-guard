// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Setup;

/// <summary>
/// The single set of setup conditions. <c>install</c> applies the machine-structural conditions, <c>init</c>
/// applies the project conditions, and <c>doctor</c> detects (and, with <c>--fix</c>, repairs) all of them — so
/// the layout logic is never re-derived per command.
/// </summary>
internal static class SetupConditions
{
    /// <summary>
    /// Gets every machine condition, including the report-only binary-hash condition, in detection order.
    /// </summary>
    internal static IReadOnlyList<ISetupCondition> Machine { get; } = new ISetupCondition[]
    {
        new VersionBinaryCondition(),
        new CurrentSymlinkCondition(),
        new BinSymlinkCondition(),
        new PathProfileCondition(),
        new BinaryHashCondition(),
    };

    /// <summary>
    /// Gets the machine-structural conditions <c>install</c> applies, in establishment order (the report-only
    /// binary-hash condition is excluded: <c>install</c> writes the hash record directly).
    /// </summary>
    internal static IReadOnlyList<ISetupCondition> MachineStructural { get; } = new ISetupCondition[]
    {
        new VersionBinaryCondition(),
        new CurrentSymlinkCondition(),
        new BinSymlinkCondition(),
        new PathProfileCondition(),
    };

    /// <summary>
    /// Gets every project condition, in establishment order.
    /// </summary>
    internal static IReadOnlyList<ISetupCondition> Project { get; } = new ISetupCondition[]
    {
        new ConfigJsonCondition(),
        new VersionStampCondition(),
        new HookEntriesCondition(),
        new GitignoreCondition(),
    };
}
