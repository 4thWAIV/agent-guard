// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The single concrete <see cref="IFileSystem"/> — the one filesystem entry point exposed on
/// <see cref="ISystemServices"/>. It holds the internal <see cref="IFileInfoFactory"/> seam (which its per-path
/// factory members delegate to, so a fresh wrapper is returned each call) and the four owned filesystem adapters
/// (which its service accessors hand back, the one injected adapter each call). It carries no OS logic and constructs
/// no primitive of its own. It is <c>internal sealed</c> with a <c>private</c> constructor (Wall 1) and handed out
/// only as its interface.
/// </summary>
internal sealed class FileSystem : IFileSystem
{
    private readonly IFileInfoFactory _factory;
    private readonly IFileReader _fileReader;
    private readonly IDirectoryEnumerator _directories;
    private readonly IFileWriter _fileWriter;
    private readonly IDirectoryWriter _directoryWriter;

    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it, from the injected seam and
    // adapters.
    private FileSystem(
        IFileInfoFactory factory,
        IFileReader fileReader,
        IDirectoryEnumerator directories,
        IFileWriter fileWriter,
        IDirectoryWriter directoryWriter)
    {
        _factory = factory;
        _fileReader = fileReader;
        _directories = directories;
        _fileWriter = fileWriter;
        _directoryWriter = directoryWriter;
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string path) => _factory.GetFileInfo(path);

    /// <inheritdoc />
    public IDirectoryInfo GetDirectoryInfo(string path) => _factory.GetDirectoryInfo(path);

    /// <inheritdoc />
    public IFileReader GetFileReader() => _fileReader;

    /// <inheritdoc />
    public IDirectoryEnumerator GetDirectoryReader() => _directories;

    /// <inheritdoc />
    public IFileWriter GetFileWriter() => _fileWriter;

    /// <inheritdoc />
    public IDirectoryWriter GetDirectoryWriter() => _directoryWriter;

    /// <summary>
    /// Creates the filesystem entry point. It builds the owned wrapper factory and the four owned filesystem adapters
    /// it needs, so no service is passed in as a method argument (constructor injection is the one path a service
    /// enters). Each adapter is stateless, so a fresh instance behaves identically to any other.
    /// </summary>
    /// <returns>The filesystem entry point, as its interface.</returns>
    internal static IFileSystem Create() => new FileSystem(
        FileInfoFactory.Create(),
        FileReaderAdapter.Create(),
        DirectoryEnumeratorAdapter.Create(),
        FileWriterAdapter.Create(),
        DirectoryWriterAdapter.Create());
}
