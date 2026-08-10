// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Text.Json;
using AgentGuard.CrossPlatform;

namespace AgentGuard.Setup;

/// <summary>
/// The hook integrity self-check that gates a <c>guard hook</c> run. It derives the install root from the
/// resolved running binary — never from <c>$HOME</c>, since a hook may run with a different or absent home — and
/// fails closed on an unresolvable <c>current</c>, or a missing, unreadable, or mismatched hash record. This build
/// does not claim the hash check defends the binary against a same-user agent; that is a later build.
/// </summary>
public static class InstallIntegrity
{
    /// <summary>
    /// Checks the integrity of the running install, deriving everything from the resolved binary path.
    /// </summary>
    /// <param name="fileSystem">The platform file system used to detect the <c>current</c> symlink OS-agnostically.</param>
    /// <param name="resolvedBinaryPath">The resolved running binary path
    /// (<see cref="Environment.ProcessPath"/>).</param>
    /// <returns>The integrity report; a denial fails the hook closed.</returns>
    public static IntegrityReport Check(IPlatformFileSystem fileSystem, string? resolvedBinaryPath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        if (string.IsNullOrEmpty(resolvedBinaryPath))
        {
            return IntegrityReport.Deny("the running binary path could not be resolved");
        }

        if (!File.Exists(resolvedBinaryPath))
        {
            return IntegrityReport.Deny("the running binary does not exist at its resolved path");
        }

        string? root = FindInstallRoot(fileSystem, resolvedBinaryPath);
        if (root is null)
        {
            return IntegrityReport.Deny("could not locate the install root (state.json) from the running binary");
        }

        if (!File.Exists(MachinePaths.CurrentBinaryIn(root)))
        {
            return IntegrityReport.Deny("`current` does not resolve to an installed version's binary");
        }

        InstallState? state = ReadState(root);
        if (state is null)
        {
            return IntegrityReport.Deny("state.json is missing, unreadable, or malformed");
        }

        string actual;
        try
        {
            actual = Hashing.Sha256HexOfFile(resolvedBinaryPath);
        }
        catch (IOException exception)
        {
            return IntegrityReport.Deny($"the running binary is unreadable: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return IntegrityReport.Deny($"the running binary is unreadable: {exception.Message}");
        }

        if (!string.Equals(actual, state.Sha256, StringComparison.Ordinal))
        {
            return IntegrityReport.Deny(
                "binary hash does not match the recorded install; re-run `guard install` from a trusted build");
        }

        return IntegrityReport.Allow();
    }

    private static string? FindInstallRoot(IPlatformFileSystem fileSystem, string binaryPath)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(binaryPath));
        while (directory is not null)
        {
            if (File.Exists(MachinePaths.StateFileIn(directory))
                && Directory.Exists(MachinePaths.VersionsDirectoryIn(directory))
                && HasCurrentEntry(fileSystem, directory))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    private static bool HasCurrentEntry(IPlatformFileSystem fileSystem, string root)
    {
        string current = MachinePaths.CurrentIn(root);
        return File.Exists(current)
            || Directory.Exists(current)
            || fileSystem.IsLinkTarget(current);
    }

    private static InstallState? ReadState(string root)
    {
        try
        {
            InstallState? state = SetupJson.DeserializeInstallState(File.ReadAllText(MachinePaths.StateFileIn(root)));
            return state is not null && !string.IsNullOrEmpty(state.Sha256) ? state : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
