// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a managed callback handed across the native boundary through a marshalled delegate pointer —
/// <c>Marshal.GetFunctionPointerForDelegate</c> or <c>Marshal.GetDelegateForFunctionPointer</c>. A native callback
/// (the macOS <c>evaluatePolicy:localizedReason:reply:</c> completion block) must instead be a static
/// <c>[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]</c> method reached by the address-of operator
/// (<c>&amp;Method</c>), which the compiler verifies has an unmanaged-callable signature and needs no runtime
/// marshalling stub or pinned delegate. A <c>Marshal</c> delegate pointer keeps a managed delegate alive by hand,
/// crosses the boundary through a generated thunk, and can be collected out from under native code — exactly the
/// fragile shape this forbids. The static-and-<c>[UnmanagedCallersOnly]</c> half is enforced by the compiler for an
/// <c>&amp;Method</c> function-pointer conversion; this rule closes the delegate-pointer escape hatch. AG0105 governs
/// the callback body. Preventive; the codebase marshals no delegate today.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NativeCallbackMustUseFunctionPointerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0104";

    private const string Category = "AgentGuard.Architecture";

    private static readonly ImmutableHashSet<string> BannedMarshalMembers = ImmutableHashSet.Create(
        StringComparer.Ordinal, "GetFunctionPointerForDelegate", "GetDelegateForFunctionPointer");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A native callback must be a static [UnmanagedCallersOnly] method reached by &, not a Marshal delegate pointer",
        messageFormat: "Native callback marshalling '{0}' is a delegate pointer; a native callback must be a static [UnmanagedCallersOnly(CallConvs=[CallConvCdecl])] method reached by &Method, not a marshalled delegate",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A managed callback handed across the native boundary must be a static [UnmanagedCallersOnly] method reached by the address-of operator (&Method), never a marshalled delegate pointer via Marshal.GetFunctionPointerForDelegate/GetDelegateForFunctionPointer. A marshalled delegate crosses through a generated thunk and can be collected out from under native code; the &Method function pointer is verified by the compiler and needs no stub. AG0105 governs the callback body.");

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
        if (NativeInteropUse.IsMarshalUse(type)
            && BannedMarshalMembers.Contains(member.Name))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
