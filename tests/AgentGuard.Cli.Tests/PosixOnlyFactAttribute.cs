// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform;
using Xunit;

namespace AgentGuard.Cli.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that runs on POSIX (macOS, Linux) and is skipped on Windows. The CLI success-path
/// tests isolate the machine install by redirecting the <c>HOME</c> environment variable, but the production entry
/// point resolves the home directory through <c>Environment.GetFolderPath(SpecialFolder.UserProfile)</c>, which
/// returns <c>$HOME</c> on POSIX and the registry profile on Windows — neither <c>HOME</c> nor <c>USERPROFILE</c>
/// redirects it there. Cross-OS isolation needs the injected environment the clr-primitive-lockdown adds
/// (issue #23), so until then these tests run on POSIX only rather than touch the Windows runner's real profile.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class PosixOnlyFactAttribute : FactAttribute
{
    public PosixOnlyFactAttribute()
    {
        // AG0009 forbids a direct OS branch outside the CrossPlatform libraries, so ask the platform services
        // instead: IPlatformFileSystem.NeedsExecutableFlag() is hard-true on POSIX and hard-false on Windows by its
        // documented contract, which is exactly the POSIX/Windows split these tests need.
        bool runningOnPosix = Platform.Create().FileSystem.NeedsExecutableFlag();
        if (!runningOnPosix)
        {
            Skip = "POSIX-only: the CLI's HOME redirect does not isolate the profile on Windows, where the guard " +
                "resolves home via Environment.GetFolderPath(UserProfile) (the registry). Cross-OS CLI isolation " +
                "lands with the clr-primitive-lockdown environment seam (issue #23).";
        }
    }
}
