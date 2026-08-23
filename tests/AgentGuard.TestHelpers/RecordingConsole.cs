// Copyright (c) 4thWAIV. All rights reserved.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The in-memory console recorder a test installs to capture standard output and error and to feed standard input. It is
/// NOT itself an <see cref="IConsole"/> — a contract implementation may expose only its interface members (AG0005), and
/// the captured <see cref="Output"/>/<see cref="Error"/> are the whole point — so it is a recorder that hands out an
/// <see cref="IConsole"/> through <see cref="Console"/>. The handed-out console writes into this recorder's buffers,
/// which the test reads back after the run. It reaches for no <c>System.Console</c> member.
/// </summary>
public sealed class RecordingConsole
{
    private readonly StringBuilder _out = new();
    private readonly StringBuilder _error = new();
    private readonly string _input;
    private IConsole? _console;

    /// <summary>Initializes a new instance of the <see cref="RecordingConsole"/> class with an empty standard input.</summary>
    public RecordingConsole()
        : this(string.Empty)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RecordingConsole"/> class with the given standard input.</summary>
    /// <param name="input">The text the handed-out console returns from a read.</param>
    public RecordingConsole(string input) => _input = input;

    /// <summary>Gets everything written to standard output.</summary>
    public string Output => _out.ToString();

    /// <summary>Gets everything written to standard error.</summary>
    public string Error => _error.ToString();

    /// <summary>Gets the console to install on the builder; its writes land in this recorder's buffers.</summary>
    public IConsole Console => _console ??= RecordingConsoleWriter.Create(_out, _error, _input);
}
