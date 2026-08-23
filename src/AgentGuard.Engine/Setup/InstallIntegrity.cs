// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Text.Json;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// The hook integrity self-check that gates a <c>guard hook</c> run. It derives the install root from the
/// resolved running binary — never from <c>$HOME</c>, since a hook may run with a different or absent home — and
/// fails closed on an unresolvable <c>current</c>, or a missing, unreadable, or mismatched hash record. This build
/// does not claim the hash check defends the binary against a same-user agent; that is a later build. It is an
/// instance built from the container: it reads the environment, filesystem, and platform through owned services
/// received by constructor.
/// </summary>
public sealed class InstallIntegrity
{
    private readonly IEnvironment _environment;
    private readonly IFileReader _fileReader;
    private readonly IDirectoryEnumerator _directories;
    private readonly IPlatformFileSystem _fileSystem;

    private InstallIntegrity(
        IEnvironment environment,
        IFileReader fileReader,
        IDirectoryEnumerator directories,
        IPlatformFileSystem fileSystem)
    {
        _environment = environment;
        _fileReader = fileReader;
        _directories = directories;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Builds the integrity self-check from the owned service container.
    /// </summary>
    /// <param name="services">The single OS/CLR service container this check reads the environment, filesystem, and
    /// platform through.</param>
    /// <returns>The integrity self-check.</returns>
    public static InstallIntegrity Create(ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new InstallIntegrity(
            services.Environment, services.FileSystem.GetFileReader(), services.FileSystem.GetDirectoryReader(), services.Platform.FileSystem);
    }

    /// <summary>
    /// Checks the integrity of the running install, deriving everything from the resolved binary path.
    /// </summary>
    /// <param name="resolvedBinaryPath">The resolved running binary path
    /// (<see cref="IEnvironment.GetProcessPath"/>).</param>
    /// <returns>The integrity report; a denial fails the hook closed.</returns>
    public IntegrityReport Check(string? resolvedBinaryPath)
    {
        if (string.IsNullOrEmpty(resolvedBinaryPath))
        {
            return IntegrityReport.Deny("the running binary path could not be resolved");
        }

        if (!_fileReader.Exists(resolvedBinaryPath))
        {
            return IntegrityReport.Deny("the running binary does not exist at its resolved path");
        }

        string? root = FindInstallRoot(resolvedBinaryPath);
        if (root is null)
        {
            return IntegrityReport.Deny("could not locate the install root (state.json) from the running binary");
        }

        if (!_fileReader.Exists(MachinePaths.CurrentBinaryIn(root)))
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
            actual = Hashing.Sha256Hex(_fileReader.ReadAllBytes(resolvedBinaryPath));
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

    private string? FindInstallRoot(string binaryPath)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(binaryPath, _environment.GetCurrentDirectory()));
        while (directory is not null)
        {
            if (_fileReader.Exists(MachinePaths.StateFileIn(directory))
                && _directories.DirectoryExists(MachinePaths.VersionsDirectoryIn(directory))
                && HasCurrentEntry(directory))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    private bool HasCurrentEntry(string root)
    {
        string current = MachinePaths.CurrentIn(root);
        return _fileReader.Exists(current)
            || _directories.DirectoryExists(current)
            || _fileSystem.IsLinkTarget(current);
    }

    private InstallState? ReadState(string root)
    {
        try
        {
            InstallState? state = SetupJson.DeserializeInstallState(_fileReader.ReadAllText(MachinePaths.StateFileIn(root)));
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
