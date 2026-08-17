// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// The immutable inputs every setup command works from. The setup commands derive the machine layout under
/// <see cref="HomeDirectory"/> and the project wiring under <see cref="ProjectRoot"/> from this context, and never
/// re-read process state on their own, so tests can drive them against throwaway home and project directories.
/// It carries the owned boundary services — the read and write sides of the filesystem, the directory enumerator,
/// the GUID factory, and the platform file system — so every setup helper routes its work through an injected
/// owner rather than a raw call, and a test substitutes them through the container.
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
    /// Gets the resolved path of the running binary (<see cref="IEnvironment.GetProcessPath"/>), or
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
    /// Gets the owned read side of the filesystem the setup helpers read files through.
    /// </summary>
    public required IFileReader FileReader { get; init; }

    /// <summary>
    /// Gets the owned file-write side of the filesystem the setup helpers write files through.
    /// </summary>
    public required IFileWriter FileWriter { get; init; }

    /// <summary>
    /// Gets the owned directory-write side of the filesystem the setup helpers create and remove directories through.
    /// </summary>
    public required IDirectoryWriter DirectoryWriter { get; init; }

    /// <summary>
    /// Gets the owned read-only directory enumerator the setup helpers probe directories through.
    /// </summary>
    public required IDirectoryEnumerator Directories { get; init; }

    /// <summary>
    /// Gets the owned GUID factory used to name atomic temporary files.
    /// </summary>
    public required IGuidFactory Guids { get; init; }

    /// <summary>
    /// Gets the platform file-system capability the setup commands use for the version-pointer symlinks and the
    /// executable bit. In production it is the per-OS implementation carried on the container; tests inject a
    /// managed test double so they never touch native.
    /// </summary>
    public required IPlatformFileSystem FileSystem { get; init; }

    /// <summary>
    /// Builds a context for the current process from the owned service container: the user's home directory, the
    /// current working directory as the project root, the resolved running binary, the running build's
    /// informational version, the PATH profile resolved from the detected login shell, and the owned boundary
    /// services. The clock is not needed by setup, so only the boundary services are unpacked.
    /// </summary>
    /// <param name="services">The single OS/CLR service container this context draws every owned service and its
    /// process/version reads from.</param>
    /// <returns>The context for the current process.</returns>
    public static SetupContext ForCurrentProcess(ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        IEnvironment environment = services.Environment;
        string home = environment.GetHomeDirectory();
        string shell = environment.GetEnvironmentVariable("SHELL") ?? string.Empty;
        return new SetupContext
        {
            HomeDirectory = home,
            ProjectRoot = environment.GetCurrentDirectory(),
            ResolvedBinaryPath = environment.GetProcessPath(),
            RunningVersion = services.BuildInfo.SemVer,
            ShellProfilePath = ShellProfile.Resolve(shell, home),
            FileReader = services.FileSystem.GetFileReader(),
            FileWriter = services.FileSystem.GetFileWriter(),
            DirectoryWriter = services.FileSystem.GetDirectoryWriter(),
            Directories = services.FileSystem.GetDirectoryReader(),
            Guids = services.Guids,
            FileSystem = services.Platform.FileSystem,
        };
    }
}
