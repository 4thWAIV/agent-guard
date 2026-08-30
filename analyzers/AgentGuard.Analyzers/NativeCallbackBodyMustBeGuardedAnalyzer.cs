// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a native callback whose body is not a single <c>try/catch(Exception)</c>. A native callback is either a method
/// or static local function carrying <c>[UnmanagedCallersOnly]</c> (the macOS <c>evaluatePolicy:localizedReason:reply:</c>
/// completion block), OR a method that implements a member of a COM-visible interface (an interface carrying
/// <c>[ComVisible(true)]</c>) — the Windows-COM completed-handler shape the coverage refactor adds coverage for: the
/// WinRT <c>IAsyncOperationCompletedHandler</c> that WinRT invokes back through a COM-callable wrapper when Hello
/// verification finishes. In both cases native code invokes the managed method directly, and an exception that unwinds out
/// of it crosses the native frame and corrupts the runtime; the callback's whole body must therefore be one <c>try</c>
/// that catches every managed exception (a bare <c>catch</c> or <c>catch (Exception)</c>) and returns a native error code
/// or completes fail-closed instead of propagating. C# permits <c>[UnmanagedCallersOnly]</c> on exactly two declaration
/// forms (CS8896: a static ordinary member method or a static local function), so this rule inspects both; the COM-visible
/// interface implementation is an ordinary instance method, so it is inspected on the method path only (a local function
/// cannot implement an interface). The static-and-<c>[UnmanagedCallersOnly]</c> shape is AG0104's / the compiler's; this
/// rule governs the body. The existing macOS reply block and Windows completed handler already comply, so this is
/// preventive; a non-guarded native callback of either shape is reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NativeCallbackBodyMustBeGuardedAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0105";

    private const string Category = "AgentGuard.Architecture";

    /// <summary>
    /// The (namespace, name) identity of <c>System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute</c> — the one
    /// attribute this rule recognizes. Single owner of the identity; passed to <see cref="AttributeIdentity.IsAnyOf"/>
    /// so a user-declared <c>UnmanagedCallersOnly</c> attribute in another namespace is not mistaken for the BCL one.
    /// </summary>
    private static readonly ImmutableArray<(string Namespace, string Name)> UnmanagedCallersOnlyAttribute =
        ImmutableArray.Create((KnownNamespaces.SystemRuntimeInteropServices, "UnmanagedCallersOnlyAttribute"));

    /// <summary>
    /// The (namespace, name) identity of <c>System.Runtime.InteropServices.ComVisibleAttribute</c> — the attribute that,
    /// carried on an interface with the argument <see langword="true"/>, marks it as one the CLR exposes to native COM
    /// through a COM-callable wrapper. A method implementing a member of such an interface is a native callback (native
    /// code invokes it through the wrapper's vtable), so its body is governed exactly as an <c>[UnmanagedCallersOnly]</c>
    /// body is. Resolved by full name so a same-named attribute in another namespace does not fire.
    /// </summary>
    private static readonly ImmutableArray<(string Namespace, string Name)> ComVisibleAttribute =
        ImmutableArray.Create((KnownNamespaces.SystemRuntimeInteropServices, "ComVisibleAttribute"));

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "An [UnmanagedCallersOnly] callback body must be one try/catch(Exception) returning a native error code",
        messageFormat: "Native callback '{0}' body is not a single try/catch(Exception); its whole body must be one try that catches every managed exception and returns a native error code, never letting an exception unwind across the native frame",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A method carrying [UnmanagedCallersOnly] is invoked directly by native code; an exception that unwinds out of it corrupts the runtime. Its whole body must be a single try statement whose catch handles every managed exception (a bare catch or catch (Exception)) and returns a native error code instead of propagating. AG0104 and the compiler enforce the static-and-[UnmanagedCallersOnly] shape; this rule governs the body.");

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

        // C# permits [UnmanagedCallersOnly] on exactly two declaration forms (CS8896: a static ordinary member method or
        // a static local function), so both are registered. MethodDeclarationSyntax and LocalFunctionStatementSyntax
        // share no base exposing AttributeLists/Body/ExpressionBody/Identifier, so each entry point extracts those four
        // pieces off its own node and hands them to the ONE shared core (AnalyzeCallbackBody) — the guard logic is spelled
        // once for both forms.
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        // A method is a native callback when it carries [UnmanagedCallersOnly] OR implements a member of a COM-visible
        // interface (the Windows-COM completed-handler shape). Only a method can implement an interface member, so the
        // COM check is on this path; the local-function path never is one.
        bool isNativeCallback =
            HasUnmanagedCallersOnly(context, method.AttributeLists) || ImplementsComVisibleInterfaceMember(context, method);
        AnalyzeCallbackBody(context, isNativeCallback, method.Body, method.ExpressionBody, method.Identifier);
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
    {
        var localFunction = (LocalFunctionStatementSyntax)context.Node;

        // A local function cannot implement an interface, so only [UnmanagedCallersOnly] makes it a native callback.
        AnalyzeCallbackBody(
            context, HasUnmanagedCallersOnly(context, localFunction.AttributeLists), localFunction.Body, localFunction.ExpressionBody, localFunction.Identifier);
    }

    private static void AnalyzeCallbackBody(
        SyntaxNodeAnalysisContext context,
        bool isNativeCallback,
        BlockSyntax? body,
        ArrowExpressionClauseSyntax? expressionBody,
        SyntaxToken identifier)
    {
        if (!isNativeCallback)
        {
            return;
        }

        // A partial/extern declaration with no body of its own is not the callback body; only a written body is
        // governed. An expression body (=> ...) can never be a try/catch, so it is always reported.
        if (body is null)
        {
            if (expressionBody is not null)
            {
                Report(context, identifier);
            }

            return;
        }

        if (!IsSingleGuardedTry(context, body))
        {
            Report(context, identifier);
        }
    }

    private static bool HasUnmanagedCallersOnly(SyntaxNodeAnalysisContext context, SyntaxList<AttributeListSyntax> attributeLists)
    {
        // Match [UnmanagedCallersOnly] by RESOLVED type through the shared resolver — this is a SyntaxNode analyzer,
        // so context.SemanticModel is available — so a user attribute of the same simple name in another namespace
        // does not fire. The syntactic fallback inside the resolver still catches a genuinely unresolved attribute.
        return attributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => AttributeIdentity.IsAnyOf(
                context.SemanticModel, attribute, UnmanagedCallersOnlyAttribute, context.CancellationToken));
    }

    private static bool ImplementsComVisibleInterfaceMember(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
    {
        // The method is a native callback when it is the implementation of a member declared on a COM-visible interface —
        // an interface the CLR exposes to native COM through a COM-callable wrapper, so native code invokes this method
        // through that wrapper's vtable. Resolve the method symbol, then, for every COM-visible interface the containing
        // type implements, ask whether THIS method is the implementation of one of that interface's members
        // (FindImplementationForInterfaceMember — the same resolution the runtime uses, so an explicit or implicit
        // implementation both match, and an unrelated same-named method does not).
        if (context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken) is not IMethodSymbol symbol)
        {
            return false;
        }

        INamedTypeSymbol containingType = symbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        // For every COM-visible interface the containing type implements, is THIS method the implementation of one of
        // that interface's members? FindImplementationForInterfaceMember is the same resolution the runtime uses, so an
        // explicit or implicit implementation both match and an unrelated same-named method does not.
        return containingType.AllInterfaces
            .Where(IsComVisibleInterface)
            .SelectMany(comInterface => comInterface.GetMembers().OfType<IMethodSymbol>())
            .Any(interfaceMethod => SymbolEqualityComparer.Default.Equals(
                containingType.FindImplementationForInterfaceMember(interfaceMethod), symbol));
    }

    private static bool IsComVisibleInterface(INamedTypeSymbol comInterface)
    {
        // A COM-visible interface carries [ComVisible(true)] — the argument the CLR reads to expose it to native COM.
        // A [ComVisible(false)] or an unmarked interface is not one native code calls a managed implementer back through.
        return comInterface.GetAttributes().Any(attribute =>
            WellKnownType.IsAnyOf(attribute.AttributeClass, ComVisibleAttribute)
            && attribute.ConstructorArguments.Length == 1
            && attribute.ConstructorArguments[0].Value is true);
    }

    private static bool IsSingleGuardedTry(SyntaxNodeAnalysisContext context, BlockSyntax body)
    {
        // The whole body is exactly one try statement with EXACTLY ONE catch clause, that clause handles every managed
        // exception — a bare catch or a catch of System.Exception — AND the finally (when present) cannot throw. A try
        // with only a finally (no catch), a narrower catch, more than one catch clause, any statement beside the try, or
        // a finally that throws leaves an escape path across the native frame and is reported. Requiring exactly one
        // catch is what closes the multi-catch hole: a leaky specific catch that rethrows AHEAD of a compliant catch-all
        // — a legal more-derived-before-Exception order — would otherwise let that specific exception unwind across the
        // native frame while any-one compliant catch kept the rule silent. And a compliant catch does not save a finally
        // whose own throw still unwinds.
        return body.Statements.Count == 1
            && body.Statements[0] is TryStatementSyntax tryStatement
            && tryStatement.Catches.Count == 1
            && CatchesEveryException(context, tryStatement.Catches[0])
            && !FinallyCanThrow(tryStatement);
    }

    private static bool FinallyCanThrow(TryStatementSyntax tryStatement)
    {
        // A finally clause runs after the catch has already returned a native error code; a throw inside it still
        // unwinds past the boundary. Reuse the same escape-detection the catch path uses (BlockReachesThrow).
        return tryStatement.Finally is not null && BlockReachesThrow(tryStatement.Finally.Block);
    }

    private static bool CatchesEveryException(SyntaxNodeAnalysisContext context, CatchClauseSyntax catchClause)
    {
        if (!HandlesEveryExceptionType(context, catchClause))
        {
            return false;
        }

        // A catch-all that re-throws still lets an exception cross the native frame: a bare `throw;` rethrow re-raises
        // the caught exception, and a `throw new X(...)` raises a fresh one — either unwinds past the boundary the rule
        // guards. So a catch clause whose body reaches ANY throw (a ThrowStatementSyntax or a ThrowExpressionSyntax)
        // does not count as catching every exception; only one that returns a native error code on every path does.
        return !ReachesThrow(catchClause);
    }

    private static bool HandlesEveryExceptionType(SyntaxNodeAnalysisContext context, CatchClauseSyntax catchClause)
    {
        // The filter check comes FIRST — before the bare-catch and the type checks — because a runtime exception filter
        // can decline the exception on ANY catch shape, bare OR typed, and let it unwind across the native frame. A
        // declaration-less filtered catch — a bare catch carrying a when-clause, which is legal C# — would otherwise be
        // misclassified by the bare-catch shortcut as catching everything even though the filter can say no. So a filter
        // never counts as catching every managed exception, whatever the declaration.
        if (catchClause.Filter is not null)
        {
            return false;
        }

        // An unfiltered bare `catch { }` catches everything.
        if (catchClause.Declaration is null)
        {
            return true;
        }

        // An unfiltered typed catch catches everything only when its type is System.Exception. C# permits catching
        // System.Exception or a subtype, never a supertype, so after the filter and bare-catch checks the only remaining
        // catch-everything shape is an unfiltered handler of System.Exception; every narrower type is rejected — that
        // closes the catch grammar.
        ITypeSymbol? caught = context.SemanticModel.GetTypeInfo(catchClause.Declaration.Type, context.CancellationToken).Type;
        return WellKnownType.Is(caught as INamedTypeSymbol, KnownNamespaces.System, "Exception");
    }

    private static bool ReachesThrow(CatchClauseSyntax catchClause)
    {
        return BlockReachesThrow(catchClause.Block);
    }

    // The one escape-detection both the catch and the finally checks route through: a block reaches a throw when any
    // descendant is a `throw;`/`throw new X(...)` statement or a `throw`-expression. Spelled once (DRY) so the two
    // callers never diverge.
    private static bool BlockReachesThrow(BlockSyntax? block)
    {
        return block is not null
            && block.DescendantNodes().Any(node => node is ThrowStatementSyntax or ThrowExpressionSyntax);
    }

    private static void Report(SyntaxNodeAnalysisContext context, SyntaxToken identifier)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), identifier.ValueText));
    }
}
