// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a member on <c>ISystemServices</c> that is neither a service accessor nor the clock — the guard of the
/// guard for the derived service set (derive-service-set-from-isystemservices). AG0024/AG0025/AG0031 no longer read a
/// hand-maintained service list; they derive it by walking <c>ISystemServices</c> for its SERVICE ACCESSORS (a
/// property, or a zero-parameter method, whose value type is a <c>Contracts</c> interface). This rule keeps that walk
/// complete: every member of the container must be a service accessor OR the one deliberately-named non-interface
/// service, the <c>System.TimeProvider</c> clock. Any other member — a field, a parameterized factory that returns a
/// <c>Contracts</c> interface (a factory, not an accessor), or a member whose type is not a service — is a build error,
/// so the container surface can never silently outgrow the walk and leave a new service ungoverned (fail-closed). The
/// check is scoped to <c>ISystemServices</c> only, and only in the assembly that DECLARES it, so the diagnostic lands
/// on the source member; sub-containers such as <c>IFileSystem</c> legitimately mix accessors and parameterized
/// factories and are not checked. It passes on the current well-formed container, so it is preventive today, like
/// AG0033. What counts as a service accessor or the clock is the single definition shared with the derivation walk in
/// <see cref="BoundaryServices"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemServicesMemberMustBeServiceAccessorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0034";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Every ISystemServices member must be a service accessor or the clock",
        messageFormat: "Member '{0}' on ISystemServices is neither a service accessor (a property or no-argument method returning an AgentGuard.Abstractions.Contracts interface) nor the TimeProvider clock; the derived service set can only see service accessors, so expose the service through one or it goes ungoverned",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every member of ISystemServices must be a service accessor (a property or zero-parameter method whose value type is an AgentGuard.Abstractions.Contracts interface) or the System.TimeProvider clock. The static-holder (AG0024), one-owner (AG0025), and no-service-parameter (AG0031) rules derive their service set by walking these accessors; a field, a parameterized factory returning a Contracts interface, or a member of a non-service type would add surface the walk cannot see, leaving a service ungoverned, so it is a build error. The rule is scoped to ISystemServices in its declaring assembly.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

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
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        INamedTypeSymbol? container = WellKnownType.Resolve(
            context.Compilation, KnownNamespaces.AgentGuardAbstractionsContracts, BoundaryServices.ContainerName);

        // Guard the container only where it is DECLARED, so the diagnostic lands on the source member: a referencing
        // assembly sees the same interface from metadata and would have no source location to report at. Absent the
        // container (a compilation that does not define it) there is nothing to keep honest.
        if (container is null
            || !WellKnownType.IsDeclaredInCompilation(container, context.Compilation))
        {
            return;
        }

        // Walk the SAME surface the derivation walks — the container's own members PLUS every interface it extends
        // (BoundaryServices.SurfaceMembers), the one definition of the ISystemServices surface — so an off-convention
        // member inherited from a base interface cannot escape this guard.
        foreach (ISymbol member in BoundaryServices.SurfaceMembers(container))
        {
            if (!IsGovernableMember(member))
            {
                continue;
            }

            // Only a member DECLARED in this compilation has a source location to report at; an inherited member
            // declared in another assembly (metadata) does not. Mirror the container-declaring-assembly scoping above,
            // applied per member.
            if (!WellKnownType.IsDeclaredInCompilation(member.ContainingType, context.Compilation))
            {
                continue;
            }

            if (BoundaryServices.ServiceAccessorInterface(member) is null && !BoundaryServices.IsClockAccessor(member))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.Locations[0], member.Name));
            }
        }
    }

    // The container's own declared API surface: a property, an event, a field, or an ordinary method. A property's
    // get/set accessor methods (and an event's add/remove) are compiler-associated companions of a member already
    // checked, so they are skipped to report each off-convention member exactly once.
    private static bool IsGovernableMember(ISymbol member)
    {
        return member switch
        {
            IMethodSymbol method => method.MethodKind == MethodKind.Ordinary,
            IPropertySymbol => true,
            IFieldSymbol => true,
            IEventSymbol => true,
            _ => false,
        };
    }
}
