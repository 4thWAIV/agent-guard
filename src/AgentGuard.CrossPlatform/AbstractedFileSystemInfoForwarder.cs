// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The one shared home of the <see cref="IFileSystemInfo.FullName"/>/<see cref="IFileSystemInfo.Attributes"/>/
/// <see cref="IFileSystemInfo.LinkTarget"/> forwarding block, so it is written once instead of copied into every
/// <c>*Info</c> wrapper. The <see cref="AbstractedFileInfo"/> and <see cref="AbstractedDirectoryInfo"/> wrappers derive
/// from this base and supply the one <see cref="IFileSystemInfo"/> core they forward to; the base declares the three
/// members once and each wrapper's contract interface (<c>IFileInfo</c>/<c>IDirectoryInfo</c>) is satisfied by these
/// inherited members. It forwards to an already-abstracted <see cref="IFileSystemInfo"/> (never a raw
/// <c>FileSystemInfo</c>), so it holds no OS primitive and needs no owner; the single raw read of the OS object lives
/// in <see cref="AbstractedFileSystemInfo"/>. It is a plain non-contract base (it does not itself implement a contract
/// interface), which is why the shared block lives here and not on <see cref="AbstractedFileSystemInfo"/>: a base that
/// implemented <c>IFileSystemInfo</c> would need a non-private constructor its derived wrappers could call, which the
/// private-constructor wall (AG0003) forbids.
/// </summary>
internal abstract class AbstractedFileSystemInfoForwarder
{
    private readonly IFileSystemInfo _core;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractedFileSystemInfoForwarder"/> class with the one
    /// already-abstracted <see cref="IFileSystemInfo"/> core the derived wrapper exposes its members through.
    /// </summary>
    /// <param name="core">The abstracted core to forward to.</param>
    private protected AbstractedFileSystemInfoForwarder(IFileSystemInfo core) => _core = core;

    /// <inheritdoc cref="IFileSystemInfo.FullName" />
    public string FullName => _core.FullName;

    /// <inheritdoc cref="IFileSystemInfo.Attributes" />
    public FileAttributes Attributes => _core.Attributes;

    /// <inheritdoc cref="IFileSystemInfo.LinkTarget" />
    public string? LinkTarget => _core.LinkTarget;
}
