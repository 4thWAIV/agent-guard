// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The ONE factory that produces the OS-uniform file-op adapters owned by <c>AgentGuard.CrossPlatform</c> — the
/// single door <c>AgentGuard.Boundaries</c> reaches into this assembly through (boundaries-calls-one-crossplatform
/// -factory, pinned by AG0023), and the door the per-OS <c>PlatformServices.Create()</c> factories use to obtain the same
/// adapters for the platform file system. Each adapter lives here because the platform code consumes it
/// (owners-live-at-lowest-consumer); each is an <c>internal</c> class with a <c>private</c> constructor (Wall 1) handed
/// out only through its own static factory and only as its interface. The bundle this factory returns exposes the
/// adapters as their interfaces, never their concrete types.
/// </summary>
internal sealed class CrossPlatformAdapters
{
    private CrossPlatformAdapters(
        IFileReader fileReader,
        IDirectoryEnumerator directories,
        IFileWriter fileWriter,
        IDirectoryWriter directoryWriter,
        IRandomGenerator random,
        IFileSystem fileSystem)
    {
        FileReader = fileReader;
        Directories = directories;
        FileWriter = fileWriter;
        DirectoryWriter = directoryWriter;
        Random = random;
        FileSystem = fileSystem;
    }

    /// <summary>
    /// Gets the owned read side of the filesystem primitive.
    /// </summary>
    public IFileReader FileReader { get; }

    /// <summary>
    /// Gets the owned read-only directory enumerator.
    /// </summary>
    public IDirectoryEnumerator Directories { get; }

    /// <summary>
    /// Gets the owned file-write side of the filesystem primitive.
    /// </summary>
    public IFileWriter FileWriter { get; }

    /// <summary>
    /// Gets the owned directory-write side of the filesystem primitive.
    /// </summary>
    public IDirectoryWriter DirectoryWriter { get; }

    /// <summary>
    /// Gets the owned random generator — the one seam through which a GUID or random file name enters the codebase.
    /// </summary>
    public IRandomGenerator Random { get; }

    /// <summary>
    /// Gets the single filesystem entry point — the <see cref="IFileSystem"/> the composition exposes on
    /// <c>ISystemServices</c>, produced here so <c>AgentGuard.Boundaries</c> reaches it through this one door.
    /// </summary>
    public IFileSystem FileSystem { get; }

    /// <summary>
    /// Creates the filesystem entry point and the GUID factory and returns them, with the four owned filesystem
    /// adapters, as one bundle. This is the single entry point the composition in <c>AgentGuard.Boundaries</c> and each
    /// per-OS <c>PlatformServices.Create()</c> call reach, so the bundle is produced in exactly one place and reached
    /// only as its interfaces. The four read/write/enumerate adapters are NOT re-constructed here: they are the ones the
    /// single <see cref="FileSystem"/> entry point already owns, pulled off its accessors, so the four-adapter
    /// construction lives in exactly one place (<c>FileSystem.Create()</c>) and is reused here.
    /// </summary>
    /// <returns>The bundle of owned adapters, each typed as its interface.</returns>
    internal static CrossPlatformAdapters Create()
    {
        IFileSystem fileSystem = global::AgentGuard.CrossPlatform.FileSystem.Create();
        return new(
            fileSystem.GetFileReader(),
            fileSystem.GetDirectoryReader(),
            fileSystem.GetFileWriter(),
            fileSystem.GetDirectoryWriter(),
            RandomGeneratorAdapter.Create(),
            fileSystem);
    }
}
