// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a version-reflection read — <c>Assembly.GetEntryAssembly</c>/<c>GetExecutingAssembly</c>/
/// <c>GetCallingAssembly</c>, <c>AssemblyName.Version</c> (the <c>GetName().Version</c> read), or a read of the
/// version attributes <c>AssemblyInformationalVersionAttribute</c>/<c>AssemblyFileVersionAttribute</c> — made
/// anywhere but the single owner class that implements <c>AgentGuard.Abstractions.Contracts.IBuildInfo</c>. The running build's
/// version is ambient: the answer depends on which binary runs, and a <c>dotnet test</c> host reports <c>dotnet</c>,
/// not the guard, so the reads are hidden behind <c>IBuildInfo</c> and a test injects a fixed version
/// (build-version-behind-ibuildinfo). Without this, <c>IBuildInfo</c> is a seam nothing must use and raw version
/// reads creep back (ag0028-version-reads-owned). The one exemption is the class implementing <c>IBuildInfo</c> AND
/// compiled into <c>AgentGuard.Boundaries</c> (managed reflection, OS-uniform); the raw read is a build error in
/// another class of the same assembly and in the same class self-granting from another assembly. Every other type
/// reads the build version through <c>IBuildInfo</c> pulled off <c>ISystemServices</c>. <c>Assembly.Location</c> and
/// the deployment-path reads are AG0012's, not this rule's.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VersionReadsOwnedAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0028";

    private const string Category = "AgentGuard.Architecture";
    private const string AssemblyTypeName = "Assembly";
    private const string AssemblyNameTypeName = "AssemblyName";
    private const string VersionMemberName = "Version";

    // The Assembly reflection entry points that resolve a running assembly to read its version off. Reading the
    // running build's identity this way is what AG0012 leaves to this rule; AG0012 owns Assembly.Location and the
    // deployment-path reads instead.
    private static readonly ImmutableHashSet<string> AssemblyEntryPointMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "GetEntryAssembly",
        "GetExecutingAssembly",
        "GetCallingAssembly");

    // The two version-attribute types whose reflection reads are banned outside the owner: the informational-version
    // attribute (SemVer) and the file-version attribute. Any member read on either is a version read.
    private static readonly ImmutableArray<(string Namespace, string Name)> VersionAttributeTypes = ImmutableArray.Create(
        (KnownNamespaces.SystemReflection, "AssemblyInformationalVersionAttribute"),
        (KnownNamespaces.SystemReflection, "AssemblyFileVersionAttribute"));

    // The owning interface whose single implementing class is the only place a raw version-reflection read is allowed
    // (one-owner-class-per-primitive). Matched structurally by full name against the enclosing type's implemented
    // interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IBuildInfo"));

    // The owner assembly: AgentGuard.Boundaries, where the IBuildInfo owner lives — managed reflection, OS-uniform
    // (owners-live-at-lowest-consumer). Half of the conjunction OwnerClass.IsOwner applies: implementing IBuildInfo in
    // any OTHER assembly does not exempt. Cached once so no per-operation allocation.
    private static readonly Func<Compilation, bool> InOwnerAssembly =
        OwnerClass.InAssembly(BoundaryAssembly.Name);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Version-reflection reads must live only in the class implementing IBuildInfo",
        messageFormat: "Version-reflection read '{0}' is outside the single owner class implementing IBuildInfo; read the build version through IBuildInfo pulled off ISystemServices so a test host reports the guard's version, not dotnet's",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A version-reflection read — Assembly.GetEntryAssembly/GetExecutingAssembly/GetCallingAssembly, AssemblyName.Version, or a read of AssemblyInformationalVersionAttribute/AssemblyFileVersionAttribute — is allowed only in the single class that implements AgentGuard.Abstractions.Contracts.IBuildInfo — not merely somewhere in its assembly. Every other type reads the build version through IBuildInfo on ISystemServices, so a dotnet test host reports the guard's version rather than the test host's. Assembly.Location and the deployment-path reads belong to AG0012.");

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
        // The single owner class that implements IBuildInfo AND compiles into AgentGuard.Boundaries is the only place
        // the raw version read is allowed; everywhere else — a sibling class in the same assembly, or the same class
        // self-granting in another assembly — is RED.
        if (OwnerClass.IsOwner(context, OwningInterfaces, InOwnerAssembly))
        {
            return;
        }

        if (IsVersionRead(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsVersionRead(ISymbol member, INamedTypeSymbol type)
    {
        // Assembly.GetEntryAssembly / GetExecutingAssembly / GetCallingAssembly — the reflection entry points that
        // resolve the running assembly to read a version off.
        if (WellKnownType.Is(type, KnownNamespaces.SystemReflection, AssemblyTypeName))
        {
            return AssemblyEntryPointMethods.Contains(member.Name);
        }

        // AssemblyName.Version — the GetName().Version read.
        if (WellKnownType.Is(type, KnownNamespaces.SystemReflection, AssemblyNameTypeName))
        {
            return string.Equals(member.Name, VersionMemberName, StringComparison.Ordinal);
        }

        // A read of any member on the informational-version or file-version attribute — the reflected version value.
        return WellKnownType.IsAnyOf(type, VersionAttributeTypes);
    }
}
