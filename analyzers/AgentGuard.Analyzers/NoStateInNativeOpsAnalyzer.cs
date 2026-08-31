// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports ANY stored state in a per-OS native-OPS owner — a class implementing <c>IObjCRuntime</c> (macOS assembly) or one of
/// the Windows native-ops interfaces (<c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c>). The coverage refactor keeps the native-ops class
/// FULLY STATELESS: every native handle is created per call and released, and no per-call state survives on the object,
/// so nothing that a test would need to observe hides behind the one class we do not fake. Stored state is a build error
/// regardless of spelling — and the implicitly-backed spellings are exactly three, all caught here: an instance or static
/// field (of any type — not merely a native handle, which is the narrower AG0112), an auto-property of ANY accessor shape
/// (get-only OR settable, whose compiler-synthesized backing field is implicitly declared and so never surfaces to the
/// field symbol action — a field-only scan would miss it; a get-only auto-property with an initializer, e.g.
/// <c>IntPtr Handle { get; } = objc_getClass(...);</c>, caches construction-time state exactly like a field), OR a
/// field-like event (<c>event EventHandler Changed;</c>, whose compiler-synthesized delegate-list backing field is
/// likewise implicitly declared and holds subscribable state a field-only scan would miss).
/// A <c>const</c> is exempt because it is a compile-time literal that holds no live state (the native library paths,
/// selector names, and error codes the class needs); an expression-bodied property holds no backing field, a
/// manually-implemented property's storage is an explicit field the field scan already catches, and a custom event with
/// explicit add/remove stores its subscriber list in an explicit field the field scan already catches (so the event
/// action must not double-report it). This is the broader superset of AG0112 (no cached native auth-context handle)
/// scoped to the native-ops layer: AG0112 still guards the flow ports against a cached handle, and this rule forbids the
/// native-ops class every field. Its field/property/event scans share the symbol-kind-agnostic
/// <see cref="FieldOwnershipScan"/> skeleton AG0112 also uses (gate to a native-ops owner, apply the predicate, report by
/// name). Those three member scans see only the implementer's OWN members, so state declared on a BASE CLASS would be
/// invisible to them; a fourth action on the implementer TYPE closes that loophole structurally by requiring a native-ops
/// implementer to derive directly from <c>System.Object</c> — a non-object base type is reported (no base class means
/// nowhere upstream for state to hide, so no hierarchy walk is needed).
/// The native-ops class stays behind its interface and DI-mockable; this rule constrains only its implementation
/// contents. Its symbol actions register through the shared <see cref="PresenceContracts.RegisterInMacOsOrWindows"/>
/// gate (exactly mirroring AG0106/AG0113/AG0115), so it fires ONLY inside the macOS and Windows PRODUCTION assemblies
/// and is silent in every other compilation — including <c>AgentGuard.CrossPlatform.Tests</c>, where the stateful
/// native-ops fakes legitimately hold state through the verified InternalsVisibleTo grants (a test fake CAN implement
/// the internal native-ops interface, so the internal-interface visibility alone would not keep the scan off it).
/// Preventive; no native-ops owner exists yet, so it compiles clean and its RED is proven by a fixture.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoStateInNativeOpsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0116";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A native-ops implementer must be stateless — no field, auto-property, or field-like event",
        messageFormat: "'{0}' holds state in a native-ops implementer; the native-ops class must be fully stateless — create every native handle per call and hold no field, auto-property, or field-like event (a const is exempt)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class implementing a per-OS native-ops interface (IObjCRuntime, IWindowsHelloNativeOps, ICredentialPromptNativeOps) must be fully stateless: no instance or static field of any type, no auto-property of any accessor shape (get-only OR settable, whose compiler-synthesized backing field is implicitly declared and would slip past a field-only scan — a get-only auto-property with an initializer caches construction-time state exactly like a field), and no field-like event (whose synthesized delegate-list backing field is likewise implicitly declared and would slip past a field-only scan while holding subscribable state), so no per-call native handle or state survives on the object behind the one class that is not faked. A const is exempt (a compile-time literal for the native library paths, selector names, and error codes); an expression-bodied property holds no backing field, a manually-implemented property's storage is the explicit field the field scan already catches, and a custom event with explicit add/remove stores its subscriber list in the explicit field the field scan already catches. This is the broader superset of AG0112 (which forbids a cached native auth-context handle field in a flow port) scoped to the native-ops layer, sharing the same field-scan skeleton. The native-ops class stays behind its interface and DI-mockable; this rule constrains only its implementation contents.");

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // The native-ops owners live only in the macOS and Windows per-OS PRODUCTION assemblies; the shared
        // gate-then-register helper (owned once next to PresenceContracts.InMacOsOrWindows, mirroring AG0106/AG0113/
        // AG0115) skips every other compilation — including AgentGuard.CrossPlatform.Tests, where the stateful native-ops
        // fakes (composing SingleCallRecorder for per-method call counts) legitimately hold state through the verified
        // InternalsVisibleTo grants. Without this gate the field/property scan would fire on those test fakes, because a
        // test fake CAN implement the internal native-ops interface via InternalsVisibleTo.
        PresenceContracts.RegisterInMacOsOrWindows(context, Register);
    }

    private static void Register(CompilationStartAnalysisContext context)
    {
        // AG0116's field/property/event actions see only the implementer's OWN members, so state declared on a BASE
        // CLASS is invisible to them and would pass clean. A native-ops implementer must therefore derive directly from
        // System.Object: this type action reports any native-ops implementer whose base is not object, closing that
        // loophole structurally — no base class means nowhere upstream for state to hide — without walking the hierarchy.
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);

        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);

        // An auto-property of ANY accessor shape (get-only OR settable) stores state in a compiler-synthesized backing
        // field that is implicitly declared and therefore never surfaces to the field action above, so a separate property
        // action catches it — stored state is a violation regardless of spelling (a field or an auto-property). A get-only
        // auto-property is not exempt: its backing field can cache construction-time state (e.g. a native handle from an
        // initializer), exactly the state this rule and AG0112 exist to prevent.
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);

        // A field-like event (`event EventHandler Changed;`) holds a subscribable delegate list in a compiler-synthesized
        // backing field that is likewise implicitly declared and never surfaces to the field action above, so a separate
        // event action catches it — the third and last implicitly-backed stored-state spelling, closing the set (non-const
        // field, settable auto-property, field-like event). A custom event with explicit add/remove stores its subscriber
        // list in an explicit field the field action already reports, so the event action must not double-report it.
        context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
    }

    // A native-ops implementer must derive directly from System.Object. The member actions below see only the
    // implementer's OWN members, so state declared on a BASE CLASS is invisible to them; forbidding any base class
    // closes that loophole structurally (no base class means nowhere upstream for state to hide) without a hierarchy
    // walk. This keys off the TYPE symbol itself, not its containing type, so it does not route through the member-scan
    // skeleton (which gates on ContainingType); it reports the implementer class by name.
    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!OwnerClass.Implements(type, PresenceContracts.AllNativeOpsOwners))
        {
            return;
        }

        if (type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
    }

    // The native-ops owner gate and report-by-name are the shared symbol-scan skeleton (FieldOwnershipScan, spelled once
    // and used by AG0112 too). The field action routes through the const-skipping ReportField wrapper and reports EVERY
    // non-const field, so its predicate is always true.
    private static void AnalyzeField(SymbolAnalysisContext context) =>
        FieldOwnershipScan.ReportField(context, PresenceContracts.AllNativeOpsOwners, static _ => true, Rule);

    // Only an AUTO-property synthesizes a hidden backing field the field scan cannot see (a manually-implemented
    // property's storage is an explicit field the field scan already catches, so reporting the property too would
    // double-count). The synthesized backing field is the reportability predicate on the same shared skeleton.
    private static void AnalyzeProperty(SymbolAnalysisContext context) =>
        FieldOwnershipScan.Report<IPropertySymbol>(context, PresenceContracts.AllNativeOpsOwners, HasSynthesizedBackingField, Rule);

    // Only a FIELD-LIKE event synthesizes a hidden delegate-list backing field the field scan cannot see — the event
    // analogue of the auto-property check (a custom event with explicit add/remove stores its subscriber list in an
    // explicit field the field scan already catches). Field-likeness is the reportability predicate on the same skeleton.
    private static void AnalyzeEvent(SymbolAnalysisContext context) =>
        FieldOwnershipScan.Report<IEventSymbol>(context, PresenceContracts.AllNativeOpsOwners, IsFieldLikeEvent, Rule);

    // A field-like event holds its subscriber list in a compiler-synthesized delegate-list backing field, exactly as an
    // auto-property holds its value in a compiler-synthesized backing field — both implicitly declared and so invisible to
    // the field symbol action. The two need different Roslyn signals, though: an auto-property's synthesized backing field
    // DOES surface through GetMembers (below), but a field-like event's does NOT, so it is detected by the tell that the
    // compiler ALSO synthesizes for it — implicitly-declared add/remove accessors (a custom event declares its own).
    private static bool IsFieldLikeEvent(IEventSymbol eventSymbol) =>
        eventSymbol.AddMethod is { IsImplicitlyDeclared: true };

    private static bool HasSynthesizedBackingField(IPropertySymbol property)
    {
        foreach (ISymbol candidate in property.ContainingType.GetMembers())
        {
            if (candidate is IFieldSymbol field
                && field.IsImplicitlyDeclared
                && SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property))
            {
                return true;
            }
        }

        return false;
    }
}
