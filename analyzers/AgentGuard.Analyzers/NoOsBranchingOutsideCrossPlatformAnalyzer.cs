// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports OS branching around two boundaries, with a diagnostic id each:
/// <list type="bullet">
/// <item><b>AG0009</b> — a call to <c>OperatingSystem.Is*</c> or <c>RuntimeInformation.IsOSPlatform</c>, or a
/// platform <c>#if</c>, declared OUTSIDE the <c>AgentGuard.CrossPlatform.*</c> platform implementation libraries.
/// OS-agnostic code must stay free of platform branches; platform-specific behavior is absorbed by the per-OS
/// implementation selected in the csproj and reached through <c>IPlatformServices</c>.</item>
/// <item><b>AG0037</b> — the inverted case: a single-OS POSITIVE branch (<c>OperatingSystem.IsMacOS()</c>,
/// <c>IsLinux()</c>, <c>IsFreeBSD()</c>, and the like — anything but the cross-POSIX <c>!IsWindows()</c> family gate)
/// declared INSIDE a per-OS implementation library (<c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c>). A file that is
/// link-shared across per-OS projects compiles a positive single-OS branch into the OTHER OS's build as permanently
/// dead, uncoverable code; the OS-divergent behavior belongs in an OS-specific file (<c>*.MacOS.cs</c>/<c>*.Linux.cs</c>/
/// <c>*.Windows.cs</c>) instead. The core <c>AgentGuard.CrossPlatform</c> contract library is shared, not per-OS, so
/// neither rule constrains it.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoOsBranchingOutsideCrossPlatformAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier for OS branching outside the platform implementation libraries.
    /// </summary>
    public const string DiagnosticId = "AG0009";

    /// <summary>
    /// The diagnostic identifier for a single-OS positive branch inside a per-OS implementation library.
    /// </summary>
    public const string SingleOsBranchDiagnosticId = "AG0037";

    private const string Category = "AgentGuard.Architecture";
    private const string OperatingSystemTypeName = "OperatingSystem";
    private const string RuntimeInformationTypeName = "RuntimeInformation";
    private const string OsPlatformQueryMethodName = "IsOSPlatform";
    private const string OperatingSystemQueryPrefix = "Is";

    private static readonly ImmutableArray<string> PlatformSymbolPrefixes = ImmutableArray.Create(
        "WINDOWS", "LINUX", "OSX", "MACOS", "MACCATALYST", "ANDROID", "IOS", "TVOS", "BROWSER", "FREEBSD", "WASI");

    // The one cross-POSIX gate a per-OS library MAY branch on: !OperatingSystem.IsWindows() (and its version-guard
    // sibling) partitions POSIX from Windows, so it is live in every per-OS build — not a single-OS positive check.
    // Every OTHER OperatingSystem.Is* names ONE specific OS and is dead in the others, so AG0037 flags it.
    private static readonly ImmutableHashSet<string> AllowedWindowsGateMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal, "IsWindows", "IsWindowsVersionAtLeast");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "OS branching is not allowed outside the AgentGuard.CrossPlatform libraries",
        messageFormat: "OS branching ('{0}') is not allowed outside the AgentGuard.CrossPlatform.* libraries; keep OS-agnostic code free of platform branches and reach platform-specific behavior through IPlatformServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calling OperatingSystem.Is* or RuntimeInformation.IsOSPlatform, or writing a platform #if, is not allowed outside the AgentGuard.CrossPlatform.* platform implementation libraries. Platform differences are absorbed by the per-OS implementations selected in the csproj, so consuming code stays OS-agnostic.");

    private static readonly DiagnosticDescriptor SingleOsBranchRule = new(
        id: SingleOsBranchDiagnosticId,
        title: "A single-OS branch is not allowed inside a per-OS implementation library",
        messageFormat: "Single-OS branch ('{0}') is not allowed inside a per-OS implementation library; a positive single-OS check compiles into the other OS's build as permanently-dead, uncoverable code — move the OS-divergent behavior into an OS-specific file (*.MacOS.cs/*.Linux.cs/*.Windows.cs). Only the cross-POSIX !OperatingSystem.IsWindows() gate stays allowed.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Inside a per-OS implementation library (AgentGuard.CrossPlatform.MacOS/.Linux/.Windows), a single-OS positive branch — OperatingSystem.IsMacOS()/IsLinux()/IsFreeBSD() and the like — is a build error, because a source file link-shared across per-OS projects compiles that branch into the other OS's build where it is permanently dead and uncoverable. Put the OS-divergent behavior in an OS-specific file (*.MacOS.cs/*.Linux.cs/*.Windows.cs) that only its own OS's project compiles. The cross-POSIX !OperatingSystem.IsWindows() family gate stays allowed because it partitions POSIX from Windows and is live in every per-OS build. The core AgentGuard.CrossPlatform contract library is shared and unconstrained.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules =
        ImmutableArray.Create(Rule, SingleOsBranchRule);

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Inside a per-OS implementation library (.MacOS/.Linux/.Windows), the OUTWARD ban (AG0009) does not apply —
        // OS-specific code belongs here — but the INVERTED ban (AG0037) does: a single-OS positive branch would
        // compile into the other OS's build as permanently-dead code. This is the inverted registration.
        if (CrossPlatformBoundary.IsPerOsImplementationAssembly(context.Compilation.AssemblyName))
        {
            context.RegisterSyntaxNodeAction(AnalyzeSingleOsBranch, SyntaxKind.InvocationExpression);
            return;
        }

        // The core AgentGuard.CrossPlatform contract library is shared, not per-OS, so neither rule constrains it.
        if (CrossPlatformBoundary.IsCrossPlatformLibrary(context.Compilation))
        {
            return;
        }

        // Every OS-agnostic assembly: AG0009 bans OS branching outright.
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxTreeAction(AnalyzePlatformDirectives);
    }

    private static void AnalyzeSingleOsBranch(SyntaxNodeAnalysisContext context)
    {
        if (!TryResolveInvokedMethod(
            context, out InvocationExpressionSyntax invocation, out IMethodSymbol method, out INamedTypeSymbol containingType))
        {
            return;
        }

        // A positive single-OS branch is any OperatingSystem.Is* EXCEPT the cross-POSIX !IsWindows() family gate,
        // matched by the containing type's identity (System.OperatingSystem via WellKnownType) AND the method name.
        if (WellKnownType.Is(containingType, KnownNamespaces.System, OperatingSystemTypeName)
            && method.Name.StartsWith(OperatingSystemQueryPrefix, StringComparison.Ordinal)
            && !AllowedWindowsGateMethods.Contains(method.Name))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SingleOsBranchRule, invocation.GetLocation(), $"{containingType.Name}.{method.Name}"));
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (!TryResolveInvokedMethod(
            context, out InvocationExpressionSyntax invocation, out IMethodSymbol method, out INamedTypeSymbol containingType))
        {
            return;
        }

        bool isOperatingSystemBranch = WellKnownType.Is(containingType, KnownNamespaces.System, OperatingSystemTypeName)
            && method.Name.StartsWith(OperatingSystemQueryPrefix, StringComparison.Ordinal);

        bool isRuntimeInformationBranch = WellKnownType.Is(containingType, KnownNamespaces.SystemRuntimeInteropServices, RuntimeInformationTypeName)
            && string.Equals(method.Name, OsPlatformQueryMethodName, StringComparison.Ordinal);

        if (isOperatingSystemBranch || isRuntimeInformationBranch)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), $"{containingType.Name}.{method.Name}"));
        }
    }

    // The shared open of both invocation rules (AG0009's AnalyzeInvocation and AG0037's AnalyzeSingleOsBranch): the
    // node is an invocation (both register only for SyntaxKind.InvocationExpression), it resolves to a method symbol,
    // and that method has a containing type. Returns false — and each caller returns — when the symbol does not resolve
    // to a method or has no containing type, so the cast + GetSymbolInfo + ContainingType guard is written exactly once.
    private static bool TryResolveInvokedMethod(
        SyntaxNodeAnalysisContext context,
        out InvocationExpressionSyntax invocation,
        out IMethodSymbol method,
        out INamedTypeSymbol containingType)
    {
        invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol resolved
            && resolved.ContainingType is { } type)
        {
            method = resolved;
            containingType = type;
            return true;
        }

        method = null!;
        containingType = null!;
        return false;
    }

    private static void AnalyzePlatformDirectives(SyntaxTreeAnalysisContext context)
    {
        SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
        var platformDirectives = root
            .DescendantNodesAndSelf(descendIntoTrivia: true)
            .OfType<ConditionalDirectiveTriviaSyntax>()
            .Where(directive => ContainsPlatformSymbol(directive.Condition));

        foreach (ConditionalDirectiveTriviaSyntax directive in platformDirectives)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, directive.GetLocation(), directive.ToString().Trim()));
        }
    }

    private static bool ContainsPlatformSymbol(ExpressionSyntax? condition)
    {
        return condition is not null
            && condition.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => IsPlatformSymbol(identifier.Identifier.ValueText));
    }

    private static bool IsPlatformSymbol(string name)
    {
        foreach (string prefix in PlatformSymbolPrefixes)
        {
            if (string.Equals(name, prefix, StringComparison.Ordinal))
            {
                return true;
            }

            if (name.Length > prefix.Length
                && name.StartsWith(prefix, StringComparison.Ordinal)
                && (name[prefix.Length] == '_' || char.IsDigit(name[prefix.Length])))
            {
                return true;
            }
        }

        return false;
    }
}
