// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers;

/// <summary>
/// The fully qualified BCL namespace names the OS-primitive boundary rules match against, named once here so the
/// same literal is not repeated across the rules.
/// </summary>
internal static class KnownNamespaces
{
    /// <summary>The <c>AgentGuard.Abstractions.Contracts</c> sub-namespace, where the owned boundary interfaces and
    /// the <c>ISystemServices</c> container are defined.</summary>
    internal const string AgentGuardAbstractionsContracts = "AgentGuard.Abstractions.Contracts";

    /// <summary>The <c>System</c> namespace.</summary>
    internal const string System = "System";

    /// <summary>The <c>System.IO</c> namespace.</summary>
    internal const string SystemIO = "System.IO";

    /// <summary>The <c>System.IO.Enumeration</c> namespace.</summary>
    internal const string SystemIOEnumeration = "System.IO.Enumeration";

    /// <summary>The <c>System.Diagnostics</c> namespace.</summary>
    internal const string SystemDiagnostics = "System.Diagnostics";

    /// <summary>The <c>System.Reflection</c> namespace.</summary>
    internal const string SystemReflection = "System.Reflection";

    /// <summary>The <c>System.Security.Cryptography</c> namespace.</summary>
    internal const string SystemSecurityCryptography = "System.Security.Cryptography";

    /// <summary>The <c>System.Runtime.InteropServices</c> namespace.</summary>
    internal const string SystemRuntimeInteropServices = "System.Runtime.InteropServices";
}
