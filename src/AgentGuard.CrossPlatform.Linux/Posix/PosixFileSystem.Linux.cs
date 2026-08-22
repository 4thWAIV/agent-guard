// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Posix;

/// <summary>
/// The Linux-only fragment of the POSIX file system (os-specific-file-naming-convention): the native
/// case-sensitivity query the shared source declares as a partial method. Linux has no <c>pathconf</c>
/// case-sensitivity name, so there is no native query to run — this body reports failure and defers to the shared
/// read-only probe, which is the OS-uniform fallback the shared source already calls. This is genuinely
/// Linux-specific behavior (no native query), not a copy of the macOS <c>pathconf</c> code, so it is not a DRY
/// violation of it; it is compiled only into the Linux project, never link-shared into the macOS build.
/// </summary>
internal sealed partial class PosixFileSystem
{
    // Linux has no native case-sensitivity query (the macOS _PC_CASE_SENSITIVE pathconf name has no Linux meaning), so
    // this reports failure and the shared PosixFileSystem falls back to the OS-uniform read-only probe.
    private static partial bool TryQueryNativeCaseSensitivity(string path, out bool caseSensitive)
    {
        caseSensitive = false;
        return false;
    }
}
