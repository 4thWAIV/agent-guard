// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a guarded construction made outside its one designated site — the consolidated construction rule the
/// bridge folds three overlapping patterns into. Three guarded constructions, each with its own diagnostic id and site:
/// <list type="bullet">
/// <item><b>AG0017</b> — a call to a static factory that hands back the <c>ISystemServices</c> container (the literal
/// <c>SystemServices.Create()</c> AND any sibling static factory whose RETURN TYPE is <c>ISystemServices</c>, so a
/// <c>CreateDefault()</c>/<c>Build()</c> cannot bypass the wall by not being named <c>Create</c>), legal only at the
/// two composition callers (<c>Program</c> and the test <c>SystemServicesBuilder</c>). The container is built once at
/// the top and threaded down by constructor injection, so there is exactly one place to mock;</item>
/// <item><b>AG0033</b> — a call to a static factory whose RETURN TYPE is an owned <c>IFileInfo</c>/<c>IDirectoryInfo</c>
/// wrapper interface (<c>AbstractedFileInfo</c>/<c>AbstractedDirectoryInfo.Create</c> return exactly these), legal only
/// inside <c>FileInfoFactory</c>. Matching by the return type — the same shape as AG0017's container pin — closes the
/// rule to modification: a renamed or additional <c>IFileInfo</c>/<c>IDirectoryInfo</c> wrapper with its own static
/// factory is caught with no edit here (Open/Closed), where a hard-coded wrapper-name list could not. This keeps every
/// caller on the mockable factory seam. An INSTANCE method returning <c>IFileInfo</c> (<c>IFileSystem.GetFileInfo</c>)
/// is not static and is never flagged, and the wrapper's own <c>new</c> inside its factory is not a static call;</item>
/// <item><b>AG0027</b> — an <see cref="IObjectCreationOperation"/> of the shared copy-on-write overlay
/// <c>InMemoryFileSystemStore</c> (matched by namespace + name in <c>AgentGuard.TestHelpers</c>), gated to the
/// <c>AgentGuard.TestHelpers</c> compilation and legal only inside <c>SystemServicesBuilder</c>
/// (store-single-construction-rule). The store is <c>internal</c> to TestHelpers with no <c>InternalsVisibleTo</c> to
/// the <c>.Tests</c> projects, so accessibility already blocks a test from constructing it; this rule closes the
/// residual within-TestHelpers path, so the filesystem fake and the platform fake are guaranteed the SAME overlay
/// instance by construction — a <c>new InMemoryFileSystemStore(...)</c> anywhere in TestHelpers other than the builder
/// is a build error. The store type ships in the IMPLEMENT phase, so this is preventive today (nothing constructs it).</item>
/// </list>
/// All three are the same shape: a construction that must happen at exactly one site, a build error elsewhere rather
/// than a matter of discipline. The designated sites live in <see cref="CompositionPoint"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GuardedConstructionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier for a container-factory call outside the composition callers.
    /// </summary>
    public const string ContainerDiagnosticId = "AG0017";

    /// <summary>
    /// The diagnostic identifier for a wrapper construction outside <c>FileInfoFactory</c>.
    /// </summary>
    public const string WrapperDiagnosticId = "AG0033";

    /// <summary>
    /// The diagnostic identifier for an <c>InMemoryFileSystemStore</c> construction outside <c>SystemServicesBuilder</c>.
    /// </summary>
    public const string StoreDiagnosticId = "AG0027";

    private const string Category = "AgentGuard.Architecture";

    // The shared copy-on-write overlay type AgentGuard.TestHelpers.InMemoryFileSystemStore. AG0027 pins its construction
    // to SystemServicesBuilder within the TestHelpers compilation (store-single-construction-rule). Matched by
    // namespace + name through WellKnownType; the namespace and the assembly are both "AgentGuard.TestHelpers"
    // (TestAssembly.TestHelpersName), the same string CompositionPoint anchors SystemServicesBuilder on. The type is
    // built in IMPLEMENT, so this is preventive today.
    private const string StoreTypeName = "InMemoryFileSystemStore";

    // The two owned wrapper interfaces AgentGuard.Abstractions.Contracts.IFileInfo/IDirectoryInfo. AG0033 pins any
    // static factory whose RETURN TYPE is one of these to FileInfoFactory (ag0033-wrapper-construction-lock, "same shape
    // as AG0017"): AbstractedFileInfo/AbstractedDirectoryInfo.Create return exactly these, so the real wrappers are
    // caught, while matching by the OWNED ABSTRACTION — not a hard-coded wrapper-name list — keeps the rule closed to
    // modification when a wrapper is renamed or added. The interfaces do not exist until the IMPLEMENT phase adds them,
    // so this rule is preventive today (nothing returns them) and starts firing the moment a wrapper factory is called
    // off-seam. The combined (namespace, name) set comes from OwnedPrimitives.InfoWrapperInterfaces — the single owner of
    // the *Info wrapper mapping, shared with AG0034 — so the IFileInfo/IDirectoryInfo union is spelled exactly once.
    private static readonly ImmutableArray<(string Namespace, string Name)> WrapperInterfaces =
        OwnedPrimitives.InfoWrapperInterfaces;

    // The one owned container interface AgentGuard.Abstractions.Contracts.ISystemServices. AG0017 pins any static
    // factory whose RETURN TYPE is this to the composition callers (ag0017-edit-factory-by-return-type). Declared as a
    // single-entry candidate list — the same shape as WrapperInterfaces above — so both guarded constructions share the
    // one IsStaticFactoryReturningAnyOf predicate and the IsStatic + return-type match lives exactly once in this file.
    private static readonly ImmutableArray<(string Namespace, string Name)> ContainerInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, BoundaryServices.ContainerName));

    private static readonly DiagnosticDescriptor ContainerRule = new(
        id: ContainerDiagnosticId,
        title: "SystemServices.Create must be called only at the single composition point",
        messageFormat: "'{0}' is called outside the single Program composition method and SystemServicesBuilder; receive ISystemServices by constructor injection instead of reconstructing it",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "SystemServices.Create() may be called only from the single Program composition method and the test SystemServicesBuilder. Everywhere else receives ISystemServices by constructor injection, so the container is built once and there is exactly one place to mock; reconstructing it deep in the graph is a build error.");

    private static readonly DiagnosticDescriptor WrapperRule = new(
        id: WrapperDiagnosticId,
        title: "An *Info wrapper must be constructed only inside FileInfoFactory",
        messageFormat: "Wrapper construction '{0}' is outside FileInfoFactory; obtain an IFileInfo/IDirectoryInfo through IFileSystem so every caller stays on the mockable factory seam",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An AbstractedFileInfo/AbstractedDirectoryInfo wrapper may be constructed only inside FileInfoFactory. Every other type obtains an IFileInfo/IDirectoryInfo through IFileSystem (which delegates to the factory), so the wrappers stay behind one mockable seam; constructing one elsewhere is a build error.");

    private static readonly DiagnosticDescriptor StoreRule = new(
        id: StoreDiagnosticId,
        title: "The InMemoryFileSystemStore overlay must be constructed only inside SystemServicesBuilder",
        messageFormat: "Store construction '{0}' is outside SystemServicesBuilder; both fakes must share the one overlay instance the builder constructs, so build them through SystemServicesBuilder instead of constructing a second store",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The shared copy-on-write overlay InMemoryFileSystemStore may be constructed only inside SystemServicesBuilder, within the AgentGuard.TestHelpers compilation. The filesystem fake and the platform fake both take the SAME overlay by constructor, so a second construction anywhere else in TestHelpers would rebuild the two-store wall the shared overlay exists to remove; constructing one outside the builder is a build error. The store is internal with no InternalsVisibleTo to the .Tests projects, so accessibility already blocks tests from constructing it — this rule closes the residual within-TestHelpers path.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules =
        ImmutableArray.Create(ContainerRule, WrapperRule, StoreRule);

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
        // AG0017: a container-factory call is legal only at the composition callers (Program + builder).
        if (IsContainerFactory(member))
        {
            if (!CompositionPoint.Encloses(context.ContainingSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ContainerRule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
            }

            return;
        }

        // AG0033: a wrapper-factory call is legal only inside FileInfoFactory.
        if (IsWrapperFactoryCall(member) && !CompositionPoint.EnclosesWrapperFactory(context.ContainingSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                WrapperRule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
            return;
        }

        // AG0027: constructing the shared overlay InMemoryFileSystemStore is legal only inside SystemServicesBuilder,
        // and only in the AgentGuard.TestHelpers compilation (store-single-construction-rule). The composition-point
        // walk (CompositionPoint.Encloses) reduces to "inside SystemServicesBuilder" here: the only OTHER composition
        // caller it admits, Program, is anchored to the "guard" assembly and so never matches within the TestHelpers
        // compilation this branch is gated to.
        if (IsStoreConstruction(context.Operation, type)
            && string.Equals(context.Compilation.AssemblyName, TestAssembly.TestHelpersName, StringComparison.Ordinal)
            && !CompositionPoint.Encloses(context.ContainingSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StoreRule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsContainerFactory(ISymbol member)
    {
        // Pin the container bypass by RETURN TYPE alone: ANY static method whose return type is the ISystemServices
        // container fires (ag0017-edit-factory-by-return-type). This single check subsumes the literal
        // SystemServices.Create() (its return type IS the container) AND any differently-named sibling factory
        // (CreateDefault/Build), so nothing bypasses the wall by not being named Create; and a decoy SystemServices in
        // an unrelated assembly whose Create() does not return the container no longer spuriously fires (the deleted
        // name-only path did). The return type is matched through the one type-identity owner WellKnownType (namespace +
        // name), never a bare name, via the shared IsStaticFactoryReturningAnyOf predicate.
        return IsStaticFactoryReturningAnyOf(member, ContainerInterfaces);
    }

    private static bool IsWrapperFactoryCall(ISymbol member)
    {
        // A static method whose RETURN TYPE is one of the owned wrapper interfaces (IFileInfo/IDirectoryInfo in
        // AgentGuard.Abstractions.Contracts) — the SAME shape as AG0017's container pin (a static method whose return
        // type is the guarded abstraction), so both share IsStaticFactoryReturningAnyOf. Matching by the return type,
        // not a hard-coded wrapper-name list, closes AG0033 to modification: a renamed or additional
        // IFileInfo/IDirectoryInfo wrapper with its own static factory (AbstractedFileInfo/AbstractedDirectoryInfo.Create)
        // is caught without editing this rule. The return type is matched through the type-identity owner WellKnownType
        // (namespace + name), so a same-named decoy interface in another namespace is not the guarded abstraction and
        // never fires. The wrapper's own private constructor is not static, so the internal `new` inside the factory is
        // never matched; and IFileSystem.GetFileInfo is an INSTANCE method returning IFileInfo, so it is not caught.
        return IsStaticFactoryReturningAnyOf(member, WrapperInterfaces);
    }

    // The one shared predicate for both guarded constructions: a static method whose RETURN TYPE is any of the given
    // owned abstractions. The IsStatic guard and the return-type identity match (through WellKnownType.IsAnyOf, which
    // keys on namespace + name) live here exactly once; AG0017 passes ContainerInterfaces and AG0033 passes
    // WrapperInterfaces.
    private static bool IsStaticFactoryReturningAnyOf(
        ISymbol member, ImmutableArray<(string Namespace, string Name)> candidates)
        => member is IMethodSymbol { IsStatic: true } method
            && WellKnownType.IsAnyOf(method.ReturnType as INamedTypeSymbol, candidates);

    // AG0027: an object creation whose created TYPE is the shared overlay AgentGuard.TestHelpers.InMemoryFileSystemStore,
    // matched by namespace + name through the type-identity owner WellKnownType. The IObjectCreationOperation gate is
    // what makes this a CONSTRUCTION branch (the AG0017/AG0033 static-factory calls are invocations, not creations, so
    // the three branches are disjoint); the created type is the constructor's containing type MemberUseScanner already
    // resolved into `type`.
    private static bool IsStoreConstruction(IOperation operation, INamedTypeSymbol type)
        => operation is IObjectCreationOperation
            && WellKnownType.Is(type, TestAssembly.TestHelpersName, StoreTypeName);
}
