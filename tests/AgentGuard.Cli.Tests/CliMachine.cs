// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using AgentGuard.TestSupport;

namespace AgentGuard.Cli.Tests;

/// <summary>
/// An inspectable, isolated machine and project the CLI runs against across several commands. It owns a persistent
/// isolated HOME and working directory, so a test can complete a real <c>guard install</c>, then <c>guard init</c>
/// against that installed machine, and assert the files each command actually wrote. Every command runs the
/// production wiring through the internal <see cref="Program.Run(ISystemServices)"/> seam (reached through the
/// <c>InternalsVisibleTo</c> grant) with a container built by <see cref="SystemServicesBuilder"/>: the real
/// filesystem, platform, signatures, build-info, and clock, but an injected <see cref="FakeEnvironment"/> whose
/// home and working directory point here and a <see cref="RecordingConsole"/> that captures the output. No real
/// process state is mutated — HOME, the working directory, and the console streams are all injected, not set on
/// the process — so the isolation holds on every OS, not only where a <c>HOME</c> redirect would (this is what
/// retired the POSIX-only gate). The isolated directories are created and torn down through the owned
/// <see cref="IDirectoryWriter"/>, never a raw <c>System.IO</c> call.
/// </summary>
internal sealed class CliMachine : IDisposable
{
    private readonly AgentGuardLayout _layout;
    private readonly ISystemServices _hostServices;
    private readonly string _binarySourcePath;

    public CliMachine()
    {
        // A real container is used only to create and later remove the isolated directories through the owned
        // interface (IDirectoryWriter.CreateTempSubdirectory is atomic and uniquely named) and to write the
        // stand-in binary the install path copies — every filesystem touch here routes through an owner, never a
        // raw System.IO call.
        _hostServices = SystemServicesBuilder.Real().Build();
        IDirectoryWriter directories = _hostServices.FileSystem.GetDirectoryWriter();
        Home = directories.CreateTempSubdirectory("agentguard-clisession-home-");
        Project = directories.CreateTempSubdirectory("agentguard-clisession-proj-");
        _layout = new AgentGuardLayout(Home, Project);

        // The "running binary" the injected environment reports as the process path: a small real file outside the
        // install root, mirroring how the production process path points at the guard executable. The install
        // command reads and hashes it, then copies it into the version store.
        _binarySourcePath = Path.Combine(Home, "guard-source");
        _hostServices.FileSystem.GetFileWriter().WriteAllText(_binarySourcePath, "agentguard-cli-test-binary");
    }

    /// <summary>Gets the isolated HOME directory the machine install lives under.</summary>
    public string Home { get; }

    /// <summary>Gets the isolated working directory the project wiring is written under.</summary>
    public string Project { get; }

    /// <summary>Gets the machine install root, <c>{Home}/.agentguard</c>.</summary>
    public string AgentGuardRoot => _layout.AgentGuardRoot;

    /// <summary>Gets the machine state record, <c>{Home}/.agentguard/state.json</c>.</summary>
    public string MachineStateFile => _layout.MachineStateFile;

    /// <summary>Gets the stable launcher, <c>{Home}/.agentguard/bin/guard</c>.</summary>
    public string BinGuard => _layout.BinGuard;

    /// <summary>Gets the project's <c>.claude/settings.json</c> path.</summary>
    public string ClaudeSettings => _layout.ClaudeSettings;

    /// <summary>Gets the project's guard directory, <c>{Project}/.agentguard</c>.</summary>
    public string ProjectAgentGuard => _layout.ProjectAgentGuard;

    /// <summary>Gets the project's config path, <c>{Project}/.agentguard/config.json</c>.</summary>
    public string ProjectConfig => _layout.ProjectConfig;

    /// <summary>Gets the project's grants directory, <c>{Project}/.agentguard/grants</c>.</summary>
    public string ProjectGrants => _layout.ProjectGrants;

    /// <summary>
    /// Gets the owned file reader over the real filesystem, for asserting the files a command wrote (existence and
    /// content) through the owned interface rather than a raw <c>System.IO</c> call.
    /// </summary>
    public IFileReader Files => _hostServices.FileSystem.GetFileReader();

    /// <summary>
    /// Gets the owned directory enumerator over the real filesystem, for asserting the directories a command wrote
    /// through the owned interface rather than a raw <c>System.IO</c> call.
    /// </summary>
    public IDirectoryEnumerator Directories => _hostServices.FileSystem.GetDirectoryReader();

    /// <summary>
    /// Runs the CLI with the given arguments and empty standard input against this machine and project.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public CliResult Run(params string[] args) => RunWithInput(string.Empty, args);

    /// <summary>
    /// Runs the CLI with the given standard input and arguments against this machine and project, driving the
    /// internal <see cref="Program.Run(ISystemServices)"/> seam with a container whose environment and console are
    /// injected fakes pointing here.
    /// </summary>
    /// <param name="standardInput">The text the injected console returns from a read.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public CliResult RunWithInput(string standardInput, params string[] args)
    {
        var recorder = new RecordingConsole(standardInput);
        IEnvironment environment = FakeEnvironment.Create(
            Home, currentDirectory: Project, processPath: _binarySourcePath);
        ISystemServices services = SystemServicesBuilder.Real()
            .With(environment)
            .With(recorder.Console)
            .Build();

        int exitCode = new Program(args).Run(services).GetAwaiter().GetResult();
        return new CliResult(exitCode, recorder.Output, recorder.Error);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DeleteBestEffort(Home);
        DeleteBestEffort(Project);
    }

    // Best-effort cleanup of a throwaway isolated directory through the owned directory writer (reached from the
    // held container, never taken as a service parameter — AG0031), swallowing the two failures a temp tree can
    // raise on teardown (a transient lock or a permission race); cleanup must never fail a test.
    private void DeleteBestEffort(string path)
    {
        try
        {
            _hostServices.FileSystem.GetDirectoryWriter().DeleteDirectory(path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a throwaway isolated directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup of a throwaway isolated directory.
        }
    }
}
