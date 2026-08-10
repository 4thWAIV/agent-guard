// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.CrossPlatform;

namespace AgentGuard.Tests;

/// <summary>
/// The engine tests' managed test double for <see cref="IPlatformFileSystem"/>. It implements the interface with
/// only managed BCL calls — no P/Invoke — so the real engine tests exercise the setup logic (idempotency compare,
/// condition detection, install/doctor flow) against real on-disk symlinks without ever touching native code; the
/// native per-OS behavior is proven separately by the one OS-agnostic spec project. It reports
/// <see cref="NeedsExecutableFlag"/> as <see langword="false"/> (the engine then never touches the executable bit
/// in tests, which the spec project proves), and it never branches on OS.
/// </summary>
internal sealed class ManagedPlatformFileSystem : IPlatformFileSystem
{
    public bool IsLinkTarget(string linkPath) => PlatformFileSystemShared.IsLinkTarget(linkPath);

    public string? ReadLinkTarget(string linkPath) => PlatformFileSystemShared.ReadLinkTarget(linkPath);

    public void MakeLinkTarget(string linkPath, string relativeTarget)
    {
        PlatformFileSystemShared.RefuseIfRealEntry(linkPath, "point");
        string linkDirectory = Path.GetDirectoryName(linkPath)!;
        Directory.CreateDirectory(linkDirectory);
        PlatformFileSystemShared.DeleteLinkEntry(linkPath);

        // Choose the symlink kind from the resolved target so the link resolves correctly on every OS (matters on
        // Windows; harmless on POSIX). The raw relative target is stored verbatim.
        if (Directory.Exists(Path.GetFullPath(Path.Combine(linkDirectory, relativeTarget))))
        {
            Directory.CreateSymbolicLink(linkPath, relativeTarget);
        }
        else
        {
            File.CreateSymbolicLink(linkPath, relativeTarget);
        }
    }

    public void RemoveLinkTarget(string linkPath)
    {
        PlatformFileSystemShared.RefuseIfRealEntry(linkPath, "remove");
        PlatformFileSystemShared.DeleteLinkEntry(linkPath);
    }

    public bool NeedsExecutableFlag() => false;

    public bool IsExecutable(string path) =>
        throw new PlatformNotSupportedException("The managed test double does not model the executable bit.");

    public void MakeExecutable(string path) =>
        throw new PlatformNotSupportedException("The managed test double does not model the executable bit.");

    public void MakeNonExecutable(string path) =>
        throw new PlatformNotSupportedException("The managed test double does not model the executable bit.");
}
