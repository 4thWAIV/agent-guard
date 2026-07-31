// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the imperative creation and merge work: the version-store copy, the atomic <c>current</c>
/// flip, the launcher symlink, the PATH line, the machine and project records, and the settings and gitignore
/// merges. <c>install</c>, <c>init</c>, <c>remove</c>, and every condition's <c>Repair</c> route through here, so
/// the layout logic is written once.
/// </summary>
internal static class CreationHelper
{
    private const UnixFileMode ExecutableMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

    /// <summary>
    /// Copies the running binary into <c>versions/&lt;version&gt;/guard</c>, skipping when a byte-identical binary
    /// is already there (which also guarantees the running image is never overwritten).
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome EnsureVersionBinary(SetupContext context)
    {
        if (string.IsNullOrEmpty(context.ResolvedBinaryPath) || !File.Exists(context.ResolvedBinaryPath))
        {
            return RepairOutcome.NotRepairable("the running binary path could not be resolved");
        }

        string version = SemVer.Normalize(context.RunningVersion);
        string destination = MachinePaths.VersionBinary(context, version);
        string sourceHash = Hashing.Sha256HexOfFile(context.ResolvedBinaryPath);
        if (File.Exists(destination)
            && string.Equals(Hashing.Sha256HexOfFile(destination), sourceHash, StringComparison.Ordinal))
        {
            return RepairOutcome.NoChangeNeeded();
        }

        Directory.CreateDirectory(MachinePaths.VersionDirectory(context, version));
        AtomicFile.CopyOver(context.ResolvedBinaryPath, destination);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(destination, ExecutableMode);
        }

        return RepairOutcome.Repaired($"installed the binary at versions/{version}/guard");
    }

    /// <summary>
    /// Atomically points <c>current</c> at the running version.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome PointCurrent(SetupContext context)
    {
        string version = SemVer.Normalize(context.RunningVersion);
        Directory.CreateDirectory(MachinePaths.VersionDirectory(context, version));
        bool changed = SymlinkOps.EnsurePointsTo(
            MachinePaths.Current(context), MachinePaths.CurrentRelativeTarget(version));
        return changed ? RepairOutcome.Repaired($"pointed current at versions/{version}") : RepairOutcome.NoChangeNeeded();
    }

    /// <summary>
    /// Ensures the stable launcher <c>bin/guard</c> resolves to <c>current/guard</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome EnsureBinGuard(SetupContext context)
    {
        Directory.CreateDirectory(MachinePaths.BinDirectory(context));
        bool changed = SymlinkOps.EnsurePointsTo(
            MachinePaths.BinGuard(context), MachinePaths.BinGuardRelativeTarget());
        return changed ? RepairOutcome.Repaired("ensured bin/guard resolves to current/guard") : RepairOutcome.NoChangeNeeded();
    }

    /// <summary>
    /// Ensures the PATH line is present in the shell profile.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome EnsurePathLine(SetupContext context)
    {
        bool changed = PathProfileWiring.Ensure(context.ShellProfilePath);
        return changed ? RepairOutcome.Repaired($"added the PATH line to {context.ShellProfilePath}") : RepairOutcome.NoChangeNeeded();
    }

    /// <summary>
    /// Writes the machine state record.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="version">The normalized installed version.</param>
    /// <param name="sha256">The installed binary's SHA-256.</param>
    internal static void WriteMachineState(SetupContext context, string version, string sha256) =>
        AtomicFile.WriteAllText(MachinePaths.StateFile(context), SetupJson.Serialize(new InstallState(version, sha256)));

    /// <summary>
    /// Creates the project's <c>.agentguard/</c> base: the directory, the grant store, and (never generating one)
    /// the committed public key when it is already present.
    /// </summary>
    /// <param name="context">The setup context.</param>
    internal static void EnsureAgentGuardBase(SetupContext context)
    {
        Directory.CreateDirectory(ProjectPaths.AgentGuardDirectory(context));
        Directory.CreateDirectory(ProjectPaths.GrantsDirectory(context));

        // The public key is never generated here. A key already committed at the project path is left in place;
        // there is no other source in this build (grant minting and keys are a later build).
    }

    /// <summary>
    /// Ensures <c>.agentguard/config.json</c> exists and parses, writing the default when it is missing or corrupt.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome EnsureConfig(SetupContext context)
    {
        string path = ProjectPaths.ConfigFile(context);
        if (File.Exists(path) && ParsesAsObject(path))
        {
            return RepairOutcome.NoChangeNeeded();
        }

        AtomicFile.WriteAllText(path, SetupJson.Serialize(ProjectConfig.Default()));
        return RepairOutcome.Repaired("wrote .agentguard/config.json");
    }

    /// <summary>
    /// Writes the per-project state record stamping the wired version.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="version">The wired version.</param>
    internal static void WriteProjectState(SetupContext context, string version) =>
        AtomicFile.WriteAllText(ProjectPaths.StateFile(context), SetupJson.Serialize(new ProjectState(version)));

    /// <summary>
    /// Merges the guard's hook entries into <c>.claude/settings.json</c>, refusing (as not-repairable) on a real
    /// conflict and leaving the file untouched.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome WireSettings(SetupContext context)
    {
        SettingsRead settings = ClaudeSettings.Read(context);
        if (!settings.Readable)
        {
            return RepairOutcome.NotRepairable(settings.UnreadableReason!);
        }

        SettingsMergeResult result = ClaudeSettingsWiring.AddGuardEntries(settings.Content, MachinePaths.BinGuard(context));
        if (!result.Success)
        {
            return RepairOutcome.NotRepairable(result.Conflict ?? "the settings file could not be merged");
        }

        AtomicFile.WriteAllText(ProjectPaths.ClaudeSettingsFile(context), result.Json!);
        return RepairOutcome.Repaired("wired .claude/settings.json");
    }

    /// <summary>
    /// Strips the guard's hook entries from <c>.claude/settings.json</c>, preserving all non-guard content.
    /// </summary>
    /// <param name="context">The setup context.</param>
    internal static void UnwireSettings(SetupContext context)
    {
        string path = ProjectPaths.ClaudeSettingsFile(context);
        if (!File.Exists(path))
        {
            return;
        }

        if (!SafeRead.TryReadText(path, out string existing, out _))
        {
            return;
        }

        SettingsMergeResult result = ClaudeSettingsWiring.RemoveGuardEntries(existing);
        if (result.Success && result.Json is not null)
        {
            AtomicFile.WriteAllText(path, result.Json);
        }
    }

    /// <summary>
    /// Adds the runtime-store ignore lines to <c>.gitignore</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    internal static RepairOutcome EnsureGitignore(SetupContext context)
    {
        bool changed = GitignoreWiring.Ensure(ProjectPaths.GitignoreFile(context));
        return changed ? RepairOutcome.Repaired("added the runtime-store lines to .gitignore") : RepairOutcome.NoChangeNeeded();
    }

    /// <summary>
    /// Removes the project's <c>.agentguard/</c> wiring directory.
    /// </summary>
    /// <param name="context">The setup context.</param>
    internal static void RemoveAgentGuardDirectory(SetupContext context)
    {
        string directory = ProjectPaths.AgentGuardDirectory(context);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static bool ParsesAsObject(string path) =>
        SafeRead.TryReadText(path, out string content, out _) && SetupJson.TryParseObject(content, out _);
}
