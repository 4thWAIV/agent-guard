// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Boundaries;

/// <summary>
/// The owned <see cref="IConsole"/> adapter. It is the single class that implements the console seam, so it is the
/// ONE place the raw <c>System.Console</c> members are allowed (AG0016 exempts exactly this owner class in
/// <c>AgentGuard.Boundaries</c>); every other type writes and reads the console through <see cref="IConsole"/>
/// pulled off <see cref="ISystemServices"/>, so console output is captured in tests instead of hitting the real
/// streams. It follows the shape of <c>System.Console</c> (iconsole-mirrors-console). Nothing below Boundaries
/// consumes the console, so the owner stays here (owners-live-at-lowest-consumer). It is <c>internal</c> with a
/// <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class ConsoleAdapter : IConsole
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private ConsoleAdapter()
    {
    }

    /// <inheritdoc />
    public void Write(string text) => Console.Write(text);

    /// <inheritdoc />
    public void WriteLine(string text) => Console.WriteLine(text);

    /// <inheritdoc />
    public void ErrorWrite(string text) => Console.Error.Write(text);

    /// <inheritdoc />
    public void ErrorWriteLine(string text) => Console.Error.WriteLine(text);

    /// <inheritdoc />
    public Task<string> ReadToEndAsync(CancellationToken ct) => Console.In.ReadToEndAsync(ct);

    /// <summary>
    /// Creates the console adapter.
    /// </summary>
    /// <returns>The console adapter, as its interface.</returns>
    internal static IConsole Create() => new ConsoleAdapter();
}
