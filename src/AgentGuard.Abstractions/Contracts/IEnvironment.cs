// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned seam for OS-environment reads — the current directory, the user's home directory, environment
/// variables, and the running process path. Every ambient environment read routes through here so it is mockable
/// and no raw <c>System.Environment</c> access leaks into the codebase.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Gets the process's current working directory.
    /// </summary>
    /// <returns>The absolute path of the current working directory.</returns>
    string GetCurrentDirectory();

    /// <summary>
    /// Gets the current user's home directory.
    /// </summary>
    /// <returns>The absolute path of the user's home directory.</returns>
    string GetHomeDirectory();

    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable to read.</param>
    /// <returns>The variable's value, or <see langword="null"/> when it is not set.</returns>
    string? GetEnvironmentVariable(string name);

    /// <summary>
    /// Gets the path of the executable that started the running process.
    /// </summary>
    /// <returns>The process's executable path, or <see langword="null"/> when it cannot be determined.</returns>
    string? GetProcessPath();
}
