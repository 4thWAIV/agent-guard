// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// Reads a text file, distinguishing an I/O or permission failure (a cannot-verify signal) from success, so the
/// conditions never treat an unreadable record as absent.
/// </summary>
internal static class SafeRead
{
    /// <summary>
    /// Attempts to read all text from a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="content">The content when the read succeeds.</param>
    /// <param name="error">The failure message when the read fails.</param>
    /// <returns><see langword="true"/> when the file was read.</returns>
    internal static bool TryReadText(string path, out string content, out string error)
    {
        content = string.Empty;
        error = string.Empty;
        try
        {
            content = File.ReadAllText(path);
            return true;
        }
        catch (IOException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = exception.Message;
            return false;
        }
    }
}
