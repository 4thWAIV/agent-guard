// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports an off-convention member on ANY container node in the <c>ISystemServices</c> tree — the guard of the guard
/// for the derived service set (derive-service-set-from-isystemservices, ag0034-recurses-every-container). AG0024/
/// AG0025/AG0031 derive their service set by walking the container tree for its SERVICE ACCESSORS (a property, or a
/// zero-parameter method, whose value type is a <c>Contracts</c> interface); AG0019 mirrors that same tree against the
/// builder. This rule keeps every level of the walk complete by recursing the ONE tree
/// <see cref="BoundaryServices.ResolveTree"/> builds and guarding each node's surface: at the ROOT, every member must
/// be a service accessor or the one deliberately-named non-interface service, the <c>System.TimeProvider</c> clock; at
/// a SUB-CONTAINER (<c>IFileSystem</c>, <c>IPlatformServices</c>, any future one), every member must be a service
/// accessor or an owned per-path <c>*Info</c> factory (a member whose return type is <c>IFileInfo</c>/
/// <c>IDirectoryInfo</c>, reused from <see cref="OwnedPrimitives"/>). Any other member — a field, a stray parameterized
/// factory, or a member of a non-service type — is a build error, so a sub-container that grew an off-convention member
/// the walk cannot classify can never silently break the "mirror at every level" guarantee or the AG0025/AG0031
/// coverage of that level (fail-closed). Each node is guarded only in the assembly that DECLARES it, so the diagnostic
/// lands on the source member. It passes on the current well-formed container, so it is preventive today. What counts
/// as a service accessor or the clock is the single definition shared with the walk in <see cref="BoundaryServices"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemServicesMemberMustBeServiceAccessorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0034";

    private const string Category = "AgentGuard.Architecture";

    // The owned per-path *Info factory return types allowed on a SUB-CONTAINER — IFileInfo/IDirectoryInfo — reused from
    // OwnedPrimitives.InfoWrapperInterfaces (the single combined union, shared with AG0033) so the wrapper-interface
    // identities and the AddRange are spelled exactly once (fileinfo-abstraction-stays-in-ag0011).
    private static readonly ImmutableArray<(string Namespace, string Name)> InfoFactoryReturnTypes =
        OwnedPrimitives.InfoWrapperInterfaces;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Every container-node member must be a service accessor (or the clock at the root, or an *Info factory at a sub-container)",
        messageFormat: "Member '{0}' on container '{1}' is neither a service accessor (a property or no-argument method returning an AgentGuard.Abstractions.Contracts interface){2}; the derived service set can only see service accessors, so expose the service through one or it goes ungoverned",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every member of every container node in the ISystemServices tree must be a service accessor (a property or zero-parameter method whose value type is an AgentGuard.Abstractions.Contracts interface), plus — at the root — the System.TimeProvider clock, or — at a sub-container — an owned per-path *Info factory (returning IFileInfo/IDirectoryInfo). The static-holder (AG0024), one-owner (AG0025), and no-service-parameter (AG0031) rules derive their service set by walking these accessors, and AG0019 mirrors the same tree against the builder; a field, a stray parameterized factory, or a member of a non-service type would add surface the walk cannot see, leaving a service ungoverned, so it is a build error. Each container node is guarded in its declaring assembly.",
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
        ContainerNode? root = BoundaryServices.ResolveTree(context.Compilation);

        // Guard the tree only where the root container is DECLARED, so the diagnostic lands on the source member: a
        // referencing assembly sees the same interfaces from metadata and would have no source location to report at.
        // Absent the container (a compilation that does not define it) there is nothing to keep honest.
        if (root is null
            || !WellKnownType.IsDeclaredInCompilation(root.Interface, context.Compilation))
        {
            return;
        }

        GuardNode(context, root, isRoot: true);
    }

    // Guard one container node's surface, then recurse into each nested container. A member is allowed when it is a
    // service accessor anywhere, the clock at the root, or an owned *Info factory at a sub-container; anything else is
    // reported. Only a member DECLARED in this compilation has a source location to report at.
    private static void GuardNode(CompilationAnalysisContext context, ContainerNode node, bool isRoot)
    {
        foreach (ISymbol member in BoundaryServices.SurfaceMembers(node.Interface))
        {
            if (!IsGovernableMember(member))
            {
                continue;
            }

            if (!WellKnownType.IsDeclaredInCompilation(member.ContainingType, context.Compilation))
            {
                continue;
            }

            if (!IsAllowedMember(member, isRoot))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule, member.Locations[0], member.Name, node.Interface.Name, ExtraAllowanceClause(isRoot)));
            }
        }

        foreach (ServiceAccessor accessor in node.Accessors)
        {
            if (accessor.Kind == ServiceAccessorKind.Container && accessor.Child is not null)
            {
                GuardNode(context, accessor.Child, isRoot: false);
            }
        }
    }

    // A member is allowed when it is a service accessor (anywhere), the TimeProvider clock (root only), or an owned
    // per-path *Info factory returning IFileInfo/IDirectoryInfo (sub-container only). The clock is a root-only service
    // type; the *Info factories live only on the filesystem sub-container.
    private static bool IsAllowedMember(ISymbol member, bool isRoot)
    {
        if (BoundaryServices.ServiceAccessorInterface(member) is not null)
        {
            return true;
        }

        return isRoot ? BoundaryServices.IsClockAccessor(member) : IsInfoFactory(member);
    }

    private static bool IsInfoFactory(ISymbol member)
    {
        return WellKnownType.IsAnyOf(BoundaryServices.ProducedType(member) as INamedTypeSymbol, InfoFactoryReturnTypes);
    }

    // The message tail naming the level-specific allowance, so the diagnostic reads correctly at the root and at a
    // sub-container.
    private static string ExtraAllowanceClause(bool isRoot)
    {
        return isRoot
            ? " nor the TimeProvider clock"
            : " nor an owned per-path *Info factory (returning IFileInfo/IDirectoryInfo)";
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
