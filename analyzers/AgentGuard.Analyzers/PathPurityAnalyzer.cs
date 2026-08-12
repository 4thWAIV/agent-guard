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
/// is a build error, with no allowlist to grow and no old contract to re-read: the single-argument
/// <c>Path.GetFullPath(path)</c> (which reads the current directory), <c>GetTempPath</c>, <c>GetTempFileName</c>,
/// <c>GetRandomFileName</c>, <c>Path.Exists</c>, the raw separator FIELDS (<c>DirectorySeparatorChar</c> and the
/// rest), and any member .NET adds later. There is no owner class — the pure members are legal everywhere and the
/// impure ones are banned everywhere; the replacement is a pure member, or the owning interface (<c>IEnvironment</c>
/// for the current directory and temp paths, <c>IPlatformFileSystem</c> for the raw separator value). This rule owns
/// all of <c>System.IO.Path</c>; AG0012's single-argument <c>Path.GetFullPath</c> clause folded into it.
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

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Only pure System.IO.Path members are allowed; everything else on Path is banned (default-deny)",
        messageFormat: "System.IO.Path member '{0}' is not a pure, input-deterministic member; use a pure Path member, or the owning interface (IEnvironment for the current directory/temp, IPlatformFileSystem for the raw separator value)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every System.IO.Path member is a build error except a small allowlist vetted as pure: Combine, Join, GetFileName, GetDirectoryName, GetExtension, IsPathRooted, and the two-argument Path.GetFullPath(path, basePath). The single-argument Path.GetFullPath, GetTempPath, GetTempFileName, GetRandomFileName, Path.Exists, the raw separator fields, and anything .NET adds later are banned automatically. Pure members are legal everywhere; the impure ones are banned everywhere and routed through IEnvironment or IPlatformFileSystem.");

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

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }

    private static bool IsPureMember(ISymbol member)
    {
        // Only the allowlisted names are pure, and GetFullPath is pure only in its two-argument overload — the
        // single-argument Path.GetFullPath(path) resolves a relative path against the current directory, so it is
        // impure and banned (folded in from AG0012). A field (the raw separator characters) is never on the
        // allowlist, so a separator-field read is always banned.
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
}
