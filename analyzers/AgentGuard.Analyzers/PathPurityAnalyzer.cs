// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a use of any <c>System.IO.Path</c> member that is not on the small allowlist of pure, input-deterministic
/// members. This is DEFAULT-DENY (pure-methods-only-default-deny-path): only pure functions of their arguments are
/// legal — <c>Combine</c>, <c>Join</c>, <c>GetFileName</c>, <c>GetDirectoryName</c>, <c>GetExtension</c>,
/// <c>IsPathRooted</c>, and the two-argument <c>Path.GetFullPath(path, basePath)</c>. Everything else on <c>Path</c>
/// is a build error, with no allowlist to grow: the single-argument <c>Path.GetFullPath(path)</c>,
/// <c>GetTempFileName</c>, <c>Path.Exists</c>, the alternate separator fields, and any member .NET adds later.
/// <para>
/// THREE impure members are reused behind a single owner each (temp-and-random-primitives-reused-behind-owners,
/// directoryseparator-owned-passthrough): <c>GetTempPath</c> is an ambient environment read owned by the
/// <c>IEnvironment</c> adapter in AgentGuard.Boundaries; <c>GetRandomFileName</c> is randomness owned by the
/// <c>IRandomGenerator</c> adapter in AgentGuard.CrossPlatform; the raw <c>DirectorySeparatorChar</c> FIELD is owned by
/// the <c>IPlatformFileSystem</c> implementers — the per-OS <c>PosixFileSystem</c>/<c>WindowsFileSystem</c> and the
/// in-memory fake — which pass it straight through. Each is legal ONLY in its one owner (the enclosing type implements
/// the owning interface AND compiles into the owner assembly, the same <see cref="OwnerClass.IsOwner"/> conjunction
/// AG0011 uses) and a build error everywhere else. No rule forbids the authorized identical
/// <c>DirectorySeparator =&gt; Path.DirectorySeparatorChar</c> across those owners
/// (directoryseparator-owned-passthrough-with-authorized-dry-exception). This rule owns all of <c>System.IO.Path</c>.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PathPurityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0020";

    private const string Category = "AgentGuard.Architecture";
    private const string PathTypeName = "Path";
    private const string GetFullPathMethodName = "GetFullPath";
    private const string GetTempPathMethodName = "GetTempPath";
    private const string GetRandomFileNameMethodName = "GetRandomFileName";
    private const string DirectorySeparatorFieldName = "DirectorySeparatorChar";

    // The pure, input-deterministic Path member names that are legal everywhere. A member NOT in this set is banned
    // (default-deny), so a future non-pure member .NET adds is caught automatically. GetFullPath is on the list but
    // only its two-argument, pure overload is allowed — the single-argument overload reads the current directory and
    // is handled specially below.
    private static readonly ImmutableHashSet<string> PureMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Combine",
        "Join",
        "GetFileName",
        "GetDirectoryName",
        "GetExtension",
        "IsPathRooted",
        GetFullPathMethodName);

    // The separator owner's assembly gate: the three per-OS implementation libraries (where PosixFileSystem/
    // WindowsFileSystem live) OR AgentGuard.TestHelpers (where the in-memory fake lives) — the assemblies whose
    // IPlatformFileSystem implementers pass DirectorySeparatorChar straight through. Broader than AG0101's per-OS-only
    // gate because the fake is an owner of the separator read too (directoryseparator-owned-passthrough).
    private static readonly Func<Compilation, bool> InSeparatorOwnerAssembly =
        compilation => CrossPlatformBoundary.IsPerOsImplementationAssembly(compilation.AssemblyName)
            || string.Equals(compilation.AssemblyName, TestAssembly.TestHelpersName, StringComparison.Ordinal);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Only pure System.IO.Path members are allowed; the impure ones are banned outside their single owner",
        messageFormat: "System.IO.Path member '{0}' is not a pure, input-deterministic member; use a pure Path member, or reach it only through its owner (IEnvironment for GetTempPath, IRandomGenerator for GetRandomFileName, IPlatformFileSystem for the raw separator)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every System.IO.Path member is a build error except a small allowlist vetted as pure: Combine, Join, GetFileName, GetDirectoryName, GetExtension, IsPathRooted, and the two-argument Path.GetFullPath(path, basePath). Three impure members are reused behind a single owner each — Path.GetTempPath in the IEnvironment adapter (AgentGuard.Boundaries), Path.GetRandomFileName in the IRandomGenerator adapter (AgentGuard.CrossPlatform), and the raw Path.DirectorySeparatorChar field in the IPlatformFileSystem implementers (the per-OS classes and the in-memory fake) — legal only in their owner and banned everywhere else. The single-argument Path.GetFullPath, GetTempFileName, Path.Exists, the alternate separator fields, and anything .NET adds later stay banned everywhere.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedRules;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context => MemberUseScanner.Register(context, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (!WellKnownType.Is(type, KnownNamespaces.SystemIO, PathTypeName))
        {
            return;
        }

        if (IsPureMember(member))
        {
            return;
        }

        // The three reused impure members are legal only inside their single owner — the same OwnerClass.IsOwner
        // conjunction (implements the owning interface AND compiles into the owner assembly) AG0011 applies. Every
        // other impure member has no owner and is banned everywhere (default-deny).
        if (ResolveImpureOwner(member) is { } owned && OwnerClass.IsOwner(context, owned.Owners, owned.Gate))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }

    private static bool IsPureMember(ISymbol member)
    {
        // Only the allowlisted names are pure, and GetFullPath is pure only in its two-argument overload — the
        // single-argument Path.GetFullPath(path) resolves a relative path against the current directory, so it is
        // impure and banned. A field (the raw separator characters) is never on the allowlist, so a separator-field
        // read is always impure (owned only for DirectorySeparatorChar below).
        if (member is not IMethodSymbol method)
        {
            return false;
        }

        if (!PureMemberNames.Contains(method.Name))
        {
            return false;
        }

        return !string.Equals(method.Name, GetFullPathMethodName, StringComparison.Ordinal)
            || method.Parameters.Length == 2;
    }

    // The single owner of each of the three reused impure Path members, or null when the impure member has no owner and
    // is banned everywhere. Keyed by the member kind and name so a same-named member on another type could never match
    // (the caller has already gated on the Path type).
    private static OwnedPrimitive? ResolveImpureOwner(ISymbol member)
    {
        if (member is IMethodSymbol)
        {
            if (string.Equals(member.Name, GetTempPathMethodName, StringComparison.Ordinal))
            {
                return OwnedPrimitive.OwnedBy(ContractInterfaces.Environment, OwnedPrimitives.InBoundaries);
            }

            if (string.Equals(member.Name, GetRandomFileNameMethodName, StringComparison.Ordinal))
            {
                return OwnedPrimitive.OwnedBy(ContractInterfaces.RandomGenerator, OwnedPrimitives.InCrossPlatform);
            }
        }

        return member is IFieldSymbol
            && string.Equals(member.Name, DirectorySeparatorFieldName, StringComparison.Ordinal)
                ? OwnedPrimitive.OwnedBy(ContractInterfaces.PlatformFileSystem, InSeparatorOwnerAssembly)
                : (OwnedPrimitive?)null;
    }
}
