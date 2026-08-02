// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the machine-install layout under <c>~/.agentguard/</c>: the version store
/// (<c>versions/&lt;v&gt;/guard</c>), the <c>current</c> version symlink, the stable <c>bin/guard</c> launcher,
/// and the <c>state.json</c> record. Every setup command and the hook integrity self-check derive these locations
/// here — from a context or from a bare root — so they cannot drift.
/// </summary>
internal static class MachinePaths
{
    /// <summary>
    /// The install root directory name under the user's home directory. It draws the guard's dot-directory segment
    /// from its single owner rather than re-spelling the literal.
    /// </summary>
    internal const string RootDirectoryName = CoreSystemPaths.InRepoDirectoryName;

    /// <summary>
    /// The file name of the guard binary within each version directory and behind the launcher.
    /// </summary>
    internal const string BinaryName = "guard";

    /// <summary>
    /// The machine state record file name.
    /// </summary>
    internal const string StateFileName = "state.json";

    /// <summary>
    /// The version-store directory name.
    /// </summary>
    internal const string VersionsName = "versions";

    /// <summary>
    /// The current-version symlink name.
    /// </summary>
    internal const string CurrentName = "current";

    private const string BinName = "bin";

    /// <summary>
    /// Returns the install root, <c>{home}/.agentguard</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute install root.</returns>
    internal static string Root(SetupContext context) => Path.Combine(context.HomeDirectory, RootDirectoryName);

    /// <summary>
    /// Returns the version store directory under an install root.
    /// </summary>
    /// <param name="root">The install root.</param>
    /// <returns>The absolute version store directory.</returns>
    internal static string VersionsDirectoryIn(string root) => Path.Combine(root, VersionsName);

    /// <summary>
    /// Returns the <c>current</c> symlink under an install root.
    /// </summary>
    /// <param name="root">The install root.</param>
    /// <returns>The absolute <c>current</c> symlink path.</returns>
    internal static string CurrentIn(string root) => Path.Combine(root, CurrentName);

    /// <summary>
    /// Returns the binary reached through <c>current</c> under an install root.
    /// </summary>
    /// <param name="root">The install root.</param>
    /// <returns>The absolute path of the current binary.</returns>
    internal static string CurrentBinaryIn(string root) => Path.Combine(CurrentIn(root), BinaryName);

    /// <summary>
    /// Returns the machine state record under an install root.
    /// </summary>
    /// <param name="root">The install root.</param>
    /// <returns>The absolute machine state path.</returns>
    internal static string StateFileIn(string root) => Path.Combine(root, StateFileName);

    /// <summary>
    /// Returns the version store directory, <c>{root}/versions</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute version store directory.</returns>
    internal static string VersionsDirectory(SetupContext context) => VersionsDirectoryIn(Root(context));

    /// <summary>
    /// Returns a single version's directory, <c>{root}/versions/{version}</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="version">The normalized version.</param>
    /// <returns>The absolute version directory.</returns>
    internal static string VersionDirectory(SetupContext context, string version) =>
        Path.Combine(VersionsDirectory(context), version);

    /// <summary>
    /// Returns a single version's binary, <c>{root}/versions/{version}/guard</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="version">The normalized version.</param>
    /// <returns>The absolute version binary path.</returns>
    internal static string VersionBinary(SetupContext context, string version) =>
        Path.Combine(VersionDirectory(context, version), BinaryName);

    /// <summary>
    /// Returns the <c>current</c> version symlink, <c>{root}/current</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute <c>current</c> symlink path.</returns>
    internal static string Current(SetupContext context) => CurrentIn(Root(context));

    /// <summary>
    /// Returns the binary reached through <c>current</c>, <c>{root}/current/guard</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute path of the current binary.</returns>
    internal static string CurrentBinary(SetupContext context) => CurrentBinaryIn(Root(context));

    /// <summary>
    /// Returns the launcher directory, <c>{root}/bin</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute launcher directory.</returns>
    internal static string BinDirectory(SetupContext context) => Path.Combine(Root(context), BinName);

    /// <summary>
    /// Returns the launcher, <c>{root}/bin/guard</c>, the absolute path project wiring calls.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute launcher path.</returns>
    internal static string BinGuard(SetupContext context) => BinGuardIn(context.HomeDirectory);

    /// <summary>
    /// Returns the launcher, <c>{home}/.agentguard/bin/guard</c>, from a bare home directory. It is the one place
    /// the launcher layout is spelled, so the engine's canonical settings check and the setup writer agree on the
    /// path the guard hooks point at.
    /// </summary>
    /// <param name="home">The user's home directory.</param>
    /// <returns>The absolute launcher path.</returns>
    internal static string BinGuardIn(string home) =>
        Path.Combine(home, RootDirectoryName, BinName, BinaryName);

    /// <summary>
    /// Returns the machine state record, <c>{root}/state.json</c>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The absolute machine state path.</returns>
    internal static string StateFile(SetupContext context) => StateFileIn(Root(context));

    /// <summary>
    /// Returns the relative target the <c>current</c> symlink points at, <c>versions/{version}</c>.
    /// </summary>
    /// <param name="version">The normalized version.</param>
    /// <returns>The relative symlink target.</returns>
    internal static string CurrentRelativeTarget(string version) => Path.Combine(VersionsName, version);

    /// <summary>
    /// Returns the relative target the launcher points at, <c>../current/guard</c>.
    /// </summary>
    /// <returns>The relative launcher target.</returns>
    internal static string BinGuardRelativeTarget() => Path.Combine("..", CurrentName, BinaryName);
}
