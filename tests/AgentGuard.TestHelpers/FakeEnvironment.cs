// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The in-memory <see cref="IEnvironment"/> fake: the current directory, the user's home directory, the process path, the
/// temp root, the base directory, and the environment variables are all values the test chooses through
/// <see cref="Create"/>, so a fake run
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
    private readonly string _baseDirectory;
    private readonly char _directorySeparator;
    private readonly string? _processPath;
    private readonly IReadOnlyDictionary<string, string> _variables;
    private readonly Architecture _processArchitecture;
    private readonly Architecture _osArchitecture;

    private FakeEnvironment(
        string home,
        string current,
        string temp,
        string baseDirectory,
        char directorySeparator,
        string? processPath,
        IReadOnlyDictionary<string, string> variables,
        Architecture processArchitecture,
        Architecture osArchitecture)
    {
        _home = home;
        _current = current;
        _temp = temp;
        _baseDirectory = baseDirectory;
        _directorySeparator = directorySeparator;
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
    /// <param name="baseDirectory">The base directory the fake reports, or <see langword="null"/> to use
    /// <paramref name="home"/>. Either way <see cref="GetBaseDirectory"/> reports it separator-terminated, so the seed
    /// need not carry a trailing separator and is never doubled when it does.</param>
    /// <param name="processPath">The process executable path the fake reports, or <see langword="null"/>.</param>
    /// <param name="variables">The environment variables the fake reports, or <see langword="null"/> for none.</param>
    /// <param name="processArchitecture">The process CPU architecture the fake reports. The parameter default is
    /// <see cref="Architecture.X64"/>, but the <see cref="SystemServicesBuilder.Fake"/> default environment no longer
    /// relies on that hardcoded value: the builder reads the REAL host process architecture once through the real adapter
    /// under <see cref="SystemServicesBuilder.Real"/> (an in-memory fake in AgentGuard.TestHelpers cannot read
    /// <c>RuntimeInformation</c>, which AG0011 owns to the real <c>EnvironmentAdapter</c>) and passes it here, so a
    /// <c>Fake()</c> run reports the true host architecture. A unit test simulating another architecture still injects it
    /// here (through the builder's <c>With(IEnvironment)</c>), which wins over the default.</param>
    /// <param name="osArchitecture">The operating-system CPU architecture the fake reports. The parameter default is
    /// <see cref="Architecture.X64"/>; like <paramref name="processArchitecture"/>, the <see cref="SystemServicesBuilder.Fake"/>
    /// default sources the real host value from the builder, and it is injectable for the same reasons.</param>
    /// <returns>The environment fake, as its interface.</returns>
    public static IEnvironment Create(
        string home,
        string? currentDirectory = null,
        string? tempDirectory = null,
        string? baseDirectory = null,
        string? processPath = null,
        IReadOnlyDictionary<string, string>? variables = null,
        Architecture processArchitecture = Architecture.X64,
        Architecture osArchitecture = Architecture.X64) =>
        new FakeEnvironment(
            home,
            currentDirectory ?? home,
            tempDirectory ?? home,
            baseDirectory ?? home,
            SystemServicesBuilder.HostDirectorySeparator,
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
    // The interface guarantees a separator-terminated value, because the real adapter mirrors AppContext.BaseDirectory
    // and that primitive always ends in one. The fake therefore reproduces the guarantee for EVERY seed, the seedless
    // home fallback included: it tests the stored value for a trailing separator and appends exactly one when absent,
    // and returns an already-terminated value unchanged so it is never doubled to "//". The separator is the owned
    // IPlatformFileSystem.DirectorySeparator, read once off the real platform service by SystemServicesBuilder — never a
    // raw Path.DirectorySeparatorChar (AG0020) and never a hardcoded '/', so the fake is correct on Windows too.
    public string GetBaseDirectory() =>
        _baseDirectory.EndsWith(_directorySeparator) ? _baseDirectory : _baseDirectory + _directorySeparator;

    /// <inheritdoc />
    public Architecture GetProcessArchitecture() => _processArchitecture;

    /// <inheritdoc />
    public Architecture GetOSArchitecture() => _osArchitecture;
}
