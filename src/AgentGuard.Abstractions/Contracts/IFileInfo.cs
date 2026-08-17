// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned view of a single file entry — the abstraction that mirrors the .NET <see cref="FileInfo"/> 1:1,
/// carrying only the members the engine needs today (through <see cref="IFileSystemInfo"/>). The raw
/// <see cref="FileInfo"/> lives only inside the wrapper class that implements this interface in
/// <c>AgentGuard.CrossPlatform</c>; every other site obtains one through <see cref="IFileSystem.GetFileInfo(string)"/>.
/// </summary>
public interface IFileInfo : IFileSystemInfo
{
}
