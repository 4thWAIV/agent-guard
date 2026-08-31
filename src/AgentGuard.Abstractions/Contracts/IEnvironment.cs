// Copyright (c) 4thWAIV. All rights reserved.

using System.Runtime.InteropServices;

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

    /// <summary>
    /// Gets the system's temporary-directory root — an ambient environment read that wraps
    /// <c>System.IO.Path.GetTempPath()</c>. It is the standalone temp-root tool; an atomic, uniquely-named temp
    /// subdirectory is created through <see cref="IDirectoryWriter.CreateTempSubdirectory(string)"/>.
    /// </summary>
    /// <returns>The absolute path of the system's temporary-directory root.</returns>
    string GetTempDirectory();

    /// <summary>
    /// Gets the base directory the application was loaded from, as the runtime reports it — an ambient environment
    /// read that wraps <c>System.AppContext.BaseDirectory</c>. The returned path always ends with a directory
    /// separator, the same as the underlying primitive.
    /// </summary>
    /// <returns>The absolute base directory the application was loaded from, ending with a directory separator.</returns>
    string GetBaseDirectory();

    /// <summary>
    /// Gets the CPU architecture the running process is executing as — an ambient environment read that wraps
    /// <c>System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture</c>. It is the process architecture, not
    /// the build target: an x64 process launched under Rosetta reports <see cref="Architecture.X64"/> even on Arm64
    /// hardware.
    /// </summary>
    /// <returns>The CPU architecture of the running process.</returns>
    Architecture GetProcessArchitecture();

    /// <summary>
    /// Gets the CPU architecture of the operating system — an ambient environment read that wraps
    /// <c>System.Runtime.InteropServices.RuntimeInformation.OSArchitecture</c>.
    /// </summary>
    /// <returns>The CPU architecture of the operating system.</returns>
    Architecture GetOSArchitecture();
}
