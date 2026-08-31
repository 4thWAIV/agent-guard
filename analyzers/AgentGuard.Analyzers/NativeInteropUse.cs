// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single recognizer for "is this member use a raw native P/Invoke call site or a <c>Marshal</c> use" — the two
/// native-interop shapes the OS-divergent filesystem rule (AG0101) and the presence native-interop owner rule (AG0113)
/// both key off. Extracted here (LESSON 1, DRY) so the two rules share ONE detector instead of each spelling out
/// <c>member is IMethodSymbol method &amp;&amp; PInvoke.IsPInvoke(method)</c> and
/// <c>WellKnownType.Is(type, SystemRuntimeInteropServices, "Marshal")</c>; a change to what counts as a native use
/// (a new interop attribute, a second marshalling type) is made once and every rule that confines native interop moves
/// with it. The recognition is pure surface — is this a P/Invoke call, is this a Marshal use; which OWNER may make the
/// call (filesystem owner vs presence port) is the caller's family policy, kept at the rule, never baked in here.
/// </summary>
internal static class NativeInteropUse
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is a call to a native P/Invoke method — one declared
    /// with <c>[DllImport]</c> or <c>[LibraryImport]</c> (the raw syscall site; AG0008 catches only the declaration).
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns><see langword="true"/> when the member is a P/Invoke method.</returns>
    internal static bool IsPInvokeCall(ISymbol member) =>
        member is IMethodSymbol method && PInvoke.IsPInvoke(method);

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is <c>System.Runtime.InteropServices.Marshal</c> — any
    /// use of a <c>Marshal</c> member.
    /// </summary>
    /// <param name="type">The type that declares the referenced member.</param>
    /// <returns><see langword="true"/> when the declaring type is <c>Marshal</c>.</returns>
    internal static bool IsMarshalUse(INamedTypeSymbol type) =>
        WellKnownType.Is(type, KnownNamespaces.SystemRuntimeInteropServices, "Marshal");

    /// <summary>
    /// Gets a value indicating whether the member use is a native P/Invoke call site OR a <c>Marshal</c> use — the
    /// native-interop family AG0101 and AG0113 both confine to an owner. Callers that must distinguish the two shapes
    /// (AG0113 skips the filesystem-native bindings for P/Invoke and the filesystem owner for Marshal) call
    /// <see cref="IsPInvokeCall"/> and <see cref="IsMarshalUse"/> directly.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <param name="type">The type that declares the referenced member.</param>
    /// <returns><see langword="true"/> when the use is a P/Invoke call or a <c>Marshal</c> use.</returns>
    internal static bool IsNativeUse(ISymbol member, INamedTypeSymbol type) =>
        IsPInvokeCall(member) || IsMarshalUse(type);
}
