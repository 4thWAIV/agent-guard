// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Text.Json.Nodes;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Setup;
using AgentGuard.TestHelpers;
using AgentGuard.TestSupport;

namespace AgentGuard.Tests;

/// <summary>
/// A throwaway machine and project fixture for the setup commands, layered over the copy-on-write simulator
/// (copy-on-write-simulator-design) rather than real disk: an isolated HOME, a project root, and per-install source
/// binaries all live in the one shared in-memory overlay, so the setup logic (idempotency compare, condition
/// detection, install/doctor flow, version-pointer symlinks) runs against it with no real-disk mutation and no
/// per-test cleanup. The executable-flag option is set so the install exercises the POSIX <c>chmod +x</c> path
/// uniformly on every CI leg. The layout paths are derived independently of the production helpers so the tests
/// cross-check the real layout, and every filesystem touch routes through an owned interface off the container.
/// </summary>
public sealed class SetupHarness : IDisposable
{
    private readonly ISystemServices _services;
    private readonly string _root;
    private readonly AgentGuardLayout _layout;

    public SetupHarness()
        : this(presence: null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SetupHarness"/> class over a chosen presence boundary.</summary>
    /// <param name="presence">The presence boundary the approval gate reaches through <c>services.Platform.Presence</c>;
    /// <see langword="null"/> leaves the builder's default (an approving fake) so the command proceeds past the gate.</param>
    public SetupHarness(IPresenceCheck? presence)
    {
        SystemServicesBuilder builder = TestSupport.FakeServices();
        SystemServicesBuilder.PlatformBuilder platform = builder.OnPlatform();
        platform.SimulateExecutableFlag(true);
        if (presence is not null)
        {
            platform.With(presence);
        }

        _services = builder.Build();

        _root = DirectoryWriter.CreateTempSubdirectory("agentguard-setup-");
        Home = System.IO.Path.Combine(_root, "home");
        Project = System.IO.Path.Combine(_root, "project");
        DirectoryWriter.CreateDirectory(Home);
        DirectoryWriter.CreateDirectory(Project);
        _layout = new AgentGuardLayout(Home, Project);
    }

    /// <summary>Gets the isolated HOME directory.</summary>
    public string Home { get; }

    /// <summary>Gets the isolated project root.</summary>
    public string Project { get; }

    /// <summary>Gets the shell profile the PATH line is written to.</summary>
    public string ShellProfilePath => _layout.ShellProfilePath;

    /// <summary>Gets the managed, native-free platform file system the setup commands run against in tests.</summary>
    public IPlatformFileSystem FileSystem => _services.Platform.FileSystem;

    /// <summary>Gets the one simulator-backed service container the hook surface runs the integrity check and the
    /// pipeline through, so a composed-hook test drives everything against the same in-memory overlay the install
    /// wrote to.</summary>
    public ISystemServices Services => _services;

    /// <summary>Gets the install-integrity checker wired to the simulator container, for the integrity tests.</summary>
    public InstallIntegrity Integrity => InstallIntegrity.Create(_services);

    /// <summary>Gets the owned file reader over the overlay, for asserting the files a command wrote.</summary>
    public IFileReader Files => _services.FileSystem.GetFileReader();

    /// <summary>Gets the owned directory enumerator over the overlay, for asserting the directories a command wrote.</summary>
    public IDirectoryEnumerator Directories => _services.FileSystem.GetDirectoryReader();

    /// <summary>Gets the owned file writer over the overlay, for arranging files a test tampers with.</summary>
    public IFileWriter FileWriter => _services.FileSystem.GetFileWriter();

    /// <summary>Gets the owned directory writer over the overlay, for arranging directories a test removes.</summary>
    public IDirectoryWriter DirectoryWriter => _services.FileSystem.GetDirectoryWriter();

    /// <summary>Gets the machine install root.</summary>
    public string AgentGuardRoot => _layout.AgentGuardRoot;

    /// <summary>Gets the launcher path.</summary>
    public string BinGuard => _layout.BinGuard;

    /// <summary>Gets the machine state record path.</summary>
    public string MachineStateFile => _layout.MachineStateFile;

    /// <summary>Gets the <c>current</c> symlink path.</summary>
    public string Current => _layout.Current;

    /// <summary>Gets the binary reached through <c>current</c>.</summary>
    public string CurrentBinary => _layout.CurrentBinary;

    /// <summary>Gets the project's <c>.claude/settings.json</c> path.</summary>
    public string ClaudeSettings => _layout.ClaudeSettings;

    /// <summary>Gets the project's <c>.gitignore</c> path.</summary>
    public string Gitignore => _layout.Gitignore;

    /// <summary>Gets the project's guard directory.</summary>
    public string ProjectAgentGuard => _layout.ProjectAgentGuard;

    /// <summary>Gets the project's config path.</summary>
    public string ProjectConfig => _layout.ProjectConfig;

    /// <summary>Gets the project's state path.</summary>
    public string ProjectStateFile => _layout.ProjectStateFile;

    /// <summary>Returns a version directory's binary path.</summary>
    /// <param name="version">The normalized version.</param>
    /// <returns>The version binary path.</returns>
    public string VersionBinary(string version) => _layout.VersionBinary(version);

    /// <summary>Creates a fresh source binary file with the given content and returns its path.</summary>
    /// <param name="content">The binary content.</param>
    /// <returns>The source binary path.</returns>
    public string MakeSourceBinary(string content)
    {
        string directory = DirectoryWriter.CreateTempSubdirectory("agentguard-src-");
        string path = System.IO.Path.Combine(directory, "guard");
        FileWriter.WriteAllText(path, content);
        return path;
    }

    /// <summary>Builds a context for the given version and resolved binary path.</summary>
    /// <param name="version">The running version.</param>
    /// <param name="binaryPath">The resolved binary path.</param>
    /// <returns>The context.</returns>
    public SetupContext Context(string version, string? binaryPath) => new()
    {
        HomeDirectory = Home,
        ProjectRoot = Project,
        ResolvedBinaryPath = binaryPath,
        RunningVersion = version,
        ShellProfilePath = ShellProfilePath,
        FileReader = Files,
        FileWriter = FileWriter,
        DirectoryWriter = DirectoryWriter,
        Directories = Directories,
        Random = _services.Random,
        FileSystem = FileSystem,
    };

    /// <summary>Installs a version from a fresh source binary and returns the outcome.</summary>
    /// <param name="version">The version to install.</param>
    /// <param name="allowDowngrade">Whether to allow a downgrade.</param>
    /// <param name="content">The binary content; defaults to a per-version marker.</param>
    /// <returns>The install outcome.</returns>
    public CommandOutcome Install(string version, bool allowDowngrade = false, string? content = null)
    {
        string binary = MakeSourceBinary(content ?? $"GUARD BINARY v{version}");
        return SetupCommands.Install(Context(version, binary), _services, allowDowngrade).GetAwaiter().GetResult();
    }

    /// <summary>Runs <c>init</c> against the project.</summary>
    /// <param name="version">The running version to present.</param>
    /// <returns>The init outcome.</returns>
    public CommandOutcome Init(string version = "0.1.0-alpha") =>
        SetupCommands.Init(Context(version, BinGuard), _services).GetAwaiter().GetResult();

    /// <summary>Runs <c>remove</c> against the project.</summary>
    /// <param name="version">The running version to present.</param>
    /// <returns>The remove outcome.</returns>
    public CommandOutcome Remove(string version = "0.1.0-alpha") =>
        SetupCommands.Remove(Context(version, BinGuard), _services).GetAwaiter().GetResult();

    /// <summary>Runs <c>doctor</c> against the machine and project.</summary>
    /// <param name="fix">Whether to repair broken structural conditions.</param>
    /// <param name="version">The running version to present.</param>
    /// <returns>The doctor outcome.</returns>
    public DoctorOutcome Doctor(bool fix, string version = "0.1.0-alpha") =>
        SetupCommands.Doctor(Context(version, BinGuard), fix);

    /// <summary>Reads and parses <c>.claude/settings.json</c>.</summary>
    /// <returns>The parsed settings root.</returns>
    public JsonObject ReadSettings() => (JsonObject)JsonNode.Parse(Files.ReadAllText(ClaudeSettings))!;

    /// <summary>Writes raw <c>.claude/settings.json</c> content, creating the directory.</summary>
    /// <param name="json">The content.</param>
    public void WriteSettings(string json)
    {
        DirectoryWriter.CreateDirectory(System.IO.Path.GetDirectoryName(ClaudeSettings)!);
        FileWriter.WriteAllText(ClaudeSettings, json);
    }

    /// <summary>Relocates the guard hook entry's command to a stale launcher path, keeping it a guard command.</summary>
    /// <param name="eventKey">The event key.</param>
    public void MakeGuardEntryStale(string eventKey)
    {
        JsonObject settings = ReadSettings();
        JsonObject group = SettingsProbe.GuardGroup(settings, eventKey)!;
        string current = SettingsProbe.CommandOf(group)!;
        group["hooks"]![0]!["command"] =
            current.Replace(BinGuard, "/old/relocated/.agentguard/bin/guard", StringComparison.Ordinal);
        FileWriter.WriteAllText(ClaudeSettings, settings.ToJsonString());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // The overlay is discarded with the container, so there is no real disk to clean; the best-effort delete
        // keeps the in-memory store tidy when a single harness is reused across arrange/act/assert.
        try
        {
            DirectoryWriter.DeleteDirectory(_root, recursive: true);
        }
        catch (System.IO.IOException)
        {
            // Best-effort cleanup of a throwaway in-memory tree.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup of a throwaway in-memory tree.
        }
    }
}
