// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The OS-uniform parts a per-OS <see cref="IPlatformFileSystem"/> is assembled from, returned as one value by
/// <see cref="PlatformFileSystemComposition.Create"/> so the composition block that builds them lives in exactly one
/// place (a named type, not a tuple, per AG0002). A plain internal data record — it implements no contract interface,
/// so it stays a data carrier — holding the shared OS-uniform file-operation helper and the wrapper factory the per-OS
/// class reads the OS-uniform symlink target through.
/// </summary>
/// <param name="Shared">The shared OS-uniform file-operation helper, wired to the owned adapters.</param>
/// <param name="Factory">The wrapper factory the per-OS class reads the OS-uniform symlink target through.</param>
internal sealed record PlatformFileSystemParts(PlatformFileSystemShared Shared, IFileInfoFactory Factory);
