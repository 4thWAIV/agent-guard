// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The single filesystem entry point — a service on <see cref="ISystemServices"/> that carries the whole owned
/// filesystem surface today and grows by contract. It has two kinds of member: the per-path factories
/// (<see cref="GetFileInfo(string)"/>/<see cref="GetDirectoryInfo(string)"/>), which return a FRESH wrapper each call
/// by delegating to the internal <see cref="IFileInfoFactory"/> seam; and the service accessors
/// (<see cref="GetFileReader"/>/<see cref="GetDirectoryReader"/>/<see cref="GetFileWriter"/>/<see cref="GetDirectoryWriter"/>),
/// which return the one injected adapter each call. Consumers reach the filesystem only through this interface; the
/// four filesystem interfaces and their adapters are unchanged, reached through here.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets a fresh <see cref="IFileInfo"/> for <paramref name="path"/>, delegated to <see cref="IFileInfoFactory"/>.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A fresh <see cref="IFileInfo"/> for the path.</returns>
    IFileInfo GetFileInfo(string path);

    /// <summary>
    /// Gets a fresh <see cref="IDirectoryInfo"/> for <paramref name="path"/>, delegated to
    /// <see cref="IFileInfoFactory"/>.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A fresh <see cref="IDirectoryInfo"/> for the path.</returns>
    IDirectoryInfo GetDirectoryInfo(string path);

    /// <summary>
    /// Gets the read side of the filesystem primitive.
    /// </summary>
    /// <returns>The owned file reader.</returns>
    IFileReader GetFileReader();

    /// <summary>
    /// Gets the read-only directory-enumeration service.
    /// </summary>
    /// <returns>The owned directory enumerator.</returns>
    IDirectoryEnumerator GetDirectoryReader();

    /// <summary>
    /// Gets the file-write side of the filesystem primitive.
    /// </summary>
    /// <returns>The owned file writer.</returns>
    IFileWriter GetFileWriter();

    /// <summary>
    /// Gets the directory-write side of the filesystem primitive.
    /// </summary>
    /// <returns>The owned directory writer.</returns>
    IDirectoryWriter GetDirectoryWriter();
}
