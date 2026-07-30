// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Text.Json.Nodes;
using AgentGuard.Setup;

namespace AgentGuard.Tests;

/// <summary>
/// A throwaway machine and project fixture for the setup commands. It owns a disposable temporary tree holding an
/// isolated HOME, a project root, and per-install source binaries, and derives the on-disk paths independently of
/// the production helpers so the tests cross-check the real layout.
/// </summary>
public sealed class SetupHarness : IDisposable
{
    private readonly string _root;

    public SetupHarness()
    {
        _root = Directory.CreateTempSubdirectory("agentguard-setup-").FullName;
        Home = Path.Combine(_root, "home");
        Project = Path.Combine(_root, "project");
        Directory.CreateDirectory(Home);
        Directory.CreateDirectory(Project);
        ShellProfilePath = Path.Combine(Home, ".zshrc");
    }

    /// <summary>Gets the isolated HOME directory.</summary>
    public string Home { get; }

    /// <summary>Gets the isolated project root.</summary>
    public string Project { get; }

    /// <summary>Gets the shell profile the PATH line is written to.</summary>
    public string ShellProfilePath { get; }

    /// <summary>Gets the machine install root.</summary>
    public string AgentGuardRoot => Path.Combine(Home, ".agentguard");

    /// <summary>Gets the launcher path.</summary>
    public string BinGuard => Path.Combine(AgentGuardRoot, "bin", "guard");

    /// <summary>Gets the machine state record path.</summary>
    public string MachineStateFile => Path.Combine(AgentGuardRoot, "state.json");

    /// <summary>Gets the <c>current</c> symlink path.</summary>
    public string Current => Path.Combine(AgentGuardRoot, "current");

    /// <summary>Gets the binary reached through <c>current</c>.</summary>
    public string CurrentBinary => Path.Combine(Current, "guard");

    /// <summary>Gets the project's <c>.claude/settings.json</c> path.</summary>
    public string ClaudeSettings => Path.Combine(Project, ".claude", "settings.json");

    /// <summary>Gets the project's <c>.gitignore</c> path.</summary>
    public string Gitignore => Path.Combine(Project, ".gitignore");

    /// <summary>Gets the project's guard directory.</summary>
    public string ProjectAgentGuard => Path.Combine(Project, ".agentguard");

    /// <summary>Gets the project's config path.</summary>
    public string ProjectConfig => Path.Combine(ProjectAgentGuard, "config.json");

    /// <summary>Gets the project's state path.</summary>
    public string ProjectStateFile => Path.Combine(ProjectAgentGuard, "state.json");

    /// <summary>Returns a version directory's binary path.</summary>
    /// <param name="version">The normalized version.</param>
    /// <returns>The version binary path.</returns>
    public string VersionBinary(string version) => Path.Combine(AgentGuardRoot, "versions", version, "guard");

    /// <summary>Creates a fresh source binary file with the given content and returns its path.</summary>
    /// <param name="content">The binary content.</param>
    /// <returns>The source binary path.</returns>
    public string MakeSourceBinary(string content)
    {
        string directory = Path.Combine(_root, "src-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "guard");
        File.WriteAllText(path, content);
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
    };

    /// <summary>Installs a version from a fresh source binary and returns the outcome.</summary>
    /// <param name="version">The version to install.</param>
    /// <param name="allowDowngrade">Whether to allow a downgrade.</param>
    /// <param name="content">The binary content; defaults to a per-version marker.</param>
    /// <returns>The install outcome.</returns>
    public CommandOutcome Install(string version, bool allowDowngrade = false, string? content = null)
    {
        string binary = MakeSourceBinary(content ?? $"GUARD BINARY v{version}");
        return SetupCommands.Install(Context(version, binary), allowDowngrade);
    }

    /// <summary>Runs <c>init</c> against the project.</summary>
    /// <param name="version">The running version to present.</param>
    /// <returns>The init outcome.</returns>
    public CommandOutcome Init(string version = "0.1.0-alpha") => SetupCommands.Init(Context(version, BinGuard));

    /// <summary>Runs <c>remove</c> against the project.</summary>
    /// <param name="version">The running version to present.</param>
    /// <returns>The remove outcome.</returns>
    public CommandOutcome Remove(string version = "0.1.0-alpha") => SetupCommands.Remove(Context(version, BinGuard));

    /// <summary>Runs <c>doctor</c> against the machine and project.</summary>
    /// <param name="fix">Whether to repair broken structural conditions.</param>
    /// <param name="version">The running version to present.</param>
    /// <returns>The doctor outcome.</returns>
    public DoctorOutcome Doctor(bool fix, string version = "0.1.0-alpha") =>
        SetupCommands.Doctor(Context(version, BinGuard), fix);

    /// <summary>Reads and parses <c>.claude/settings.json</c>.</summary>
    /// <returns>The parsed settings root.</returns>
    public JsonObject ReadSettings() => (JsonObject)JsonNode.Parse(File.ReadAllText(ClaudeSettings))!;

    /// <summary>Writes raw <c>.claude/settings.json</c> content, creating the directory.</summary>
    /// <param name="json">The content.</param>
    public void WriteSettings(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ClaudeSettings)!);
        File.WriteAllText(ClaudeSettings, json);
    }

    /// <summary>Rewrites a guard hook entry's command to a stale path, keeping the sentinel.</summary>
    /// <param name="eventKey">The event key.</param>
    /// <param name="matcher">The matcher of the guard group to make stale.</param>
    public void MakeGuardEntryStale(string eventKey, string matcher)
    {
        JsonObject settings = ReadSettings();
        JsonObject group = SettingsProbe.GuardGroup(settings, eventKey, matcher)!;
        group["hooks"]![0]!["command"] =
            "/old/relocated/.agentguard/bin/guard hook pre --host claude-code --agentguard-owned";
        File.WriteAllText(ClaudeSettings, settings.ToJsonString());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
