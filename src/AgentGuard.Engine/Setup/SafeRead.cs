// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// Reads a text file, distinguishing an I/O or permission failure (a cannot-verify signal) from success, so the
/// conditions never treat an unreadable record as absent. It reads through the owned <see cref="IFileReader"/>, so
/// no raw filesystem call lives here; it is built from the container at the composition point.
/// </summary>
internal sealed class SafeRead
{
    private readonly IFileReader _fileReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeRead"/> class over the owned file reader.
    /// </summary>
    /// <param name="fileReader">The owned read side of the filesystem.</param>
    internal SafeRead(IFileReader fileReader) => _fileReader = fileReader;

    /// <summary>
    /// Builds a safe reader from a setup context's owned file reader.
    /// </summary>
    /// <param name="context">The setup context carrying the owned file reader.</param>
    /// <returns>The safe reader.</returns>
    internal static SafeRead For(SetupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new SafeRead(context.FileReader);
    }

    /// <summary>
    /// Attempts to read all text from a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="content">The content when the read succeeds.</param>
    /// <param name="error">The failure message when the read fails.</param>
    /// <returns><see langword="true"/> when the file was read.</returns>
    internal bool TryReadText(string path, out string content, out string error)
    {
        content = string.Empty;
        error = string.Empty;
        try
        {
            content = _fileReader.ReadAllText(path);
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
