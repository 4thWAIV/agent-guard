// Copyright (c) 4thWAIV. All rights reserved.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The <see cref="IConsole"/> a <see cref="RecordingConsole"/> hands out: a contract implementation with a private
/// constructor (AG0003), handed out only as its interface, whose writes land in the recorder's shared buffers and whose
/// read returns the recorder's supplied standard input.
/// </summary>
internal sealed class RecordingConsoleWriter : IConsole
{
    private readonly StringBuilder _out;
    private readonly StringBuilder _error;
    private readonly string _input;

    private RecordingConsoleWriter(StringBuilder standardOut, StringBuilder standardError, string input)
    {
        _out = standardOut;
        _error = standardError;
        _input = input;
    }

    /// <inheritdoc />
    public void Write(string text) => _out.Append(text);

    /// <inheritdoc />
    public void WriteLine(string text) => _out.Append(text).Append('\n');

    /// <inheritdoc />
    public void ErrorWrite(string text) => _error.Append(text);

    /// <inheritdoc />
    public void ErrorWriteLine(string text) => _error.Append(text).Append('\n');

    /// <inheritdoc />
    public Task<string> ReadToEndAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_input);
    }

    /// <summary>Creates the console writer over the recorder's shared buffers and standard input.</summary>
    /// <param name="standardOut">The buffer standard-output writes land in.</param>
    /// <param name="standardError">The buffer standard-error writes land in.</param>
    /// <param name="input">The text a read returns.</param>
    /// <returns>The console, as its interface.</returns>
    internal static IConsole Create(StringBuilder standardOut, StringBuilder standardError, string input) =>
        new RecordingConsoleWriter(standardOut, standardError, input);
}
