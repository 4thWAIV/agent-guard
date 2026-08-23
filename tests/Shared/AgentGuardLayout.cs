// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;

namespace AgentGuard.TestSupport;

/// <summary>
/// The machine-and-project on-disk layout the guard writes, derived here — independently of the production path
/// helpers (<c>MachinePaths</c>, <c>ProjectPaths</c>) — so the tests cross-check the real layout rather than trust
/// it. It is the single owner of that derivation shared by the setup and CLI test harnesses, so the two never
/// re-spell the same paths. The source file is linked into each test project that needs it.
/// </summary>
internal sealed class AgentGuardLayout
{
    /// <summary>Initializes the layout for an isolated home and project directory.</summary>
    /// <param name="home">The isolated HOME directory the machine install lives under.</param>
    /// <param name="project">The isolated project root the wiring is written under.</param>
    public AgentGuardLayout(string home, string project)
    {
        Home = home;
        Project = project;
    }

    /// <summary>Gets the isolated HOME directory the machine install lives under.</summary>
    public string Home { get; }

    /// <summary>Gets the isolated project root the wiring is written under.</summary>
    public string Project { get; }

    /// <summary>Gets the machine install root, <c>{Home}/.agentguard</c>.</summary>
    public string AgentGuardRoot => Path.Combine(Home, ".agentguard");

    /// <summary>Gets the stable launcher, <c>{Home}/.agentguard/bin/guard</c>.</summary>
    public string BinGuard => Path.Combine(AgentGuardRoot, "bin", "guard");

    /// <summary>Gets the machine state record, <c>{Home}/.agentguard/state.json</c>.</summary>
    public string MachineStateFile => Path.Combine(AgentGuardRoot, "state.json");

    /// <summary>Gets the <c>current</c> version symlink, <c>{Home}/.agentguard/current</c>.</summary>
    public string Current => Path.Combine(AgentGuardRoot, "current");

    /// <summary>Gets the binary reached through <c>current</c>, <c>{Home}/.agentguard/current/guard</c>.</summary>
    public string CurrentBinary => Path.Combine(Current, "guard");

    /// <summary>Gets the shell profile the PATH line is written to, <c>{Home}/.zshrc</c>.</summary>
    public string ShellProfilePath => Path.Combine(Home, ".zshrc");

    /// <summary>Gets the project's <c>.claude/settings.json</c> path.</summary>
    public string ClaudeSettings => Path.Combine(Project, ".claude", "settings.json");

    /// <summary>Gets the project's <c>.gitignore</c> path.</summary>
    public string Gitignore => Path.Combine(Project, ".gitignore");

    /// <summary>Gets the project's guard directory, <c>{Project}/.agentguard</c>.</summary>
    public string ProjectAgentGuard => Path.Combine(Project, ".agentguard");

    /// <summary>Gets the project's config path, <c>{Project}/.agentguard/config.json</c>.</summary>
    public string ProjectConfig => Path.Combine(ProjectAgentGuard, "config.json");

    /// <summary>Gets the project's state path, <c>{Project}/.agentguard/state.json</c>.</summary>
    public string ProjectStateFile => Path.Combine(ProjectAgentGuard, "state.json");

    /// <summary>Gets the project's grants directory, <c>{Project}/.agentguard/grants</c>.</summary>
    public string ProjectGrants => Path.Combine(ProjectAgentGuard, "grants");

    /// <summary>Returns a version directory's binary, <c>{Home}/.agentguard/versions/{version}/guard</c>.</summary>
    /// <param name="version">The normalized version.</param>
    /// <returns>The version binary path.</returns>
    public string VersionBinary(string version) => Path.Combine(AgentGuardRoot, "versions", version, "guard");
}
