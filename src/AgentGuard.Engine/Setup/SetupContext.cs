// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using AgentGuard.CrossPlatform;

namespace AgentGuard.Setup;

/// <summary>
/// The immutable inputs every setup command works from. The setup commands derive the machine layout under
/// <see cref="HomeDirectory"/> and the project wiring under <see cref="ProjectRoot"/> from this context, and never
/// re-read process state on their own, so tests can drive them against throwaway home and project directories.
/// </summary>
public sealed record SetupContext
{
    /// <summary>
    /// Gets the user's home directory; the machine install lives at <c>{HomeDirectory}/.agentguard</c>.
    /// </summary>
    public required string HomeDirectory { get; init; }

    /// <summary>
    /// Gets the repository root the project wiring is written under.
    /// </summary>
    public required string ProjectRoot { get; init; }

    /// <summary>
    /// Gets the resolved path of the running binary (<see cref="Environment.ProcessPath"/>), or
    /// <see langword="null"/> when it cannot be resolved. <c>install</c> copies this file into the version store.
    /// </summary>
    public string? ResolvedBinaryPath { get; init; }

    /// <summary>
    /// Gets the version string of the running binary; parsed with SemVer precedence for the downgrade check.
    /// </summary>
    public required string RunningVersion { get; init; }

    /// <summary>
    /// Gets the shell profile file the PATH line is appended to.
    /// </summary>
    public required string ShellProfilePath { get; init; }

    /// <summary>
    /// Gets the platform file-system capability the setup commands use for the version-pointer symlinks and the
    /// executable bit. In production it is the per-OS implementation from <c>Platform.Create()</c>; tests inject a
    /// managed test double so they never touch native.
    /// </summary>
    public required IPlatformFileSystem FileSystem { get; init; }

    /// <summary>
    /// Builds a context for the current process: the user's home directory, the current working directory as the
    /// project root, the resolved running binary, the entry assembly's informational version, the PATH profile
    /// resolved from the detected login shell, and the per-OS platform file system from <c>Platform.Create()</c>.
    /// </summary>
    /// <returns>The context for the current process.</returns>
    public static SetupContext ForCurrentProcess()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string shell = Environment.GetEnvironmentVariable("SHELL") ?? string.Empty;
        return new SetupContext
        {
            HomeDirectory = home,
            ProjectRoot = Directory.GetCurrentDirectory(),
            ResolvedBinaryPath = Environment.ProcessPath,
            RunningVersion = ReadRunningVersion(),
            ShellProfilePath = ShellProfile.Resolve(shell, home),
            FileSystem = Platform.Create().FileSystem,
        };
    }

    private static string ReadRunningVersion()
    {
        Assembly? entry = Assembly.GetEntryAssembly();
        string? informational = entry?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        Version? assemblyVersion = entry?.GetName().Version;
        return assemblyVersion?.ToString() ?? "0.0.0";
    }
}
