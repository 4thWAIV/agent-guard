// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The in-memory <see cref="IEnvironment"/> fake: the current directory, the user's home directory, the process path, the
/// temp root, and the environment variables are all values the test chooses through <see cref="Create"/>, so a fake run
/// reads a test-controlled environment instead of the real process environment. A CLI test points the home directory at a
/// test directory so the setup commands operate inside the simulator with no real-process mutation. It reaches for no
/// <c>System.Environment</c> member and no <c>Path.GetTempPath()</c>; every value is held in memory. Its constructor is
/// private (AG0003) and it is handed out only as its interface.
/// </summary>
public sealed class FakeEnvironment : IEnvironment
{
    private readonly string _home;
    private readonly string _current;
    private readonly string _temp;
    private readonly string? _processPath;
    private readonly IReadOnlyDictionary<string, string> _variables;
    private readonly Architecture _processArchitecture;
    private readonly Architecture _osArchitecture;

    private FakeEnvironment(
        string home,
        string current,
        string temp,
        string? processPath,
        IReadOnlyDictionary<string, string> variables,
        Architecture processArchitecture,
        Architecture osArchitecture)
    {
        _home = home;
        _current = current;
        _temp = temp;
        _processPath = processPath;
        _variables = variables;
        _processArchitecture = processArchitecture;
        _osArchitecture = osArchitecture;
    }

    /// <summary>
    /// Creates the fake environment. The current and temp directories default to <paramref name="home"/> when not given,
    /// which is what a setup command run inside an isolated home wants.
    /// </summary>
    /// <param name="home">The absolute home directory the fake reports.</param>
    /// <param name="currentDirectory">The current working directory, or <see langword="null"/> to use
    /// <paramref name="home"/>.</param>
    /// <param name="tempDirectory">The temp-directory root, or <see langword="null"/> to use
    /// <paramref name="home"/>.</param>
    /// <param name="processPath">The process executable path the fake reports, or <see langword="null"/>.</param>
    /// <param name="variables">The environment variables the fake reports, or <see langword="null"/> for none.</param>
    /// <param name="processArchitecture">The process CPU architecture the fake reports. It defaults to
    /// <see cref="Architecture.X64"/>; a unit test simulating another architecture injects it here. The real host
    /// architecture is served by the real adapter under <see cref="SystemServicesBuilder.Real"/> (an in-memory fake in
    /// AgentGuard.TestHelpers cannot read <c>RuntimeInformation</c>, which AG0011 owns to the real
    /// <c>EnvironmentAdapter</c>), so the one test that asserts the true process architecture runs on <c>Real()</c>.</param>
    /// <param name="osArchitecture">The operating-system CPU architecture the fake reports. It defaults to
    /// <see cref="Architecture.X64"/> and is injectable for the same reasons as
    /// <paramref name="processArchitecture"/>.</param>
    /// <returns>The environment fake, as its interface.</returns>
    public static IEnvironment Create(
        string home,
        string? currentDirectory = null,
        string? tempDirectory = null,
        string? processPath = null,
        IReadOnlyDictionary<string, string>? variables = null,
        Architecture processArchitecture = Architecture.X64,
        Architecture osArchitecture = Architecture.X64) =>
        new FakeEnvironment(
            home,
            currentDirectory ?? home,
            tempDirectory ?? home,
            processPath,
            variables ?? new Dictionary<string, string>(System.StringComparer.Ordinal),
            processArchitecture,
            osArchitecture);

    /// <inheritdoc />
    public string GetCurrentDirectory() => _current;

    /// <inheritdoc />
    public string GetHomeDirectory() => _home;

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name) =>
        _variables.TryGetValue(name, out string? value) ? value : null;

    /// <inheritdoc />
    public string? GetProcessPath() => _processPath;

    /// <inheritdoc />
    public string GetTempDirectory() => _temp;

    /// <inheritdoc />
    public Architecture GetProcessArchitecture() => _processArchitecture;

    /// <inheritdoc />
    public Architecture GetOSArchitecture() => _osArchitecture;
}
