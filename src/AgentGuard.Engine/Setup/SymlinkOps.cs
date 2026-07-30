// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AgentGuard.Setup;

/// <summary>
/// Creates and atomically re-points the symlinks the machine layout depends on. A re-point writes a temporary
/// symlink and renames it over the old one, so the link is never momentarily absent.
/// </summary>
internal static class SymlinkOps
{
    /// <summary>
    /// Ensures a symlink at <paramref name="linkPath"/> points at <paramref name="relativeTarget"/>, re-pointing
    /// it atomically only when it is missing or points elsewhere.
    /// </summary>
    /// <param name="linkPath">The symlink path.</param>
    /// <param name="relativeTarget">The relative target the link should resolve to.</param>
    /// <returns><see langword="true"/> when the link was created or changed; <see langword="false"/> when it
    /// already pointed at the target.</returns>
    internal static bool EnsurePointsTo(string linkPath, string relativeTarget)
    {
        if (string.Equals(ReadRawTarget(linkPath), relativeTarget, StringComparison.Ordinal))
        {
            return false;
        }

        PointAtomically(linkPath, relativeTarget);
        return true;
    }

    /// <summary>
    /// Reads a symlink's raw (unresolved) target string, or <see langword="null"/> when the path is not a symlink
    /// or cannot be read.
    /// </summary>
    /// <param name="linkPath">The symlink path.</param>
    /// <returns>The raw target string, or <see langword="null"/>.</returns>
    internal static string? ReadRawTarget(string linkPath)
    {
        try
        {
            string? fileTarget = new FileInfo(linkPath).LinkTarget;
            return fileTarget ?? new DirectoryInfo(linkPath).LinkTarget;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void PointAtomically(string linkPath, string relativeTarget)
    {
        string directory = Path.GetDirectoryName(linkPath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(
            directory,
            Path.GetFileName(linkPath) + ".tmp-" + Guid.NewGuid().ToString("N"));
        DeleteIfExists(temporaryPath);
        Directory.CreateSymbolicLink(temporaryPath, relativeTarget);

        int result = NativeInterop.Rename(temporaryPath, linkPath);
        if (result != 0)
        {
            int error = Marshal.GetLastPInvokeError();
            DeleteIfExists(temporaryPath);
            throw new IOException(
                $"Failed to atomically point '{linkPath}' at '{relativeTarget}' (errno {error}).");
        }
    }

    private static void DeleteIfExists(string linkPath)
    {
        try
        {
            if (File.Exists(linkPath) || ReadRawTarget(linkPath) is not null)
            {
                File.Delete(linkPath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of a stray temporary symlink.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup of a stray temporary symlink.
        }
    }
}
