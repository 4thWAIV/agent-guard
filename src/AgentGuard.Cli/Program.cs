// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Boundaries;
using AgentGuard.Setup;

namespace AgentGuard.Cli;

/// <summary>
/// Hosts the entry point for the AgentGuard command-line interface. It is the single composition point: it builds the
/// one <see cref="ISystemServices"/> container with <see cref="SystemServices.Create"/> and threads it into every
/// handler, so console, environment, version, and clock access all run through the owned services. It exposes the
/// whole surface — <c>hook</c>, <c>install</c>, <c>init</c>, <c>remove</c>, <c>doctor</c> — through System.CommandLine,
/// with the setup logic living in the Engine behind these thin handlers. The <c>hook</c> handler fails closed: every
/// unhandled or unparsable condition denies (exit 2), so the host never reads a non-blocking code by mistake.
/// </summary>
internal sealed class Program
{
    private readonly string[] _args;

    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class holding the command-line arguments. It is
    /// <c>internal</c> (not private) so <c>AgentGuard.Cli.Tests</c> can construct it through the
    /// <c>InternalsVisibleTo</c> grant and drive <see cref="Run(ISystemServices)"/> with a test-built container
    /// (program-run-seam: an instantiable internal class; cli-tests-reach-run-via-ivt). <see cref="Main"/> is the
    /// only in-assembly caller.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    internal Program(string[] args) => _args = args;

    /// <summary>
    /// Parses the arguments and invokes the matched command, threading the given container into every handler. This is
    /// the seam that owns the real work: production passes the real container from <see cref="Main"/>, and a test passes
    /// a container built by the test <c>SystemServicesBuilder</c> whose fake environment points at a test directory.
    /// </summary>
    /// <param name="services">The OS/CLR service container every handler draws its owned services from.</param>
    /// <returns>The process exit code.</returns>
    internal async Task<int> Run(ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        RootCommand root = BuildRootCommand(services);
        return await root.Parse(_args).InvokeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the real service container and runs the CLI. The static entry point owns only the composition — building
    /// the one <see cref="ISystemServices"/> container with <see cref="SystemServices.Create"/> — and delegates every
    /// bit of real work to <see cref="Run(ISystemServices)"/>, which a test drives with a container built by the test
    /// <c>SystemServicesBuilder</c>, so <see cref="Main"/> itself is never under test.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    private static Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new Program(args).Run(SystemServices.Create());
    }

    private static RootCommand BuildRootCommand(ISystemServices services)
    {
        var root = new RootCommand("AgentGuard — guards critical files from agent changes.");
        root.Subcommands.Add(BuildHookCommand(services));
        root.Subcommands.Add(BuildInstallCommand(services));
        root.Subcommands.Add(BuildInitCommand(services));
        root.Subcommands.Add(BuildRemoveCommand(services));
        root.Subcommands.Add(BuildDoctorCommand(services));
        root.Subcommands.Add(BuildVersionCommand(services));
        return root;
    }

    /// <summary>
    /// Builds the <c>version</c> command, which prints the three versions this binary was built with
    /// (decision 18): the SemVer (informational), the AssemblyVersion, and the FileVersion.
    /// </summary>
    /// <param name="services">The composition container the version and console owners are pulled off.</param>
    /// <returns>The configured <c>version</c> command.</returns>
    private static Command BuildVersionCommand(ISystemServices services)
    {
        var command = new Command("version", "Print the SemVer, AssemblyVersion, and FileVersion this binary was built with.");
        command.SetAction(_ =>
        {
            IBuildInfo buildInfo = services.BuildInfo;
            services.Console.WriteLine($"SemVer: {buildInfo.SemVer}");
            services.Console.WriteLine($"AssemblyVersion: {buildInfo.AssemblyVersion}");
            services.Console.WriteLine($"FileVersion: {buildInfo.FileVersion}");
        });
        return command;
    }

    private static Command BuildHookCommand(ISystemServices services)
    {
        var eventArgument = new Argument<string?>("event")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The lifecycle event: pre or post.",
        };
        var hostOption = new Option<string>(HookCommand.HostOption)
        {
            DefaultValueFactory = _ => GuardHost.ClaudeCodeHost,
            Description = "The host runtime.",
        };

        var command = new Command(HookCommand.Verb, "Run the File Guard for a Claude Code PreToolUse/PostToolUse event.")
        {
            eventArgument,
            hostOption,
        };

        // A legacy settings file may still pass a now-retired ownership flag; unmatched tokens are tolerated so the
        // hook run still executes rather than erroring.
        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction((parseResult, cancellationToken) =>
            RunHookAsync(services, parseResult.GetValue(eventArgument), parseResult.GetValue(hostOption), cancellationToken));
        return command;
    }

    private static Command BuildInstallCommand(ISystemServices services)
    {
        var allowDowngrade = new Option<bool>("--allow-downgrade")
        {
            Description = "Permit installing an older version than the recorded one.",
        };
        var command = new Command(SetupVerb.Install, "Install or update the guard binary under ~/.agentguard.")
        {
            allowDowngrade,
        };
        command.SetAction((parseResult, _) =>
            RunCommandAsync(services, () => SetupCommands.Install(SetupContext.ForCurrentProcess(services), services, parseResult.GetValue(allowDowngrade))));
        return command;
    }

    private static Command BuildInitCommand(ISystemServices services)
    {
        var command = new Command(SetupVerb.Init, "Wire the guard into the current repository.");
        command.SetAction((_, _) => RunCommandAsync(services, () => SetupCommands.Init(SetupContext.ForCurrentProcess(services), services)));
        return command;
    }

    private static Command BuildRemoveCommand(ISystemServices services)
    {
        var command = new Command(SetupVerb.Remove, "Remove the guard's wiring from the current repository.");
        command.SetAction((_, _) => RunCommandAsync(services, () => SetupCommands.Remove(SetupContext.ForCurrentProcess(services), services)));
        return command;
    }

    private static Command BuildDoctorCommand(ISystemServices services)
    {
        var fix = new Option<bool>("--fix")
        {
            Description = "Repair the structural conditions.",
        };
        var command = new Command("doctor", "Check (and with --fix, repair) the guard install and project wiring.")
        {
            fix,
        };
        command.SetAction(parseResult => RunDoctor(services, parseResult.GetValue(fix)));
        return command;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: any caught exception on a hook run must become a deny (exit 2).")]
    private static async Task<int> RunHookAsync(ISystemServices services, string? eventToken, string? host, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryParseEvent(eventToken, out HookEvent hookEvent))
            {
                services.Console.ErrorWriteLine("Usage: guard hook <pre|post>");
                return 2;
            }

            string payload = await services.Console.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            HookExecution execution = await GuardHost
                .ExecuteHookAsync(hookEvent, services.Environment.GetProcessPath(), payload, host ?? GuardHost.ClaudeCodeHost, services, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(execution.Message))
            {
                services.Console.ErrorWriteLine(execution.Message);
            }

            return execution.ExitCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            services.Console.ErrorWriteLine($"agentguard: failing closed — {exception.Message}");
            return 2;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: a setup-command failure must exit non-zero, never success.")]
    private static async Task<int> RunCommandAsync(ISystemServices services, Func<Task<CommandOutcome>> action)
    {
        try
        {
            CommandOutcome outcome = await action().ConfigureAwait(false);
            foreach (string message in outcome.Messages)
            {
                services.Console.WriteLine(message);
            }

            return outcome.ExitCode;
        }
        catch (Exception exception)
        {
            services.Console.ErrorWriteLine($"agentguard: {exception.Message}");
            return 1;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: a doctor failure must exit non-zero, never success.")]
    private static int RunDoctor(ISystemServices services, bool fix)
    {
        try
        {
            DoctorOutcome outcome = SetupCommands.Doctor(SetupContext.ForCurrentProcess(services), fix);
            foreach (DoctorReport report in outcome.Reports)
            {
                string line = report.Detail.Length == 0
                    ? $"[{report.Status}] ({report.Scope}) {report.Name}"
                    : $"[{report.Status}] ({report.Scope}) {report.Name} — {report.Detail}";
                services.Console.WriteLine(line);
            }

            services.Console.WriteLine(outcome.Healthy ? "doctor: healthy" : "doctor: not healthy");
            return outcome.ExitCode;
        }
        catch (Exception exception)
        {
            services.Console.ErrorWriteLine($"agentguard: {exception.Message}");
            return 1;
        }
    }

    private static bool TryParseEvent(string? value, out HookEvent hookEvent)
    {
        switch (value)
        {
            case HookCommand.PreEvent:
            case "PreToolUse":
                hookEvent = HookEvent.PreToolUse;
                return true;
            case HookCommand.PostEvent:
            case "PostToolUse":
                hookEvent = HookEvent.PostToolUse;
                return true;
            default:
                hookEvent = HookEvent.PreToolUse;
                return false;
        }
    }
}
