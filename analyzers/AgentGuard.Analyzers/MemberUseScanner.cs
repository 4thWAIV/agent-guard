// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Registers the semantic operation actions that surface a use of a member — an invocation, an object creation, a
/// property read or write, a field read, or a method-group reference — and hands each one to a rule's inspector
/// already resolved to the referenced member symbol and the type that declares it. Detection is semantic (Roslyn
/// <see cref="IOperation"/>), never a syntax or text match, so it survives <c>using static</c>, aliases, and
/// fully-qualified names, and it never fires on a name that only appears in a comment or a <c>nameof</c>. Every
/// OS-primitive boundary rule (the filesystem, environment, randomness, time, console, and OS-divergent analyzers)
/// walks the same kinds through this one scanner rather than re-registering the operation actions itself.
/// </summary>
internal static class MemberUseScanner
{
    /// <summary>
    /// The operation kinds that can reference an OS primitive: a call, a <c>new</c>, a property access, a field read,
    /// or a method-group reference. Field references are included so the Path rule (AG0020) can see the raw separator
    /// FIELDS <c>Path.DirectorySeparatorChar</c>/<c>AltDirectorySeparatorChar</c> (scanner-sees-field-reads); the
    /// type-gated rules are unaffected because they filter by banned TYPE, and an enum value read as data
    /// (<c>UnixFileMode.UserRead</c>, <c>FileAttributes.Hidden</c>) is a field on a type no rule bans, so it stays
    /// legal.
    /// </summary>
    private static readonly ImmutableArray<OperationKind> MemberUseKinds = ImmutableArray.Create(
        OperationKind.Invocation,
        OperationKind.ObjectCreation,
        OperationKind.PropertyReference,
        OperationKind.FieldReference,
        OperationKind.MethodReference);

    /// <summary>
    /// Registers an operation action that resolves each member use to its referenced member symbol and declaring
    /// type, then invokes <paramref name="inspect"/> so a rule can decide whether to report it.
    /// </summary>
    /// <param name="context">The compilation-start context to register on.</param>
    /// <param name="inspect">The rule's inspector, given the operation context, the referenced member, and the
    /// type that declares it.</param>
    internal static void Register(
        CompilationStartAnalysisContext context,
        Action<OperationAnalysisContext, ISymbol, INamedTypeSymbol> inspect)
    {
        context.RegisterOperationAction(operationContext => Dispatch(operationContext, inspect), MemberUseKinds);
    }

    /// <summary>
    /// Registers the member-use scan for <paramref name="inspect"/> only when the compilation being analyzed is the
    /// assembly named <paramref name="assemblyName"/>; a compilation that is any other assembly registers nothing.
    /// The "gate to one assembly, then scan" setup is owned once here rather than re-spelled per rule: the two
    /// one-door-out-of-Boundaries rules (AG0023 into CrossPlatform-core, AG0029 into a per-OS assembly) both gate on
    /// <see cref="BoundaryAssembly.Name"/> and differ only in the door their own inspector allows.
    /// </summary>
    /// <param name="context">The compilation-start context to gate and register on.</param>
    /// <param name="assemblyName">The assembly name the compilation must match for the scan to be registered.</param>
    /// <param name="inspect">The rule's inspector, given the operation context, the referenced member, and the
    /// type that declares it.</param>
    internal static void RegisterForAssembly(
        CompilationStartAnalysisContext context,
        string assemblyName,
        Action<OperationAnalysisContext, ISymbol, INamedTypeSymbol> inspect)
    {
        // Only the named assembly's compilation is gated — these rules are about what that one assembly reaches into.
        if (!string.Equals(context.Compilation.AssemblyName, assemblyName, StringComparison.Ordinal))
        {
            return;
        }

        Register(context, inspect);
    }

    /// <summary>
    /// Builds the human-readable name of a member use for a diagnostic message: <c>new TypeName</c> for a
    /// constructor, or <c>TypeName.MemberName</c> otherwise.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <param name="type">The type that declares it.</param>
    /// <returns>The display text for the diagnostic message.</returns>
    internal static string Describe(ISymbol member, INamedTypeSymbol type)
    {
        return member is IMethodSymbol { MethodKind: MethodKind.Constructor }
            ? "new " + type.Name
            : type.Name + "." + member.Name;
    }

    private static void Dispatch(
        OperationAnalysisContext context,
        Action<OperationAnalysisContext, ISymbol, INamedTypeSymbol> inspect)
    {
        (ISymbol? member, INamedTypeSymbol? type) = Resolve(context.Operation);
        if (member is not null && type is not null)
        {
            inspect(context, member, type);
        }
    }

    /// <summary>
    /// Resolves an operation to the member it references and the type that declares that member. Object creation
    /// resolves to the constructor and the created type; the others resolve to the invoked method, referenced
    /// property, referenced field, or referenced method group and its containing type.
    /// </summary>
    /// <param name="operation">The operation to resolve.</param>
    /// <returns>The referenced member and its declaring type, or a pair of <see langword="null"/> when the
    /// operation references neither.</returns>
    private static (ISymbol? Member, INamedTypeSymbol? Type) Resolve(IOperation operation)
    {
        return operation switch
        {
            IInvocationOperation invocation => (invocation.TargetMethod, invocation.TargetMethod.ContainingType),
            IObjectCreationOperation creation when creation.Constructor is not null =>
                (creation.Constructor, creation.Constructor.ContainingType),
            IPropertyReferenceOperation property => (property.Property, property.Property.ContainingType),
            IFieldReferenceOperation field => (field.Field, field.Field.ContainingType),
            IMethodReferenceOperation method => (method.Method, method.Method.ContainingType),
            _ => (null, null),
        };
    }
}
