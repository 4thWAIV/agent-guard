// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Setup;

namespace AgentGuard.Cli;

/// <summary>
/// Hosts the entry point for the AgentGuard command-line interface. It exposes the whole surface —
/// <c>hook</c>, <c>install</c>, <c>init</c>, <c>remove</c>, <c>doctor</c> — through System.CommandLine, with the
/// setup logic living in the Engine behind these thin handlers. The <c>hook</c> handler fails closed: every
/// unhandled or unparsable condition denies (exit 2), so the host never reads a non-blocking code by mistake.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Parses the arguments and invokes the matched command.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    private static async Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        RootCommand root = BuildRootCommand();
        return await root.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    private static RootCommand BuildRootCommand()
    {
        var root = new RootCommand("AgentGuard — guards critical files from agent changes.");
        root.Subcommands.Add(BuildHookCommand());
        root.Subcommands.Add(BuildInstallCommand());
        root.Subcommands.Add(BuildInitCommand());
        root.Subcommands.Add(BuildRemoveCommand());
        root.Subcommands.Add(BuildDoctorCommand());
        return root;
    }

    private static Command BuildHookCommand()
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
        var ownedOption = new Option<bool>(HookCommand.OwnedFlag)
        {
            Description = "Marker identifying an AgentGuard-owned hook entry; ignored at runtime.",
        };

        var command = new Command(HookCommand.Verb, "Run the File Guard for a Claude Code PreToolUse/PostToolUse event.")
        {
            eventArgument,
            hostOption,
            ownedOption,
        };
        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction((parseResult, cancellationToken) =>
            RunHookAsync(parseResult.GetValue(eventArgument), parseResult.GetValue(hostOption), cancellationToken));
        return command;
    }

    private static Command BuildInstallCommand()
    {
        var allowDowngrade = new Option<bool>("--allow-downgrade")
        {
            Description = "Permit installing an older version than the recorded one.",
        };
        var command = new Command("install", "Install or update the guard binary under ~/.agentguard.")
        {
            allowDowngrade,
        };
        command.SetAction(parseResult =>
            RunCommand(() => SetupCommands.Install(SetupContext.ForCurrentProcess(), parseResult.GetValue(allowDowngrade))));
        return command;
    }

    private static Command BuildInitCommand()
    {
        var command = new Command("init", "Wire the guard into the current repository.");
        command.SetAction(_ => RunCommand(() => SetupCommands.Init(SetupContext.ForCurrentProcess())));
        return command;
    }

    private static Command BuildRemoveCommand()
    {
        var command = new Command("remove", "Remove the guard's wiring from the current repository.");
        command.SetAction(_ => RunCommand(() => SetupCommands.Remove(SetupContext.ForCurrentProcess())));
        return command;
    }

    private static Command BuildDoctorCommand()
    {
        var fix = new Option<bool>("--fix")
        {
            Description = "Repair the structural conditions.",
        };
        var command = new Command("doctor", "Check (and with --fix, repair) the guard install and project wiring.")
        {
            fix,
        };
        command.SetAction(parseResult => RunDoctor(parseResult.GetValue(fix)));
        return command;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: any caught exception on a hook run must become a deny (exit 2).")]
    private static async Task<int> RunHookAsync(string? eventToken, string? host, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryParseEvent(eventToken, out HookEvent hookEvent))
            {
                await Console.Error.WriteLineAsync("Usage: guard hook <pre|post>").ConfigureAwait(false);
                return 2;
            }

            string payload = await Console.In.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            HookExecution execution = await GuardHost
                .ExecuteHookAsync(hookEvent, Environment.ProcessPath, payload, host ?? GuardHost.ClaudeCodeHost, cancellationToken)
                .ConfigureAwait(false);

            if (execution.BinaryWritable)
            {
                await Console.Error
                    .WriteLineAsync("agentguard: warning — the guard binary is user-writable; this build reports but does not block that.")
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(execution.Message))
            {
                await Console.Error.WriteLineAsync(execution.Message).ConfigureAwait(false);
            }

            return execution.ExitCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"agentguard: failing closed — {exception.Message}").ConfigureAwait(false);
            return 2;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: a setup-command failure must exit non-zero, never success.")]
    private static int RunCommand(Func<CommandOutcome> action)
    {
        try
        {
            CommandOutcome outcome = action();
            foreach (string message in outcome.Messages)
            {
                Console.Out.WriteLine(message);
            }

            return outcome.ExitCode;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"agentguard: {exception.Message}");
            return 1;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: a doctor failure must exit non-zero, never success.")]
    private static int RunDoctor(bool fix)
    {
        try
        {
            DoctorOutcome outcome = SetupCommands.Doctor(SetupContext.ForCurrentProcess(), fix);
            foreach (DoctorReport report in outcome.Reports)
            {
                string line = report.Detail.Length == 0
                    ? $"[{report.Status}] ({report.Scope}) {report.Name}"
                    : $"[{report.Status}] ({report.Scope}) {report.Name} — {report.Detail}";
                Console.Out.WriteLine(line);
            }

            Console.Out.WriteLine(outcome.Healthy ? "doctor: healthy" : "doctor: not healthy");
            return outcome.ExitCode;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"agentguard: {exception.Message}");
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
