// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw environment or deployment-path read made anywhere but the single owner class that implements
/// <c>AgentGuard.Abstractions.IEnvironment</c>: any <c>System.Environment</c> member (except <c>TickCount</c>, which
/// is time — AG0015), <c>Directory.GetCurrentDirectory</c>/<c>SetCurrentDirectory</c>, the single-argument
/// <c>Path.GetFullPath</c> (which is impure — it resolves a relative path against the current directory),
/// <c>Assembly.Location</c>, <c>AppContext.BaseDirectory</c>, <c>AppDomain.CurrentDomain.BaseDirectory</c>, and the
/// <c>RuntimeInformation</c> host-description properties. The one exemption is the class implementing
/// <c>IEnvironment</c> AND compiled into <c>AgentGuard.Boundaries</c> (where the <c>EnvironmentAdapter</c> lives); the
/// raw read is a build error in another class of the same assembly and in the same class self-granting from another
/// assembly. Every other type
/// reads the environment through <c>IEnvironment</c> pulled off <c>ISystemServices</c>, and canonicalizes with the
/// pure two-argument <c>Path.GetFullPath(path, basePath)</c> using <c>IEnvironment.GetCurrentDirectory()</c> as the base.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnvironmentOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0012";

    private const string Category = "AgentGuard.Architecture";
    private const string GetFullPathMethodName = "GetFullPath";

    // The owning interface whose single implementing class is the only place a raw environment read is allowed
    // (one-owner-class-per-primitive). Matched structurally by full name against the enclosing type's implemented
    // interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractions, "IEnvironment"));

    // The owner assembly: AgentGuard.Boundaries, where the EnvironmentAdapter lives — nothing below Boundaries
    // consumes the environment, so it stays there (owners-live-at-lowest-consumer). Half of the conjunction
    // OwnerClass.IsOwner applies: implementing IEnvironment in any OTHER assembly does not exempt. Reuses the shared
    // BoundaryAssembly.Name constant; cached once so no per-operation allocation.
    private static readonly Func<Compilation, bool> InOwnerAssembly =
        OwnerClass.InAssembly(BoundaryAssembly.Name);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw environment and deployment-path reads must live only in the class implementing IEnvironment",
        messageFormat: "Raw environment read '{0}' is outside the single owner class implementing IEnvironment; read the environment through IEnvironment pulled off ISystemServices, and use the two-argument Path.GetFullPath(path, basePath)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A System.Environment member, Directory.GetCurrentDirectory/SetCurrentDirectory, the single-argument Path.GetFullPath, Assembly.Location, AppContext.BaseDirectory, AppDomain.CurrentDomain.BaseDirectory, or a RuntimeInformation host-description property is allowed only in the single class that implements AgentGuard.Abstractions.IEnvironment — not merely somewhere in its assembly. Every other type reads the environment through IEnvironment on ISystemServices and canonicalizes with the pure two-argument Path.GetFullPath(path, basePath).");

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
        // The single owner class that implements IEnvironment AND compiles into AgentGuard.Boundaries is the only
        // place the raw read is allowed (one-owner-class-per-primitive + owners-live-at-lowest-consumer); everywhere
        // else — a sibling class in the same assembly, or the same class self-granting in another assembly — is RED.
        if (OwnerClass.IsOwner(context, OwningInterfaces, InOwnerAssembly))
        {
            return;
        }

        if (IsBanned(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsBanned(ISymbol member, INamedTypeSymbol type)
    {
        // System.Environment (all members) — except TickCount/TickCount64, which are time and belong to AG0015.
        if (WellKnownType.Is(type, KnownNamespaces.System, "Environment"))
        {
            return !TimeMembers.IsEnvironmentClockMember(member);
        }

        // Directory.GetCurrentDirectory / SetCurrentDirectory — an environment read on the filesystem type.
        if (WellKnownType.Is(type, KnownNamespaces.SystemIO, "Directory"))
        {
            return FilesystemMembers.IsCurrentDirectoryMember(member);
        }

        // Single-argument Path.GetFullPath(path) — impure. The two-argument overload is pure and stays legal.
        if (WellKnownType.Is(type, KnownNamespaces.SystemIO, "Path"))
        {
            return string.Equals(member.Name, GetFullPathMethodName, StringComparison.Ordinal)
                && member is IMethodSymbol { Parameters.Length: 1 };
        }

        // Assembly.Location (covers GetEntryAssembly().Location too, since both read the same property).
        if (WellKnownType.Is(type, KnownNamespaces.SystemReflection, "Assembly"))
        {
            return string.Equals(member.Name, "Location", StringComparison.Ordinal);
        }

        // AppContext.BaseDirectory and AppDomain.CurrentDomain.BaseDirectory.
        if (WellKnownType.Is(type, KnownNamespaces.System, "AppContext") || WellKnownType.Is(type, KnownNamespaces.System, "AppDomain"))
        {
            return string.Equals(member.Name, "BaseDirectory", StringComparison.Ordinal);
        }

        // RuntimeInformation host-description properties (OSDescription, FrameworkDescription, and the like). The
        // IsOSPlatform method is OS branching, owned by AG0009, so only the properties are claimed here.
        if (WellKnownType.Is(type, KnownNamespaces.SystemRuntimeInteropServices, "RuntimeInformation"))
        {
            return member.Kind == SymbolKind.Property;
        }

        return false;
    }
}
