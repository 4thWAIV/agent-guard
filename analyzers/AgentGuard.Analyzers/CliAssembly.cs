// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies the one shipping CLI whose single <c>Program</c> composition method is the one place
/// <c>SystemServices.Create()</c> may be called (AG0017). The CLI has TWO distinct identities that must be matched
/// separately: its <see cref="RootNamespace"/> is <c>AgentGuard.Cli</c> (where the <c>Program</c> type is declared),
/// while its real compiled assembly name is <c>guard</c> — set by <c>&lt;AssemblyName&gt;guard&lt;/AssemblyName&gt;</c>
/// in <c>AgentGuard.Cli.csproj</c>, so <c>Program.ContainingAssembly.Name</c> reports <c>guard</c>, not the namespace.
/// Matching the <c>Program</c> TYPE by namespace + name AND the ASSEMBLY by its real compiled name stops a class merely
/// named <c>Program</c> in another namespace or another assembly from self-granting the right to reconstruct the
/// container. The single owner of both values, mirroring <see cref="BoundaryAssembly"/> and <see cref="TestAssembly"/>.
/// </summary>
internal static class CliAssembly
{
    /// <summary>
    /// The CLI's root namespace, <c>AgentGuard.Cli</c>, in which the <c>Program</c> composition type is declared. This
    /// is NOT the compiled assembly name — that is <see cref="CompiledName"/>. The single owner of this value.
    /// </summary>
    internal const string RootNamespace = "AgentGuard.Cli";

    /// <summary>
    /// The CLI's real compiled assembly name, <c>guard</c>, set by
    /// <c>&lt;AssemblyName&gt;guard&lt;/AssemblyName&gt;</c> in <c>AgentGuard.Cli.csproj</c> — the name
    /// <c>Program.ContainingAssembly.Name</c> reports at build time, which differs from the root namespace. Binding the
    /// assembly half of the composition-point match to this real name is what makes the exemption match the actual
    /// compiled <c>Program</c>. The single owner of this value.
    /// </summary>
    internal const string CompiledName = "guard";
}
