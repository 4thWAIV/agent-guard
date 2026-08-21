// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The one table that maps every raw OS/CLR primitive to its single owner (or to "no owner — banned everywhere"),
/// resolved once in <see cref="Resolve"/>. This is the consolidation the bridge's rule-phase makes: the per-primitive
/// boundary analyzers that each spelled out one owner (filesystem, environment, randomness, console, crypto, version,
/// Process, and the <c>*Info</c> half of the OS-divergent rule) collapse into this single table-driven owner rule
/// (AG0011). <c>File</c>/<c>Directory</c> keep their per-member split across the four filesystem interfaces (through
/// <see cref="FilesystemMembers.OwningInterfaceFor"/>); every other type maps whole to one owner. The families are
/// keyed by disjoint types, so <see cref="Resolve"/> chains the per-family resolvers with <c>??</c>: a member a
/// DIFFERENT standalone rule still owns (the OS-divergent static <c>File</c>/<c>Directory</c> members → AG0101, the
/// clock members on <c>Environment</c> → AG0015, <c>RuntimeInformation.IsOSPlatform</c> → AG0009) resolves to
/// <see langword="null"/> and — because no other family's types match it — stays <see langword="null"/> to the end.
/// </summary>
internal static class OwnedPrimitives
{
    /// <summary>
    /// The single authoritative (namespace, name) identity of the owned <c>IFileInfo</c> wrapper — the owner of the raw
    /// <c>System.IO.FileInfo</c> primitive (the AbstractedFileInfo wrapper in AgentGuard.CrossPlatform, where raw
    /// <c>FileInfo</c> construction and every <c>FileInfo</c> member is legal). Held as a one-element set so the resolver
    /// hands it straight to <see cref="OwnerClass"/> without allocating per operation. AG0033's WrapperInterfaces is
    /// built from this owner (and <see cref="DirectoryInfoOwner"/>), so the <c>IFileInfo</c> literal is spelled once.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> FileInfoOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IFileInfo"));

    /// <summary>
    /// The single authoritative (namespace, name) identity of the owned <c>IDirectoryInfo</c> wrapper — the owner of the
    /// raw <c>System.IO.DirectoryInfo</c> primitive (the AbstractedDirectoryInfo wrapper in AgentGuard.CrossPlatform).
    /// AG0033's WrapperInterfaces is built from this owner (and <see cref="FileInfoOwner"/>), so the
    /// <c>IDirectoryInfo</c> literal is spelled once.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> DirectoryInfoOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IDirectoryInfo"));

    /// <summary>
    /// The two owned <c>*Info</c> wrapper interfaces (<c>IFileInfo</c> + <c>IDirectoryInfo</c>) as one combined set,
    /// composed once from <see cref="FileInfoOwner"/> and <see cref="DirectoryInfoOwner"/> so the
    /// <c>FileInfoOwner.AddRange(DirectoryInfoOwner)</c> is spelled exactly once. Both AG0033's wrapper-construction pin
    /// (<see cref="GuardedConstructionAnalyzer"/>) and AG0034's sub-container <c>*Info</c>-factory allowance
    /// (<see cref="SystemServicesMemberMustBeServiceAccessorAnalyzer"/>) reference this instead of each recomputing the
    /// same union.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> InfoWrapperInterfaces =
        FileInfoOwner.AddRange(DirectoryInfoOwner);

    /// <summary>
    /// The assembly gate for the owners that live in AgentGuard.CrossPlatform (owners-live-at-lowest-consumer): the
    /// file-op adapters, the <c>*Info</c> wrappers, and the randomness adapter. Cached once so no delegate is allocated
    /// per analyzed operation. Internal so AG0020 (<see cref="PathPurityAnalyzer"/>) references this exact gate for its
    /// randomness owner exemption (<c>GetRandomFileName</c>) rather than reconstructing an identical one.
    /// </summary>
    internal static readonly Func<Compilation, bool> InCrossPlatform =
        OwnerClass.InAssembly(CrossPlatformBoundary.RootName);

    /// <summary>
    /// The assembly gate for the owners that live in AgentGuard.Boundaries (owners-live-at-lowest-consumer): the
    /// environment, console, signature, and build-info owners. Cached once so no delegate is allocated per analyzed
    /// operation. Internal so AG0020 (<see cref="PathPurityAnalyzer"/>) references this exact gate for its temp-path
    /// owner exemption (<c>GetTempPath</c>) rather than reconstructing an identical one.
    /// </summary>
    internal static readonly Func<Compilation, bool> InBoundaries =
        OwnerClass.InAssembly(BoundaryAssembly.Name);

    // The FileSystemInfo base owner (fileinfo-abstraction-stays-in-ag0011): the FileSystemInfo base is owned wholesale by
    // the class implementing IFileSystemInfo — the *Info wrappers in AgentGuard.CrossPlatform, where raw *Info
    // construction and every *Info member is legal, fully. IFileSystemInfo is the base, not an AG0033 wrapper, so it is
    // not part of WrapperInterfaces. Held as a one-element set so the resolver hands it straight to OwnerClass.IsOwner
    // without allocating per operation.
    private static readonly ImmutableArray<(string Namespace, string Name)> FileSystemInfoOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IFileSystemInfo"));

    // The environment owner (IEnvironment) — the identity is spelled once in ContractInterfaces because AG0020 shares
    // it for the Path.GetTempPath owner exemption.
    private static readonly ImmutableArray<(string Namespace, string Name)> EnvironmentOwner =
        ContractInterfaces.Environment;

    // The randomness owner (IRandomGenerator, renamed from IGuidFactory: the owner is the randomness concern, so a
    // random file name has a home beside NewGuid — iguidfactory-renamed-to-irandomgenerator). It owns Guid.NewGuid,
    // System.Random, and RandomNumberGenerator here; the identity is spelled once in ContractInterfaces because AG0020
    // shares it for the Path.GetRandomFileName owner exemption. Until IMPLEMENT renames the adapter, the old
    // GuidFactoryAdapter (which implements IGuidFactory) no longer matches this owner and its Guid.NewGuid() goes RED —
    // the forcing function for the rename.
    private static readonly ImmutableArray<(string Namespace, string Name)> RandomGeneratorOwner =
        ContractInterfaces.RandomGenerator;

    private static readonly ImmutableArray<(string Namespace, string Name)> ConsoleOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IConsole"));

    private static readonly ImmutableArray<(string Namespace, string Name)> SignatureServiceOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "ISignatureService"));

    private static readonly ImmutableArray<(string Namespace, string Name)> BuildInfoOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IBuildInfo"));

    // The impure Guid-factory method set — every non-deterministic Guid factory, not just NewGuid. .NET 10 added
    // CreateVersion7/CreateVersion1, equally non-deterministic, which a name-only check on "NewGuid" would miss
    // (ag0014-edit-impure-guid-set); a future CreateVersionN added deliberately is never legal by omission. Building a
    // Guid from bytes or parsing text stays legal (those names are not in this set).
    private static readonly ImmutableHashSet<string> ImpureGuidFactoryMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal, "NewGuid", "CreateVersion7", "CreateVersion1");

    // The Assembly reflection entry points that resolve a running assembly to read its version off — the version reads
    // (IBuildInfo, was AG0028). Assembly.Location is instead a deployment-path read (IEnvironment, was AG0012); both
    // live on System.Reflection.Assembly and are disambiguated by member name in ResolveEnvironmentAndReflection.
    private static readonly ImmutableHashSet<string> AssemblyVersionEntryPointMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal, "GetEntryAssembly", "GetExecutingAssembly", "GetCallingAssembly");

    // The two version-attribute types whose reflection reads are banned outside the IBuildInfo owner (was AG0028): any
    // member read on either is a version read.
    private static readonly ImmutableArray<(string Namespace, string Name)> VersionAttributeTypes = ImmutableArray.Create(
        (KnownNamespaces.SystemReflection, "AssemblyInformationalVersionAttribute"),
        (KnownNamespaces.SystemReflection, "AssemblyFileVersionAttribute"));

    // The banned BouncyCastle Ed25519 types (was AG0021), matched by full name so a same-named type in another
    // namespace is not caught. The signer sequence and the public-key parameters both hide inside the ISignatureService
    // owner; no BouncyCastle type crosses the boundary.
    private static readonly ImmutableArray<(string Namespace, string Name)> BouncyCastleEd25519Types = ImmutableArray.Create(
        ("Org.BouncyCastle.Crypto.Signers", "Ed25519Signer"),
        ("Org.BouncyCastle.Crypto.Parameters", "Ed25519PublicKeyParameters"));

    // Process launching is not a wrapped primitive (was AG0013): System.Diagnostics.Process and ProcessStartInfo are
    // banned everywhere; grow an owned interface first if the capability is ever needed.
    private static readonly ImmutableArray<(string Namespace, string Name)> ProcessTypes = ImmutableArray.Create(
        (KnownNamespaces.SystemDiagnostics, "Process"),
        (KnownNamespaces.SystemDiagnostics, "ProcessStartInfo"));

    /// <summary>
    /// Resolves the single owner of a raw OS/CLR primitive reference, or <see langword="null"/> when the reference is
    /// not a primitive this rule governs (including the members a different standalone rule owns). A non-null result
    /// with an empty <see cref="OwnedPrimitive.Owners"/> means the primitive is banned everywhere.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <param name="type">The type that declares the member.</param>
    /// <returns>The owner, or <see langword="null"/> when the reference is not this rule's.</returns>
    internal static OwnedPrimitive? Resolve(ISymbol member, INamedTypeSymbol type)
    {
        // The families are keyed by disjoint types, so a null from one resolver (either "not my type" or "my type but
        // a carved-out member") falls through the chain and — because no later family's types match — stays null.
        return ResolveInfoTypes(type)
            ?? ResolveFileAndDirectory(member, type)
            ?? ResolveStreamsAndEnumeration(type)
            ?? ResolveEnvironmentAndReflection(member, type)
            ?? ResolveRandomness(member, type)
            ?? ResolveConsoleCryptoProcess(type);
    }

    /// <summary>
    /// Builds a short, actionable owner label for the diagnostic message: the single owning interface's name, or
    /// "no owner (grow an owned interface first)" for a primitive banned everywhere.
    /// </summary>
    /// <param name="owned">The resolved owner.</param>
    /// <returns>The owner label for the diagnostic message.</returns>
    internal static string OwnerLabel(OwnedPrimitive owned)
    {
        return owned.Owners.IsEmpty
            ? "no owner (grow an owned interface first)"
            : "the class implementing " + owned.Owners[0].Name;
    }

    // The *Info family, owned wholesale by the wrapper interfaces (fileinfo-abstraction-stays-in-ag0011): raw
    // construction and every member of a FileInfo/DirectoryInfo/FileSystemInfo is legal only inside the wrapper class
    // that implements IFileInfo/IDirectoryInfo/IFileSystemInfo in AgentGuard.CrossPlatform. Resolved first so the
    // *Info instance members (LinkTarget, UnixFileMode, Attributes) route to the wrapper, not the per-member split.
    private static OwnedPrimitive? ResolveInfoTypes(INamedTypeSymbol type)
    {
        if (WellKnownType.Is(type, KnownNamespaces.SystemIO, "FileInfo"))
        {
            return OwnedPrimitive.OwnedBy(FileInfoOwner, InCrossPlatform);
        }

        if (WellKnownType.Is(type, KnownNamespaces.SystemIO, "DirectoryInfo"))
        {
            return OwnedPrimitive.OwnedBy(DirectoryInfoOwner, InCrossPlatform);
        }

        return WellKnownType.Is(type, KnownNamespaces.SystemIO, "FileSystemInfo")
            ? OwnedPrimitive.OwnedBy(FileSystemInfoOwner, InCrossPlatform)
            : (OwnedPrimitive?)null;
    }

    // File / Directory — the OS-uniform static filesystem surface, split per member.
    private static OwnedPrimitive? ResolveFileAndDirectory(ISymbol member, INamedTypeSymbol type)
    {
        if (!WellKnownType.Is(type, KnownNamespaces.SystemIO, "File")
            && !WellKnownType.Is(type, KnownNamespaces.SystemIO, "Directory"))
        {
            return null;
        }

        // The OS-divergent static members (CreateSymbolicLink/ResolveLinkTarget/Get+SetUnixFileMode) are the per-OS
        // rule's (AG0101) — carve out so exactly one rule owns each.
        if (FilesystemMembers.IsOsDivergentMember(member))
        {
            return null;
        }

        // Directory.GetCurrentDirectory/SetCurrentDirectory is an environment read on the filesystem type: it is owned
        // by IEnvironment in Boundaries (was AG0012), not by the filesystem interfaces.
        if (FilesystemMembers.IsCurrentDirectoryMember(member))
        {
            return OwnedPrimitive.OwnedBy(EnvironmentOwner, InBoundaries);
        }

        // Every other File/Directory member → its one filesystem interface (IFileReader/IDirectoryEnumerator/
        // IFileWriter/IDirectoryWriter) in CrossPlatform, or an empty set (banned everywhere) for an unmapped member
        // such as a directory-side Move or a creation/access-time getter.
        return OwnedPrimitive.OwnedBy(FilesystemMembers.OwningInterfaceFor(member, type), InCrossPlatform);
    }

    // The stream, watcher, and drive System.IO types and every System.IO.Enumeration type: no owning interface, so
    // banned everywhere until an interface is grown (was AG0011's no-owner set).
    private static OwnedPrimitive? ResolveStreamsAndEnumeration(INamedTypeSymbol type)
    {
        return WellKnownType.IsAnyOf(type, FilesystemMembers.StreamWatcherAndDriveTypes)
            || WellKnownType.IsInNamespace(type, KnownNamespaces.SystemIOEnumeration)
            ? OwnedPrimitive.BannedEverywhere
            : (OwnedPrimitive?)null;
    }

    // Environment, the deployment-path reads, and the version-reflection reads — all owned in Boundaries, split between
    // IEnvironment (deployment paths) and IBuildInfo (version) and disambiguated by member name where a type carries
    // both (System.Reflection.Assembly).
    private static OwnedPrimitive? ResolveEnvironmentAndReflection(ISymbol member, INamedTypeSymbol type)
    {
        // System.Environment — every member except the clock members (TickCount/TickCount64), which are AG0015's.
        if (WellKnownType.Is(type, KnownNamespaces.System, "Environment"))
        {
            return TimeMembers.IsEnvironmentClockMember(member)
                ? (OwnedPrimitive?)null
                : OwnedPrimitive.OwnedBy(EnvironmentOwner, InBoundaries);
        }

        // System.Reflection.Assembly carries both version reads (the entry points → IBuildInfo) and a deployment-path
        // read (Location → IEnvironment); disambiguated by member name so each keeps its distinct owner.
        if (WellKnownType.Is(type, KnownNamespaces.SystemReflection, "Assembly"))
        {
            if (AssemblyVersionEntryPointMethods.Contains(member.Name))
            {
                return OwnedPrimitive.OwnedBy(BuildInfoOwner, InBoundaries);
            }

            return string.Equals(member.Name, "Location", StringComparison.Ordinal)
                ? OwnedPrimitive.OwnedBy(EnvironmentOwner, InBoundaries)
                : (OwnedPrimitive?)null;
        }

        // AppContext.BaseDirectory / AppDomain.CurrentDomain.BaseDirectory — deployment-path reads (IEnvironment).
        if (WellKnownType.Is(type, KnownNamespaces.System, "AppContext")
            || WellKnownType.Is(type, KnownNamespaces.System, "AppDomain"))
        {
            return string.Equals(member.Name, "BaseDirectory", StringComparison.Ordinal)
                ? OwnedPrimitive.OwnedBy(EnvironmentOwner, InBoundaries)
                : (OwnedPrimitive?)null;
        }

        // RuntimeInformation host-description properties (OSDescription, FrameworkDescription, …) → IEnvironment. The
        // IsOSPlatform method is OS branching, owned by AG0009, so only the properties are claimed here.
        if (WellKnownType.Is(type, KnownNamespaces.SystemRuntimeInteropServices, "RuntimeInformation"))
        {
            return member.Kind == SymbolKind.Property
                ? OwnedPrimitive.OwnedBy(EnvironmentOwner, InBoundaries)
                : (OwnedPrimitive?)null;
        }

        // AssemblyName.Version — the GetName().Version read (IBuildInfo).
        if (WellKnownType.Is(type, KnownNamespaces.SystemReflection, "AssemblyName"))
        {
            return string.Equals(member.Name, "Version", StringComparison.Ordinal)
                ? OwnedPrimitive.OwnedBy(BuildInfoOwner, InBoundaries)
                : (OwnedPrimitive?)null;
        }

        // A read of any member on the informational-version or file-version attribute — the reflected version value.
        return WellKnownType.IsAnyOf(type, VersionAttributeTypes)
            ? OwnedPrimitive.OwnedBy(BuildInfoOwner, InBoundaries)
            : (OwnedPrimitive?)null;
    }

    // System.Random / RandomNumberGenerator / the impure Guid factory set → IRandomGenerator (the one randomness seam).
    private static OwnedPrimitive? ResolveRandomness(ISymbol member, INamedTypeSymbol type)
    {
        if (WellKnownType.Is(type, KnownNamespaces.System, "Random")
            || WellKnownType.Is(type, KnownNamespaces.SystemSecurityCryptography, "RandomNumberGenerator"))
        {
            return OwnedPrimitive.OwnedBy(RandomGeneratorOwner, InCrossPlatform);
        }

        // Guid.NewGuid / CreateVersion7 / CreateVersion1 → IRandomGenerator; building a GUID from bytes or parsing text
        // stays legal (those names are not in the impure set).
        if (WellKnownType.Is(type, KnownNamespaces.System, "Guid"))
        {
            return ImpureGuidFactoryMethods.Contains(member.Name)
                ? OwnedPrimitive.OwnedBy(RandomGeneratorOwner, InCrossPlatform)
                : (OwnedPrimitive?)null;
        }

        return null;
    }

    // System.Console → IConsole ; BouncyCastle Ed25519 → ISignatureService ; Process → banned everywhere.
    private static OwnedPrimitive? ResolveConsoleCryptoProcess(INamedTypeSymbol type)
    {
        if (WellKnownType.Is(type, KnownNamespaces.System, "Console"))
        {
            return OwnedPrimitive.OwnedBy(ConsoleOwner, InBoundaries);
        }

        if (WellKnownType.IsAnyOf(type, BouncyCastleEd25519Types))
        {
            return OwnedPrimitive.OwnedBy(SignatureServiceOwner, InBoundaries);
        }

        return WellKnownType.IsAnyOf(type, ProcessTypes)
            ? OwnedPrimitive.BannedEverywhere
            : (OwnedPrimitive?)null;
    }
}
