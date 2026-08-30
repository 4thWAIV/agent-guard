// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a macOS native-interop (P/Invoke) declaration with a <c>bool</c> return or parameter that is NOT explicitly
/// marshalled as <c>UnmanagedType.I1</c>. The Darwin/Objective-C <c>BOOL</c> and C99 <c>_Bool</c> are one byte (a
/// signed char), so a <c>bool</c> crossing the boundary must be marshalled as <c>I1</c>; the default managed bool
/// marshalling is the 4-byte Win32 <c>BOOL</c>, which reads three bytes of adjacent memory as flags and silently
/// corrupts the result on macOS. Every <c>bool</c> return and parameter of a P/Invoke in the macOS implementation
/// library must carry <c>[return: MarshalAs(UnmanagedType.I1)]</c> / <c>[MarshalAs(UnmanagedType.I1)]</c>. Scoped to
/// the macOS assembly (the Windows bindings correctly use the 4-byte <c>UnmanagedType.Bool</c>). Preventive against the
/// LocalAuthentication <c>evaluatePolicy</c> reply; the existing macOS binding returns <c>long</c>, no <c>bool</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MacOsNativeBoolMustBeI1Analyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0103";

    private const string Category = "AgentGuard.Architecture";
    private const string MarshalAsAttributeName = "MarshalAsAttribute";

    // UnmanagedType.I1 == 3 (the one-byte native bool). Compared against the MarshalAs attribute's first constructor
    // argument, which is the UnmanagedType enum's underlying integral value.
    private const int UnmanagedTypeI1 = 3;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A macOS native bool must be marshalled as UnmanagedType.I1",
        messageFormat: "Native interop '{0}' has a bool {1} without [MarshalAs(UnmanagedType.I1)]; the macOS/Darwin bool is one byte and must be marshalled as I1",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In the macOS implementation library, every bool return and parameter of a P/Invoke declaration must carry [MarshalAs(UnmanagedType.I1)]. The Darwin/Objective-C BOOL and C99 _Bool are one byte; the default managed bool marshalling is the 4-byte Win32 BOOL, which corrupts the result on macOS. The Windows bindings correctly use the 4-byte UnmanagedType.Bool, so this rule is scoped to the macOS assembly.");

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
        // Scoped to the macOS per-OS implementation library: the one-byte bool is a Darwin ABI fact. The Windows and
        // (native-call-free) Linux libraries register nothing.
        if (!PresenceContracts.InMacOs(context.Compilation))
        {
            return;
        }

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!PInvoke.IsPInvoke(method))
        {
            return;
        }

        if (method.ReturnType.SpecialType == SpecialType.System_Boolean
            && !IsMarshalledAsI1(method.GetReturnTypeAttributes()))
        {
            Report(context, method, "return");
        }

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (parameter.Type.SpecialType == SpecialType.System_Boolean
                && !IsMarshalledAsI1(parameter.GetAttributes()))
            {
                Report(context, method, $"parameter '{parameter.Name}'");
            }
        }
    }

    private static bool IsMarshalledAsI1(ImmutableArray<AttributeData> attributes)
    {
        AttributeData? marshalAs = attributes.FirstOrDefault(
            attribute => WellKnownType.Is(
                attribute.AttributeClass, KnownNamespaces.SystemRuntimeInteropServices, MarshalAsAttributeName));

        return marshalAs is not null
            && !marshalAs.ConstructorArguments.IsEmpty
            && marshalAs.ConstructorArguments[0].Value is { } value
            && Convert.ToInt32(value, CultureInfo.InvariantCulture) == UnmanagedTypeI1;
    }

    private static void Report(SymbolAnalysisContext context, IMethodSymbol method, string position)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name, position));
    }
}
