// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single owner a raw OS/CLR primitive resolves to: the interface(s) whose implementing class may make the raw
/// call, and the assembly gate that class compiles into. An empty <see cref="Owners"/> means the primitive has NO
/// owner and is banned everywhere (<c>Process</c>, the stream/drive/watcher types, a directory-side <c>Move</c>)
/// until an interface is deliberately grown for it. This is the pair the consolidated owner rule (AG0011) hands to
/// <see cref="OwnerClass.IsOwner"/>; it replaces the per-primitive owner/assembly pair each folded boundary analyzer
/// used to spell out for itself.
/// </summary>
internal readonly struct OwnedPrimitive
{
    private OwnedPrimitive(ImmutableArray<(string Namespace, string Name)> owners, Func<Compilation, bool> gate)
    {
        Owners = owners;
        Gate = gate;
    }

    /// <summary>Gets a primitive with no owner: never exempt anywhere, so always reported.</summary>
    internal static OwnedPrimitive BannedEverywhere { get; } =
        new(ImmutableArray<(string Namespace, string Name)>.Empty, static _ => false);

    /// <summary>Gets the owning interface(s); empty means banned everywhere (no owner).</summary>
    internal ImmutableArray<(string Namespace, string Name)> Owners { get; }

    /// <summary>Gets the assembly gate the owner class must compile into.</summary>
    internal Func<Compilation, bool> Gate { get; }

    /// <summary>Builds an owned primitive from its owning interface(s) and its assembly gate.</summary>
    /// <param name="owners">The owning interface(s) whose implementing class is exempt.</param>
    /// <param name="gate">The assembly gate the owner class must compile into.</param>
    /// <returns>The owned primitive.</returns>
    internal static OwnedPrimitive OwnedBy(
        ImmutableArray<(string Namespace, string Name)> owners, Func<Compilation, bool> gate)
    {
        return new OwnedPrimitive(owners, gate);
    }
}
